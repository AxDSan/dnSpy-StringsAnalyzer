' Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
' Copyright (C) de4dot@gmail.com

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Class RenamedParametersMethodSymbol
        Inherits MethodSymbol

        Private ReadOnly _originalMethod As MethodSymbol
        Private ReadOnly _meParameter As ParameterSymbol
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        Public Sub New(originalMethod As MethodSymbol, methodDebugInfo As MethodDebugInfo(Of TypeSymbol, LocalSymbol))
            _originalMethod = originalMethod
            Dim parameters = originalMethod.Parameters
            Dim builder = ArrayBuilder(Of ParameterSymbol).GetInstance()

            Dim meParameter = originalMethod.MeParameter
            Dim substitutedSourceHasMeParameter = meParameter IsNot Nothing
            If substitutedSourceHasMeParameter Then
                _meParameter = MakeParameterSymbol(-1, GeneratedNames.MakeStateMachineCapturedMeName(), meParameter)
                Debug.Assert(TypeSymbol.Equals(_meParameter.Type, originalMethod.ContainingType, TypeCompareKind.ConsiderEverything))
            End If

            For Each p In originalMethod.Parameters
                Dim ordinal = p.Ordinal
                Debug.Assert(ordinal = builder.Count)
                Dim name = methodDebugInfo.GetParameterName(ordinal + If(substitutedSourceHasMeParameter, 1, 0), p)
                Dim parameter = MakeParameterSymbol(ordinal, p.Name, p)
                builder.Add(parameter)
            Next

            _parameters = builder.ToImmutableAndFree()
        End Sub

        Private Function MakeParameterSymbol(ordinal As Integer, name As String, sourceParameter As ParameterSymbol) As ParameterSymbol
            Return SynthesizedParameterSymbol.Create(
                Me,
                sourceParameter.Type,
                ordinal,
                sourceParameter.IsByRef,
                name,
                sourceParameter.CustomModifiers,
                sourceParameter.RefCustomModifiers)
        End Function

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return _originalMethod.MethodKind
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return _originalMethod.IsVararg
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _originalMethod.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _originalMethod.TypeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return _originalMethod.TypeArguments
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _originalMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return _originalMethod.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return _originalMethod.IsIterator
            End Get
        End Property

        Public Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return _originalMethod.IsInitOnly
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _originalMethod.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _originalMethod.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalMethod.ReturnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalMethod.RefCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _originalMethod.AssociatedSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return _originalMethod.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return _originalMethod.IsExternalMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return _originalMethod.IsExtensionMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return _originalMethod.IsOverloads
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _originalMethod.ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _originalMethod.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _originalMethod.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _originalMethod.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _originalMethod.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return _originalMethod.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _originalMethod.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return _originalMethod.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return _originalMethod.IsNotOverridable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return _originalMethod.IsMethodKindBasedOnSyntax
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return _originalMethod.Syntax
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _originalMethod.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return _originalMethod.ReturnTypeMarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As MethodImplAttributes
            Get
                Return _originalMethod.ImplementationAttributes
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return _originalMethod.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
            Get
                Return _originalMethod.CallingConvention
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return _originalMethod.GenerateDebugInfoImpl
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _originalMethod.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _originalMethod.Name
            End Get
        End Property

        Friend Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            meParameter = _meParameter
            Return True
        End Function

        Friend Overrides ReadOnly Property PreserveOriginalLocals As Boolean
            Get
                Return _originalMethod.PreserveOriginalLocals
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return _originalMethod.GetDllImportData()
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return _originalMethod.GetAppliedConditionalSymbols()
        End Function

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Return _originalMethod.GetSecurityInformation()
        End Function

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Return _originalMethod.CalculateLocalSyntaxOffset(localPosition, localTree)
        End Function
    End Class
End Namespace
