using CppAst;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPIRV_Cross_Beef.Generator
{
    public static partial class BeefCodeGenerator
    {
        private static readonly HashSet<string> s_keywords = new HashSet<string>
        {
            "function",
            "var"
        };

        private static readonly Dictionary<string, string> s_bfNameMappings = new Dictionary<string, string>()
        {
            { "uint8_t", "uint8" },
            { "uint16_t", "uint16" },
            { "uint32_t", "uint32" },
            { "uint64_t", "uint64" },
            { "int8_t", "int8" },
            { "int32_t", "int32" },
            { "int16_t", "int16" },
            { "int64_t", "int64" },
            { "int64_t*", "int64*" },
            { "char", "char8" },
            { "size_t", "uint" },
            { "long long", "int64" },
            { "unsigned long long", "uint64" },

            { "spvc_bool", "bool" },
            { "spvc_constant_id", "uint32" },
            { "spvc_variable_id", "uint32" },
            { "spvc_type_id", "uint32" },
            { "spvc_hlsl_binding_flags", "uint32" },
        };

        public static void AddCsMapping(string typeName, string csTypeName)
        {
            s_bfNameMappings[typeName] = csTypeName;
        }

        private static string NormalizeFieldName(string name)
        {
            if (s_keywords.Contains(name))
                return "@" + name;

            return name;
        }

        private static string GetBfCleanName(string name)
        {
            if (s_bfNameMappings.TryGetValue(name, out string mappedName))
            {
                return GetBfCleanName(mappedName);
            }
            else if (name.StartsWith("PFN"))
            {
                //return "IntPtr";
                return "void*";
            }

            return name;
        }

        private static string GetBfTypeName(CppType type, bool isPointer = false)
        {
            if (type is CppPrimitiveType primitiveType)
            {
                //return GetBfTypeName((CppType)primitiveType, isPointer);
                return GetBfTypeName(primitiveType, isPointer);
            }

            if (type is CppQualifiedType qualifiedType)
            {
                return GetBfTypeName(qualifiedType.ElementType, isPointer);
            }

            if (type is CppEnum enumType)
            {
                var enumBfName = GetBfCleanName(enumType.Name);
                if (isPointer)
                    return enumBfName + "*";

                return enumBfName;
            }

            if (type is CppTypedef typedef)
            {
                var typeDefBfName = GetBfCleanName(typedef.Name);
                if (isPointer)
                    return typeDefBfName + "*";

                return typeDefBfName;
            }

            if (type is CppClass @class)
            {
                var className = GetBfCleanName(@class.Name);
                if (isPointer)
                    return className + "*";

                return className;
            }

            if (type is CppPointerType pointerType)
            {
                //return GetBfTypeName((CppType)pointerType);
                return GetBfTypeName(pointerType);
            }

            if (type is CppArrayType arrayType)
            {
                return GetBfTypeName(arrayType.ElementType, true);
            }

            return string.Empty;
        }

        private static string GetBfTypeName(CppPrimitiveType primitiveType, bool isPointer)
        {
            switch (primitiveType.Kind)
            {
                case CppPrimitiveKind.Void:
                    return isPointer ? "void*" : "void";

                case CppPrimitiveKind.Char:
                    //return isPointer ? "byte*" : "byte";
                    return isPointer ? "char8*" : "char8";

                case CppPrimitiveKind.Bool:
                    break;
                case CppPrimitiveKind.WChar:
                    break;
                case CppPrimitiveKind.Short:
                    //return isPointer ? "short*" : "short";
                    return isPointer ? "int16*" : "int16";
                case CppPrimitiveKind.Int:
                    //return isPointer ? "int*" : "int";
                    return isPointer ? "int32*" : "int32";

                case CppPrimitiveKind.LongLong:
                    return isPointer ? "int64*" : "int64";

                case CppPrimitiveKind.UnsignedChar:
                    return isPointer ? "uint8*" : "uint8";

                case CppPrimitiveKind.UnsignedShort:
                    //return isPointer ? "ushort*" : "ushort";
                    return isPointer ? "uint16*" : "uint16";
                case CppPrimitiveKind.UnsignedInt:
                    //return isPointer ? "uint*" : "uint";
                    return isPointer ? "uint32*" : "uint32";

                case CppPrimitiveKind.UnsignedLongLong:
                    return isPointer ? "uint64*" : "uint64";

                case CppPrimitiveKind.Float:
                    //return isPointer ? "float*" : "float";
                    return isPointer ? "float*" : "float";
                case CppPrimitiveKind.Double:
                    //return isPointer ? "double*" : "double";
                    return isPointer ? "double*" : "double";
                case CppPrimitiveKind.LongDouble:
                    break;
                default:
                    return string.Empty;
            }

            return string.Empty;
        }

        private static string GetBfTypeName(CppPointerType pointerType)
        {
            if (pointerType.ElementType is CppQualifiedType qualifiedType)
            {
                if (qualifiedType.ElementType is CppPrimitiveType primitiveType)
                {
                    return GetBfTypeName(primitiveType, true);
                }
                else if (qualifiedType.ElementType is CppClass @classType)
                {
                    return GetBfTypeName(@classType, true);
                }
                else if (qualifiedType.ElementType is CppPointerType subPointerType)
                {
                    return GetBfTypeName(subPointerType, true) + "*";
                }
                else if (qualifiedType.ElementType is CppTypedef typedef)
                {
                    return GetBfTypeName(typedef, true);
                }
                else if (qualifiedType.ElementType is CppEnum @enum)
                {
                    return GetBfTypeName(@enum, true);
                }

                return GetBfTypeName(qualifiedType.ElementType, true);
            }

            return GetBfTypeName(pointerType.ElementType, true);
        }

        public static int Generate(CppCompilation compilation, string projectNamespace, string outputPath)
        {
            string spirvOutputPath = outputPath;
            try
            {
                Directory.CreateDirectory(spirvOutputPath);
            }
            catch (Exception) { }

            int result = GenerateConstants(compilation, projectNamespace, spirvOutputPath);

            if (result == 0)
                result = GenerateEnums(compilation, projectNamespace, spirvOutputPath);

            if (result == 0)
                result = GenerateHandles(compilation, projectNamespace, spirvOutputPath);

            if (result == 0)
                result = GenerateStructs(compilation, projectNamespace, spirvOutputPath);

            if (result == 0)
                result = GenerateCommands(compilation, projectNamespace, spirvOutputPath);

            return result;
        }
    }
}
