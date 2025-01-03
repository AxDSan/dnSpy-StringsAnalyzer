' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ExceptionLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _getExceptionMethodName As String

        Friend Sub New(method As MethodSymbol, name As String, displayName As String, type As TypeSymbol, getExceptionMethodName As String, localKind As LocalAndMethodKind)
            MyBase.New(method, name, displayName, type)
            _getExceptionMethodName = getExceptionMethodName
            LocalAndMethodKind = localKind
        End Sub

        Friend Overrides ReadOnly Property LocalAndMethodKind As LocalAndMethodKind

        Friend Overrides ReadOnly Property Index As Integer
            Get
                Return -1
            End Get
        End Property

        Friend Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As SyntaxNode,
            isLValue As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            Dim method = GetIntrinsicMethod(compilation, _getExceptionMethodName)
            Dim [call] As New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray(Of BoundExpression).Empty,
                constantValueOpt:=Nothing,
                suppressObjectClone:=False, ' Doesn't matter, since no arguments.
                type:=method.ReturnType)
            Return ConvertToLocalType(compilation, [call], Type, diagnostics)
        End Function

    End Class

End Namespace
