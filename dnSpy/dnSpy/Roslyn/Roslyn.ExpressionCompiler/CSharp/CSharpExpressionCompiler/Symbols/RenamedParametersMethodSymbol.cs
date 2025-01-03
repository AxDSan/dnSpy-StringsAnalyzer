// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// Copyright (C) de4dot@gmail.com

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    sealed class RenamedParametersMethodSymbol : WrappedMethodSymbol
    {
        private readonly MethodSymbol _originalMethod;
        private readonly ParameterSymbol? _thisParameter;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        public RenamedParametersMethodSymbol(MethodSymbol originalMethod, MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            _originalMethod = originalMethod;
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();

            var thisParameter = originalMethod.ThisParameter;
            var hasThisParameter = (object)thisParameter != null;
            if (hasThisParameter)
            {
                _thisParameter = MakeParameterSymbol(-1, GeneratedNames.ThisProxyFieldName(), thisParameter!);
                Debug.Assert(TypeSymbol.Equals(_thisParameter.Type, originalMethod.ContainingType, TypeCompareKind.ConsiderEverything));
            }

            foreach (var p in originalMethod.Parameters)
            {
                var ordinal = p.Ordinal;
                Debug.Assert(ordinal == builder.Count);
                var name = methodDebugInfo.GetParameterName(ordinal + (hasThisParameter ? 1 : 0), p);
                var parameter = MakeParameterSymbol(ordinal, name, p);
                builder.Add(parameter);
            }

            _parameters = builder.ToImmutableAndFree();
        }

        private ParameterSymbol MakeParameterSymbol(int ordinal, string name, ParameterSymbol sourceParameter) =>
            SynthesizedParameterSymbol.Create(this, sourceParameter.TypeWithAnnotations, ordinal, sourceParameter.RefKind, name, sourceParameter.EffectiveScope, sourceParameter.RefCustomModifiers);

        public override MethodSymbol UnderlyingMethod => _originalMethod;

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        internal override CSharpCompilation DeclaringCompilation => _originalMethod.DeclaringCompilation;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _originalMethod.ReturnTypeWithAnnotations;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => _originalMethod.TypeArgumentsWithAnnotations;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _originalMethod.TypeParameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => _originalMethod.ExplicitInterfaceImplementations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _originalMethod.RefCustomModifiers;

        public override Symbol? AssociatedSymbol => _originalMethod.AssociatedSymbol;

        public override Symbol ContainingSymbol => _originalMethod.ContainingSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => _originalMethod.CalculateLocalSyntaxOffset(localPosition, localTree);

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => _originalMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        internal override bool IsNullableAnalysisEnabled() => _originalMethod.IsNullableAnalysisEnabled();

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = _thisParameter;
            return true;
        }
    }
}
