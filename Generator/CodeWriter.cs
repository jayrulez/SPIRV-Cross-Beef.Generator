// This code has been based from the sample repository "Vortice.Vulkan": https://github.com/amerkoleci/Vortice.Vulkan
// Copyright (c) Amer Koleci and contributors.
// Copyright (c) 2020 - 2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;

namespace SPIRV_Cross_Beef.Generator
{
    class CodeWriter : IDisposable
    {
        private class CodeBlock : IDisposable
        {
            private readonly CodeWriter _writer;

            public CodeBlock(CodeWriter writer, string content)
            {
                _writer = writer;
                _writer.BeginBlock(content);
            }

            public void Dispose()
            {
                _writer.EndBlock();
            }
        }

        private bool _shouldIndent = true;
        private readonly StreamWriter _writer;
        private static readonly Dictionary<int, string> _indentStrings = new Dictionary<int, string>()
        {
            { 0, "" },
            { 1, "\t" },
            { 2, new string('\t', 2) },
            { 3, new string('\t', 3) },
            { 4, new string('\t', 4) },
            { 5, new string('\t', 5) },
            { 6, new string('\t', 6) },
            { 7, new string('\t', 7) },
            { 8, new string('\t', 8) },
            { 9, new string('\t', 9) },

        };

        private string _indentString = null;

        public int IndentLevel { get; private set; }


        private void Indent(int count = 1)
        {
            IndentLevel += count;
            if (!_indentStrings.ContainsKey(IndentLevel))
            {
                _indentStrings.Add(IndentLevel, new string('\t', IndentLevel));
            }

            _indentString = _indentStrings[IndentLevel];
        }

        private void Unindent(int count = 1)
        {
            if (count > IndentLevel)
                throw new ArgumentException("count out of range.", nameof(count));


            IndentLevel -= count;
            if (!_indentStrings.ContainsKey(IndentLevel))
            {
                _indentStrings.Add(IndentLevel, new string('\t', IndentLevel));
            }

            _indentString = _indentStrings[IndentLevel];
        }

        private void WriteIndented(char content)
        {
            if (_shouldIndent)
            {
                _writer.Write(_indentString);
                _shouldIndent = false;
            }

            _writer.Write(content);
        }

        private void WriteIndented(string content)
        {
            if (_shouldIndent)
            {
                _writer.Write(_indentString);
                _shouldIndent = false;
            }

            _writer.Write(content);
        }

        public CodeWriter(string fileName, string fileNamespace, params string[] usedNamespaces)
        {
            _writer = File.CreateText(fileName);

            foreach (var usedNamespace in usedNamespaces)
            {
                _writer.WriteLine($"using {usedNamespace};");
            }

            if(usedNamespaces.Length > 0)
            {
                _writer.WriteLine();
            }
            _writer.WriteLine($"namespace {fileNamespace};");
            _writer.WriteLine();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public IDisposable PushBlock(string marker = "{") => new CodeBlock(this, marker);

        public void Write(char content)
        {
            WriteIndented(content);
        }

        public void Write(string content)
        {
            WriteIndented(content);
        }

        public void WriteLine()
        {
            _writer.WriteLine();

            _shouldIndent = true;
        }

        public void WriteLine(string content)
        {
            WriteIndented(content);
            _writer.WriteLine();
            _shouldIndent = true;
        }

        public void BeginBlock(string content)
        {
            WriteLine(content);
            WriteLine("{");
            Indent(1);
        }

        public void EndBlock()
        {
            Unindent(1);
            WriteLine("}");
        }
    }
}
