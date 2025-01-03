// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB {
	/// <summary>
	/// Description of OutputVisitor.
	/// </summary>
	public class OutputVisitor : IAstVisitor<object, object>
	{
		readonly IOutputFormatter formatter;
		readonly VBFormattingOptions policy;
		readonly NumberFormatter numberFormatter;
		
		readonly Stack<AstNode> containerStack = new Stack<AstNode>();
		readonly Stack<AstNode> positionStack = new Stack<AstNode>();
		struct MethodRefs {
			public object MethodReference; 
			public static MethodRefs Create() => new MethodRefs {
				MethodReference = new object(),
			};
		}
		MethodRefs currentMethodRefs;
		object currentTryReference;
		object currentDoReference;
		object currentForReference;
		object currentWhileReference;
		object currentSelectReference;
		int lastEndBlockOffset;
		int lastDeclarationOffset;

		void SaveDeclarationOffset() => lastDeclarationOffset = formatter.NextPosition;

		/// <summary>
		/// Used to insert the minimal amount of spaces so that the lexer recognizes the tokens that were written.
		/// </summary>
		LastWritten lastWritten;
		
		enum LastWritten
		{
			Whitespace,
			Other,
			KeywordOrIdentifier
		}
		
		public OutputVisitor(TextWriter textWriter, VBFormattingOptions formattingPolicy)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");
			if (formattingPolicy == null)
				throw new ArgumentNullException("formattingPolicy");
			this.formatter = new TextWriterOutputFormatter(textWriter);
			this.policy = formattingPolicy;
			numberFormatter = formattingPolicy.NumberFormatter;
		}
		
		public OutputVisitor(IOutputFormatter formatter, VBFormattingOptions formattingPolicy)
		{
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			if (formattingPolicy == null)
				throw new ArgumentNullException("formattingPolicy");
			this.formatter = formatter;
			this.policy = formattingPolicy;
			numberFormatter = formattingPolicy.NumberFormatter;
		}
		
		struct BraceHelper {
			readonly OutputVisitor owner;
			readonly CodeBracesRangeFlags flags;
			int leftStart, leftEnd;

			BraceHelper(OutputVisitor owner, CodeBracesRangeFlags flags) {
				this.owner = owner;
				this.leftStart = owner.formatter.NextPosition;
				this.leftEnd = 0;
				this.flags = flags;
			}

			public static BraceHelper LeftParen(OutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken("(", AstNode.Roles.LPar, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.formatter.NextPosition;
				return bh;
			}

			public void RightParen() {
				int rightStart = owner.formatter.NextPosition;
				owner.WriteToken(")", AstNode.Roles.LPar, BoxedTextColor.Punctuation);
				int rightEnd = owner.formatter.NextPosition;
				owner.formatter.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftChevron(OutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken("<", AstNode.Roles.LChevron, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.formatter.NextPosition;
				return bh;
			}

			public void RightChevron() {
				int rightStart = owner.formatter.NextPosition;
				owner.WriteToken(">", AstNode.Roles.RChevron, BoxedTextColor.Punctuation);
				int rightEnd = owner.formatter.NextPosition;
				owner.formatter.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftBrace(OutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken("{", AstNode.Roles.LBrace, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.formatter.NextPosition;
				return bh;
			}

			public void RightBrace() {
				int rightStart = owner.formatter.NextPosition;
				owner.WriteToken("}", AstNode.Roles.RBrace, BoxedTextColor.Punctuation);
				int rightEnd = owner.formatter.NextPosition;
				owner.formatter.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftBracket(OutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken("[", AstNode.Roles.LBracket, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.formatter.NextPosition;
				return bh;
			}

			public void RightBracket() {
				int rightStart = owner.formatter.NextPosition;
				owner.WriteToken("]", AstNode.Roles.RBracket, BoxedTextColor.Punctuation);
				int rightEnd = owner.formatter.NextPosition;
				owner.formatter.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}
		}

		static CodeBracesRangeFlags GetTypeBlockKind(AstNode node) {
			var td = node.Annotation<TypeDef>();
			if (td != null) {
				if (td.IsInterface)
					return CodeBracesRangeFlags.BlockKind_Interface;
				if (td.IsValueType)
					return CodeBracesRangeFlags.BlockKind_ValueType;
				if (IsModule(td))
					return CodeBracesRangeFlags.BlockKind_Module;
			}
			return CodeBracesRangeFlags.BlockKind_Type;
		}

		static bool IsModule(TypeDef type) =>
			type != null && type.DeclaringType == null && type.IsSealed && type.IsDefined(stringMicrosoftVisualBasicCompilerServices, stringStandardModuleAttribute);
		static readonly UTF8String stringMicrosoftVisualBasicCompilerServices = new UTF8String("Microsoft.VisualBasic.CompilerServices");
		static readonly UTF8String stringStandardModuleAttribute = new UTF8String("StandardModuleAttribute");

		bool MaybeNewLinesAfterUsings(AstNode node)
		{
			var nextSibling = node.NextSibling;
			if (node is ImportsStatement && !(nextSibling is ImportsStatement)) {
				const int MinimumBlankLinesAfterUsings = 1;
				for (int i = 0; i < MinimumBlankLinesAfterUsings; i++)
					NewLine();
				return true;
			}

			return false;
		}
		
		public object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			// don't do node tracking as we visit all children directly
			bool addedLastLineSep = false;
			int totalCount = 0;
			int lastLineSepOffset = 0;
			foreach (AstNode node in compilationUnit.Children) {
				totalCount++;
				node.AcceptVisitor(this, data);
				var addLineSep = MaybeNewLinesAfterUsings(node) || node is NamespaceDeclaration;
				if (addLineSep) {
					lastLineSepOffset = Math.Max(lastEndBlockOffset, lastDeclarationOffset);
					formatter.AddLineSeparator(lastLineSepOffset);
					addedLastLineSep = true;
				}
				else
					addedLastLineSep = false;
			}
			if (!addedLastLineSep && totalCount > 0) {
				int newOffs = Math.Max(lastEndBlockOffset, lastDeclarationOffset);
				if (newOffs != lastLineSepOffset && newOffs != 0)
					formatter.AddLineSeparator(newOffs);
			}
			return null;
		}
		
		public object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			// prepare new block
			NewLine();
			Indent();

			StartNode(blockStatement);
			foreach (var stmt in blockStatement) {
				stmt.AcceptVisitor(this, data);
				NewLine();
			}
			// finish block
			Unindent();
			return EndNode(blockStatement);
		}
		
		public object VisitPatternPlaceholder(AstNode placeholder, Pattern pattern, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			StartNode(typeParameterDeclaration);
			
			switch (typeParameterDeclaration.Variance) {
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Invariant:
					break;
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Covariant:
					WriteKeyword("Out");
					break;
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Contravariant:
					WriteKeyword("In");
					break;
				default:
					throw new Exception("Invalid value for VarianceModifier");
			}
			
			WriteIdentifier(typeParameterDeclaration.NameToken);
			if (typeParameterDeclaration.Constraints.Any()) {
				WriteKeyword("As");
				if (typeParameterDeclaration.Constraints.Count > 1) {
					var braceHelper = BraceHelper.LeftBrace(this, CodeBracesRangeFlags.CurlyBraces);
					WriteCommaSeparatedList(typeParameterDeclaration.Constraints);
					braceHelper.RightBrace();
				}
				else
					WriteCommaSeparatedList(typeParameterDeclaration.Constraints);
			}

			SaveDeclarationOffset();
			return EndNode(typeParameterDeclaration);
		}
		
		public object VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			StartNode(parameterDeclaration);
			WriteAttributes(parameterDeclaration.Attributes);
			WriteModifiers(parameterDeclaration.ModifierTokens);
			WriteIdentifier(parameterDeclaration.Name);
			if (!parameterDeclaration.Type.IsNull) {
				WriteKeyword("As");
				parameterDeclaration.Type.AcceptVisitor(this, data);
			}
			if (!parameterDeclaration.OptionalValue.IsNull) {
				Space();
				WriteToken("=", ParameterDeclaration.Roles.Assign, BoxedTextColor.Operator);
				Space();
				parameterDeclaration.OptionalValue.AcceptVisitor(this, data);
			}
			SaveDeclarationOffset();
			return EndNode(parameterDeclaration);
		}
		
		public object VisitVBTokenNode(VBTokenNode vBTokenNode, object data)
		{
			var mod = vBTokenNode as VBModifierToken;
			if (mod != null) {
				StartNode(vBTokenNode);
				WriteKeyword(VBModifierToken.GetModifierName(mod.Modifier));
				return EndNode(vBTokenNode);
			} else {
				throw new NotSupportedException("Should never visit individual tokens");
			}
		}
		
		public object VisitAliasImportsClause(AliasImportsClause aliasImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitAttribute(ICSharpCode.NRefactory.VB.Ast.Attribute attribute, object data)
		{
			StartNode(attribute);
			
			if (attribute.Target != AttributeTarget.None) {
				switch (attribute.Target) {
					case AttributeTarget.None:
						break;
					case AttributeTarget.Assembly:
						WriteKeyword("Assembly");
						break;
					case AttributeTarget.Module:
						WriteKeyword("Module");
						break;
					default:
						throw new Exception("Invalid value for AttributeTarget");
				}
				WriteToken(":", Ast.Attribute.Roles.Colon, BoxedTextColor.Punctuation);
				Space();
			}
			attribute.Type.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(attribute.Arguments, false, CodeBracesRangeFlags.Parentheses);
			
			return EndNode(attribute);
		}
		
		public object VisitAttributeBlock(AttributeBlock attributeBlock, object data)
		{
			StartNode(attributeBlock);
			
			var braceHelper = BraceHelper.LeftChevron(this, CodeBracesRangeFlags.AngleBrackets);
			WriteCommaSeparatedList(attributeBlock.Attributes);
			braceHelper.RightChevron();
			if (attributeBlock.Parent is ParameterDeclaration)
				Space();
			else if ((attributeBlock.Parent is DelegateDeclaration) && ((DelegateDeclaration)attributeBlock.Parent).ReturnTypeAttributes.Contains(attributeBlock))
				Space();
			else if ((attributeBlock.Parent is MethodDeclaration) && ((MethodDeclaration)attributeBlock.Parent).ReturnTypeAttributes.Contains(attributeBlock))
				Space();
			else if ((attributeBlock.Parent is PropertyDeclaration) && ((PropertyDeclaration)attributeBlock.Parent).ReturnTypeAttributes.Contains(attributeBlock))
				Space();
			else if ((attributeBlock.Parent is OperatorDeclaration) && ((OperatorDeclaration)attributeBlock.Parent).ReturnTypeAttributes.Contains(attributeBlock))
				Space();
			else if ((attributeBlock.Parent is ExternalMethodDeclaration) && ((ExternalMethodDeclaration)attributeBlock.Parent).ReturnTypeAttributes.Contains(attributeBlock))
				Space();
			else
				NewLine();
			
			return EndNode(attributeBlock);
		}
		
		public object VisitImportsStatement(ImportsStatement importsStatement, object data)
		{
			StartNode(importsStatement);
			
			WriteKeyword("Imports", AstNode.Roles.Keyword);
			Space();
			WriteCommaSeparatedList(importsStatement.ImportsClauses);
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(importsStatement);
		}
		
		public object VisitMemberImportsClause(MemberImportsClause memberImportsClause, object data)
		{
			StartNode(memberImportsClause);
			memberImportsClause.Member.AcceptVisitor(this, data);
			return EndNode(memberImportsClause);
		}
		
		public object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			StartNode(namespaceDeclaration);
			var reference = new object();
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteKeyword("Namespace");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			bool isFirst = true;
			foreach (Identifier node in namespaceDeclaration.Identifiers) {
				if (isFirst) {
					isFirst = false;
				} else {
					WriteToken(".", NamespaceDeclaration.Roles.Dot, BoxedTextColor.Operator);
				}
				node.AcceptVisitor(this, null);
				MaybeNewLinesAfterUsings(node);
			}
			NewLine();
			WriteMembers(namespaceDeclaration.Members);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Namespace);
			WriteKeyword("End");
			WriteKeyword("Namespace");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			SaveDeclarationOffset();
			NewLine();
			return EndNode(namespaceDeclaration);
		}
		
		public object VisitOptionStatement(OptionStatement optionStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			StartNode(typeDeclaration);
			WriteAttributes(typeDeclaration.Attributes);
			var reference = new object();
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteModifiers(typeDeclaration.ModifierTokens);
			WriteClassTypeKeyword(typeDeclaration);
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			WriteIdentifier(typeDeclaration.Name);
			WriteTypeParameters(typeDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			NewLine();
			
			if (!typeDeclaration.InheritsType.IsNull) {
				Indent();
				WriteKeyword("Inherits");
				typeDeclaration.InheritsType.AcceptVisitor(this, data);
				Unindent();
				NewLine();
			}
			if (typeDeclaration.ImplementsTypes.Any()) {
				Indent();
				WriteImplementsClause(typeDeclaration.ImplementsTypes, typeDeclaration.ClassType == ClassType.Interface);
				Unindent();
				NewLine();
			}
			
			if (!typeDeclaration.InheritsType.IsNull || typeDeclaration.ImplementsTypes.Any())
				NewLine();
			
			WriteMembers(typeDeclaration.Members);

			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, GetTypeBlockKind(typeDeclaration));
			WriteKeyword("End");
			WriteClassTypeKeyword(typeDeclaration);
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			SaveDeclarationOffset();
			NewLine();
			return EndNode(typeDeclaration);
		}

		void WriteClassTypeKeyword(TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.ClassType) {
				case ClassType.Class:
					WriteKeyword("Class");
					break;
				case ClassType.Interface:
					WriteKeyword("Interface");
					break;
				case ClassType.Struct:
					WriteKeyword("Structure");
					break;
				case ClassType.Module:
					WriteKeyword("Module");
					break;
				default:
					throw new Exception("Invalid value for ClassType");
			}
		}
		
		public object VisitXmlNamespaceImportsClause(XmlNamespaceImportsClause xmlNamespaceImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitEnumDeclaration(EnumDeclaration enumDeclaration, object data)
		{
			StartNode(enumDeclaration);
			
			WriteAttributes(enumDeclaration.Attributes);
			var reference = new object();
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteModifiers(enumDeclaration.ModifierTokens);
			WriteKeyword("Enum");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			WriteIdentifier(enumDeclaration.Name);
			if (!enumDeclaration.UnderlyingType.IsNull) {
				Space();
				WriteKeyword("As");
				enumDeclaration.UnderlyingType.AcceptVisitor(this, data);
			}
			NewLine();
			
			Indent();
			foreach (var member in enumDeclaration.Members) {
				member.AcceptVisitor(this, null);
			}
			Unindent();

			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_ValueType);
			WriteKeyword("End");
			WriteKeyword("Enum");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(enumDeclaration);
		}
		
		public object VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			StartNode(enumMemberDeclaration);
			
			WriteAttributes(enumMemberDeclaration.Attributes);
			WriteIdentifier(enumMemberDeclaration.Name);
			
			if (!enumMemberDeclaration.Value.IsNull) {
				Space();
				WriteToken("=", EnumMemberDeclaration.Roles.Assign, BoxedTextColor.Operator);
				Space();
				enumMemberDeclaration.Value.AcceptVisitor(this, data);
			}
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(enumMemberDeclaration);
		}
		
		public object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			StartNode(delegateDeclaration);
			
			WriteAttributes(delegateDeclaration.Attributes);
			WriteModifiers(delegateDeclaration.ModifierTokens);
			WriteKeyword("Delegate");
			if (delegateDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			WriteIdentifier(delegateDeclaration.Name);
			WriteTypeParameters(delegateDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			WriteCommaSeparatedListInParenthesis(delegateDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!delegateDeclaration.IsSub) {
				Space();
				WriteKeyword("As");
				Space();
				WriteAttributes(delegateDeclaration.ReturnTypeAttributes);
				delegateDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(delegateDeclaration);
		}
		
		public object VisitIdentifier(Identifier identifier, object data)
		{
			StartNode(identifier);
			WriteIdentifier(identifier);
			WriteTypeCharacter(identifier.TypeCharacter, VisualBasicMetadataTextColorProvider.Instance.GetColor(identifier.Annotation<object>()));
			return EndNode(identifier);
		}
		
		public object VisitXmlIdentifier(XmlIdentifier xmlIdentifier, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitXmlLiteralString(XmlLiteralString xmlLiteralString, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitSimpleNameExpression(SimpleNameExpression simpleNameExpression, object data)
		{
			DebugExpression(simpleNameExpression);
			StartNode(simpleNameExpression);
			
			simpleNameExpression.Identifier.AcceptVisitor(this, data);
			WriteTypeArguments(simpleNameExpression.TypeArguments, CodeBracesRangeFlags.Parentheses);
			
			return EndNode(simpleNameExpression);
		}
		
		public object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			DebugExpression(primitiveExpression);
			StartNode(primitiveExpression);
			
			if (lastWritten == LastWritten.KeywordOrIdentifier)
				Space();
			WritePrimitiveValue(primitiveExpression.Value);
			
			return EndNode(primitiveExpression);
		}
		
		public object VisitInstanceExpression(InstanceExpression instanceExpression, object data)
		{
			DebugExpression(instanceExpression);
			StartNode(instanceExpression);
			
			switch (instanceExpression.Type) {
				case InstanceExpressionType.Me:
					WriteKeyword("Me");
					break;
				case InstanceExpressionType.MyBase:
					WriteKeyword("MyBase");
					break;
				case InstanceExpressionType.MyClass:
					WriteKeyword("MyClass");
					break;
				default:
					throw new Exception("Invalid value for InstanceExpressionType");
			}
			
			return EndNode(instanceExpression);
		}
		
		public object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			DebugExpression(parenthesizedExpression);
			StartNode(parenthesizedExpression);
			
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			parenthesizedExpression.Expression.AcceptVisitor(this, data);
			braceHelper.RightParen();
			
			return EndNode(parenthesizedExpression);
		}
		
		public object VisitGetTypeExpression(GetTypeExpression getTypeExpression, object data)
		{
			DebugExpression(getTypeExpression);
			StartNode(getTypeExpression);
			
			WriteKeyword("GetType");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			getTypeExpression.Type.AcceptVisitor(this, data);
			braceHelper.RightParen();
			
			return EndNode(getTypeExpression);
		}
		
		public object VisitTypeOfIsExpression(TypeOfIsExpression typeOfIsExpression, object data)
		{
			DebugExpression(typeOfIsExpression);
			StartNode(typeOfIsExpression);
			
			WriteKeyword("TypeOf");
			typeOfIsExpression.TypeOfExpression.AcceptVisitor(this, data);
			WriteKeyword("Is");
			typeOfIsExpression.Type.AcceptVisitor(this, data);
			
			return EndNode(typeOfIsExpression);
		}
		
		public object VisitGetXmlNamespaceExpression(GetXmlNamespaceExpression getXmlNamespaceExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitMemberAccessExpression(MemberAccessExpression memberAccessExpression, object data)
		{
			DebugExpression(memberAccessExpression);
			StartNode(memberAccessExpression);
			
			memberAccessExpression.Target.AcceptVisitor(this, data);
			WriteToken(".", MemberAccessExpression.Roles.Dot, BoxedTextColor.Operator);
			memberAccessExpression.MemberName.AcceptVisitor(this, data);
			WriteTypeArguments(memberAccessExpression.TypeArguments, CodeBracesRangeFlags.Parentheses);
			
			return EndNode(memberAccessExpression);
		}
		
		public object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			DebugExpression(typeReferenceExpression);
			StartNode(typeReferenceExpression);
			
			typeReferenceExpression.Type.AcceptVisitor(this, data);
			
			return EndNode(typeReferenceExpression);
		}
		
		public object VisitEventMemberSpecifier(EventMemberSpecifier eventMemberSpecifier, object data)
		{
			StartNode(eventMemberSpecifier);
			
			eventMemberSpecifier.Target.AcceptVisitor(this, data);
			WriteToken(".", EventMemberSpecifier.Roles.Dot, BoxedTextColor.Operator);
			eventMemberSpecifier.Member.AcceptVisitor(this, data);
			
			return EndNode(eventMemberSpecifier);
		}
		
		public object VisitInterfaceMemberSpecifier(InterfaceMemberSpecifier interfaceMemberSpecifier, object data)
		{
			StartNode(interfaceMemberSpecifier);
			
			interfaceMemberSpecifier.Target.AcceptVisitor(this, data);
			WriteToken(".", EventMemberSpecifier.Roles.Dot, BoxedTextColor.Operator);
			interfaceMemberSpecifier.Member.AcceptVisitor(this, data);
			
			return EndNode(interfaceMemberSpecifier);
		}
		
		#region TypeMembers
		public object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			StartNode(constructorDeclaration);

			WriteAttributes(constructorDeclaration.Attributes);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			int start = formatter.NextPosition;
			int blockStart = start;
			var builder = constructorDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = start;
			WriteModifiers(constructorDeclaration.ModifierTokens);
			if (lastWritten != LastWritten.Whitespace)
				Space();
			DebugStart(constructorDeclaration);
			DebugHidden(constructorDeclaration.Body.HiddenStart);
			WriteKeyword("Sub");
			WriteKeyword("New");
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			DebugEnd(constructorDeclaration, false);
			WriteCommaSeparatedListInParenthesis(constructorDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			WriteBlock(constructorDeclaration.Body);
			
			DebugStart(constructorDeclaration);
			DebugHidden(constructorDeclaration.Body.HiddenEnd);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Constructor);
			WriteKeyword("End");
			WriteKeyword("Sub");
			if (builder != null)
				builder.EndPosition = formatter.NextPosition;
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			SaveDeclarationOffset();
			DebugEnd(constructorDeclaration, false);
			NewLine();
			currentMethodRefs = oldRef;
			
			return EndNode(constructorDeclaration);
		}
		
		public object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			StartNode(methodDeclaration);
			
			WriteAttributes(methodDeclaration.Attributes);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			int start = formatter.NextPosition;
			int blockStart = start;
			var builder = methodDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = start;
			WriteModifiers(methodDeclaration.ModifierTokens);
			DebugStart(methodDeclaration);
			DebugHidden(methodDeclaration.Body.HiddenStart);
			if (methodDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			if (!methodDeclaration.Body.IsNull)
				formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			DebugEnd(methodDeclaration, false);
			methodDeclaration.Name.AcceptVisitor(this, data);
			WriteTypeParameters(methodDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!methodDeclaration.IsSub && !methodDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				Space();
				WriteAttributes(methodDeclaration.ReturnTypeAttributes);
				methodDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			WriteHandlesClause(methodDeclaration.HandlesClause);
			WriteImplementsClause(methodDeclaration.ImplementsClause);
			if (!methodDeclaration.Body.IsNull) {
				WriteBlock(methodDeclaration.Body);
				DebugStart(methodDeclaration);
				DebugHidden(methodDeclaration.Body.HiddenEnd);
				start = formatter.NextPosition;
				lastEndBlockOffset = start;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Method);
				WriteKeyword("End");
				if (methodDeclaration.IsSub)
					WriteKeyword("Sub");
				else
					WriteKeyword("Function");
				if (builder != null)
					builder.EndPosition = formatter.NextPosition;
				formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
				DebugEnd(methodDeclaration, false);
			}
			SaveDeclarationOffset();
			NewLine();
			currentMethodRefs = oldRef;
			
			return EndNode(methodDeclaration);
		}

		public object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			StartNode(fieldDeclaration);
			
			WriteAttributes(fieldDeclaration.Attributes);
			WriteModifiers(fieldDeclaration.ModifierTokens);
			if (lastWritten != LastWritten.Whitespace)
				Space();
			DebugStart(fieldDeclaration);
			WriteCommaSeparatedList(fieldDeclaration.Variables);
			DebugEnd(fieldDeclaration);
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(fieldDeclaration);
		}
		
		public object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			StartNode(propertyDeclaration);
			
			WriteAttributes(propertyDeclaration.Attributes);
			bool needsBody = !propertyDeclaration.Getter.Body.IsNull || !propertyDeclaration.Setter.Body.IsNull;
			var reference = new object();
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteModifiers(propertyDeclaration.ModifierTokens);
			WriteKeyword("Property");
			if (needsBody)
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			WriteIdentifier(propertyDeclaration.Name);
			if (propertyDeclaration.Parameters.Any())
				WriteCommaSeparatedListInParenthesis(propertyDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!propertyDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				Space();
				WriteAttributes(propertyDeclaration.ReturnTypeAttributes);
				propertyDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			WriteImplementsClause(propertyDeclaration.ImplementsClause);
		
			
			if (needsBody) {
				NewLine();
				Indent();
				
				if (!propertyDeclaration.Getter.Body.IsNull) {
					propertyDeclaration.Getter.AcceptVisitor(this, data);
				}
				
				if (!propertyDeclaration.Setter.Body.IsNull) {
					propertyDeclaration.Setter.AcceptVisitor(this, data);
				}
				Unindent();

				start = formatter.NextPosition;
				lastEndBlockOffset = start;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Property);
				WriteKeyword("End");
				WriteKeyword("Property");
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			}
			SaveDeclarationOffset();
			if (propertyDeclaration.Variables.Any())
				WriteCommaSeparatedList(propertyDeclaration.Variables);
			NewLine();
			
			return EndNode(propertyDeclaration);
		}
		#endregion
		
		#region TypeName
		public object VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			StartNode(primitiveType);
			
			WriteKeyword(primitiveType.Keyword);
			
			return EndNode(primitiveType);
		}
		
		public object VisitQualifiedType(QualifiedType qualifiedType, object data)
		{
			StartNode(qualifiedType);
			
			qualifiedType.Target.AcceptVisitor(this, data);
			WriteToken(".", AstNode.Roles.Dot, BoxedTextColor.Operator);
			WriteIdentifier(qualifiedType.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(qualifiedType.NameToken.Annotation<object>() ?? qualifiedType.Annotation<object>()), null, qualifiedType.NameToken.Annotation<NamespaceReference>());
			WriteTypeArguments(qualifiedType.TypeArguments, CodeBracesRangeFlags.Parentheses);
			
			return EndNode(qualifiedType);
		}
		
		public object VisitComposedType(ComposedType composedType, object data)
		{
			StartNode(composedType);
			
			composedType.BaseType.AcceptVisitor(this, data);
			if (composedType.HasNullableSpecifier)
				WriteToken("?", ComposedType.Roles.QuestionMark, BoxedTextColor.Punctuation);
			WriteArraySpecifiers(composedType.ArraySpecifiers);
			
			return EndNode(composedType);
		}
		
		public object VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			StartNode(arraySpecifier);
			
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			for (int i = 0; i < arraySpecifier.Dimensions - 1; i++) {
				WriteToken(",", ArraySpecifier.Roles.Comma, BoxedTextColor.Punctuation);
			}
			braceHelper.RightParen();
			
			return EndNode(arraySpecifier);
		}
		
		public object VisitSimpleType(SimpleType simpleType, object data)
		{
			StartNode(simpleType);

			if (simpleType.Identifier.Length == 0 && CSharp.SimpleType.DummyTypeGenericParam.Equals(simpleType.Annotation<string>(), StringComparison.Ordinal)) {
				// It's the empty string. Don't call WriteIdentifier() since it will write "<<EMPTY_NAME>>"
			}
			else
				WriteIdentifier(simpleType.Identifier, VisualBasicMetadataTextColorProvider.Instance.GetColor(simpleType.IdentifierToken.Annotation<object>() ?? simpleType.Annotation<object>()), null, simpleType.IdentifierToken.Annotation<NamespaceReference>());
			WriteTypeArguments(simpleType.TypeArguments, CodeBracesRangeFlags.Parentheses);
			
			return EndNode(simpleType);
		}
		#endregion
		
		#region StartNode/EndNode
		void StartNode(AstNode node)
		{
			// Ensure that nodes are visited in the proper nested order.
			// Jumps to different subtrees are allowed only for the child of a placeholder node.
			Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek());
			if (positionStack.Count > 0)
				WriteSpecialsUpToNode(node);
			containerStack.Push(node);
			positionStack.Push(node.FirstChild);
			formatter.StartNode(node);
			for (var child = node.FirstChild; ; child = child.NextSibling) {
				var comment = child as Comment;
				if (comment == null)
					break;
				WriteComment(comment);
			}
		}

		object EndNode(AstNode node)
		{
			Debug.Assert(node == containerStack.Peek());
			AstNode pos = positionStack.Pop();
			Debug.Assert(pos == null || pos.Parent == node);
			WriteSpecials(pos, null);
			containerStack.Pop();
			formatter.EndNode(node);
			return null;
		}
		#endregion
		
		#region debug statements
		void DebugStart(AstNode node)
		{
			formatter.DebugStart(node);
		}

		void DebugHidden(object hiddenILSpans)
		{
			formatter.DebugHidden(hiddenILSpans);
		}

		int DebugStart(AstNode node, string keyword)
		{
			return WriteKeyword(keyword, null, node);
		}

		void DebugExpression(AstNode node)
		{
			formatter.DebugExpression(node);
		}

		void DebugEnd(AstNode node, bool addSelf = true)
		{
			if (addSelf)
				formatter.DebugExpression(node);
			formatter.DebugEnd(node);
		}
		#endregion
		
		#region WriteSpecials
		/// <summary>
		/// Writes all specials from start to end (exclusive). Does not touch the positionStack.
		/// </summary>
		void WriteSpecials(AstNode start, AstNode end)
		{
			for (AstNode pos = start; pos != end; pos = pos.NextSibling) {
				if (pos.Role == AstNode.Roles.Comment) {
					pos.AcceptVisitor(this, null);
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the next
		/// node with the specified role. Advances the current position.
		/// </summary>
		void WriteSpecialsUpToRole(Role role)
		{
			for (AstNode pos = positionStack.Peek(); pos != null; pos = pos.NextSibling) {
				if (pos.Role == role) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the specified node.
		/// Advances the current position.
		/// </summary>
		void WriteSpecialsUpToNode(AstNode node)
		{
			for (AstNode pos = positionStack.Peek(); pos != null; pos = pos.NextSibling) {
				if (pos == node) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		
		void WriteSpecialsUpToRole(Role role, AstNode nextNode)
		{
			// Look for the role between the current position and the nextNode.
			for (AstNode pos = positionStack.Peek(); pos != null && pos != nextNode; pos = pos.NextSibling) {
				if (pos.Role == AstNode.Roles.Comma) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		#endregion
		
		#region Comma
		/// <summary>
		/// Writes a comma.
		/// </summary>
		/// <param name="nextNode">The next node after the comma.</param>
		/// <param name="noSpacesAfterComma">When set prevents printing a space after comma.</param>
		void Comma(AstNode nextNode, bool noSpaceAfterComma = false)
		{
			WriteSpecialsUpToRole(AstNode.Roles.Comma, nextNode);
			formatter.WriteToken(",", BoxedTextColor.Punctuation);
			lastWritten = LastWritten.Other;
			Space(!noSpaceAfterComma); // TODO: Comma policy has changed.
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstNode> list)
		{
			bool isFirst = true;
			foreach (AstNode node in list) {
				if (isFirst) {
					isFirst = false;
				} else {
					Comma(node);
				}
				node.AcceptVisitor(this, null);
			}
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<AstNode> list, bool spaceWithin, CodeBracesRangeFlags flags)
		{
			var braceHelper = BraceHelper.LeftParen(this, flags);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			braceHelper.RightParen();
		}
		
		#if DOTNET35
		void WriteCommaSeparatedList(IEnumerable<VariableInitializer> list)
		{
			WriteCommaSeparatedList(list);
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstType> list)
		{
			WriteCommaSeparatedList(list);
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<Expression> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<Expression, AstNode>(), spaceWithin);
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<ParameterDeclaration, AstNode>(), spaceWithin);
		}

		#endif

		void WriteCommaSeparatedListInBrackets(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			var braceHelper = BraceHelper.LeftBracket(this, CodeBracesRangeFlags.SquareBrackets);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			braceHelper.RightBracket();
		}
		#endregion
		
		#region Write tokens
		/// <summary>
		/// Writes a keyword, and all specials up to
		/// </summary>
		int WriteKeyword(string keyword, Role<VBTokenNode> tokenRole = null, AstNode node = null)
		{
			WriteSpecialsUpToRole(tokenRole ?? AstNode.Roles.Keyword);
			if (lastWritten == LastWritten.KeywordOrIdentifier)
				formatter.Space();
			int pos = formatter.NextPosition;
			if (node != null)
				DebugStart(node);
			formatter.WriteKeyword(keyword);
			lastWritten = LastWritten.KeywordOrIdentifier;
			return pos;
		}
		
		void WriteIdentifier(Identifier identifier, Role<Identifier> identifierRole = null)
		{
			var data = VisualBasicMetadataTextColorProvider.Instance.GetColor(identifier.Annotation<object>());
			if (BoxedTextColor.Keyword.Equals(data)) {
				var ilv = identifier.Annotation<Decompiler.ILAst.ILVariable>();
				if ((ilv != null && ilv.IsParameter) || identifier.Parent is ParameterDeclaration)
					data = BoxedTextColor.Parameter;
			}
			var nsRef = identifier.Annotation<NamespaceReference>();
			WriteIdentifier(identifier.Name, data, identifierRole, nsRef);
		}

		void WriteIdentifier(string identifier, object data, Role<Identifier> identifierRole = null, object extraData = null)
		{
			WriteSpecialsUpToRole(identifierRole ?? AstNode.Roles.Identifier);

			if (lastWritten == LastWritten.KeywordOrIdentifier)
				Space(); // this space is not strictly required, so we call Space()

			if (IsKeyword(identifier, containerStack.Peek()))
				formatter.WriteIdentifier("[" + identifier + "]", data, extraData);
			else
				formatter.WriteIdentifier(identifier, data, extraData);

			lastWritten = LastWritten.KeywordOrIdentifier;
		}
		
		void WriteToken(string token, Role<VBTokenNode> tokenRole, object data)
		{
			WriteSpecialsUpToRole(tokenRole);
			// Avoid that two +, - or ? tokens are combined into a ++, -- or ?? token.
			// Note that we don't need to handle tokens like = because there's no valid
			// C# program that contains the single token twice in a row.
			// (for +, - and &, this can happen with unary operators;
			// for ?, this can happen in "a is int? ? b : c" or "a as int? ?? 0";
			// and for /, this can happen with "1/ *ptr" or "1/ //comment".)
//			if (lastWritten == LastWritten.Plus && token[0] == '+'
//			    || lastWritten == LastWritten.Minus && token[0] == '-'
//			    || lastWritten == LastWritten.Ampersand && token[0] == '&'
//			    || lastWritten == LastWritten.QuestionMark && token[0] == '?'
//			    || lastWritten == LastWritten.Division && token[0] == '*')
//			{
//				formatter.Space();
//			}
			formatter.WriteToken(token, data);
//			if (token == "+")
//				lastWritten = LastWritten.Plus;
//			else if (token == "-")
//				lastWritten = LastWritten.Minus;
//			else if (token == "&")
//				lastWritten = LastWritten.Ampersand;
//			else if (token == "?")
//				lastWritten = LastWritten.QuestionMark;
//			else if (token == "/")
//				lastWritten = LastWritten.Division;
//			else
			lastWritten = LastWritten.Other;
		}
		
		void WriteTypeCharacter(TypeCode typeCharacter, object data)
		{
			switch (typeCharacter) {
				case TypeCode.Empty:
				case TypeCode.Object:
				case TypeCode.DBNull:
				case TypeCode.Boolean:
				case TypeCode.Char:
					
					break;
				case TypeCode.SByte:
					
					break;
				case TypeCode.Byte:
					
					break;
				case TypeCode.Int16:
					
					break;
				case TypeCode.UInt16:
					
					break;
				case TypeCode.Int32:
					WriteToken("%", null, data);
					break;
				case TypeCode.UInt32:
					
					break;
				case TypeCode.Int64:
					WriteToken("&", null, data);
					break;
				case TypeCode.UInt64:
					
					break;
				case TypeCode.Single:
					WriteToken("!", null, data);
					break;
				case TypeCode.Double:
					WriteToken("#", null, data);
					break;
				case TypeCode.Decimal:
					WriteToken("@", null, data);
					break;
				case TypeCode.DateTime:
					
					break;
				case TypeCode.String:
					WriteToken("$", null, data);
					break;
				default:
					throw new Exception("Invalid value for TypeCode");
			}
		}
		
		/// <summary>
		/// Writes a space depending on policy.
		/// </summary>
		void Space(bool addSpace = true)
		{
			if (addSpace) {
				formatter.Space();
				lastWritten = LastWritten.Whitespace;
			}
		}
		
		void SpaceIfNeeded() {
			if (lastWritten != LastWritten.Whitespace)
				Space();
		}

		void NewLine()
		{
			formatter.NewLine();
			lastWritten = LastWritten.Whitespace;
		}
		
		void Indent()
		{
			formatter.Indent();
		}
		
		void Unindent()
		{
			formatter.Unindent();
		}
		#endregion
		
		#region IsKeyword Test
		static readonly HashSet<string> unconditionalKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"AddHandler", "AddressOf", "Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte",
			"ByVal", "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CInt", "Class", "CLng",
			"CObj", "Const", "Continue", "CSByte", "CShort", "CSng", "CStr", "CType", "CUInt",
			"CULng", "CUShort", "Date", "Decimal", "Declare", "Default", "Delegate", "Dim",
			"DirectCast", "Do", "Double", "Each", "Else", "ElseIf", "End", "EndIf", "Enum", "Erase",
			"Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get",
			"GetType", "GetXmlNamespace", "Global", "GoSub", "GoTo", "Handles", "If", "Implements",
			"Imports", "In", "Inherits", "Integer", "Interface", "Is", "IsNot", "Let", "Lib", "Like",
			"Long", "Loop", "Me", "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass",
			"Namespace", "Narrowing", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable",
			"Object", "Of", "On", "Operator", "Option", "Optional", "Or", "OrElse", "Overloads",
			"Overridable", "Overrides", "ParamArray", "Partial", "Private", "Property", "Protected",
			"Public", "RaiseEvent", "ReadOnly", "ReDim", "REM", "RemoveHandler", "Resume", "Return",
			"SByte", "Select", "Set", "Shadows", "Shared", "Short", "Single", "Static", "Step", "Stop",
			"String", "Structure", "Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast",
			"TypeOf", "UInteger", "ULong", "UShort", "Using", "Variant", "Wend", "When", "While",
			"Widening", "With", "WithEvents", "WriteOnly", "Xor"
		};
		
		static readonly HashSet<string> queryKeywords = new HashSet<string> {
			
		};
		
		/// <summary>
		/// Determines whether the specified identifier is a keyword in the given context.
		/// </summary>
		public static bool IsKeyword(string identifier, AstNode context)
		{
			if (identifier == "New") {
				if (context.PrevSibling is InstanceExpression)
					return false;
				return true;
			}
			if (unconditionalKeywords.Contains(identifier))
				return true;
//			if (context.Ancestors.Any(a => a is QueryExpression)) {
//				if (queryKeywords.Contains(identifier))
//					return true;
//			}
			return false;
		}
		#endregion
		
		#region Write constructs
		void WriteTypeArguments(IEnumerable<AstType> typeArguments, CodeBracesRangeFlags flags)
		{
			if (typeArguments.Any()) {
				var braceHelper = BraceHelper.LeftParen(this, flags);
				WriteKeyword("Of");
				WriteCommaSeparatedList(typeArguments);
				braceHelper.RightParen();
			}
		}
		
		void WriteTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters, CodeBracesRangeFlags flags)
		{
			if (typeParameters.Any()) {
				var braceHelper = BraceHelper.LeftParen(this, flags);
				WriteKeyword("Of");
				WriteCommaSeparatedList(typeParameters);
				braceHelper.RightParen();
			}
		}
		
		void WriteModifiers(IEnumerable<VBModifierToken> modifierTokens)
		{
			foreach (VBModifierToken modifier in modifierTokens) {
				modifier.AcceptVisitor(this, null);
			}
		}
		
		void WriteArraySpecifiers(IEnumerable<ArraySpecifier> arraySpecifiers)
		{
			foreach (ArraySpecifier specifier in arraySpecifiers) {
				specifier.AcceptVisitor(this, null);
			}
		}
		
		void WriteQualifiedIdentifier(IEnumerable<Identifier> identifiers)
		{
			bool first = true;
			foreach (Identifier ident in identifiers) {
				if (first) {
					first = false;
					if (lastWritten == LastWritten.KeywordOrIdentifier)
						formatter.Space();
				} else {
					WriteSpecialsUpToRole(AstNode.Roles.Dot, ident);
					formatter.WriteToken(".", BoxedTextColor.Operator);
					lastWritten = LastWritten.Other;
				}
				WriteSpecialsUpToNode(ident);
				formatter.WriteIdentifier(ident.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(ident.Annotation<object>()));
				lastWritten = LastWritten.KeywordOrIdentifier;
			}
		}
		
		void WriteEmbeddedStatement(Statement embeddedStatement)
		{
			if (embeddedStatement.IsNull)
				return;
			BlockStatement block = embeddedStatement as BlockStatement;
			if (block != null) {
				Debug.Assert((block.HiddenStart == null || block.HiddenStart.Count == 0) && (block.HiddenEnd == null || block.HiddenEnd.Count == 0), "Block has hidden code. Needs to be handled by caller");
				VisitBlockStatement(block, null);
			}
			else
				embeddedStatement.AcceptVisitor(this, null);
		}
		
		void WriteBlock(BlockStatement body)
		{
			if (body.IsNull) {
				NewLine();
				Indent();
				NewLine();
				Unindent();
			} else
				VisitBlockStatement(body, null);
		}

		bool IsSameGroup(AstNode a, AstNode b) {
			if (a == null)
				return true;
			if (a is FieldDeclaration)
				return b is FieldDeclaration;
			return false;
		}
		
		void WriteMembers(IEnumerable<AstNode> members)
		{
			Indent();
			bool isFirst = true;
			AstNode lastMember = null;
			foreach (var member in members) {
				if (isFirst) {
					isFirst = false;
				} else {
					NewLine();
				}
				if (!IsSameGroup(lastMember, member))
					formatter.AddLineSeparator(Math.Max(lastEndBlockOffset, lastDeclarationOffset));
				member.AcceptVisitor(this, null);
				lastMember = member;
			}
			Unindent();
		}
		
		void WriteAttributes(IEnumerable<AttributeBlock> attributes)
		{
			foreach (AttributeBlock attr in attributes) {
				attr.AcceptVisitor(this, null);
			}
		}
		
		void WritePrivateImplementationType(AstType privateImplementationType)
		{
			if (!privateImplementationType.IsNull) {
				privateImplementationType.AcceptVisitor(this, null);
				WriteToken(".", AstNode.Roles.Dot, BoxedTextColor.Operator);
			}
		}
		
		void WriteImplementsClause(AstNodeCollection<InterfaceMemberSpecifier> implementsClause)
		{
			if (implementsClause.Any()) {
				Space();
				WriteKeyword("Implements");
				WriteCommaSeparatedList(implementsClause);
			}
		}
		
		void WriteImplementsClause(AstNodeCollection<AstType> implementsClause, bool isInterface)
		{
			if (implementsClause.Any()) {
				WriteKeyword(isInterface ? "Inherits" : "Implements");
				WriteCommaSeparatedList(implementsClause);
			}
		}
		
		void WriteHandlesClause(AstNodeCollection<EventMemberSpecifier> handlesClause)
		{
			if (handlesClause.Any()) {
				Space();
				WriteKeyword("Handles");
				WriteCommaSeparatedList(handlesClause);
			}
		}
		
		void WritePrimitiveValue(object val)
		{
			if (val == null) {
				WriteKeyword("Nothing");
				return;
			}
			
			if (val is bool) {
				if ((bool)val) {
					WriteKeyword("True");
				} else {
					WriteKeyword("False");
				}
				return;
			}
			
			if (val is string) {
				int startPos = formatter.NextPosition;
				formatter.WriteToken("\"" + ConvertString(val.ToString()) + "\"", BoxedTextColor.String);
				int endPos = formatter.NextPosition;
				formatter.AddBracePair(startPos, startPos + 1, endPos - 1, endPos, CodeBracesRangeFlags.DoubleQuotes);
				lastWritten = LastWritten.Other;
			} else if (val is char) {
				int startPos = formatter.NextPosition;
				formatter.WriteToken("\"" + ConvertCharLiteral((char)val) + "\"c", BoxedTextColor.Char);
				int endPos = formatter.NextPosition;
				formatter.AddBracePair(startPos, startPos + 1, endPos - 2, endPos, CodeBracesRangeFlags.DoubleQuotes);
				lastWritten = LastWritten.Other;
			} else if (val is decimal) {
				formatter.WriteToken(((decimal)val).ToString(NumberFormatInfo.InvariantInfo) + "D", BoxedTextColor.Number);
				lastWritten = LastWritten.Other;
			} else if (val is float) {
				float f = (float)val;
				if (float.IsInfinity(f) || float.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					WriteKeyword("Single");
					WriteToken(".", AstNode.Roles.Dot, BoxedTextColor.Operator);
					if (float.IsPositiveInfinity(f))
						WriteIdentifier("PositiveInfinity", BoxedTextColor.LiteralField);
					else if (float.IsNegativeInfinity(f))
						WriteIdentifier("NegativeInfinity", BoxedTextColor.LiteralField);
					else
						WriteIdentifier("NaN", BoxedTextColor.LiteralField);
					return;
				}
				formatter.WriteToken(f.ToString("R", NumberFormatInfo.InvariantInfo) + "F", BoxedTextColor.Number, val);
				lastWritten = LastWritten.Other;
			} else if (val is double) {
				double f = (double)val;
				if (double.IsInfinity(f) || double.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					WriteKeyword("Double");
					WriteToken(".", AstNode.Roles.Dot, BoxedTextColor.Operator);
					if (double.IsPositiveInfinity(f))
						WriteIdentifier("PositiveInfinity", BoxedTextColor.LiteralField);
					else if (double.IsNegativeInfinity(f))
						WriteIdentifier("NegativeInfinity", BoxedTextColor.LiteralField);
					else
						WriteIdentifier("NaN", BoxedTextColor.LiteralField);
					return;
				}
				string number = f.ToString("R", NumberFormatInfo.InvariantInfo);
				if (number.IndexOf('.') < 0 && number.IndexOf('E') < 0)
					number += ".0";
				formatter.WriteToken(number, BoxedTextColor.Number, val);
				// needs space if identifier follows number; this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else if (val is IFormattable) {
				string valueStr;
				switch (val) {
				case int v:
					valueStr = numberFormatter.Format(v);
					break;
				case uint v:
					valueStr = numberFormatter.Format(v) + "UI";
					break;
				case long v:
					valueStr = numberFormatter.Format(v) + "L";
					break;
				case ulong v:
					valueStr = numberFormatter.Format(v) + "UL";
					break;
				case byte v:
					valueStr = numberFormatter.Format(v);
					break;
				case ushort v:
					valueStr = numberFormatter.Format(v) + "US";
					break;
				case short v:
					valueStr = numberFormatter.Format(v) + "S";
					break;
				case sbyte v:
					valueStr = numberFormatter.Format(v);
					break;
				default:
					valueStr = ((IFormattable)val).ToString(null, NumberFormatInfo.InvariantInfo);
					break;
				}

				formatter.WriteToken(valueStr, VisualBasicMetadataTextColorProvider.Instance.GetColor(val), val);
				// needs space if identifier follows number; this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else {
				formatter.WriteToken(val.ToString(), VisualBasicMetadataTextColorProvider.Instance.GetColor(val));
				lastWritten = LastWritten.Other;
			}
		}
		#endregion
		
		#region ConvertLiteral
		static string ConvertCharLiteral(char ch)
		{
			if (ch == '"') return "\"\"";
			return ch.ToString();
		}
		
		static string ConvertString(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in str) {
				sb.Append(ConvertCharLiteral(ch));
			}
			return sb.ToString();
		}
		#endregion
		
		public object VisitVariableIdentifier(VariableIdentifier variableIdentifier, object data)
		{
			StartNode(variableIdentifier);
			
			WriteIdentifier(variableIdentifier.Name);
			if (variableIdentifier.HasNullableSpecifier)
				WriteToken("?", VariableIdentifier.Roles.QuestionMark, BoxedTextColor.Punctuation);
			if (variableIdentifier.ArraySizeSpecifiers.Count > 0)
				WriteCommaSeparatedListInParenthesis(variableIdentifier.ArraySizeSpecifiers, false, CodeBracesRangeFlags.Parentheses);
			WriteArraySpecifiers(variableIdentifier.ArraySpecifiers);
			
			return EndNode(variableIdentifier);
		}
		
		public object VisitAccessor(Accessor accessor, object data)
		{
			StartNode(accessor);
			WriteAttributes(accessor.Attributes);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			int start = formatter.NextPosition;
			int blockStart = start;
			var builder = accessor.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = start;
			WriteModifiers(accessor.ModifierTokens);
			DebugStart(accessor);
			DebugHidden(accessor.Body.HiddenStart);
			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeyword("Get");
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeyword("Set");
			} else if (accessor.Role == EventDeclaration.AddHandlerRole) {
				WriteKeyword("AddHandler");
			} else if (accessor.Role == EventDeclaration.RemoveHandlerRole) {
				WriteKeyword("RemoveHandler");
			} else if (accessor.Role == EventDeclaration.RaiseEventRole) {
				WriteKeyword("RaiseEvent");
			}
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			DebugEnd(accessor, false);
			if (accessor.Parameters.Any())
				WriteCommaSeparatedListInParenthesis(accessor.Parameters, false, CodeBracesRangeFlags.Parentheses);
			WriteBlock(accessor.Body);
			DebugStart(accessor);
			DebugHidden(accessor.Body.HiddenEnd);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Accessor);
			WriteKeyword("End");

			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeyword("Get");
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeyword("Set");
			} else if (accessor.Role == EventDeclaration.AddHandlerRole) {
				WriteKeyword("AddHandler");
			} else if (accessor.Role == EventDeclaration.RemoveHandlerRole) {
				WriteKeyword("RemoveHandler");
			} else if (accessor.Role == EventDeclaration.RaiseEventRole) {
				WriteKeyword("RaiseEvent");
			}
			if (builder != null)
				builder.EndPosition = formatter.NextPosition;
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			SaveDeclarationOffset();
			DebugEnd(accessor, false);
			NewLine();
			currentMethodRefs = oldRef;
			
			return EndNode(accessor);
		}

		
		public object VisitLabelDeclarationStatement(LabelDeclarationStatement labelDeclarationStatement, object data)
		{
			DebugStart(labelDeclarationStatement);
			StartNode(labelDeclarationStatement);
			
			labelDeclarationStatement.Label.AcceptVisitor(this, data);
			WriteToken(":", LabelDeclarationStatement.Roles.Colon, BoxedTextColor.Punctuation);
			DebugEnd(labelDeclarationStatement);
			
			return EndNode(labelDeclarationStatement);
		}
		
		public object VisitLocalDeclarationStatement(LocalDeclarationStatement localDeclarationStatement, object data)
		{
			StartNode(localDeclarationStatement);
			
			DebugStart(localDeclarationStatement);
			if (!(localDeclarationStatement.Parent is UsingStatement) && localDeclarationStatement.ModifierToken != null && !localDeclarationStatement.ModifierToken.IsNull)
				WriteModifiers(new [] { localDeclarationStatement.ModifierToken });
			WriteCommaSeparatedList(localDeclarationStatement.Variables);
			DebugEnd(localDeclarationStatement);
			
			return EndNode(localDeclarationStatement);
		}
		
		public object VisitWithStatement(WithStatement withStatement, object data)
		{
			StartNode(withStatement);
			var reference = new object();
			int start = DebugStart(withStatement, "With");
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			withStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(withStatement);
			withStatement.Body.AcceptVisitor(this, data);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Other);
			WriteKeyword("End");
			WriteKeyword("With");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			return EndNode(withStatement);
		}
		
		public object VisitSyncLockStatement(SyncLockStatement syncLockStatement, object data)
		{
			StartNode(syncLockStatement);
			var reference = new object();
			int start = DebugStart(syncLockStatement, "SyncLock");
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			syncLockStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(syncLockStatement);
			syncLockStatement.Body.AcceptVisitor(this, data);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Lock);
			WriteKeyword("End");
			WriteKeyword("SyncLock");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			return EndNode(syncLockStatement);
		}
		
		public object VisitTryStatement(TryStatement tryStatement, object data)
		{
			StartNode(tryStatement);
			var reference = new object();
			var oldRef = currentTryReference;
			currentTryReference = reference;
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteKeyword("Try");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			tryStatement.Body.AcceptVisitor(this, data);
			foreach (var clause in tryStatement.CatchBlocks) {
				clause.AcceptVisitor(this, data);
			}
			if (!tryStatement.FinallyBlock.IsNull) {
				start = formatter.NextPosition;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Try);
				blockStart = start;
				WriteKeyword("Finally");
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
				tryStatement.FinallyBlock.AcceptVisitor(this, data);
			}
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, tryStatement.FinallyBlock.IsNull ? CodeBracesRangeFlags.BlockKind_Try : CodeBracesRangeFlags.BlockKind_Finally);
			WriteKeyword("End");
			WriteKeyword("Try");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			currentTryReference = oldRef;
			return EndNode(tryStatement);
		}
		
		public object VisitCatchBlock(CatchBlock catchBlock, object data)
		{
			StartNode(catchBlock);
			int start = DebugStart(catchBlock, "Catch");
			formatter.AddHighlightedKeywordReference(currentTryReference, start, formatter.NextPosition);
			if (!catchBlock.ExceptionVariable.IsNull)
				catchBlock.ExceptionVariable.AcceptVisitor(this, data);
			if (!catchBlock.ExceptionType.IsNull) {
				Debug.Assert(!catchBlock.ExceptionVariable.IsNull);
				WriteKeyword("As");
				catchBlock.ExceptionType.AcceptVisitor(this, data);
			}
			if (!catchBlock.WhenExpression.IsNull) {
				Space();
				start = formatter.NextPosition;
				WriteKeyword("When");
				formatter.AddHighlightedKeywordReference(currentTryReference, start, formatter.NextPosition);
				Space();
				catchBlock.WhenExpression.AcceptVisitor(this, data);
			}
			DebugEnd(catchBlock);
			NewLine();
			Indent();
			foreach (var stmt in catchBlock) {
				stmt.AcceptVisitor(this, data);
				NewLine();
			}
			Unindent();
			return EndNode(catchBlock);
		}
		
		public object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			StartNode(expressionStatement);
			DebugStart(expressionStatement);
			expressionStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(expressionStatement);
			return EndNode(expressionStatement);
		}
		
		public object VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			StartNode(throwStatement);
			
			DebugStart(throwStatement, "Throw");
			throwStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(throwStatement);
			
			return EndNode(throwStatement);
		}

		bool isElseIfStatement;
		object currentIfReference;
		int elseIfStartPos;
		public object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			StartNode(ifElseStatement);
			var oldRef = currentIfReference;
			if (!isElseIfStatement)
				currentIfReference = new object();
			int start = DebugStart(ifElseStatement, isElseIfStatement ? "ElseIf" : "If");
			if (isElseIfStatement)
				formatter.AddBlock(elseIfStartPos, start, CodeBracesRangeFlags.BlockKind_Conditional);
			int blockStart = start;
			isElseIfStatement = false;
			formatter.AddHighlightedKeywordReference(currentIfReference, start, formatter.NextPosition);
			ifElseStatement.Condition.AcceptVisitor(this, data);
			DebugEnd(ifElseStatement);
			Space();
			start = formatter.NextPosition;
			WriteKeyword("Then");
			formatter.AddHighlightedKeywordReference(currentIfReference, start, formatter.NextPosition);
			bool needsEndIf = ifElseStatement.Body is BlockStatement;
			ifElseStatement.Body.AcceptVisitor(this, data);
			if (!ifElseStatement.ElseBlock.IsNull) {
				if (ifElseStatement.ElseBlock is IfElseStatement) {
					isElseIfStatement = true;
					elseIfStartPos = blockStart;
				}
				else {
					start = formatter.NextPosition;
					formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Conditional);
					blockStart = start;
					WriteKeyword("Else");
					formatter.AddHighlightedKeywordReference(currentIfReference, start, formatter.NextPosition);
				}
				needsEndIf = ifElseStatement.ElseBlock is BlockStatement;
				ifElseStatement.ElseBlock.AcceptVisitor(this, data);
				if (ifElseStatement.ElseBlock is IfElseStatement)
					blockStart = elseIfStartPos;
			}
			if (needsEndIf) {
				start = formatter.NextPosition;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Conditional);
				lastEndBlockOffset = start;
				WriteKeyword("End");
				WriteKeyword("If");
				formatter.AddHighlightedKeywordReference(currentIfReference, start, formatter.NextPosition);
			}
			currentIfReference = oldRef;
			elseIfStartPos = blockStart;
			return EndNode(ifElseStatement);
		}
		
		public object VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			StartNode(returnStatement);
			int start = DebugStart(returnStatement, "Return");
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			returnStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(returnStatement);
			return EndNode(returnStatement);
		}
		
		public object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			DebugExpression(binaryOperatorExpression);
			StartNode(binaryOperatorExpression);
			binaryOperatorExpression.Left.AcceptVisitor(this, data);
			Space();
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.BitwiseAnd:
					WriteKeyword("And");
					break;
				case BinaryOperatorType.BitwiseOr:
					WriteKeyword("Or");
					break;
				case BinaryOperatorType.LogicalAnd:
					WriteKeyword("AndAlso");
					break;
				case BinaryOperatorType.LogicalOr:
					WriteKeyword("OrElse");
					break;
				case BinaryOperatorType.ExclusiveOr:
					WriteKeyword("Xor");
					break;
				case BinaryOperatorType.GreaterThan:
					WriteToken(">", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					WriteToken(">=", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Equality:
					WriteToken("=", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.InEquality:
					WriteToken("<>", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.LessThan:
					WriteToken("<", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.LessThanOrEqual:
					WriteToken("<=", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Add:
					WriteToken("+", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Subtract:
					WriteToken("-", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Multiply:
					WriteToken("*", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Divide:
					WriteToken("/", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Modulus:
					WriteKeyword("Mod");
					break;
				case BinaryOperatorType.DivideInteger:
					WriteToken("\\", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Power:
					WriteToken("*", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.Concat:
					WriteToken("&", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.ShiftLeft:
					WriteToken("<<", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.ShiftRight:
					WriteToken(">>", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case BinaryOperatorType.ReferenceEquality:
					WriteKeyword("Is");
					break;
				case BinaryOperatorType.ReferenceInequality:
					WriteKeyword("IsNot");
					break;
				case BinaryOperatorType.Like:
					WriteKeyword("Like");
					break;
				case BinaryOperatorType.DictionaryAccess:
					WriteToken("!", BinaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				default:
					throw new Exception("Invalid value for BinaryOperatorType: " + binaryOperatorExpression.Operator);
			}
			Space();
			binaryOperatorExpression.Right.AcceptVisitor(this, data);
			return EndNode(binaryOperatorExpression);
		}
		
		public object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			DebugExpression(identifierExpression);
			StartNode(identifierExpression);
			identifierExpression.Identifier.AcceptVisitor(this, data);
			WriteTypeArguments(identifierExpression.TypeArguments, CodeBracesRangeFlags.Parentheses);
			return EndNode(identifierExpression);
		}
		
		public object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			DebugExpression(assignmentExpression);
			StartNode(assignmentExpression);
			assignmentExpression.Left.AcceptVisitor(this, data);
			Space();
			switch (assignmentExpression.Operator) {
				case AssignmentOperatorType.Assign:
					if (assignmentExpression.Parent is Ast.Attribute)
						WriteToken(":=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					else
						WriteToken("=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.Add:
					WriteToken("+=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.Subtract:
					WriteToken("-=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.Multiply:
					WriteToken("*=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.Divide:
					WriteToken("/=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.Power:
					WriteToken("^=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.DivideInteger:
					WriteToken("\\=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.ConcatString:
					WriteToken("&=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.ShiftLeft:
					WriteToken("<<=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case AssignmentOperatorType.ShiftRight:
					WriteToken(">>=", AssignmentExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				default:
					throw new Exception("Invalid value for AssignmentOperatorType: " + assignmentExpression.Operator);
			}
			Space();
			assignmentExpression.Right.AcceptVisitor(this, data);
			return EndNode(assignmentExpression);
		}
		
		public object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			DebugExpression(invocationExpression);
			StartNode(invocationExpression);
			invocationExpression.Target.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(invocationExpression.Arguments, false, CodeBracesRangeFlags.Parentheses);
			return EndNode(invocationExpression);
		}
		
		public object VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			DebugExpression(arrayInitializerExpression);
			StartNode(arrayInitializerExpression);
			var braceHelper = BraceHelper.LeftBrace(this, CodeBracesRangeFlags.CurlyBraces);
			Space();
			WriteCommaSeparatedList(arrayInitializerExpression.Elements);
			Space();
			braceHelper.RightBrace();
			return EndNode(arrayInitializerExpression);
		}
		
		public object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			DebugExpression(arrayCreateExpression);
			StartNode(arrayCreateExpression);
			WriteKeyword("New");
			Space();
			arrayCreateExpression.Type.AcceptVisitor(this, data);
			if (arrayCreateExpression.Arguments.Any())
				WriteCommaSeparatedListInParenthesis(arrayCreateExpression.Arguments, false, CodeBracesRangeFlags.Parentheses);
			foreach (var specifier in arrayCreateExpression.AdditionalArraySpecifiers) {
				specifier.AcceptVisitor(this, data);
			}
			if (lastWritten != LastWritten.Whitespace)
				Space();
			if (arrayCreateExpression.Initializer.IsNull) {
				var braceHelper = BraceHelper.LeftBrace(this, CodeBracesRangeFlags.CurlyBraces);
				braceHelper.RightBrace();
			} else {
				arrayCreateExpression.Initializer.AcceptVisitor(this, data);
			}
			return EndNode(arrayCreateExpression);
		}
		
		public object VisitObjectCreationExpression(ObjectCreationExpression objectCreationExpression, object data)
		{
			DebugExpression(objectCreationExpression);
			StartNode(objectCreationExpression);
			
			WriteKeyword("New");
			objectCreationExpression.Type.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(objectCreationExpression.Arguments, false, CodeBracesRangeFlags.Parentheses);
			if (!objectCreationExpression.Initializer.IsNull) {
				Space();
				if (objectCreationExpression.Initializer.Elements.Any(x => x is FieldInitializerExpression))
					WriteKeyword("With");
				else
					WriteKeyword("From");
				Space();
				objectCreationExpression.Initializer.AcceptVisitor(this, data);
			}
			
			return EndNode(objectCreationExpression);
		}
		
		public object VisitCastExpression(CastExpression castExpression, object data)
		{
			DebugExpression(castExpression);
			StartNode(castExpression);
			
			switch (castExpression.CastType) {
				case CastType.DirectCast:
					WriteKeyword("DirectCast");
					break;
				case CastType.TryCast:
					WriteKeyword("TryCast");
					break;
				case CastType.CType:
					WriteKeyword("CType");
					break;
				case CastType.CBool:
					WriteKeyword("CBool");
					break;
				case CastType.CByte:
					WriteKeyword("CByte");
					break;
				case CastType.CChar:
					WriteKeyword("CChar");
					break;
				case CastType.CDate:
					WriteKeyword("CDate");
					break;
				case CastType.CDec:
					WriteKeyword("CDec");
					break;
				case CastType.CDbl:
					WriteKeyword("CDbl");
					break;
				case CastType.CInt:
					WriteKeyword("CInt");
					break;
				case CastType.CLng:
					WriteKeyword("CLng");
					break;
				case CastType.CObj:
					WriteKeyword("CObj");
					break;
				case CastType.CSByte:
					WriteKeyword("CSByte");
					break;
				case CastType.CShort:
					WriteKeyword("CShort");
					break;
				case CastType.CSng:
					WriteKeyword("CSng");
					break;
				case CastType.CStr:
					WriteKeyword("CStr");
					break;
				case CastType.CUInt:
					WriteKeyword("CUInt");
					break;
				case CastType.CULng:
					WriteKeyword("CULng");
					break;
				case CastType.CUShort:
					WriteKeyword("CUShort");
					break;
				default:
					throw new Exception("Invalid value for CastType");
			}

			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			castExpression.Expression.AcceptVisitor(this, data);
			
			if (castExpression.CastType == CastType.CType ||
			    castExpression.CastType == CastType.DirectCast ||
			    castExpression.CastType == CastType.TryCast) {
				WriteToken(",", CastExpression.Roles.Comma, BoxedTextColor.Punctuation);
				Space();
				castExpression.Type.AcceptVisitor(this, data);
			}

			braceHelper.RightParen();

			return EndNode(castExpression);
		}
		
		public object VisitComment(Comment comment, object data)
		{
			if (comment.IsDocumentationComment)
				WriteComment(comment);
			return null;
		}

		void WriteComment(Comment comment)
		{
			formatter.WriteComment(comment.IsDocumentationComment, comment.Content, comment.References);
		}

		public object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			StartNode(eventDeclaration);
			
			WriteAttributes(eventDeclaration.Attributes);
			var reference = new object();
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteModifiers(eventDeclaration.ModifierTokens);
			if (eventDeclaration.IsCustom)
				WriteKeyword("Custom");
			WriteKeyword("Event");
			if (eventDeclaration.IsCustom)
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			WriteIdentifier(eventDeclaration.Name);
			if (!eventDeclaration.IsCustom && eventDeclaration.ReturnType.IsNull)
				WriteCommaSeparatedListInParenthesis(eventDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!eventDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				eventDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			WriteImplementsClause(eventDeclaration.ImplementsClause);
			
			if (eventDeclaration.IsCustom) {
				NewLine();
				Indent();
				
				eventDeclaration.AddHandlerBlock.AcceptVisitor(this, data);
				eventDeclaration.RemoveHandlerBlock.AcceptVisitor(this, data);
				eventDeclaration.RaiseEventBlock.AcceptVisitor(this, data);
				
				Unindent();
				start = formatter.NextPosition;
				lastEndBlockOffset = start;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Event);
				WriteKeyword("End");
				WriteKeyword("Event");
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			}
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(eventDeclaration);
		}
		
		public object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			DebugExpression(unaryOperatorExpression);
			StartNode(unaryOperatorExpression);
			
			switch (unaryOperatorExpression.Operator) {
				case UnaryOperatorType.Not:
					WriteKeyword("Not");
					break;
				case UnaryOperatorType.Minus:
					WriteToken("-", UnaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case UnaryOperatorType.Plus:
					WriteToken("+", UnaryOperatorExpression.OperatorRole, BoxedTextColor.Operator);
					break;
				case UnaryOperatorType.AddressOf:
					WriteKeyword("AddressOf");
					break;
				case UnaryOperatorType.Await:
					SpaceIfNeeded();
					int start = formatter.NextPosition;
					WriteKeyword("Await");
					formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
					break;
				default:
					throw new Exception("Invalid value for UnaryOperatorType");
			}
			
			unaryOperatorExpression.Expression.AcceptVisitor(this, data);
			
			return EndNode(unaryOperatorExpression);
		}
		
		public object VisitFieldInitializerExpression(FieldInitializerExpression fieldInitializerExpression, object data)
		{
			DebugExpression(fieldInitializerExpression);
			StartNode(fieldInitializerExpression);
			
			if (fieldInitializerExpression.IsKey && fieldInitializerExpression.Parent is AnonymousObjectCreationExpression) {
				WriteKeyword("Key");
				Space();
			}
			
			WriteToken(".", FieldInitializerExpression.Roles.Dot, BoxedTextColor.Operator);
			fieldInitializerExpression.Identifier.AcceptVisitor(this, data);
			
			Space();
			WriteToken("=", FieldInitializerExpression.Roles.Assign, BoxedTextColor.Operator);
			Space();
			fieldInitializerExpression.Expression.AcceptVisitor(this, data);
			
			return EndNode(fieldInitializerExpression);
		}
		
		public object VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			DebugExpression(namedArgumentExpression);
			throw new NotImplementedException();
		}
		
		public object VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			DebugExpression(conditionalExpression);
			StartNode(conditionalExpression);
			
			WriteKeyword("If");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			
			conditionalExpression.ConditionExpression.AcceptVisitor(this, data);
			WriteToken(",", ConditionalExpression.Roles.Comma, BoxedTextColor.Punctuation);
			Space();
			
			if (!conditionalExpression.TrueExpression.IsNull) {
				conditionalExpression.TrueExpression.AcceptVisitor(this, data);
				WriteToken(",", ConditionalExpression.Roles.Comma, BoxedTextColor.Punctuation);
				Space();
			}
			
			conditionalExpression.FalseExpression.AcceptVisitor(this, data);
			
			braceHelper.RightParen();
			
			return EndNode(conditionalExpression);
		}
		
		public object VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			StartNode(whileStatement);

			var reference = new object();
			var oldRef = currentWhileReference;
			currentWhileReference = reference;
			int start = DebugStart(whileStatement, "While");
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			Space();
			whileStatement.Condition.AcceptVisitor(this, data);
			DebugEnd(whileStatement);
			whileStatement.Body.AcceptVisitor(this, data);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Loop);
			WriteKeyword("End");
			WriteKeyword("While");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			currentWhileReference = oldRef;

			return EndNode(whileStatement);
		}
		
		public object VisitExitStatement(ExitStatement exitStatement, object data)
		{
			StartNode(exitStatement);
			
			int start = DebugStart(exitStatement, "Exit");
			
			switch (exitStatement.ExitKind) {
				case ExitKind.Sub:
					WriteKeyword("Sub");
					formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
					break;
				case ExitKind.Function:
					WriteKeyword("Function");
					formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
					break;
				case ExitKind.Property:
					WriteKeyword("Property");
					formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
					break;
				case ExitKind.Do:
					WriteKeyword("Do");
					formatter.AddHighlightedKeywordReference(currentDoReference, start, formatter.NextPosition);
					break;
				case ExitKind.For:
					WriteKeyword("For");
					formatter.AddHighlightedKeywordReference(currentForReference, start, formatter.NextPosition);
					break;
				case ExitKind.While:
					WriteKeyword("While");
					formatter.AddHighlightedKeywordReference(currentWhileReference, start, formatter.NextPosition);
					break;
				case ExitKind.Select:
					WriteKeyword("Select");
					formatter.AddHighlightedKeywordReference(currentSelectReference, start, formatter.NextPosition);
					break;
				case ExitKind.Try:
					WriteKeyword("Try");
					formatter.AddHighlightedKeywordReference(currentTryReference, start, formatter.NextPosition);
					break;
				default:
					throw new Exception("Invalid value for ExitKind");
			}
			DebugEnd(exitStatement);
			
			return EndNode(exitStatement);
		}
		
		public object VisitForStatement(ForStatement forStatement, object data)
		{
			StartNode(forStatement);

			var reference = new object();
			var oldRef = currentForReference;
			currentForReference = reference;
			int start = DebugStart(forStatement, "For");
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			Space();
			forStatement.Variable.AcceptVisitor(this, data);
			DebugEnd(forStatement, false);
			start = DebugStart(forStatement, "To");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			forStatement.ToExpression.AcceptVisitor(this, data);
			DebugEnd(forStatement, false);
			if (!forStatement.StepExpression.IsNull) {
				start = DebugStart(forStatement, "Step");
				formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
				Space();
				forStatement.StepExpression.AcceptVisitor(this, data);
				DebugEnd(forStatement, false);
			}
			forStatement.Body.AcceptVisitor(this, data);
			start = formatter.NextPosition;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Loop);
			WriteKeyword("Next");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			currentForReference = oldRef;
			
			return EndNode(forStatement);
		}
		
		public object VisitForEachStatement(ForEachStatement forEachStatement, object data)
		{
			StartNode(forEachStatement);
			
			DebugStart(forEachStatement);
			var reference = new object();
			var oldRef = currentForReference;
			currentForReference = reference;
			int start = formatter.NextPosition;
			int blockStart = start;
			WriteKeyword("For");
			WriteKeyword("Each");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugHidden(forEachStatement.HiddenInitializer);
			DebugHidden(forEachStatement.Body.HiddenStart);
			DebugEnd(forEachStatement, false);
			Space();
			DebugStart(forEachStatement);
			forEachStatement.Variable.AcceptVisitor(this, data);
			DebugHidden(forEachStatement.HiddenGetCurrentILSpans);
			DebugEnd(forEachStatement, false);
			Space();
			DebugStart(forEachStatement);
			start = formatter.NextPosition;
			WriteKeyword("In");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugHidden(forEachStatement.HiddenMoveNextILSpans);
			DebugEnd(forEachStatement, false);
			Space();
			DebugStart(forEachStatement);
			forEachStatement.InExpression.AcceptVisitor(this, data);
			DebugHidden(forEachStatement.HiddenGetEnumeratorILSpans);
			DebugEnd(forEachStatement, false);
			forEachStatement.Body.AcceptVisitor(this, data);
			DebugStart(forEachStatement);
			start = formatter.NextPosition;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Loop);
			WriteKeyword("Next");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugHidden(forEachStatement.Body.HiddenEnd);
			DebugEnd(forEachStatement, false);
			currentForReference = oldRef;
			
			return EndNode(forEachStatement);
		}
		
		public object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			StartNode(operatorDeclaration);
			
			WriteAttributes(operatorDeclaration.Attributes);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			int start = formatter.NextPosition;
			int blockStart = start;
			var builder = operatorDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = start;
			WriteModifiers(operatorDeclaration.ModifierTokens);
			DebugStart(operatorDeclaration);
			DebugHidden(operatorDeclaration.Body.HiddenStart);
			bool writeEndOperator = !operatorDeclaration.Body.IsNull || (operatorDeclaration.Body.HiddenEnd != null && operatorDeclaration.Body.HiddenEnd.Count > 0);
			WriteKeyword("Operator");
			if (writeEndOperator)
				formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			Space();
			switch (operatorDeclaration.Operator) {
				case OverloadableOperatorType.Add:
				case OverloadableOperatorType.UnaryPlus:
					WriteToken("+", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Subtract:
				case OverloadableOperatorType.UnaryMinus:
					WriteToken("-", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Multiply:
					WriteToken("*", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Divide:
					WriteToken("/", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Modulus:
					WriteKeyword("Mod");
					break;
				case OverloadableOperatorType.Concat:
					WriteToken("&", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Not:
					WriteKeyword("Not");
					break;
				case OverloadableOperatorType.BitwiseAnd:
					WriteKeyword("And");
					break;
				case OverloadableOperatorType.BitwiseOr:
					WriteKeyword("Or");
					break;
				case OverloadableOperatorType.ExclusiveOr:
					WriteKeyword("Xor");
					break;
				case OverloadableOperatorType.ShiftLeft:
					WriteToken("<<", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.ShiftRight:
					WriteToken(">>", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.GreaterThan:
					WriteToken(">", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.GreaterThanOrEqual:
					WriteToken(">=", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.Equality:
					WriteToken("=", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.InEquality:
					WriteToken("<>", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.LessThan:
					WriteToken("<", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.LessThanOrEqual:
					WriteToken("<=", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.IsTrue:
					WriteKeyword("IsTrue");
					break;
				case OverloadableOperatorType.IsFalse:
					WriteKeyword("IsFalse");
					break;
				case OverloadableOperatorType.Like:
					WriteKeyword("Like");
					break;
				case OverloadableOperatorType.Power:
					WriteToken("^", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				case OverloadableOperatorType.CType:
					WriteKeyword("CType");
					break;
				case OverloadableOperatorType.DivideInteger:
					WriteToken("\\", OperatorDeclaration.Roles.Keyword, BoxedTextColor.Operator);
					break;
				default:
					throw new Exception("Invalid value for OverloadableOperatorType");
			}
			DebugEnd(operatorDeclaration, false);
			WriteCommaSeparatedListInParenthesis(operatorDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!operatorDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				Space();
				WriteAttributes(operatorDeclaration.ReturnTypeAttributes);
				operatorDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			if (writeEndOperator) {
				WriteBlock(operatorDeclaration.Body);
				DebugStart(operatorDeclaration);
				DebugHidden(operatorDeclaration.Body.HiddenEnd);
				start = formatter.NextPosition;
				lastEndBlockOffset = start;
				formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Operator);
				WriteKeyword("End");
				WriteKeyword("Operator");
				if (builder != null)
					builder.EndPosition = formatter.NextPosition;
				formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
				DebugEnd(operatorDeclaration, false);
			}
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(operatorDeclaration);
		}
		
		public object VisitSelectStatement(SelectStatement selectStatement, object data)
		{
			StartNode(selectStatement);

			var reference = new object();
			var oldRef = currentSelectReference;
			currentSelectReference = reference;
			int start = DebugStart(selectStatement, "Select");
			int blockStart = start;
			WriteKeyword("Case");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			selectStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(selectStatement);
			NewLine();
			Indent();
			
			foreach (CaseStatement stmt in selectStatement.Cases) {
				stmt.AcceptVisitor(this, data);
			}
			
			Unindent();
			DebugStart(selectStatement);
			DebugHidden(selectStatement.HiddenEnd);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Conditional);
			WriteKeyword("End");
			WriteKeyword("Select");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugEnd(selectStatement, false);
			currentSelectReference = oldRef;
			
			return EndNode(selectStatement);
		}
		
		public object VisitCaseStatement(CaseStatement caseStatement, object data)
		{
			DebugStart(caseStatement);
			StartNode(caseStatement);
			
			int start = formatter.NextPosition;
			WriteKeyword("Case");
			if (caseStatement.Clauses.Count == 1 && caseStatement.Clauses.First().Expression.IsNull) {
				WriteKeyword("Else");
				formatter.AddHighlightedKeywordReference(currentSelectReference, start, formatter.NextPosition);
			}
			else {
				formatter.AddHighlightedKeywordReference(currentSelectReference, start, formatter.NextPosition);
				Space();
				WriteCommaSeparatedList(caseStatement.Clauses);
			}
			DebugEnd(caseStatement, false);
			caseStatement.Body.AcceptVisitor(this, data);

			return EndNode(caseStatement);
		}
		
		public object VisitSimpleCaseClause(SimpleCaseClause simpleCaseClause, object data)
		{
			StartNode(simpleCaseClause);
			DebugStart(simpleCaseClause);
			simpleCaseClause.Expression.AcceptVisitor(this, data);
			DebugEnd(simpleCaseClause);
			return EndNode(simpleCaseClause);
		}
		
		public object VisitRangeCaseClause(RangeCaseClause rangeCaseClause, object data)
		{
			StartNode(rangeCaseClause);
			DebugStart(rangeCaseClause);
			rangeCaseClause.Expression.AcceptVisitor(this, data);
			WriteKeyword("To");
			rangeCaseClause.ToExpression.AcceptVisitor(this, data);
			DebugEnd(rangeCaseClause);
			return EndNode(rangeCaseClause);
		}
		
		public object VisitComparisonCaseClause(ComparisonCaseClause comparisonCaseClause, object data)
		{
			StartNode(comparisonCaseClause);
			DebugStart(comparisonCaseClause);
			switch (comparisonCaseClause.Operator) {
				case ComparisonOperator.Equality:
					WriteToken("=", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				case ComparisonOperator.InEquality:
					WriteToken("<>", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				case ComparisonOperator.LessThan:
					WriteToken("<", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				case ComparisonOperator.GreaterThan:
					WriteToken(">", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				case ComparisonOperator.LessThanOrEqual:
					WriteToken("<=", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				case ComparisonOperator.GreaterThanOrEqual:
					WriteToken(">=", ComparisonCaseClause.OperatorRole, BoxedTextColor.Operator);
					break;
				default:
					throw new Exception("Invalid value for ComparisonOperator");
			}
			Space();
			comparisonCaseClause.Expression.AcceptVisitor(this, data);
			DebugEnd(comparisonCaseClause);
			return EndNode(comparisonCaseClause);
		}

		
		public object VisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			StartNode(yieldStatement);
			int start = DebugStart(yieldStatement, "Yield");
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			yieldStatement.Expression.AcceptVisitor(this, data);
			DebugEnd(yieldStatement);
			return EndNode(yieldStatement);
		}
		
		public object VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			StartNode(variableInitializer);
			
			DebugStart(variableInitializer);
			variableInitializer.Identifier.AcceptVisitor(this, data);
			if (!variableInitializer.Type.IsNull) {
				if (lastWritten != LastWritten.Whitespace)
					Space();
				WriteKeyword("As");
				variableInitializer.Type.AcceptVisitor(this, data);
			}
			if (!variableInitializer.Expression.IsNull) {
				Space();
				WriteToken("=", VariableInitializer.Roles.Assign, BoxedTextColor.Operator);
				Space();
				variableInitializer.Expression.AcceptVisitor(this, data);
			}
			DebugEnd(variableInitializer);
			
			return EndNode(variableInitializer);
		}
		
		public object VisitVariableDeclaratorWithTypeAndInitializer(VariableDeclaratorWithTypeAndInitializer variableDeclaratorWithTypeAndInitializer, object data)
		{
			StartNode(variableDeclaratorWithTypeAndInitializer);

			bool ownerIsProp = variableDeclaratorWithTypeAndInitializer.Parent is PropertyDeclaration;
			if (ownerIsProp) {
				if (lastWritten != LastWritten.Whitespace)
					Space();
				WriteToken("=", VariableDeclarator.Roles.Assign, BoxedTextColor.Operator);
				Space();
				DebugStart(variableDeclaratorWithTypeAndInitializer);
				variableDeclaratorWithTypeAndInitializer.Initializer.AcceptVisitor(this, data);
			}
			else {
				if (lastWritten != LastWritten.Whitespace)
					Space();
				DebugStart(variableDeclaratorWithTypeAndInitializer);
				WriteCommaSeparatedList(variableDeclaratorWithTypeAndInitializer.Identifiers);
				if (lastWritten != LastWritten.Whitespace)
					Space();
				WriteKeyword("As");
				variableDeclaratorWithTypeAndInitializer.Type.AcceptVisitor(this, data);
				if (!variableDeclaratorWithTypeAndInitializer.Initializer.IsNull) {
					Space();
					WriteToken("=", VariableDeclarator.Roles.Assign, BoxedTextColor.Operator);
					Space();
					variableDeclaratorWithTypeAndInitializer.Initializer.AcceptVisitor(this, data);
				}
			}
			DebugEnd(variableDeclaratorWithTypeAndInitializer);
			
			return EndNode(variableDeclaratorWithTypeAndInitializer);
		}
		
		public object VisitVariableDeclaratorWithObjectCreation(VariableDeclaratorWithObjectCreation variableDeclaratorWithObjectCreation, object data)
		{
			StartNode(variableDeclaratorWithObjectCreation);
			
			if (lastWritten != LastWritten.Whitespace)
				Space();
			DebugStart(variableDeclaratorWithObjectCreation);
			WriteCommaSeparatedList(variableDeclaratorWithObjectCreation.Identifiers);
			if (lastWritten != LastWritten.Whitespace)
				Space();
			WriteKeyword("As");
			variableDeclaratorWithObjectCreation.Initializer.AcceptVisitor(this, data);
			DebugEnd(variableDeclaratorWithObjectCreation);
			
			return EndNode(variableDeclaratorWithObjectCreation);
		}
		
		public object VisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			StartNode(doLoopStatement);

			var reference = new object();
			var oldRef = currentDoReference;
			currentDoReference = reference;
			int start;
			if (doLoopStatement.ConditionType == ConditionType.DoUntil) {
				start = DebugStart(doLoopStatement, "Do");
				WriteKeyword("Until");
				doLoopStatement.Expression.AcceptVisitor(this, data);
				DebugEnd(doLoopStatement);
			}
			else if (doLoopStatement.ConditionType == ConditionType.DoWhile) {
				start = DebugStart(doLoopStatement, "Do");
				WriteKeyword("While");
				doLoopStatement.Expression.AcceptVisitor(this, data);
				DebugEnd(doLoopStatement);
			}
			else {
				start = formatter.NextPosition;
				WriteKeyword("Do");
			}
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			doLoopStatement.Body.AcceptVisitor(this, data);
			if (doLoopStatement.ConditionType == ConditionType.LoopUntil) {
				start = DebugStart(doLoopStatement, "Loop");
				WriteKeyword("Until");
				doLoopStatement.Expression.AcceptVisitor(this, data);
				DebugEnd(doLoopStatement);
			}
			else if (doLoopStatement.ConditionType == ConditionType.LoopWhile) {
				start = DebugStart(doLoopStatement, "Loop");
				WriteKeyword("While");
				doLoopStatement.Expression.AcceptVisitor(this, data);
				DebugEnd(doLoopStatement);
			}
			else {
				start = formatter.NextPosition;
				WriteKeyword("Loop");
			}
			formatter.AddBlock(blockStart, start, CodeBracesRangeFlags.BlockKind_Loop);
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			currentDoReference = oldRef;
			
			return EndNode(doLoopStatement);
		}
		
		public object VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			StartNode(usingStatement);

			var reference = new object();
			int start = DebugStart(usingStatement, "Using");
			int blockStart = start;
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugHidden(usingStatement.Body.HiddenStart);
			WriteCommaSeparatedList(usingStatement.Resources);
			DebugEnd(usingStatement, false);
			usingStatement.Body.AcceptVisitor(this, data);
			DebugStart(usingStatement);
			DebugHidden(usingStatement.Body.HiddenEnd);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_Using);
			WriteKeyword("End");
			WriteKeyword("Using");
			formatter.AddHighlightedKeywordReference(reference, start, formatter.NextPosition);
			DebugEnd(usingStatement, false);
			
			return EndNode(usingStatement);
		}
		
		public object VisitGoToStatement(GoToStatement goToStatement, object data)
		{
			StartNode(goToStatement);
			
			DebugStart(goToStatement, "GoTo");
			goToStatement.Label.AcceptVisitor(this, data);
			DebugEnd(goToStatement);
			
			return EndNode(goToStatement);
		}
		
		public object VisitSingleLineSubLambdaExpression(SingleLineSubLambdaExpression singleLineSubLambdaExpression, object data)
		{
			DebugExpression(singleLineSubLambdaExpression);
			StartNode(singleLineSubLambdaExpression);
			
			WriteModifiers(singleLineSubLambdaExpression.ModifierTokens);
			WriteKeyword("Sub");
			WriteCommaSeparatedListInParenthesis(singleLineSubLambdaExpression.Parameters, false, CodeBracesRangeFlags.Parentheses);
			Space();
			singleLineSubLambdaExpression.EmbeddedStatement.AcceptVisitor(this, data);
			
			return EndNode(singleLineSubLambdaExpression);
		}
		
		public object VisitSingleLineFunctionLambdaExpression(SingleLineFunctionLambdaExpression singleLineFunctionLambdaExpression, object data)
		{
			DebugExpression(singleLineFunctionLambdaExpression);
			StartNode(singleLineFunctionLambdaExpression);

			var builder = singleLineFunctionLambdaExpression.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = formatter.NextPosition;
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			int start = formatter.NextPosition;
			WriteModifiers(singleLineFunctionLambdaExpression.ModifierTokens);
			WriteKeyword("Function");
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			WriteCommaSeparatedListInParenthesis(singleLineFunctionLambdaExpression.Parameters, false, CodeBracesRangeFlags.Parentheses);
			Space();
			singleLineFunctionLambdaExpression.EmbeddedExpression.AcceptVisitor(this, data);
			if (builder != null)
				builder.EndPosition = formatter.NextPosition;
			currentMethodRefs = oldRef;

			return EndNode(singleLineFunctionLambdaExpression);
		}
		
		public object VisitMultiLineLambdaExpression(MultiLineLambdaExpression multiLineLambdaExpression, object data)
		{
			StartNode(multiLineLambdaExpression);

			int start = formatter.NextPosition;
			int blockStart = start;
			var builder = multiLineLambdaExpression.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = start;
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			WriteModifiers(multiLineLambdaExpression.ModifierTokens);
			if (multiLineLambdaExpression.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			WriteCommaSeparatedListInParenthesis(multiLineLambdaExpression.Parameters, false, CodeBracesRangeFlags.Parentheses);
			multiLineLambdaExpression.Body.AcceptVisitor(this, data);
			start = formatter.NextPosition;
			lastEndBlockOffset = start;
			formatter.AddBlock(blockStart, formatter.NextPosition, CodeBracesRangeFlags.BlockKind_AnonymousMethod);
			WriteKeyword("End");
			if (multiLineLambdaExpression.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			if (builder != null)
				builder.EndPosition = formatter.NextPosition;
			formatter.AddHighlightedKeywordReference(currentMethodRefs.MethodReference, start, formatter.NextPosition);
			currentMethodRefs = oldRef;

			return EndNode(multiLineLambdaExpression);
		}
		
		public object VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			StartNode(queryExpression);
			
			foreach (var op in queryExpression.QueryOperators) {
				op.AcceptVisitor(this, data);
			}
			
			return EndNode(queryExpression);
		}
		
		public object VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			StartNode(continueStatement);
			
			int start = DebugStart(continueStatement, "Continue");
			
			switch (continueStatement.ContinueKind) {
				case ContinueKind.Do:
					WriteKeyword("Do");
					formatter.AddHighlightedKeywordReference(currentDoReference, start, formatter.NextPosition);
					break;
				case ContinueKind.For:
					WriteKeyword("For");
					formatter.AddHighlightedKeywordReference(currentForReference, start, formatter.NextPosition);
					break;
				case ContinueKind.While:
					WriteKeyword("While");
					formatter.AddHighlightedKeywordReference(currentWhileReference, start, formatter.NextPosition);
					break;
				default:
					throw new Exception("Invalid value for ContinueKind");
			}
			DebugEnd(continueStatement);
			
			return EndNode(continueStatement);
		}
		
		public object VisitExternalMethodDeclaration(ExternalMethodDeclaration externalMethodDeclaration, object data)
		{
			StartNode(externalMethodDeclaration);
			
			WriteAttributes(externalMethodDeclaration.Attributes);
			WriteModifiers(externalMethodDeclaration.ModifierTokens);
			WriteKeyword("Declare");
			switch (externalMethodDeclaration.CharsetModifier) {
				case CharsetModifier.None:
					break;
				case CharsetModifier.Auto:
					WriteKeyword("Auto");
					break;
				case CharsetModifier.Unicode:
					WriteKeyword("Unicode");
					break;
				case CharsetModifier.Ansi:
					WriteKeyword("Ansi");
					break;
				default:
					throw new Exception("Invalid value for CharsetModifier");
			}
			if (externalMethodDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			externalMethodDeclaration.Name.AcceptVisitor(this, data);
			WriteKeyword("Lib");
			Space();
			WritePrimitiveValue(externalMethodDeclaration.Library);
			Space();
			if (externalMethodDeclaration.Alias != null) {
				WriteKeyword("Alias");
				Space();
				WritePrimitiveValue(externalMethodDeclaration.Alias);
				Space();
			}
			WriteCommaSeparatedListInParenthesis(externalMethodDeclaration.Parameters, false, CodeBracesRangeFlags.Parentheses);
			if (!externalMethodDeclaration.IsSub && !externalMethodDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				Space();
				WriteAttributes(externalMethodDeclaration.ReturnTypeAttributes);
				externalMethodDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			SaveDeclarationOffset();
			NewLine();
			
			return EndNode(externalMethodDeclaration);
		}
		
		public static string ToVBNetString(PrimitiveExpression primitiveExpression)
		{
			var writer = new StringWriter();
			new OutputVisitor(writer, new VBFormattingOptions()).WritePrimitiveValue(primitiveExpression.Value);
			return writer.ToString();
		}
		
		public object VisitEmptyExpression(EmptyExpression emptyExpression, object data)
		{
			DebugExpression(emptyExpression);
			StartNode(emptyExpression);
			
			return EndNode(emptyExpression);
		}
		
		public object VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpression anonymousObjectCreationExpression, object data)
		{
			DebugExpression(anonymousObjectCreationExpression);
			StartNode(anonymousObjectCreationExpression);
			
			WriteKeyword("New");
			WriteKeyword("With");

			var braceHelper = BraceHelper.LeftBrace(this, CodeBracesRangeFlags.CurlyBraces);
			Space();
			WriteCommaSeparatedList(anonymousObjectCreationExpression.Initializer);
			Space();
			braceHelper.RightBrace();

			return EndNode(anonymousObjectCreationExpression);
		}
		
		public object VisitCollectionRangeVariableDeclaration(CollectionRangeVariableDeclaration collectionRangeVariableDeclaration, object data)
		{
			DebugExpression(collectionRangeVariableDeclaration);
			StartNode(collectionRangeVariableDeclaration);
			
			collectionRangeVariableDeclaration.Identifier.AcceptVisitor(this, data);
			if (!collectionRangeVariableDeclaration.Type.IsNull) {
				WriteKeyword("As");
				collectionRangeVariableDeclaration.Type.AcceptVisitor(this, data);
			}
			WriteKeyword("In");
			collectionRangeVariableDeclaration.Expression.AcceptVisitor(this, data);
			
			return EndNode(collectionRangeVariableDeclaration);
		}
		
		public object VisitFromQueryOperator(FromQueryOperator fromQueryOperator, object data)
		{
			DebugExpression(fromQueryOperator);
			StartNode(fromQueryOperator);
			
			WriteKeyword("From");
			WriteCommaSeparatedList(fromQueryOperator.Variables);
			
			return EndNode(fromQueryOperator);
		}
		
		public object VisitAggregateQueryOperator(AggregateQueryOperator aggregateQueryOperator, object data)
		{
			DebugExpression(aggregateQueryOperator);
			StartNode(aggregateQueryOperator);
			
			WriteKeyword("Aggregate");
			aggregateQueryOperator.Variable.AcceptVisitor(this, data);
			
			foreach (var operators in aggregateQueryOperator.SubQueryOperators) {
				operators.AcceptVisitor(this, data);
			}
			
			WriteKeyword("Into");
			WriteCommaSeparatedList(aggregateQueryOperator.IntoExpressions);
			
			return EndNode(aggregateQueryOperator);
		}
		
		public object VisitSelectQueryOperator(SelectQueryOperator selectQueryOperator, object data)
		{
			DebugExpression(selectQueryOperator);
			StartNode(selectQueryOperator);
			
			WriteKeyword("Select");
			WriteCommaSeparatedList(selectQueryOperator.Variables);
			
			return EndNode(selectQueryOperator);
		}
		
		public object VisitDistinctQueryOperator(DistinctQueryOperator distinctQueryOperator, object data)
		{
			DebugExpression(distinctQueryOperator);
			StartNode(distinctQueryOperator);
			
			WriteKeyword("Distinct");
			
			return EndNode(distinctQueryOperator);
		}
		
		public object VisitWhereQueryOperator(WhereQueryOperator whereQueryOperator, object data)
		{
			DebugExpression(whereQueryOperator);
			StartNode(whereQueryOperator);
			
			WriteKeyword("Where");
			whereQueryOperator.Condition.AcceptVisitor(this, data);
			
			return EndNode(whereQueryOperator);
		}
		
		public object VisitPartitionQueryOperator(PartitionQueryOperator partitionQueryOperator, object data)
		{
			DebugExpression(partitionQueryOperator);
			StartNode(partitionQueryOperator);
			
			switch (partitionQueryOperator.Kind) {
				case PartitionKind.Take:
					WriteKeyword("Take");
					break;
				case PartitionKind.TakeWhile:
					WriteKeyword("Take");
					WriteKeyword("While");
					break;
				case PartitionKind.Skip:
					WriteKeyword("Skip");
					break;
				case PartitionKind.SkipWhile:
					WriteKeyword("Skip");
					WriteKeyword("While");
					break;
				default:
					throw new Exception("Invalid value for PartitionKind");
			}
			
			partitionQueryOperator.Expression.AcceptVisitor(this, data);
			
			return EndNode(partitionQueryOperator);
		}
		
		public object VisitOrderExpression(OrderExpression orderExpression, object data)
		{
			DebugExpression(orderExpression);
			StartNode(orderExpression);
			
			orderExpression.Expression.AcceptVisitor(this, data);
			
			switch (orderExpression.Direction) {
				case QueryOrderingDirection.None:
					break;
				case QueryOrderingDirection.Ascending:
					WriteKeyword("Ascending");
					break;
				case QueryOrderingDirection.Descending:
					WriteKeyword("Descending");
					break;
				default:
					throw new Exception("Invalid value for QueryExpressionOrderingDirection");
			}
			
			return EndNode(orderExpression);
		}
		
		public object VisitOrderByQueryOperator(OrderByQueryOperator orderByQueryOperator, object data)
		{
			DebugExpression(orderByQueryOperator);
			StartNode(orderByQueryOperator);
			
			WriteKeyword("Order");
			WriteKeyword("By");
			WriteCommaSeparatedList(orderByQueryOperator.Expressions);
			
			return EndNode(orderByQueryOperator);
		}
		
		public object VisitLetQueryOperator(LetQueryOperator letQueryOperator, object data)
		{
			DebugExpression(letQueryOperator);
			StartNode(letQueryOperator);
			
			WriteKeyword("Let");
			WriteCommaSeparatedList(letQueryOperator.Variables);
			
			return EndNode(letQueryOperator);
		}
		
		public object VisitGroupByQueryOperator(GroupByQueryOperator groupByQueryOperator, object data)
		{
			DebugExpression(groupByQueryOperator);
			StartNode(groupByQueryOperator);
			
			WriteKeyword("Group");
			WriteCommaSeparatedList(groupByQueryOperator.GroupExpressions);
			WriteKeyword("By");
			WriteCommaSeparatedList(groupByQueryOperator.ByExpressions);
			WriteKeyword("Into");
			WriteCommaSeparatedList(groupByQueryOperator.IntoExpressions);
			
			return EndNode(groupByQueryOperator);
		}
		
		public object VisitJoinQueryOperator(JoinQueryOperator joinQueryOperator, object data)
		{
			DebugExpression(joinQueryOperator);
			StartNode(joinQueryOperator);
			
			WriteKeyword("Join");
			joinQueryOperator.JoinVariable.AcceptVisitor(this, data);
			if (!joinQueryOperator.SubJoinQuery.IsNull) {
				joinQueryOperator.SubJoinQuery.AcceptVisitor(this, data);
			}
			WriteKeyword("On");
			bool first = true;
			foreach (var cond in joinQueryOperator.JoinConditions) {
				if (first)
					first = false;
				else
					WriteKeyword("And");
				cond.AcceptVisitor(this, data);
			}
			
			return EndNode(joinQueryOperator);
		}
		
		public object VisitJoinCondition(JoinCondition joinCondition, object data)
		{
			DebugExpression(joinCondition);
			StartNode(joinCondition);
			
			joinCondition.Left.AcceptVisitor(this, data);
			WriteKeyword("Equals");
			joinCondition.Right.AcceptVisitor(this, data);
			
			return EndNode(joinCondition);
		}
		
		public object VisitGroupJoinQueryOperator(GroupJoinQueryOperator groupJoinQueryOperator, object data)
		{
			DebugExpression(groupJoinQueryOperator);
			StartNode(groupJoinQueryOperator);
			
			WriteKeyword("Group");
			WriteKeyword("Join");
			groupJoinQueryOperator.JoinVariable.AcceptVisitor(this, data);
			if (!groupJoinQueryOperator.SubJoinQuery.IsNull) {
				groupJoinQueryOperator.SubJoinQuery.AcceptVisitor(this, data);
			}
			WriteKeyword("On");
			bool first = true;
			foreach (var cond in groupJoinQueryOperator.JoinConditions) {
				if (first)
					first = false;
				else
					WriteKeyword("And");
				cond.AcceptVisitor(this, data);
			}
			WriteKeyword("Into");
			WriteCommaSeparatedList(groupJoinQueryOperator.IntoExpressions);
			
			return EndNode(groupJoinQueryOperator);
		}
		
		public object VisitAddRemoveHandlerStatement(AddRemoveHandlerStatement addRemoveHandlerStatement, object data)
		{
			DebugStart(addRemoveHandlerStatement);
			StartNode(addRemoveHandlerStatement);
			
			if (addRemoveHandlerStatement.IsAddHandler)
				WriteKeyword("AddHandler");
			else
				WriteKeyword("RemoveHandler");
			
			addRemoveHandlerStatement.EventExpression.AcceptVisitor(this, data);
			Comma(addRemoveHandlerStatement.DelegateExpression);
			addRemoveHandlerStatement.DelegateExpression.AcceptVisitor(this, data);
			DebugEnd(addRemoveHandlerStatement);
			
			return EndNode(addRemoveHandlerStatement);
		}
	}
}
