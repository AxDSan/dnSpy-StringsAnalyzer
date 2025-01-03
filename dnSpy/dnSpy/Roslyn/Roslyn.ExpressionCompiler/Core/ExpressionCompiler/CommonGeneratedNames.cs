// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code copied from GeneratedNames.{cs,vb} and CommonGeneratedNameKind.{cs,vb}, converted to C# if needed
// It's needed so we can debug C#/VB compiled code but still use VB/C# debug language (opposite language)
// and we must also support mcs.

//TODO: Whenever Roslyn gets updated, do this:
//  - Update this file if needed (is there a new GeneratedNameKind, etc)
//  - Check C# EC code for refs to GeneratedNames class and redirect to GeneratedNames2 class
//  - Check VB EC code for refs to GeneratedNames class and redirect to GeneratedNames2 class
//  - Check VB EC code for refs to StringConstants and use methods in this file instead

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum CompilerKind
    {
        Unknown,
        MicrosoftCSharp,
        MicrosoftVisualBasic,
        MonoCSharp,
    }

    internal enum CommonGeneratedNameKind
    {
        // VB
        None,
        //HoistedMeField,
        HoistedSynthesizedLocalField,
        HoistedUserVariableField,
        IteratorCurrentField,
        IteratorInitialThreadIdField,
        //IteratorParameterProxyField,
        StateMachineAwaiterField,
        StateMachineStateField,
        StateMachineHoistedUserVariableOrDisplayClassField,
        HoistedWithLocalPrefix,
        StaticLocalField,
        TransparentIdentifier,
        AnonymousTransparentIdentifier,
        AnonymousType,
        LambdaCacheField,
        LambdaDisplayClass,
        HoistedSpecialVariableField,

        // C#
        //None,
        //StateMachineStateField,
        IteratorCurrentBackingField,
        StateMachineParameterProxyField,
        ThisProxyField,
        HoistedLocalField,
        Deprecated_OuterscopeLocals,
        ReusableHoistedLocalField,
        DisplayClassLocalOrField,
        //LambdaCacheField,
        Deprecated_IteratorInstance,
        LambdaMethod,
        //LambdaDisplayClass,
        StateMachineType,
        FixedBufferField,
        FileType,
        //AnonymousType,
        LocalFunction,
        Deprecated_InitializerLocal,
        //TransparentIdentifier,
        AnonymousTypeField,
        AnonymousTypeTypeParameter,
        AutoPropertyBackingField,
        IteratorCurrentThreadIdField,
        IteratorFinallyMethod,
        BaseMethodWrapper,
        DynamicCallSiteContainerType,
        DynamicCallSiteField,
        Deprecated_DynamicDelegate,
        Deprecated_ComrefCallLocal,
        //HoistedSynthesizedLocalField,
        AsyncBuilderField,
        DelegateCacheContainerType,
        //AwaiterField,
        AsyncIteratorPromiseOfValueOrEndBackingField,
        DisposeModeField,
        CombinedTokensField,

        // mcs
        StateMachineDisposingField,
    }

    internal abstract class CommonGeneratedNames
    {
        static CommonGeneratedNames()
        {
            var mscs = new CSharp_GeneratedNames();
            var msvb = new VisualBasic_GeneratedNames();
            var mcs = new MonoCSharp_GeneratedNames();
            microsoftCSharpGeneratedName = new CommonGeneratedNames[] { mscs };
            microsoftVisualBasicGeneratedName = new CommonGeneratedNames[] { msvb };
            monoCSharpGeneratedName = new CommonGeneratedNames[] { mcs };
            allGeneratedNames = new CommonGeneratedNames[]
            {
                mscs,
                msvb,
                mcs,
            };
        }
        static readonly CommonGeneratedNames[] microsoftCSharpGeneratedName;
        static readonly CommonGeneratedNames[] microsoftVisualBasicGeneratedName;
        static readonly CommonGeneratedNames[] monoCSharpGeneratedName;
        static readonly CommonGeneratedNames[] allGeneratedNames;

        static CommonGeneratedNames[] GetGeneratedNames(CompilerKind compilerKind)
        {
            switch (compilerKind)
            {
            case CompilerKind.MicrosoftCSharp: return microsoftCSharpGeneratedName;
            case CompilerKind.MicrosoftVisualBasic: return microsoftVisualBasicGeneratedName;
            case CompilerKind.MonoCSharp: return monoCSharpGeneratedName;
            case CompilerKind.Unknown: return allGeneratedNames;
            default:
                Debug.Fail($"Unknown compiler kind: {compilerKind}");
                return allGeneratedNames;
            }
        }

        protected abstract CommonGeneratedNameKind GetKind(string name);
        protected abstract bool? IsSynthesizedLocalName(string name);
        protected abstract bool TryParseSlotIndex(string fieldName, out int slotIndex);
        protected abstract bool TryParseSourceMethodNameFromGeneratedName(string generatedName, CommonGeneratedNameKind requiredKind, out string? methodName);
        protected abstract bool TryParseGeneratedName(string name, out CommonGeneratedNameKind kind, out string? part);
        protected abstract bool TryParseHoistedUserVariableName(string proxyName, out string? variableName);
        protected abstract bool TryParseStateMachineHoistedUserVariableOrDisplayClassName(string proxyName, out string? variableName, out int index);
        protected abstract bool TryParseStateMachineTypeName(string stateMachineTypeName, out string? methodName);
        protected abstract bool TryParseStaticLocalFieldName(string fieldName, out string? methodName, out string? methodSignature, out string? localName);
        protected abstract bool TryGetUnmangledTypeParameterName(string typeParameterName, out string? unmangedTypeParameterName);
        protected abstract bool? IsDisplayClassInstance(string fieldType, string fieldName);

        public static CommonGeneratedNameKind GetKind(CompilerKind compiler, string name)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                var res = g.GetKind(name);
                if (res != CommonGeneratedNameKind.None)
                    return res;
            }
            return CommonGeneratedNameKind.None;
        }

        public static bool IsSynthesizedLocalName(CompilerKind compiler, string name)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                var res = g.IsSynthesizedLocalName(name);
                if (res != null)
                    return res.Value;
            }
            return false;
        }

        public static bool TryParseSlotIndex(CompilerKind compiler, string fieldName, out int slotIndex)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseSlotIndex(fieldName, out slotIndex))
                    return true;
            }
            slotIndex = -1;
            return false;
        }

        public static bool TryParseSourceMethodNameFromGeneratedName(CompilerKind compiler, string generatedName, CommonGeneratedNameKind requiredKind, out string? methodName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseSourceMethodNameFromGeneratedName(generatedName, requiredKind, out methodName))
                    return true;
            }
            methodName = null;
            return false;
        }

        public static bool TryParseGeneratedName(CompilerKind compiler, string name, out CommonGeneratedNameKind kind, out string? part)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                var b = g.TryParseGeneratedName(name, out kind, out part);
                if (kind != CommonGeneratedNameKind.None)
                    return b;
            }
            kind = CommonGeneratedNameKind.None;
            part = null;
            return false;
        }

        public static bool TryParseHoistedUserVariableName(CompilerKind compiler, string proxyName, out string? variableName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseHoistedUserVariableName(proxyName, out variableName))
                    return true;
            }
            variableName = null;
            return false;
        }

        public static bool TryParseStateMachineHoistedUserVariableOrDisplayClassName(CompilerKind compiler, string proxyName, out string? variableName, out int index)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseStateMachineHoistedUserVariableOrDisplayClassName(proxyName, out variableName, out index))
                    return true;
            }
            variableName = null;
            index = -1;
            return false;
        }

        public static bool TryParseStateMachineTypeName(CompilerKind compiler, string stateMachineTypeName, out string? methodName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseStateMachineTypeName(stateMachineTypeName, out methodName))
                    return true;
            }
            methodName = null;
            return false;
        }

        public static bool TryParseStaticLocalFieldName(CompilerKind compiler, string fieldName, out string? methodName, out string? methodSignature, out string? localName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryParseStaticLocalFieldName(fieldName, out methodName, out methodSignature, out localName))
                    return true;
            }
            methodName = null;
            methodSignature = null;
            localName = null;
            return false;
        }

        public static string? GetUnmangledTypeParameterName(CompilerKind compiler, string typeParameterName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                if (g.TryGetUnmangledTypeParameterName(typeParameterName, out var unmangedTypeParameterName))
                    return unmangedTypeParameterName;
            }
            return typeParameterName;
        }

        public static bool IsDisplayClassInstance(CompilerKind compiler, string fieldType, string fieldName)
        {
            foreach (var g in GetGeneratedNames(compiler))
            {
                var res = g.IsDisplayClassInstance(fieldType, fieldName);
                if (res != null)
                    return res.Value;
            }
            return false;
        }

        sealed class MonoCSharp_GeneratedNames : CommonGeneratedNames
        {
            internal const char DotReplacementInTypeNames = '-';

            protected override CommonGeneratedNameKind GetKind(string name)
            {
                return TryParseGeneratedName(name, out var kind, out _, out _) ? kind : CommonGeneratedNameKind.None;
            }

            internal static bool TryParseGeneratedName(
                string name,
                out CommonGeneratedNameKind kind,
                out int openBracketOffset,
                out int closeBracketOffset)
            {
                openBracketOffset = -1;
                closeBracketOffset = -1;

                if (name.StartsWith("$", StringComparison.Ordinal))
                {
                    switch (name)
                    {
                    case "$current":
                        kind = CommonGeneratedNameKind.IteratorCurrentBackingField;
                        return true;
                    case "$disposing":
                        kind = CommonGeneratedNameKind.StateMachineDisposingField;
                        return true;
                    case "$PC":
                        kind = CommonGeneratedNameKind.StateMachineStateField;
                        return true;
                    case "$this":
                        kind = CommonGeneratedNameKind.ThisProxyField;
                        return true;
                    case "$builder":
                        kind = CommonGeneratedNameKind.AsyncBuilderField;
                        return true;
                    default:
                        if (name.StartsWith("$locvar", StringComparison.Ordinal))
                        {
                            kind = CommonGeneratedNameKind.HoistedSynthesizedLocalField;
                            return true;
                        }
                        if (name.StartsWith("$awaiter", StringComparison.Ordinal))
                        {
                            kind = CommonGeneratedNameKind.StateMachineAwaiterField;
                            return true;
                        }
                        if (name.StartsWith("$stack", StringComparison.Ordinal))
                        {
                            kind = CommonGeneratedNameKind.HoistedSynthesizedLocalField;
                            return true;
                        }
                        break;
                    }
                }

                if (name == "<>f__this")
                {
                    kind = CommonGeneratedNameKind.ThisProxyField;
                    return true;
                }

                if (name.StartsWith("<", StringComparison.Ordinal))
                {
                    openBracketOffset = 0;
                    closeBracketOffset = StringExtensions.IndexOfBalancedParenthesis(name, openBracketOffset, '>');
                    if (closeBracketOffset >= 0 && closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                    {
                        int index = closeBracketOffset + 1;
                        var c = name[index++];
                        if (c == '_' && index < name.Length && name[index] == '_')
                        {
                            kind = CommonGeneratedNameKind.HoistedLocalField;
                            return true;
                        }
                        switch (c)
                        {
                        case 'c':
                            var s = name.Substring(index);
                            if (s.StartsWith("__Iterator", StringComparison.Ordinal))
                            {
                                kind = CommonGeneratedNameKind.StateMachineType;
                                return true;
                            }
                            else if (s.StartsWith("__async", StringComparison.Ordinal))
                            {
                                kind = CommonGeneratedNameKind.StateMachineType;
                                return true;
                            }
                            else if (s.StartsWith("__AnonStorey", StringComparison.Ordinal))
                            {
                                kind = CommonGeneratedNameKind.LambdaDisplayClass;
                                return true;
                            }
                            break;
                        case 'm':
                            kind = CommonGeneratedNameKind.LambdaMethod;
                            return true;
                        case 'k':
                            kind = CommonGeneratedNameKind.AutoPropertyBackingField;
                            return true;
                        }
                    }
                }

                kind = CommonGeneratedNameKind.None;
                openBracketOffset = -1;
                closeBracketOffset = -1;
                return false;
            }

            protected override bool? IsSynthesizedLocalName(string name)
            {
                return null;
            }

            protected override bool TryParseSlotIndex(string fieldName, out int slotIndex)
            {
                int lastUnder = fieldName.LastIndexOf('_');
                if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_')
                {
                    slotIndex = -1;
                    return false;
                }

                if (int.TryParse(fieldName.Substring(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 0)
                {
                    return true;
                }

                slotIndex = -1;
                return false;
            }

            protected override bool TryParseSourceMethodNameFromGeneratedName(string generatedName, CommonGeneratedNameKind requiredKind, out string? methodName)
            {
                int openBracketOffset;
                int closeBracketOffset;
                CommonGeneratedNameKind kind;
                if (!TryParseGeneratedName(generatedName, out kind, out openBracketOffset, out closeBracketOffset))
                {
                    methodName = null;
                    return false;
                }

                if (requiredKind != 0 && kind != requiredKind)
                {
                    methodName = null;
                    return false;
                }

                methodName = generatedName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

                if (GeneratedNameKindExtensions.IsTypeName(kind))
                {
                    methodName = methodName.Replace(DotReplacementInTypeNames, '.');
                }

                return true;
            }

            protected override bool TryParseGeneratedName(string name, out CommonGeneratedNameKind kind, out string? part)
            {
                bool result = TryParseGeneratedName(name, out kind, out int openBracketOffset, out int closeBracketOffset);
                switch (kind)
                {
                case CommonGeneratedNameKind.AnonymousTypeField:
                case CommonGeneratedNameKind.HoistedLocalField:
                    part = name.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    break;
                default:
                    part = null;
                    break;
                }
                return result;
            }

            protected override bool TryParseHoistedUserVariableName(string proxyName, out string? variableName)
            {
                variableName = null;
                return false;
            }

            protected override bool TryParseStateMachineHoistedUserVariableOrDisplayClassName(string proxyName, out string? variableName, out int index)
            {
                TryParseGeneratedName(proxyName, out var kind, out int openBracketOffset, out int closeBracketOffset);
                if (kind == CommonGeneratedNameKind.HoistedLocalField)
                {
                    variableName = proxyName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    if (TryParseSlotIndex(proxyName, out index))
                        return true;
                }

                variableName = null;
                index = -1;
                return false;
            }

            protected override bool TryParseStateMachineTypeName(string stateMachineTypeName, out string? methodName)
            {
                TryParseGeneratedName(stateMachineTypeName, out var kind, out int openBracketOffset, out int closeBracketOffset);
                switch (kind)
                {
                case CommonGeneratedNameKind.StateMachineType:
                    methodName = stateMachineTypeName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    return true;
                default:
                    methodName = null;
                    return false;
                }
            }

            protected override bool TryParseStaticLocalFieldName(string fieldName, out string? methodName, out string? methodSignature, out string? localName)
            {
                methodName = null;
                methodSignature = null;
                localName = null;
                return false;
            }

            protected override bool TryGetUnmangledTypeParameterName(string typeParameterName, out string? unmangedTypeParameterName)
            {
                unmangedTypeParameterName = null;
                return false;
            }

            protected override bool? IsDisplayClassInstance(string fieldType, string fieldName)
            {
                TryParseGeneratedName(fieldName, out var fieldKind, out _);

                switch (fieldKind)
                {
                case CommonGeneratedNameKind.DisplayClassLocalOrField:
                    break;
                case CommonGeneratedNameKind.ThisProxyField:
                    if (GetKind(fieldType) != CommonGeneratedNameKind.LambdaDisplayClass)
                    {
                        return null;
                    }
                    // Async lambda case.
                    break;
                default:
                    return null;
                }
                return true;
            }
        }

        sealed class CSharp_GeneratedNames : CommonGeneratedNames
        {
            internal const string SynthesizedLocalNamePrefix = "CS$";
            internal const char DotReplacementInTypeNames = '-';
            private const string SuffixSeparator = "__";
            private const char IdSeparator = '_';
            private const char GenerationSeparator = '#';
            private const char LocalFunctionNameTerminator = '|';

            protected override CommonGeneratedNameKind GetKind(string name)
            {
                return TryParseGeneratedName(name, out var kind, out _, out _) ? kind : CommonGeneratedNameKind.None;
            }

            protected override bool? IsSynthesizedLocalName(string name)
            {
                return name.StartsWith(SynthesizedLocalNamePrefix, StringComparison.Ordinal);
            }

            // Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
            // Such a name ends with "__{slot index + 1}".
            // Returned slot index is >= 0.
            protected override bool TryParseSlotIndex(string fieldName, out int slotIndex)
            {
                int lastUnder = fieldName.LastIndexOf('_');
                if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_')
                {
                    slotIndex = -1;
                    return false;
                }

                if (int.TryParse(fieldName.Substring(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 1)
                {
                    slotIndex--;
                    return true;
                }

                slotIndex = -1;
                return false;
            }

            protected override bool TryParseSourceMethodNameFromGeneratedName(string generatedName, CommonGeneratedNameKind requiredKind, out string? methodName)
            {
                if (!TryParseGeneratedName(generatedName, out var kind, out var openBracketOffset, out var closeBracketOffset))
                {
                    methodName = null;
                    return false;
                }

                if (requiredKind != 0 && kind != requiredKind)
                {
                    methodName = null;
                    return false;
                }

                methodName = generatedName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

                if (GeneratedNameKindExtensions.IsTypeName(kind))
                    methodName = methodName.Replace(DotReplacementInTypeNames, '.');

                return true;
            }

            protected override bool TryParseGeneratedName(string name, out CommonGeneratedNameKind kind, out string? part)
            {
                bool result = TryParseGeneratedName(name, out kind, out int openBracketOffset, out int closeBracketOffset);
                switch (kind)
                {
                case CommonGeneratedNameKind.AnonymousTypeField:
                case CommonGeneratedNameKind.HoistedLocalField:
                    part = name.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    break;
                default:
                    part = null;
                    break;
                }
                return result;
            }

            protected override bool TryParseHoistedUserVariableName(string proxyName, out string? variableName)
            {
                variableName = null;
                return false;
            }

            protected override bool TryParseStateMachineHoistedUserVariableOrDisplayClassName(string proxyName, out string? variableName, out int index)
            {
                TryParseGeneratedName(proxyName, out var kind, out int openBracketOffset, out int closeBracketOffset);
                if (kind == CommonGeneratedNameKind.HoistedLocalField)
                {
                    variableName = proxyName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    if (TryParseSlotIndex(proxyName, out index))
                        return true;
                }

                variableName = null;
                index = -1;
                return false;
            }

            protected override bool TryParseStateMachineTypeName(string stateMachineTypeName, out string? methodName)
            {
                TryParseGeneratedName(stateMachineTypeName, out var kind, out int openBracketOffset, out int closeBracketOffset);
                switch (kind)
                {
                case CommonGeneratedNameKind.StateMachineType:
                    methodName = stateMachineTypeName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    return true;
                default:
                    methodName = null;
                    return false;
                }
            }

            protected override bool TryParseStaticLocalFieldName(string fieldName, out string? methodName, out string? methodSignature, out string? localName)
            {
                methodName = null;
                methodSignature = null;
                localName = null;
                return false;
            }

            protected override bool TryGetUnmangledTypeParameterName(string typeParameterName, out string? unmangedTypeParameterName)
            {
                unmangedTypeParameterName = null;
                return false;
            }

            protected override bool? IsDisplayClassInstance(string fieldType, string fieldName)
            {
                TryParseGeneratedName(fieldName, out var fieldKind, out _);

                switch (fieldKind)
                {
                case CommonGeneratedNameKind.DisplayClassLocalOrField:
                    break;
                case CommonGeneratedNameKind.ThisProxyField:
                    if (GetKind(fieldType) != CommonGeneratedNameKind.LambdaDisplayClass)
                    {
                        return null;
                    }
                    // Async lambda case.
                    break;
                default:
                    return null;
                }
                return true;
            }

            // Parse the generated name. Returns true for names of the form
            // [CS$]<[middle]>c[__[suffix]] where [CS$] is included for certain
            // generated names, where [middle] and [__[suffix]] are optional,
            // and where c is a single character in [1-9a-z]
            // (csharp\LanguageAnalysis\LIB\SpecialName.cpp).
            internal static bool TryParseGeneratedName(
                string name,
                out CommonGeneratedNameKind kind,
                out int openBracketOffset,
                out int closeBracketOffset)
            {
                openBracketOffset = -1;
                if (name.StartsWith("CS$<", StringComparison.Ordinal))
                {
                    openBracketOffset = 3;
                }
                else if (name.StartsWith("<", StringComparison.Ordinal))
                {
                    openBracketOffset = 0;
                }

                if (openBracketOffset >= 0)
                {
                    closeBracketOffset = StringExtensions.IndexOfBalancedParenthesis(name, openBracketOffset, '>');
                    if (closeBracketOffset >= 0 && closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                    {
                        int c = name[closeBracketOffset + 1];
                        if ((c >= '1' && c <= '9') || (c >= 'a' && c <= 'z') || c == 'F' || c == 'O') // Note '0' is not special.
                        {
                            switch (c)
                            {
                            case '4': kind = CommonGeneratedNameKind.ThisProxyField; break;
                            case '5': kind = CommonGeneratedNameKind.HoistedLocalField; break;
                            case '8': kind = CommonGeneratedNameKind.DisplayClassLocalOrField; break;
                            case 'b': kind = CommonGeneratedNameKind.LambdaMethod; break;
                            case 'c': kind = CommonGeneratedNameKind.LambdaDisplayClass; break;
                            case 'd': kind = CommonGeneratedNameKind.StateMachineType; break;
                            case 'u': kind = CommonGeneratedNameKind.StateMachineAwaiterField; break;
                            case 's': kind = CommonGeneratedNameKind.HoistedSynthesizedLocalField; break;
                            case '1': kind = CommonGeneratedNameKind.StateMachineStateField; break;
                            case '2': kind = CommonGeneratedNameKind.IteratorCurrentBackingField; break;
                            case '3': kind = CommonGeneratedNameKind.StateMachineParameterProxyField; break;
                            case '7': kind = CommonGeneratedNameKind.ReusableHoistedLocalField; break;
                            case '9': kind = CommonGeneratedNameKind.LambdaCacheField; break;
                            case 'e': kind = CommonGeneratedNameKind.FixedBufferField; break;
                            case 'F': kind = CommonGeneratedNameKind.FileType; break;
                            case 'f': kind = CommonGeneratedNameKind.AnonymousType; break;
                            case 'h': kind = CommonGeneratedNameKind.TransparentIdentifier; break;
                            case 'i': kind = CommonGeneratedNameKind.AnonymousTypeField; break;
                            case 'k': kind = CommonGeneratedNameKind.AutoPropertyBackingField; break;
                            case 'l': kind = CommonGeneratedNameKind.IteratorCurrentThreadIdField; break;
                            case 'm': kind = CommonGeneratedNameKind.IteratorFinallyMethod; break;
                            case 'n': kind = CommonGeneratedNameKind.BaseMethodWrapper; break;
                            case 't': kind = CommonGeneratedNameKind.AsyncBuilderField; break;
                            case 'O': kind = CommonGeneratedNameKind.DelegateCacheContainerType; break;
                            case 'o': kind = CommonGeneratedNameKind.DynamicCallSiteContainerType; break;
                            case 'p': kind = CommonGeneratedNameKind.DynamicCallSiteField; break;
                            case '6': kind = CommonGeneratedNameKind.Deprecated_OuterscopeLocals; break;
                            case 'a': kind = CommonGeneratedNameKind.Deprecated_IteratorInstance; break;
                            case 'g': kind = CommonGeneratedNameKind.LocalFunction; break;// Also Deprecated_InitializerLocal
                            case 'j': kind = CommonGeneratedNameKind.AnonymousTypeTypeParameter; break;
                            case 'q': kind = CommonGeneratedNameKind.Deprecated_DynamicDelegate; break;
                            case 'r': kind = CommonGeneratedNameKind.Deprecated_ComrefCallLocal; break;
                            case 'v': kind = CommonGeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField; break;
                            case 'w': kind = CommonGeneratedNameKind.DisposeModeField; break;
                            case 'x': kind = CommonGeneratedNameKind.CombinedTokensField; break;
                            default: kind = CommonGeneratedNameKind.None; break;
                            }
                            return kind != CommonGeneratedNameKind.None;
                        }
                    }
                }

                kind = CommonGeneratedNameKind.None;
                openBracketOffset = -1;
                closeBracketOffset = -1;
                return false;
            }
        }

        sealed class VisualBasic_GeneratedNames : CommonGeneratedNames
        {
            internal const string AnonymousTypeOrDelegateCommonPrefix = "VB$Anonymous";
            internal const string AnonymousTypeTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix + "Type_";
            internal const string AnonymousDelegateTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix + "Delegate_";
            internal const char s_methodNameSeparator = '_';

            protected override CommonGeneratedNameKind GetKind(string name)
            {
                if (name.StartsWith(VisualBasic_StringConstants.HoistedMeName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.ThisProxyField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.StateMachineStateFieldName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineStateField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StaticLocalField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.HoistedSynthesizedLocalPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.HoistedSynthesizedLocalField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.HoistedUserVariablePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.HoistedUserVariableField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.HoistedSpecialVariablePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.HoistedSpecialVariableField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.IteratorCurrentFieldName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.IteratorCurrentField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.IteratorInitialThreadIdName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.IteratorInitialThreadIdField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.IteratorParameterProxyPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineParameterProxyField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.StateMachineAwaiterFieldPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineAwaiterField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.HoistedWithLocalPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.HoistedWithLocalPrefix;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineHoistedUserVariableOrDisplayClassField;
                }
                else if (name.StartsWith(AnonymousTypeTemplateNamePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.AnonymousType;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.DisplayClassPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.LambdaDisplayClass;
                }
                else if (name.Equals(VisualBasic_StringConstants.It, StringComparison.Ordinal) || name.Equals(VisualBasic_StringConstants.It1, StringComparison.Ordinal) || name.Equals(VisualBasic_StringConstants.It2, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.TransparentIdentifier;
                }
                else if (name.Equals(VisualBasic_StringConstants.ItAnonymous, StringComparison.Ordinal))
                {
                    // We distinguish VisualBasic_StringConstants.ItAnonymous, because it won't be an instance
                    // of an anonymous type.
                    return CommonGeneratedNameKind.AnonymousTransparentIdentifier;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineType;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.LambdaCacheFieldPrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.LambdaCacheField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.ClosureVariablePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.DisplayClassLocalOrField;
                }
                else if (name.StartsWith(VisualBasic_StringConstants.LambdaMethodNamePrefix, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.LambdaMethod;
                }
                else if (name.Equals(VisualBasic_StringConstants.StateMachineBuilderFieldName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.AsyncBuilderField;
                }
                else if (name.Equals(VisualBasic_StringConstants.StateMachineStateFieldName, StringComparison.Ordinal))
                {
                    return CommonGeneratedNameKind.StateMachineStateField;
                }
                return CommonGeneratedNameKind.None;
            }

            protected override bool? IsSynthesizedLocalName(string name)
            {
                return null;
            }

            protected override bool TryParseSlotIndex(string fieldName, out int slotIndex)
            {
                if (TryParseStateMachineHoistedUserVariableOrDisplayClassName(fieldName, out _, out slotIndex))
                    return true;
                slotIndex = -1;
                return false;
            }

            protected override bool TryParseSourceMethodNameFromGeneratedName(string generatedName, CommonGeneratedNameKind requiredKind, out string? methodName)
            {
                methodName = null;
                return false;
            }

            protected override bool TryParseGeneratedName(string name, out CommonGeneratedNameKind kind, out string? part)
            {
                kind = GetKind(name);
                if (kind != CommonGeneratedNameKind.None)
                {
                    if (TryParseStateMachineHoistedUserVariableOrDisplayClassName(name, out part, out _))
                        return true;
                    switch (kind)
                    {
                    case CommonGeneratedNameKind.HoistedSpecialVariableField:
                        part = name.Substring(VisualBasic_StringConstants.HoistedSpecialVariablePrefix.Length);
                        return true;
                    case CommonGeneratedNameKind.HoistedUserVariableField:
                        part = name.Substring(VisualBasic_StringConstants.HoistedUserVariablePrefix.Length);
                        return true;
                    }

                    part = null;
                    return true;
                }

                part = null;
                return false;
            }

            protected override bool TryParseHoistedUserVariableName(string proxyName, out string? variableName)
            {
                variableName = null;

                int prefixLen = VisualBasic_StringConstants.HoistedUserVariablePrefix.Length;
                if (proxyName.Length <= prefixLen)
                {
                    return false;
                }

                if (!proxyName.StartsWith(VisualBasic_StringConstants.HoistedUserVariablePrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                variableName = proxyName.Substring(prefixLen);
                return true;
            }

            protected override bool TryParseStateMachineHoistedUserVariableOrDisplayClassName(string proxyName, out string? variableName, out int index)
            {
                variableName = null;
                index = 0;

                if (!proxyName.StartsWith(VisualBasic_StringConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                int prefixLen = VisualBasic_StringConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix.Length;
                int separator = proxyName.LastIndexOf('$');
                if (separator <= prefixLen)
                {
                    return false;
                }

                variableName = proxyName.Substring(prefixLen, separator - prefixLen);
                return int.TryParse(proxyName.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out index);
            }

            protected override bool TryParseStateMachineTypeName(string stateMachineTypeName, out string? methodName)
            {
                methodName = null;

                if (!stateMachineTypeName.StartsWith(VisualBasic_StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                int prefixLength = VisualBasic_StringConstants.StateMachineTypeNamePrefix.Length;
                int separatorPos = stateMachineTypeName.IndexOf(s_methodNameSeparator, prefixLength);
                if (separatorPos < 0 || separatorPos == stateMachineTypeName.Length - 1)
                {
                    return false;
                }

                methodName = stateMachineTypeName.Substring(separatorPos + 1);
                return true;
            }

            protected override bool TryParseStaticLocalFieldName(string fieldName, out string? methodName, out string? methodSignature, out string? localName)
            {
                if (fieldName.StartsWith(VisualBasic_StringConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal))
                {
                    var parts = fieldName.Split('$');
                    if (parts.Length == 5)
                    {
                        methodName = parts[2];
                        methodSignature = parts[3];
                        localName = parts[4];
                        return true;
                    }
                }

                methodName = null;
                methodSignature = null;
                localName = null;
                return false;
            }

            protected override bool TryGetUnmangledTypeParameterName(string typeParameterName, out string? unmangedTypeParameterName)
            {
                if (typeParameterName.StartsWith(VisualBasic_StringConstants.StateMachineTypeParameterPrefix, StringComparison.Ordinal))
                {
                    unmangedTypeParameterName = typeParameterName.Substring(VisualBasic_StringConstants.StateMachineTypeParameterPrefix.Length);
                    return true;
                }

                unmangedTypeParameterName = null;
                return false;
            }

            protected override bool? IsDisplayClassInstance(string fieldType, string fieldName)
            {
                return fieldName.StartsWith(VisualBasic_StringConstants.HoistedSpecialVariablePrefix + VisualBasic_StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) ||
                       fieldName.StartsWith(VisualBasic_StringConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix + VisualBasic_StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) ||
                       fieldName.StartsWith(VisualBasic_StringConstants.HoistedSpecialVariablePrefix + VisualBasic_StringConstants.DisplayClassPrefix, StringComparison.Ordinal); // Async lambda case
            }

            sealed class VisualBasic_StringConstants
            {
                internal const string AnonymousTypeName = "<anonymous type>";
                internal const string AnonymousMethodName = "<anonymous method>";
                internal const string AsEnumerableMethod = "AsEnumerable";
                internal const string AsQueryableMethod = "AsQueryable";
                internal const string DistinctMethod = "Distinct";
                internal const string CastMethod = "Cast";
                internal const string DelegateConstructorInstanceParameterName = "TargetObject";
                internal const string DelegateConstructorMethodParameterName = "TargetMethod";
                internal const string DelegateMethodCallbackParameterName = "DelegateCallback";
                internal const string DelegateMethodInstanceParameterName = "DelegateAsyncState";
                internal const string DelegateMethodResultParameterName = "DelegateAsyncResult";
                internal const string DelegateStubParameterPrefix = "a";
                internal const string ElementAtMethod = "ElementAtOrDefault";
                internal const string Group = "$VB$Group";
                internal const string GroupByMethod = "GroupBy";
                internal const string GroupJoinMethod = "GroupJoin";
                internal const string It = "$VB$It";
                internal const string It1 = "$VB$It1";
                internal const string It2 = "$VB$It2";
                internal const string ItAnonymous = "$VB$ItAnonymous";
                internal const string JoinMethod = "Join";
                internal const string Lambda = "Lambda";
                internal const string NamedSymbolErrorName = "?";
                internal const string OrderByDescendingMethod = "OrderByDescending";
                internal const string OrderByMethod = "OrderBy";
                internal const string SelectManyMethod = "SelectMany";
                internal const string SelectMethod = "Select";
                internal const string SkipMethod = "Skip";
                internal const string SkipWhileMethod = "SkipWhile";
                internal const string TakeMethod = "Take";
                internal const string TakeWhileMethod = "TakeWhile";
                internal const string ThenByDescendingMethod = "ThenByDescending";
                internal const string ThenByMethod = "ThenBy";
                internal const string UnnamedNamespaceErrName = "<Default>";
                internal const string WhereMethod = "Where";
                internal const string HoistedMeName = "$VB$Me";
                internal const string HoistedUserVariablePrefix = "$VB$Local_";
                internal const string HoistedSpecialVariablePrefix = "$VB$NonLocal_";
                internal const string HoistedWithLocalPrefix = "$W";
                internal const string StateMachineHoistedUserVariableOrDisplayClassPrefix = "$VB$ResumableLocal_";
                internal const string ClosureVariablePrefix = "$VB$Closure_";
                internal const string DisplayClassPrefix = "_Closure$__";
                internal const string StateMachineTypeNamePrefix = "VB$StateMachine_";
                internal const string LambdaMethodNamePrefix = "_Lambda$__";
                internal const string DisplayClassGenericParameterNamePrefix = "$CLS";
                internal const string BaseMethodWrapperNamePrefix = "$VB$ClosureStub_";
                internal const string StateMachineBuilderFieldName = "$Builder";
                internal const string StateMachineStateFieldName = "$State";
                internal const string DelegateRelaxationDisplayClassPrefix = "_Closure$__R";
                internal const string DelegateRelaxationMethodNamePrefix = "_Lambda$__R";
                internal const string HoistedSynthesizedLocalPrefix = "$S";
                internal const string LambdaCacheFieldPrefix = "$I";
                internal const string DelegateRelaxationCacheFieldPrefix = "$IR";
                internal const string StateMachineAwaiterFieldPrefix = "$A";
                internal const string ReusableHoistedLocalFieldName = "$U";
                internal const string StateMachineExpressionCapturePrefix = "$V";
                internal const string StateMachineTypeParameterPrefix = "SM$";
                internal const string IteratorCurrentFieldName = "$Current";
                internal const string IteratorInitialThreadIdName = "$InitialThreadId";
                internal const string IteratorParameterProxyPrefix = "$P_";
                internal const string StaticLocalFieldNamePrefix = "$STATIC$";
                internal const string PropertyGetPrefix = "get_";
                internal const string PropertySetPrefix = "set_";
                internal const string WinMdPropertySetPrefix = "put_";
                internal const string ValueParameterName = "Value";
                internal const string WithEventsValueParameterName = "WithEventsValue";
                internal const string AutoPropertyValueParameterName = "AutoPropertyValue";
                internal const string DefaultXmlnsPrefix = "";
                internal const string DefaultXmlNamespace = "";
                internal const string XmlPrefix = "xml";
                internal const string XmlnsPrefix = "xmlns";
                internal const string XmlAddMethodName = "Add";
                internal const string XmlGetMethodName = "Get";
                internal const string XmlElementsMethodName = "Elements";
                internal const string XmlDescendantsMethodName = "Descendants";
                internal const string XmlAttributeValueMethodName = "AttributeValue";
                internal const string XmlCreateAttributeMethodName = "CreateAttribute";
                internal const string XmlCreateNamespaceAttributeMethodName = "CreateNamespaceAttribute";
                internal const string XmlRemoveNamespaceAttributesMethodName = "RemoveNamespaceAttributes";
                internal const string ValueProperty = "Value";
            }
        }

        internal static class GeneratedNameKindExtensions
        {
            internal static bool IsTypeName(CommonGeneratedNameKind kind)
            {
                switch (kind)
                {
                case CommonGeneratedNameKind.LambdaDisplayClass:
                case CommonGeneratedNameKind.StateMachineType:
                case CommonGeneratedNameKind.DynamicCallSiteContainerType:
                    return true;

                default:
                    return false;
                }
            }
        }

        static class StringExtensions
        {
            internal static int IndexOfBalancedParenthesis(string str, int openingOffset, char closing)
            {
                char opening = str[openingOffset];

                int depth = 1;
                for (int i = openingOffset + 1; i < str.Length; i++)
                {
                    var c = str[i];
                    if (c == opening)
                    {
                        depth++;
                    }
                    else if (c == closing)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }
        }
    }
}
