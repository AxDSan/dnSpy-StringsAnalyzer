using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
#pragma warning disable 0612 // Type or member is obsolete
    static class CommonGeneratedNameKindExtensions
    {
        public static GeneratedNameKind ToGeneratedNameKind(this CommonGeneratedNameKind kind)
        {
            const GeneratedNameKind hiddenKind = (GeneratedNameKind)(-1);
            switch (kind)
            {
            case CommonGeneratedNameKind.None: return GeneratedNameKind.None;
            case CommonGeneratedNameKind.HoistedSynthesizedLocalField: return GeneratedNameKind.HoistedSynthesizedLocalField;
            // Return None so it will be recognized as a parameter (iterators/async methods)
            case CommonGeneratedNameKind.HoistedUserVariableField: return GeneratedNameKind.None;
            case CommonGeneratedNameKind.IteratorCurrentField: return GeneratedNameKind.IteratorCurrentBackingField;
            case CommonGeneratedNameKind.IteratorInitialThreadIdField: return GeneratedNameKind.IteratorCurrentThreadIdField;
            case CommonGeneratedNameKind.StateMachineAwaiterField: return GeneratedNameKind.AwaiterField;
            case CommonGeneratedNameKind.StateMachineStateField: return GeneratedNameKind.StateMachineStateField;
            case CommonGeneratedNameKind.StateMachineHoistedUserVariableOrDisplayClassField: return GeneratedNameKind.HoistedLocalField;
            case CommonGeneratedNameKind.HoistedWithLocalPrefix: return hiddenKind;
            case CommonGeneratedNameKind.StaticLocalField: return hiddenKind;
            case CommonGeneratedNameKind.TransparentIdentifier: return GeneratedNameKind.TransparentIdentifier;
            case CommonGeneratedNameKind.AnonymousTransparentIdentifier: return GeneratedNameKind.TransparentIdentifier;
            case CommonGeneratedNameKind.AnonymousType: return GeneratedNameKind.AnonymousType;
            case CommonGeneratedNameKind.LambdaCacheField: return GeneratedNameKind.LambdaCacheField;
            case CommonGeneratedNameKind.LambdaDisplayClass: return GeneratedNameKind.LambdaDisplayClass;
            case CommonGeneratedNameKind.HoistedSpecialVariableField: return GeneratedNameKind.HoistedLocalField;
            case CommonGeneratedNameKind.IteratorCurrentBackingField: return GeneratedNameKind.IteratorCurrentBackingField;
            case CommonGeneratedNameKind.StateMachineParameterProxyField: return GeneratedNameKind.StateMachineParameterProxyField;
            case CommonGeneratedNameKind.ThisProxyField: return GeneratedNameKind.ThisProxyField;
            case CommonGeneratedNameKind.HoistedLocalField: return GeneratedNameKind.HoistedLocalField;
            case CommonGeneratedNameKind.Deprecated_OuterscopeLocals: return GeneratedNameKind.Deprecated_OuterscopeLocals;
            case CommonGeneratedNameKind.ReusableHoistedLocalField: return GeneratedNameKind.ReusableHoistedLocalField;
            case CommonGeneratedNameKind.DisplayClassLocalOrField: return GeneratedNameKind.DisplayClassLocalOrField;
            case CommonGeneratedNameKind.Deprecated_IteratorInstance: return GeneratedNameKind.Deprecated_IteratorInstance;
            case CommonGeneratedNameKind.LambdaMethod: return GeneratedNameKind.LambdaMethod;
            case CommonGeneratedNameKind.StateMachineType: return GeneratedNameKind.StateMachineType;
            case CommonGeneratedNameKind.FixedBufferField: return GeneratedNameKind.FixedBufferField;
            case CommonGeneratedNameKind.FileType: return GeneratedNameKind.FileType;
            case CommonGeneratedNameKind.LocalFunction: return GeneratedNameKind.LocalFunction;
            case CommonGeneratedNameKind.Deprecated_InitializerLocal: return GeneratedNameKind.Deprecated_InitializerLocal;
            case CommonGeneratedNameKind.AnonymousTypeField: return GeneratedNameKind.AnonymousTypeField;
            case CommonGeneratedNameKind.AnonymousTypeTypeParameter: return GeneratedNameKind.AnonymousTypeTypeParameter;
            case CommonGeneratedNameKind.AutoPropertyBackingField: return GeneratedNameKind.AutoPropertyBackingField;
            case CommonGeneratedNameKind.IteratorCurrentThreadIdField: return GeneratedNameKind.IteratorCurrentThreadIdField;
            case CommonGeneratedNameKind.IteratorFinallyMethod: return GeneratedNameKind.IteratorFinallyMethod;
            case CommonGeneratedNameKind.BaseMethodWrapper: return GeneratedNameKind.BaseMethodWrapper;
            case CommonGeneratedNameKind.DynamicCallSiteContainerType: return GeneratedNameKind.DynamicCallSiteContainerType;
            case CommonGeneratedNameKind.DynamicCallSiteField: return GeneratedNameKind.DynamicCallSiteField;
            case CommonGeneratedNameKind.Deprecated_DynamicDelegate: return GeneratedNameKind.Deprecated_DynamicDelegate;
            case CommonGeneratedNameKind.Deprecated_ComrefCallLocal: return GeneratedNameKind.Deprecated_ComrefCallLocal;
            case CommonGeneratedNameKind.AsyncBuilderField: return GeneratedNameKind.AsyncBuilderField;
            case CommonGeneratedNameKind.DelegateCacheContainerType: return GeneratedNameKind.DelegateCacheContainerType;
            case CommonGeneratedNameKind.StateMachineDisposingField: return hiddenKind;
            case CommonGeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField: return GeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField;
            case CommonGeneratedNameKind.DisposeModeField: return GeneratedNameKind.DisposeModeField;
            case CommonGeneratedNameKind.CombinedTokensField: return GeneratedNameKind.CombinedTokensField;
            default:
                Debug.Fail($"Unknown kind: {kind}");
                return hiddenKind;
            }
        }
        public static CommonGeneratedNameKind ToCommonGeneratedNameKind(this GeneratedNameKind kind)
        {
            switch (kind)
            {
            case GeneratedNameKind.None: return CommonGeneratedNameKind.None;
            case GeneratedNameKind.StateMachineStateField: return CommonGeneratedNameKind.StateMachineStateField;
            case GeneratedNameKind.IteratorCurrentBackingField: return CommonGeneratedNameKind.IteratorCurrentBackingField;
            case GeneratedNameKind.StateMachineParameterProxyField: return CommonGeneratedNameKind.StateMachineParameterProxyField;
            case GeneratedNameKind.ThisProxyField: return CommonGeneratedNameKind.ThisProxyField;
            case GeneratedNameKind.HoistedLocalField: return CommonGeneratedNameKind.HoistedLocalField;
            case GeneratedNameKind.Deprecated_OuterscopeLocals: return CommonGeneratedNameKind.Deprecated_OuterscopeLocals;
            case GeneratedNameKind.ReusableHoistedLocalField: return CommonGeneratedNameKind.ReusableHoistedLocalField;
            case GeneratedNameKind.DisplayClassLocalOrField: return CommonGeneratedNameKind.DisplayClassLocalOrField;
            case GeneratedNameKind.LambdaCacheField: return CommonGeneratedNameKind.LambdaCacheField;
            case GeneratedNameKind.Deprecated_IteratorInstance: return CommonGeneratedNameKind.Deprecated_IteratorInstance;
            case GeneratedNameKind.LambdaMethod: return CommonGeneratedNameKind.LambdaMethod;
            case GeneratedNameKind.LambdaDisplayClass: return CommonGeneratedNameKind.LambdaDisplayClass;
            case GeneratedNameKind.StateMachineType: return CommonGeneratedNameKind.StateMachineType;
            case GeneratedNameKind.FixedBufferField: return CommonGeneratedNameKind.FixedBufferField;
            case GeneratedNameKind.FileType: return CommonGeneratedNameKind.FileType;
            case GeneratedNameKind.AnonymousType: return CommonGeneratedNameKind.AnonymousType;
            case GeneratedNameKind.LocalFunction: return CommonGeneratedNameKind.LocalFunction;
            case GeneratedNameKind.TransparentIdentifier: return CommonGeneratedNameKind.TransparentIdentifier;
            case GeneratedNameKind.AnonymousTypeField: return CommonGeneratedNameKind.AnonymousTypeField;
            case GeneratedNameKind.AnonymousTypeTypeParameter: return CommonGeneratedNameKind.AnonymousTypeTypeParameter;
            case GeneratedNameKind.AutoPropertyBackingField: return CommonGeneratedNameKind.AutoPropertyBackingField;
            case GeneratedNameKind.IteratorCurrentThreadIdField: return CommonGeneratedNameKind.IteratorCurrentThreadIdField;
            case GeneratedNameKind.IteratorFinallyMethod: return CommonGeneratedNameKind.IteratorFinallyMethod;
            case GeneratedNameKind.BaseMethodWrapper: return CommonGeneratedNameKind.BaseMethodWrapper;
            case GeneratedNameKind.DynamicCallSiteContainerType: return CommonGeneratedNameKind.DynamicCallSiteContainerType;
            case GeneratedNameKind.DynamicCallSiteField: return CommonGeneratedNameKind.DynamicCallSiteField;
            case GeneratedNameKind.Deprecated_DynamicDelegate: return CommonGeneratedNameKind.Deprecated_DynamicDelegate;
            case GeneratedNameKind.Deprecated_ComrefCallLocal: return CommonGeneratedNameKind.Deprecated_ComrefCallLocal;
            case GeneratedNameKind.HoistedSynthesizedLocalField: return CommonGeneratedNameKind.HoistedSynthesizedLocalField;
            case GeneratedNameKind.AsyncBuilderField: return CommonGeneratedNameKind.AsyncBuilderField;
            case GeneratedNameKind.DelegateCacheContainerType: return CommonGeneratedNameKind.DelegateCacheContainerType;
            case GeneratedNameKind.AwaiterField: return CommonGeneratedNameKind.StateMachineAwaiterField;
            case GeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField: return CommonGeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField;
            case GeneratedNameKind.DisposeModeField: return CommonGeneratedNameKind.DisposeModeField;
            case GeneratedNameKind.CombinedTokensField: return CommonGeneratedNameKind.CombinedTokensField;
            default:
                Debug.Fail($"Unknown kind: {kind}");
                return CommonGeneratedNameKind.None;
            }
        }
    }
#pragma warning restore 0612 // Type or member is obsolete
}
