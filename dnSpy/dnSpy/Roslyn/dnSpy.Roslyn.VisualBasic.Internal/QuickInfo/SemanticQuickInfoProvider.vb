' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports dnSpy.Roslyn.Internal
Imports dnSpy.Roslyn.Internal.QuickInfo
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Global.Microsoft.CodeAnalysis.Editor.VisualBasic.QuickInfo
	<ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Semantic, LanguageNames.VisualBasic)>
	Friend Class SemanticQuickInfoProvider
		Inherits AbstractSemanticQuickInfoProvider

		<ImportingConstructor>
		Public Sub New()
		End Sub

		Protected Overrides Async Function BuildContentAsync(context As QuickInfoContext, token As SyntaxToken) As Task(Of QuickInfoContent)
			Dim semanticModel = Await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(False)
			Dim services = context.Document.Project.Solution.Workspace.Services
			Dim info = Await BuildQuickInfoAsync(services, semanticModel, token, context.Options, context.CancellationToken).ConfigureAwait(False)
			If info IsNot Nothing Then
				Return info
			End If

			Return Await MyBase.BuildContentAsync(context, token).ConfigureAwait(False)
		End Function

		Private Overloads Async Function BuildQuickInfoAsync(services As HostWorkspaceServices,
		                                                     semanticModel As SemanticModel,
		                                                     token As SyntaxToken,
		                                                     options As SymbolDescriptionOptions,
		                                                     cancellationToken As CancellationToken) As Task(Of QuickInfoContent)

			Dim parent = token.Parent

			Dim predefinedCastExpression = TryCast(parent, PredefinedCastExpressionSyntax)
			If predefinedCastExpression IsNot Nothing AndAlso token = predefinedCastExpression.Keyword Then
				Dim documentation = New PredefinedCastExpressionDocumentation(predefinedCastExpression.Keyword.Kind, semanticModel.Compilation)
				Return BuildContentForIntrinsicOperator(semanticModel, token, parent, documentation, Glyph.MethodPublic, cancellationToken)
			End If

			Select Case token.Kind
				Case SyntaxKind.AddHandlerKeyword
					If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New AddHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken)
					End If

				Case SyntaxKind.DimKeyword
					If TypeOf parent Is FieldDeclarationSyntax Then
						Return Await BuildContentAsync(services, semanticModel, token, DirectCast(parent, FieldDeclarationSyntax).Declarators, options, cancellationToken).ConfigureAwait(False)
					ElseIf TypeOf parent Is LocalDeclarationStatementSyntax Then
						Return Await BuildContentAsync(services, semanticModel, token, DirectCast(parent, LocalDeclarationStatementSyntax).Declarators, options, cancellationToken).ConfigureAwait(False)
					End If

				Case SyntaxKind.CTypeKeyword
					If TypeOf parent Is CTypeExpressionSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New CTypeCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.DirectCastKeyword
					If TypeOf parent Is DirectCastExpressionSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New DirectCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.GetTypeKeyword
					If TypeOf parent Is GetTypeExpressionSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New GetTypeExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.GetXmlNamespaceKeyword
					If TypeOf parent Is GetXmlNamespaceExpressionSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New GetXmlNamespaceExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.IfKeyword
					If parent.IsKind(SyntaxKind.BinaryConditionalExpression) Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New BinaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					ElseIf parent.IsKind(SyntaxKind.TernaryConditionalExpression) Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New TernaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.RemoveHandlerKeyword
					If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New RemoveHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken)
					End If

				Case SyntaxKind.TryCastKeyword
					If TypeOf parent Is TryCastExpressionSyntax Then
						Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New TryCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken)
					End If

				Case SyntaxKind.IdentifierToken
					If SyntaxFacts.GetContextualKeywordKind(token.ToString()) = SyntaxKind.MidKeyword Then
						If parent.IsKind(SyntaxKind.MidExpression) Then
							Return BuildContentForIntrinsicOperator(semanticModel, token, parent, New MidAssignmentDocumentation(), Glyph.MethodPublic, cancellationToken)
						End If
					End If
			End Select

			Return Nothing
		End Function

		''' <summary>
		''' If the token is a 'Sub' or 'Function' in a lambda, returns the syntax for the whole lambda
		''' </summary>
		Protected Overrides Function GetBindableNodeForTokenIndicatingLambda(token As SyntaxToken, <Out> ByRef found As SyntaxNode) As Boolean
			If token.IsKind(SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword) AndAlso token.Parent.IsKind(SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader) Then
				found = token.Parent.Parent
				Return True
			End If

			found = Nothing
			Return False
		End Function

		Protected Overrides Function GetBindableNodeForTokenIndicatingPossibleIndexerAccess(token As SyntaxToken, ByRef found As SyntaxNode) As Boolean
			If token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken) AndAlso
			   token.Parent?.Parent.IsKind(SyntaxKind.InvocationExpression) = True Then
				found = token.Parent.Parent
				Return True
			End If

			found = Nothing
			Return False
		End Function

		Protected Overrides Function GetBindableNodeForTokenIndicatingMemberAccess(token As SyntaxToken, ByRef found As SyntaxToken) As Boolean
			If token.IsKind(SyntaxKind.DotToken) AndAlso
			   token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then

				found = DirectCast(token.Parent, MemberAccessExpressionSyntax).Name.Identifier
				Return True
			End If

			found = Nothing
			Return False
		End Function

		Private Overloads Async Function BuildContentAsync(services As HostWorkspaceServices,
		                                                   semanticModel As SemanticModel,
		                                                   token As SyntaxToken,
		                                                   declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax),
		                                                   options As SymbolDescriptionOptions,
		                                                   cancellationToken As CancellationToken) As Task(Of QuickInfoContent)

			If declarators.Count = 0 Then
				Return Nothing
			End If

			Dim types = dnSpy.Roslyn.Utilities2.EnumerableExtensions2.WhereNotNull(
				declarators.SelectMany(Function(d) d.Names).Select(
					Function(n) As ISymbol
						Dim symbol = semanticModel.GetDeclaredSymbol(n, cancellationToken)
						If symbol Is Nothing Then
							Return Nothing
						End If

						If TypeOf symbol Is ILocalSymbol Then
							Return DirectCast(symbol, ILocalSymbol).Type
						ElseIf TypeOf symbol Is IFieldSymbol Then
							Return DirectCast(symbol, IFieldSymbol).Type
						Else
							Return Nothing
						End If
					End Function)).Distinct(SymbolEqualityComparer.Default).ToImmutableArray()

			If types.Length = 0 Then
				Return Nothing
			End If

			'If types.Count > 1 Then
			'	Dim contentBuilder = New List(Of TaggedText)
			'	contentBuilder.AddText(VBEditorResources.Multiple_Types)
			'	Return Me.CreateClassifiableDeferredContent(contentBuilder)
			'End If

			Return Await CreateContentAsync(services, semanticModel, token, New TokenInformation(types), supportedPlatforms := Nothing, options, cancellationToken := cancellationToken).ConfigureAwait(False)
		End Function

		Private Function BuildContentForIntrinsicOperator(semanticModel As SemanticModel,
		                                                  token As SyntaxToken,
		                                                  expression As SyntaxNode,
		                                                  documentation As AbstractIntrinsicOperatorDocumentation,
		                                                  glyph As Glyph,
		                                                  cancellationToken As CancellationToken) As QuickInfoContent
			Dim builder = New List(Of SymbolDisplayPart)

			builder.AddRange(documentation.PrefixParts)

			Dim position = expression.SpanStart

			For i = 0 To documentation.ParameterCount - 1
				If i <> 0 Then
					builder.AddPunctuation(",")
					builder.AddSpace()
				End If

				Dim typeNameToBind = documentation.TryGetTypeNameParameter(expression, i)

				If typeNameToBind IsNot Nothing Then
					' We'll try to bind the type name
					Dim typeInfo = semanticModel.GetTypeInfo(typeNameToBind, cancellationToken)

					If typeInfo.Type IsNot Nothing Then
						builder.AddRange(typeInfo.Type.ToMinimalDisplayParts(semanticModel, position))
						Continue For
					End If
				End If

				builder.AddRange(documentation.GetParameterDisplayParts(i))
			Next

			builder.AddRange(documentation.GetSuffix(semanticModel, position, expression, cancellationToken))

			Return CreateQuickInfoDisplayDeferredContent(
				glyph,
				builder.ToTaggedText(),
				CreateDocumentationCommentDeferredContent(documentation.DocumentationText),
				Array.Empty(Of TaggedText),
				Array.Empty(Of TaggedText),
				Array.Empty(Of TaggedText),
				Array.Empty(Of TaggedText))
		End Function
	End Class
End Namespace
