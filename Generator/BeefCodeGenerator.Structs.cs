using CppAst;
using System.IO;
using System.Text;

namespace SPIRV_Cross_Beef.Generator
{
    public static partial class BeefCodeGenerator
    {
        private static bool generateSizeOfStructs = false;

        public static int GenerateStructs(CppCompilation compilation, string projectNamespace, string outputPath)
		{
			using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Structs.bf"), projectNamespace, "System"))
			{
                // Print All classes, structs
                foreach (CppClass cppClass in compilation.Classes)
                {
                    if (cppClass.ClassKind == CppClassKind.Class ||
                        cppClass.SizeOf == 0 ||
                        cppClass.Name.EndsWith("_T"))
                    {
                        continue;
                    }



                    bool isUnion = cppClass.ClassKind == CppClassKind.Union;


                    string csName = cppClass.Name;
                    if (isUnion)
                    {
                        codeWriter.WriteLine("[CRepr, Union]");
                    }
                    else
                    {
                        codeWriter.WriteLine("[CRepr]");
                    }

                    bool isReadOnly = false;
                    //string modifier = "partial";
                    //if (csName == "VkClearDepthStencilValue")
                    //{
                    //    modifier = "readonly partial";
                    //    isReadOnly = true;
                    //}

                    //using (codeWriter.PushBlock($"public {modifier} struct {csName}"))
                    using (codeWriter.PushBlock($"public struct {csName}"))
                    {
                        if (generateSizeOfStructs && cppClass.SizeOf > 0)
                        {
                            codeWriter.WriteLine("/// <summary>");
                            codeWriter.WriteLine($"/// The size of the <see cref=\"{csName}\"/> type, in bytes.");
                            codeWriter.WriteLine("/// </summary>");
                            codeWriter.WriteLine($"public static readonly int32 SizeInBytes = {cppClass.SizeOf};");
                            codeWriter.WriteLine();
                        }

                        foreach (CppField cppField in cppClass.Fields)
                        {
                            WriteField(codeWriter, cppField, isUnion, isReadOnly);
                        }
                    }

                    codeWriter.WriteLine();
                }
                return 0;
			}
		}

        private static void WriteField(CodeWriter writer, CppField field, bool isUnion = false, bool isReadOnly = false)
        {
            string csFieldName = NormalizeFieldName(field.Name);

            if (isUnion)
            {
                writer.WriteLine("[FieldOffset(0)]");
            }

            if (field.Type is CppArrayType arrayType)
            {
                bool canUseFixed = false;
                if (arrayType.ElementType is CppPrimitiveType)
                {
                    canUseFixed = true;
                }
                else if (arrayType.ElementType is CppTypedef typedef
                    && typedef.ElementType is CppPrimitiveType)
                {
                    canUseFixed = true;
                }

                if (canUseFixed)
                {
                    string csFieldType = GetBfTypeName(arrayType.ElementType, false);
                    //writer.WriteLine($"public unsafe fixed {csFieldType} {csFieldName}[{arrayType.Size}];");
                    writer.WriteLine($"public {csFieldType}[{arrayType.Size}] {csFieldName};");
                }
                else
                {
                    string unsafePrefix = string.Empty;
                    string csFieldType = GetBfTypeName(arrayType.ElementType, false);
                    if (csFieldType.EndsWith('*'))
                    {
                        unsafePrefix = "unsafe ";
                    }

                    for (int i = 0; i < arrayType.Size; i++)
                    {
                        //writer.WriteLine($"public {unsafePrefix}{csFieldType} {csFieldName}_{i};");
                        writer.WriteLine($"public {csFieldType} {csFieldName}_{i};");
                    }
                }
            }
            else
            {
                // VkAllocationCallbacks members
                if (field.Type is CppTypedef typedef &&
                    typedef.ElementType is CppPointerType pointerType &&
                    pointerType.ElementType is CppFunctionType functionType)
                {
                    StringBuilder builder = new();
                    foreach (CppParameter parameter in functionType.Parameters)
                    {
                        string paramCsType = GetBfTypeName(parameter.Type, false);
                        // Otherwise we get interop issues with non blittable types

                        builder.Append(paramCsType).Append(", ");
                    }

                    string returnCsName = GetBfTypeName(functionType.ReturnType, false);


                    builder.Append(returnCsName);

                    return;
                }

                string csFieldType = GetBfTypeName(field.Type, false);




                string fieldPrefix = isReadOnly ? "readonly " : string.Empty;
                if (csFieldType.EndsWith('*'))
                {
                    fieldPrefix += "unsafe ";
                }

                //writer.WriteLine($"public {fieldPrefix}{csFieldType} {csFieldName};");
                writer.WriteLine($"public {csFieldType} {csFieldName};");
            }
        }
    }
}
