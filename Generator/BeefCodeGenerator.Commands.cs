using CppAst;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;

namespace SPIRV_Cross_Beef.Generator
{
    public static partial class BeefCodeGenerator
    {
        private static readonly HashSet<string> s_instanceFunctions = new HashSet<string>
        {

        };

        private static readonly HashSet<string> s_outReturnFunctions = new HashSet<string>
        {

        };

        //      public static int GenerateCommands(CppCompilation compilation, string projectNamespace, string outputPath)
        //{
        //	using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Commands.bf"), projectNamespace, "System"))
        //	{
        //              var commands = new Dictionary<string, CppFunction>();
        //              var instanceCommands = new Dictionary<string, CppFunction>();
        //              var deviceCommands = new Dictionary<string, CppFunction>();
        //              foreach (CppFunction cppFunction in compilation.Functions)
        //              {
        //                  string returnType = GetBfTypeName(cppFunction.ReturnType, false);
        //                  bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
        //                  string bfName = cppFunction.Name;

        //                  commands.Add(bfName, cppFunction);

        //                  if (cppFunction.Parameters.Count > 0)
        //                  {
        //                      var firstParameter = cppFunction.Parameters[0];
        //                      if (firstParameter.Type is CppTypedef typedef)
        //                      {


        //                          deviceCommands.Add(bfName, cppFunction);

        //                      }
        //                  }
        //              }

        //              using (codeWriter.PushBlock($"public class SPIRV"))
        //              {
        //                  codeWriter.WriteLine("internal static IntPtr s_NativeLibrary = LoadNativeLibrary();");
        //                  codeWriter.WriteLine("internal static T LoadFunction<T>(string name) => LibraryLoader.LoadFunction<T>(s_NativeLibrary, name);");

        //                  foreach (KeyValuePair<string, CppFunction> command in commands)
        //                  {
        //                      CppFunction cppFunction = command.Value;


        //                      string returnBfName = GetBfTypeName(cppFunction.ReturnType, false);
        //                      bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
        //                      var argumentsString = GetParameterSignature(cppFunction, canUseOut);

        //                      codeWriter.WriteLine("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
        //                      codeWriter.WriteLine($"private delegate {returnBfName}  PFN_{cppFunction.Name}({argumentsString});");
        //                      //        private static readonly PFN_shaderc_compile_options_set_suppress_warnings shaderc_compile_options_set_suppress_warnings_ = LoadFunction<PFN_shaderc_compile_options_set_suppress_warnings>("shaderc_compile_options_set_suppress_warnings");
        //                      codeWriter.WriteLine($"private static readonly PFN_{cppFunction.Name} {cppFunction.Name}_ = LoadFunction<PFN_{cppFunction.Name}>(nameof({cppFunction.Name}));");

        //                      using (codeWriter.PushBlock($"public static {returnBfName} {cppFunction.Name}({argumentsString})"))
        //                      {
        //                          if (returnBfName != "void")
        //                          {
        //                              codeWriter.Write("return ");
        //                          }

        //                          codeWriter.Write($"{command.Key}_(");
        //                          int index = 0;
        //                          foreach (CppParameter cppParameter in cppFunction.Parameters)
        //                          {
        //                              string paramCsName = GetParameterName(cppParameter.Name);

        //                              if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
        //                              {
        //                                  codeWriter.Write("out ");
        //                              }

        //                              codeWriter.Write($"{paramCsName}");

        //                              if (index < cppFunction.Parameters.Count - 1)
        //                              {
        //                                  codeWriter.Write(", ");
        //                              }

        //                              index++;
        //                          }

        //                          codeWriter.WriteLine(");");
        //                      }

        //                      codeWriter.WriteLine();
        //                  }
        //              }
        //              return 0;
        //	}
        //}



        public static int GenerateCommands(CppCompilation compilation, string projectNamespace, string outputPath)
        {
            using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Commands.bf"), projectNamespace, "System"))
            {
                var commands = new Dictionary<string, CppFunction>();
                var instanceCommands = new Dictionary<string, CppFunction>();
                var deviceCommands = new Dictionary<string, CppFunction>();
                foreach (CppFunction cppFunction in compilation.Functions)
                {
                    string returnType = GetBfTypeName(cppFunction.ReturnType, false);
                    bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
                    string bfName = cppFunction.Name;

                    commands.Add(bfName, cppFunction);

                    if (cppFunction.Parameters.Count > 0)
                    {
                        var firstParameter = cppFunction.Parameters[0];
                        if (firstParameter.Type is CppTypedef typedef)
                        {


                            deviceCommands.Add(bfName, cppFunction);

                        }
                    }
                }

                using (codeWriter.PushBlock($"public class SPIRV"))
                {
                    foreach (KeyValuePair<string, CppFunction> command in commands)
                    {
                        CppFunction cppFunction = command.Value;


                        string returnBfName = GetBfTypeName(cppFunction.ReturnType, false);
                        bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
                        var argumentsString = GetParameterSignature(cppFunction, canUseOut);


                        codeWriter.WriteLine($"[CallingConvention(.Stdcall), CLink]");
                        codeWriter.WriteLine($"public static extern {returnBfName} {cppFunction.Name}({argumentsString});");

                        codeWriter.WriteLine();
                    }
                }
                return 0;
            }
        }

        public static string GetParameterSignature(CppFunction cppFunction, bool canUseOut)
        {
            return GetParameterSignature(cppFunction.Parameters, canUseOut);
        }

        private static string GetParameterSignature(IList<CppParameter> parameters, bool canUseOut)
        {
            var argumentBuilder = new StringBuilder();
            int index = 0;

            foreach (CppParameter cppParameter in parameters)
            {
                string direction = string.Empty;
                var paramCsTypeName = GetBfTypeName(cppParameter.Type, false);
                var paramCsName = GetParameterName(cppParameter.Name);

                if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
                {
                    argumentBuilder.Append("out ");
                    paramCsTypeName = GetBfTypeName(cppTypeDeclaration, false);
                }

                argumentBuilder.Append(paramCsTypeName).Append(' ').Append(paramCsName);
                if (index < parameters.Count - 1)
                {
                    argumentBuilder.Append(", ");
                }

                index++;
            }

            return argumentBuilder.ToString();
        }

        private static string GetParameterName(string name)
        {
            if (name == "event")
                return "@event";

            if (name == "object")
                return "@object";

            if (name == "function")
                return "@function";

            if (name.StartsWith('p')
                && char.IsUpper(name[1]))
            {
                name = char.ToLower(name[1]) + name.Substring(2);
                return GetParameterName(name);
            }

            return name;
        }

        private static bool CanBeUsedAsOutput(CppType type, out CppTypeDeclaration? elementTypeDeclaration)
        {
            if (type is CppPointerType pointerType)
            {
                if (pointerType.ElementType is CppTypedef typedef)
                {
                    elementTypeDeclaration = typedef;
                    return true;
                }
                else if (pointerType.ElementType is CppClass @class
                    && @class.ClassKind != CppClassKind.Class
                    && @class.SizeOf > 0)
                {
                    elementTypeDeclaration = @class;
                    return true;
                }
                else if (pointerType.ElementType is CppEnum @enum
                    && @enum.SizeOf > 0)
                {
                    elementTypeDeclaration = @enum;
                    return true;
                }
            }

            elementTypeDeclaration = null;
            return false;
        }
    }
}
