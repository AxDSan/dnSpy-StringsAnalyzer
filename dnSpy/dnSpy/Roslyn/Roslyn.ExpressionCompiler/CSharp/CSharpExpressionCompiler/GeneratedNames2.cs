using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    static class GeneratedNames2
    {
        public static GeneratedNameKind GetKind(this CompilerKind compiler, string name)
        {
            return CommonGeneratedNames.GetKind(compiler, name).ToGeneratedNameKind();
        }

        public static bool IsSynthesizedLocalName(this CompilerKind compiler, string name)
        {
            return CommonGeneratedNames.IsSynthesizedLocalName(compiler, name);
        }

        public static bool TryParseSlotIndex(this CompilerKind compiler, string fieldName, out int slotIndex)
        {
            return CommonGeneratedNames.TryParseSlotIndex(compiler, fieldName, out slotIndex);
        }

        public static bool TryParseSourceMethodNameFromGeneratedName(this CompilerKind compiler, string generatedName, GeneratedNameKind requiredKind, [NotNullWhen(true)] out string? methodName)
        {
            return CommonGeneratedNames.TryParseSourceMethodNameFromGeneratedName(compiler, generatedName, requiredKind.ToCommonGeneratedNameKind(), out methodName);
        }

        public static bool TryParseGeneratedName(this CompilerKind compiler, string name, out GeneratedNameKind kind, [NotNullWhen(true)] out string? part)
        {
            var res = CommonGeneratedNames.TryParseGeneratedName(compiler, name, out var commonKind, out part);
            kind = commonKind.ToGeneratedNameKind();
            return res;
        }

        public static string? GetUnmangledTypeParameterName(this CompilerKind compiler, string typeParameterName)
        {
            return CommonGeneratedNames.GetUnmangledTypeParameterName(compiler, typeParameterName);
        }

        public static bool IsDisplayClassInstance(this CompilerKind compiler, string fieldType, string fieldName)
        {
            return CommonGeneratedNames.IsDisplayClassInstance(compiler, fieldType, fieldName);
        }
    }
}
