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
		public static int GenerateHandles(CppCompilation compilation, string projectNamespace, string outputPath)
		{
			using (var codeWriter = new CodeWriter(Path.Combine(outputPath, "Handles.bf"), projectNamespace, "System"))
			{
                foreach (CppTypedef typedef in compilation.Typedefs)
                {
                    if (!(typedef.ElementType is CppPointerType))
                    {
                        continue;
                    }

                    var isDispatchable = true;

                    var bfName = typedef.Name;

                    codeWriter.WriteLine($"/// <summary>");
                    codeWriter.WriteLine($"/// A {(isDispatchable ? "dispatchable" : "non-dispatchable")} handle.");
                    codeWriter.WriteLine("/// </summary>");
                    //codeWriter.WriteLine($"//[DebuggerDisplay(\"{{DebuggerDisplay,nq}}\")]");
                    using (codeWriter.PushBlock($"[CRepr]public struct {bfName} : IEquatable<{bfName}>, IHashable"))
                    {
                        string handleType = isDispatchable ? "int" : "uint64";
                        string nullValue = "0";

                        codeWriter.WriteLine($"public this({handleType} handle) {{ Handle = handle; }}");
                        codeWriter.WriteLine($"public {handleType} Handle {{ get; set mut; }}");
                        codeWriter.WriteLine($"public bool IsNull => Handle == 0;");

                        codeWriter.WriteLine($"public static {bfName} Null => {bfName}({nullValue});");
                        codeWriter.WriteLine($"public static implicit operator {bfName}({handleType} handle) => {bfName}(handle);");
                        codeWriter.WriteLine($"public static bool operator ==({bfName} left, {bfName} right) => left.Handle == right.Handle;");
                        codeWriter.WriteLine($"public static bool operator !=({bfName} left, {bfName} right) => left.Handle != right.Handle;");
                        codeWriter.WriteLine($"public static bool operator ==({bfName} left, {handleType} right) => left.Handle == right;");
                        codeWriter.WriteLine($"public static bool operator !=({bfName} left, {handleType} right) => left.Handle != right;");
                        codeWriter.WriteLine($"public bool Equals({bfName} other) => Handle == other.Handle;");
                        //codeWriter.WriteLine("/// <inheritdoc/>");
                        //using (codeWriter.PushBlock("public bool Equals(Object obj)"))
                        //{
                            //codeWriter.WriteLine($"public bool Equals(Object obj) => obj is {bfName} handle && Equals(handle);");
                        //}
                        //codeWriter.WriteLine("/// <inheritdoc/>");
                        codeWriter.WriteLine($"public int GetHashCode() => Handle.GetHashCode();");
                        //codeWriter.WriteLine($"private string DebuggerDisplay => string.Format(\"{bfName} [0x{{0}}]\", Handle.ToString(\"X\"));");
                    }

                    codeWriter.WriteLine();
                }
                return 0;
			}
		}
	}
}
