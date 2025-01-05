using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static StringsAnalyzer.Extension.ToolWindowVM;

namespace StringsAnalyzer.Extension {
    public class StringAnalyzer {
        private readonly ModuleDefMD module;

        public StringAnalyzer(ModuleDefMD module) {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public List<StringAnalyzerData> Analyze() {
            var results = new List<StringAnalyzerData>();
            
            // Analyze all types in the module
            foreach (var type in module.Types) {
                // Skip if it's a nested type, we'll handle it through its declaring type
                if (type.IsNested)
                    continue;

                AnalyzeType(type, results);
            }

            // Sort results by string value for easier reading
            results.Sort((a, b) => string.Compare(a.StringValue, b.StringValue, StringComparison.Ordinal));
            
            return results;
        }

        private void AnalyzeType(TypeDef type, List<StringAnalyzerData> results) {
            // Analyze methods in the current type
            foreach (var method in type.Methods) {
                if (!method.HasBody)
                    continue;

                var instructions = method.Body.Instructions;
                foreach (var instr in instructions) {
                    if (!instr.OpCode.Equals(OpCodes.Ldstr))
                        continue;

                    var stringValue = instr.Operand?.ToString();
                    if (string.IsNullOrEmpty(stringValue))
                        continue;

                    var formattedOffset = $"IL_{instr.GetOffset():X4}";
                    var token = method.MDToken;

                    results.Add(new StringAnalyzerData() {
                        StringValue = stringValue,
                        IlOffset = formattedOffset,
                        MdToken = $"0x{token.ToString().Remove(0, 1):x}",
                        MdName = method.Name,
                        FullmdName = method.FullName,
                        ModuleID = module.Location,
                        Method = method,
                        Offset = instr.GetOffset()
                    });
                }
            }

            // Recursively analyze nested types
            foreach (var nestedType in type.NestedTypes) {
                AnalyzeType(nestedType, results);
            }
        }
    }
}