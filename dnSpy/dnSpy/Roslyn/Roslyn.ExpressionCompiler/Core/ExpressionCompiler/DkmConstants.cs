using System;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    internal enum DkmClrAliasKind
    {
        Exception,
        StowedException,
        ReturnValue,
        Variable,
        ObjectId,
    }
}
namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    [Flags]
    internal enum DkmEvaluationFlags
    {
        None = 0,
        TreatAsExpression = 1,
        TreatFunctionAsAddress = 2,
        NoSideEffects = 4,
        NoFuncEval = 8,
        DesignTime = 16,
        AllowImplicitVariables = 32,
        ForceEvaluationNow = 64,
        ShowValueRaw = 128,
        ForceRealFuncEval = 256,
        HideNonPublicMembers = 512,
        NoToString = 1024,
        NoFormatting = 2048,
        NoRawView = 4096,
        NoQuotes = 8192,
        DynamicView = 16384,
        ResultsOnly = 32768,
        NoExpansion = 65536,
        EnableExtendedSideEffects = 131072,
    }
    public enum DkmEvaluationResultCategory
    {
        Other,
        Data,
        Method,
        Event,
        Property,
        Class,
        Interface,
        BaseClass,
        InnerClass,
        MostDerivedClass,
        Field,
        Local,
        Parameter,
    }
    public enum DkmEvaluationResultAccessType
    {
        None,
        Public,
        Private,
        Protected,
        Final,
        Internal,
    }
    public enum DkmEvaluationResultStorageType
    {
        None,
        Global,
        Static,
        Register,
    }
    [Flags]
    public enum DkmEvaluationResultTypeModifierFlags
    {
        None = 0,
        Virtual = 1,
        Constant = 2,
        Synchronized = 4,
        Volatile = 8,
    }
}
namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    [Flags]
    internal enum DkmClrCompilationResultFlags
    {
        None = 0,
        PotentialSideEffect = 1,
        ReadOnlyResult = 2,
        BoolResult = 4,
    }
}
