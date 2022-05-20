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
		private static readonly Dictionary<string, string> s_knownEnumValueNames = new Dictionary<string, string>
		{
			{  "", "" },

		};
		private static readonly Dictionary<string, string> s_knownEnumPrefixes = new Dictionary<string, string>
		{

		};

		private static readonly HashSet<string> s_ignoredParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"bit",
		};


		private static readonly HashSet<string> s_preserveCaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"",
		};
		public static int GenerateEnums(CppCompilation compilation, string projectNamespace, string outputPath)
		{
			using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Enums.bf"), projectNamespace, "System"))
			{
				var createdEnums = new Dictionary<string, string>();

				foreach (CppEnum cppEnum in compilation.Enums)
				{
					string bfName = GetBfCleanName(cppEnum.Name);
					string enumNamePrefix = GetEnumNamePrefix(cppEnum.Name);

					if (bfName.EndsWith("_"))
					{
						bfName = bfName.Remove(bfName.Length - 1);
					}

					// Remove extension suffix from enum item values
					string extensionPrefix = "";

					createdEnums.Add(bfName, cppEnum.Name);

					bool noneAdded = false;

					using (codeWriter.PushBlock($"[AllowDuplicates, CRepr] public enum {bfName}"))
					{
						foreach (var enumMember in cppEnum.Items)
						{
							var enumMemberName = GetEnumMemberName(cppEnum, enumMember.Name, enumNamePrefix);

							if (!string.IsNullOrEmpty(extensionPrefix) && enumMemberName.EndsWith(extensionPrefix))
							{
								enumMemberName = enumMemberName.Remove(enumMemberName.Length - extensionPrefix.Length);
							}

							if (enumMemberName == "None" && noneAdded)
							{
								continue;
							}

							if (enumMember.ValueExpression is CppRawExpression rawExpression)
							{
								string enumValueName = GetEnumMemberName(cppEnum, rawExpression.Text, enumNamePrefix);


								if (!string.IsNullOrEmpty(extensionPrefix) && enumValueName.EndsWith(extensionPrefix))
								{
									enumValueName = enumValueName.Remove(enumValueName.Length - extensionPrefix.Length);

									if (enumMemberName == enumValueName)
										continue;
								}

								codeWriter.WriteLine($"{enumMemberName} = {enumValueName},");
							}
							else
							{
								codeWriter.WriteLine($"{enumMemberName} = {enumMember.Value},");
							}
						}
					}

					if (bfName == "spvc_msl_shader_input_format")
					{
						codeWriter.WriteLine();
						codeWriter.WriteLine($"typealias spvc_msl_vertex_format = {bfName};");
					}
					codeWriter.WriteLine();
				}

				codeWriter.WriteLine();
				return 0;
			}
		}

		private static string GetEnumMemberName(CppEnum @enum, string cppEnumMemberName, string enumNamePrefix)
		{
			string enumItemName = GetPrettyEnumName(cppEnumMemberName, enumNamePrefix);


			return enumItemName;
		}

		private static string GetPrettyEnumName(string value, string enumPrefix)
		{
			if (s_knownEnumValueNames.TryGetValue(value, out string knownName))
			{
				return knownName;
			}

			if (value.IndexOf(enumPrefix) != 0)
			{
				return value;
			}

			string[] parts = value[enumPrefix.Length..].Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

			var sb = new StringBuilder();
			foreach (string part in parts)
			{
				if (s_ignoredParts.Contains(part))
				{
					continue;
				}

				if (s_preserveCaps.Contains(part))
				{
					sb.Append(part);
				}
				else
				{
					sb.Append(char.ToUpper(part[0]));
					for (int i = 1; i < part.Length; i++)
					{
						sb.Append(char.ToLower(part[i]));
					}
				}
			}

			string prettyName = sb.ToString();
			return (char.IsNumber(prettyName[0])) ? "_" + prettyName : prettyName;
		}

		public static string GetEnumNamePrefix(string typeName)
		{
			if (s_knownEnumPrefixes.TryGetValue(typeName, out string knownValue))
			{
				return knownValue;
			}

			List<string> parts = new List<string>(4);
			int chunkStart = 0;
			for (int i = 0; i < typeName.Length; i++)
			{
				if (char.IsUpper(typeName[i]))
				{
					if (chunkStart != i)
					{
						parts.Add(typeName.Substring(chunkStart, i - chunkStart));
					}

					chunkStart = i;
					if (i == typeName.Length - 1)
					{
						parts.Add(typeName.Substring(i, 1));
					}
				}
				else if (i == typeName.Length - 1)
				{
					parts.Add(typeName.Substring(chunkStart, typeName.Length - chunkStart));
				}
			}

			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i] == "Flag" ||
					parts[i] == "Flags" ||
					(parts[i] == "K" && (i + 2) < parts.Count && parts[i + 1] == "H" && parts[i + 2] == "R") ||
					(parts[i] == "A" && (i + 2) < parts.Count && parts[i + 1] == "M" && parts[i + 2] == "D") ||
					(parts[i] == "E" && (i + 2) < parts.Count && parts[i + 1] == "X" && parts[i + 2] == "T") ||
					(parts[i] == "Type" && (i + 2) < parts.Count && parts[i + 1] == "N" && parts[i + 2] == "V") ||
					(parts[i] == "Type" && (i + 3) < parts.Count && parts[i + 1] == "N" && parts[i + 2] == "V" && parts[i + 3] == "X") ||
					(parts[i] == "Scope" && (i + 2) < parts.Count && parts[i + 1] == "N" && parts[i + 2] == "V") ||
					(parts[i] == "Mode" && (i + 2) < parts.Count && parts[i + 1] == "N" && parts[i + 2] == "V") ||
					(parts[i] == "Mode" && (i + 5) < parts.Count && parts[i + 1] == "I" && parts[i + 2] == "N" && parts[i + 3] == "T" && parts[i + 4] == "E" && parts[i + 5] == "L") ||
					(parts[i] == "Type" && (i + 5) < parts.Count && parts[i + 1] == "I" && parts[i + 2] == "N" && parts[i + 3] == "T" && parts[i + 4] == "E" && parts[i + 5] == "L")
					)
				{
					parts = new List<string>(parts.Take(i));
					break;
				}
			}

			return string.Join("_", parts.Select(s => s.ToUpper()));
		}

		private static string NormalizeEnumValue(string value)
		{
			if (value == "(~0U)")
			{
				return "~0u";
			}

			if (value == "(~0ULL)")
			{
				return "~0ul";
			}

			if (value == "(~0U-1)")
			{
				return "~0u - 1";
			}

			if (value == "(~0U-2)")
			{
				return "~0u - 2";
			}

			if (value == "(~0U-3)")
			{
				return "~0u - 3";
			}

			return value.Replace("ULL", "UL");
		}
	}
}
