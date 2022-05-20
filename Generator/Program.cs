using CppAst;
using System;
using System.IO;

namespace SPIRV_Cross_Beef.Generator
{
    public class Program
    {
        private static string SPRIVCrossSourceDir = "../../../../../SPIRV-Cross";
        private static string OutputDir = "../../../../../SPIRV-Cross-Beef/SPIRV-Cross/src/Generated";
        private static string ProjectNamespace = "SPIRV_Cross";

        public static int Main(string[] args)
        {
            var options = new CppParserOptions
            {
                ParseMacros = true,
            };

            CppCompilation spirvCrossCompilation = CppParser.ParseFile(Path.Combine(SPRIVCrossSourceDir,"spirv_cross_c.h"), options);

            // Print diagnostic messages
            if (spirvCrossCompilation.HasErrors)
            {
                foreach (var message in spirvCrossCompilation.Diagnostics.Messages)
                {
                    if (message.Type == CppLogMessageType.Error)
                    {
                        var currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message);
                        Console.ForegroundColor = currentColor;
                    }
                }

                return 0;
            }

            return BeefCodeGenerator.Generate(spirvCrossCompilation, ProjectNamespace, Path.GetFullPath(OutputDir));
        }
    }
}
