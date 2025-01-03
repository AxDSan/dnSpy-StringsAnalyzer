' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    ''' <summary>
    ''' A display class field representing a local, exposed
    ''' as a local on the original method.
    ''' </summary>
    Friend NotInheritable Class EEDisplayClassFieldLocalSymbol
        Inherits EELocalSymbolBase

        Private ReadOnly _variable As DisplayClassVariable

        Public Sub New(variable As DisplayClassVariable)
            MyBase.New(variable.ContainingSymbol, variable.Type)

            Me._variable = variable
        End Sub

        Friend Overrides ReadOnly Property LocalAndMethodKind As LocalAndMethodKind
            Get
                Select Case _variable.Kind
                    Case DisplayClassVariableKind.Local
                        Return LocalAndMethodKind.Local
                    Case DisplayClassVariableKind.Parameter
                        Return LocalAndMethodKind.Parameter
                    Case DisplayClassVariableKind.Me
                        Return LocalAndMethodKind.This
                    Case Else
                        Throw New InvalidOperationException()
                End Select
            End Get
        End Property

        Friend Overrides ReadOnly Property Index As Integer
            Get
                Select Case _variable.Kind
                    Case DisplayClassVariableKind.Local
                        Return -1
                    Case DisplayClassVariableKind.Parameter
                        Return -1
                    Case DisplayClassVariableKind.Me
                        Return 0
                    Case Else
                        Throw New InvalidOperationException()
                End Select
            End Get
        End Property

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return LocalDeclarationKind.Variable
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase
            Return New EEDisplayClassFieldLocalSymbol(Me._variable.ToOtherMethod(method, typeMap))
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Debug.Assert(Me._variable.Kind <> DisplayClassVariableKind.Me)
                Return Me._variable.Name
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return NoLocations
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property
    End Class
End Namespace
