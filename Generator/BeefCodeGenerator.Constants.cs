using CppAst;
using System.IO;

namespace SPIRV_Cross_Beef.Generator
{
    public static partial class BeefCodeGenerator
	{
		public static int GenerateConstants(CppCompilation compilation, string projectNamespace, string outputPath)
		{
			using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Constants.bf"), projectNamespace, "System"))
			{
				using (codeWriter.PushBlock($"public static"))
				{
					foreach (CppField field in compilation.Fields)
					{
						string fieldTypeName = GetBfTypeName(field.Type, false);
						string fieldName = GetBfCleanName(field.Name);
						codeWriter.WriteLine($"public const {fieldTypeName} {fieldName} = {field.InitValue.Value};");
					}
				}
				return 0;
			}
		}
	}
}
