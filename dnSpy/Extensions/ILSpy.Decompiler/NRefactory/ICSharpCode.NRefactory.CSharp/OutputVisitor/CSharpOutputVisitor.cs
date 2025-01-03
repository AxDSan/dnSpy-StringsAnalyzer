// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp {
	/// <summary>
	/// Outputs the AST.
	/// </summary>
	public class CSharpOutputVisitor : IAstVisitor
	{
		readonly protected TokenWriter writer;
		readonly protected CSharpFormattingOptions policy;
		readonly protected Stack<AstNode> containerStack = new Stack<AstNode> ();
		CancellationToken cancellationToken;
		const int CANCEL_CHECK_LOOP_COUNT = 100;
		int lastBraceOffset;
		int lastDeclarationOffset;

		struct MethodRefs {
			public object MethodReference;
			public object AwaitReference;
			public static MethodRefs Create() => new MethodRefs {
				MethodReference = new object(),
				AwaitReference = new object(),
			};
		}
		MethodRefs currentMethodRefs;
		object currentIfReference;
		object currentLoopReference;// for, foreach, while, do..while
		object currentSwitchReference;
		object currentTryReference;
		object currentBreakReference;// either a loop ref or a switch ref
		int elseIfStart = -1;

		void SaveDeclarationOffset() => lastDeclarationOffset = writer.GetLocation() ?? 0;
		void SaveDeclarationOffset(int offset) => lastDeclarationOffset = offset;

		struct BraceHelper {
			readonly CSharpOutputVisitor owner;
			readonly CodeBracesRangeFlags flags;
			int leftStart, leftEnd;

			BraceHelper(CSharpOutputVisitor owner, CodeBracesRangeFlags flags) {
				this.owner = owner;
				this.leftStart = owner.writer.GetLocation() ?? 0;
				this.leftEnd = 0;
				this.flags = flags;
			}

			public static BraceHelper LeftParen(CSharpOutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken(Roles.LPar, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.writer.GetLocation() ?? 0;
				return bh;
			}

			public void RightParen() {
				int rightStart = owner.writer.GetLocation() ?? 0;
				owner.WriteToken(Roles.RPar, BoxedTextColor.Punctuation);
				int rightEnd = owner.writer.GetLocation() ?? 0;
				if (flags != 0)
					owner.writer.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftChevron(CSharpOutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken(Roles.LChevron, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.writer.GetLocation() ?? 0;
				return bh;
			}

			public void RightChevron() {
				int rightStart = owner.writer.GetLocation() ?? 0;
				owner.WriteToken(Roles.RChevron, BoxedTextColor.Punctuation);
				int rightEnd = owner.writer.GetLocation() ?? 0;
				if (flags != 0)
					owner.writer.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftBrace(CSharpOutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken(Roles.LBrace, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.writer.GetLocation() ?? 0;
				return bh;
			}

			public void RightBrace() {
				int rightStart = owner.writer.GetLocation() ?? 0;
				owner.lastBraceOffset = rightStart;
				owner.WriteToken(Roles.RBrace, BoxedTextColor.Punctuation);
				int rightEnd = owner.writer.GetLocation() ?? 0;
				if (flags != 0)
					owner.writer.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}

			public static BraceHelper LeftBracket(CSharpOutputVisitor owner, CodeBracesRangeFlags flags) {
				var bh = new BraceHelper(owner, flags);
				owner.WriteToken(Roles.LBracket, BoxedTextColor.Punctuation);
				bh.leftEnd = owner.writer.GetLocation() ?? 0;
				return bh;
			}

			public void RightBracket() {
				int rightStart = owner.writer.GetLocation() ?? 0;
				owner.WriteToken(Roles.RBracket, BoxedTextColor.Punctuation);
				int rightEnd = owner.writer.GetLocation() ?? 0;
				if (flags != 0)
					owner.writer.AddBracePair(leftStart, leftEnd, rightStart, rightEnd, flags);
			}
		}

		static CodeBracesRangeFlags GetTypeBlockKind(AstNode node) {
			var td = node.Annotation<TypeDef>();
			if (td != null) {
				if (td.IsInterface)
					return CodeBracesRangeFlags.InterfaceBraces;
				if (td.IsValueType)
					return CodeBracesRangeFlags.ValueTypeBraces;
			}
			return CodeBracesRangeFlags.TypeBraces;
		}

		public CSharpOutputVisitor (TextWriter textWriter, CSharpFormattingOptions formattingPolicy)
		{
			if (textWriter == null) {
				throw new ArgumentNullException ("textWriter");
			}
			if (formattingPolicy == null) {
				throw new ArgumentNullException ("formattingPolicy");
			}
			this.writer = TokenWriter.Create(textWriter);
			this.policy = formattingPolicy;
			this.cancellationToken = new CancellationToken();
		}

		public CSharpOutputVisitor (TokenWriter writer, CSharpFormattingOptions formattingPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (writer == null) {
				throw new ArgumentNullException ("writer");
			}
			if (formattingPolicy == null) {
				throw new ArgumentNullException ("formattingPolicy");
			}
			this.writer = new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(writer));
			this.policy = formattingPolicy;
			this.cancellationToken = cancellationToken;
		}

		#region StartNode/EndNode
		protected virtual void StartNode(AstNode node)
		{
			// Ensure that nodes are visited in the proper nested order.
			// Jumps to different subtrees are allowed only for the child of a placeholder node.
			Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek() || node == containerStack.Peek() || containerStack.Peek().NodeType == NodeType.Pattern);
			containerStack.Push(node);
			writer.StartNode(node);
		}

		protected virtual void EndNode(AstNode node)
		{
			Debug.Assert(node == containerStack.Peek());
			containerStack.Pop();
			writer.EndNode(node);
		}
		#endregion

		#region debug statements
		void DebugStart(AstNode node, int? start = null)
		{
			writer.DebugStart(node, start);
		}

		void DebugStartReference(AstNode node, TokenRole role, object reference, ref int keywordStartIndex)
		{
			int start = keywordStartIndex < 0 ? writer.GetLocation() ?? 0 : keywordStartIndex;
			keywordStartIndex = -1;
			WriteKeyword(role, node);
			int end = writer.GetLocation() ?? 0;
			writer.AddHighlightedKeywordReference(reference, start, end);
		}

		void DebugHidden(AstNode hiddenNode)
		{
			writer.DebugHidden(hiddenNode);
		}

		void DebugExpression(AstNode node)
		{
			writer.DebugExpression(node);
		}

		void SemicolonDebugEnd(AstNode node)
		{
			Semicolon(node);
		}

		void DebugEnd(AstNode node, bool addSelf = true)
		{
			DebugEnd(node, null, addSelf);
		}

		void DebugEnd(AstNode node, int? end, bool addSelf = true)
		{
			if (addSelf)
				writer.DebugExpression(node);
			writer.DebugEnd(node, end);
		}
		#endregion

		#region Comma
		/// <summary>
		/// Writes a comma.
		/// </summary>
		/// <param name="nextNode">The next node after the comma.</param>
		/// <param name="noSpaceAfterComma">When set prevents printing a space after comma.</param>
		protected virtual void Comma(AstNode nextNode, bool noSpaceAfterComma = false)
		{
			Space(policy.SpaceBeforeBracketComma);
			// TODO: Comma policy has changed.
			writer.WriteTokenPunctuation(Roles.Comma, ",");
			isAfterSpace = false;
			Space(!noSpaceAfterComma && policy.SpaceAfterBracketComma);
			// TODO: Comma policy has changed.
		}

		/// <summary>
		/// Writes an optional comma, e.g. at the end of an enum declaration or in an array initializer
		/// </summary>
		protected virtual void OptionalComma(AstNode pos)
		{
			// Look if there's a comma after the current node, and insert it if it exists.
			while (pos != null && pos.NodeType == NodeType.Whitespace) {
				pos = pos.NextSibling;
			}
			if (pos != null && pos.Role == Roles.Comma) {
				Comma(null, noSpaceAfterComma: true);
			}
		}

		/// <summary>
		/// Writes an optional semicolon, e.g. at the end of a type or namespace declaration.
		/// </summary>
		protected virtual void OptionalSemicolon(AstNode pos)
		{
			// Look if there's a semicolon after the current node, and insert it if it exists.
			while (pos != null && pos.NodeType == NodeType.Whitespace) {
				pos = pos.PrevSibling;
			}
			if (pos != null && pos.Role == Roles.Semicolon) {
				Semicolon();
			}
		}

		protected virtual void WriteCommaSeparatedList(IEnumerable<AstNode> list)
		{
			bool isFirst = true;
			int count = 0;
			foreach (AstNode node in list) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (isFirst) {
					isFirst = false;
				} else {
					Comma(node);
				}
				node.AcceptVisitor(this);
			}
		}

		protected virtual void WriteCommaSeparatedListInParenthesis(IEnumerable<AstNode> list, bool spaceWithin, CodeBracesRangeFlags flags)
		{
			var braceHelper = BraceHelper.LeftParen(this, flags);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			braceHelper.RightParen();
		}

		protected virtual void WriteCommaSeparatedListInBrackets(IEnumerable<ParameterDeclaration> list, bool spaceWithin, CodeBracesRangeFlags flags)
		{
			var braceHelper = BraceHelper.LeftBracket(this, flags);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			braceHelper.RightBracket();
		}

		protected virtual void WriteCommaSeparatedListInBrackets(IEnumerable<Expression> list, CodeBracesRangeFlags flags)
		{
			var braceHelper = BraceHelper.LeftBracket(this, flags);
			if (list.Any()) {
				Space(policy.SpacesWithinBrackets);
				WriteCommaSeparatedList(list);
				Space(policy.SpacesWithinBrackets);
			}
			braceHelper.RightBracket();
		}
		#endregion

		#region Write tokens
		protected bool isAtStartOfLine = true;
		protected bool isAfterSpace;

		void WriteKeywordReference(TokenRole tokenRole) => WriteKeywordReference(tokenRole, new object());

		void WriteKeywordReference(TokenRole tokenRole, object reference)
		{
			int start = writer.GetLocation() ?? 0;
			WriteKeyword(tokenRole, null);
			int end = writer.GetLocation() ?? 0;
			writer.AddHighlightedKeywordReference(reference, start, end);
		}

		void WriteKeywordReferences(TokenRole tokenRole1, TokenRole tokenRole2, object reference)
		{
			int start = writer.GetLocation() ?? 0;
			WriteKeyword(tokenRole1);
			WriteKeyword(tokenRole2);
			int end = writer.GetLocation() ?? 0;
			writer.AddHighlightedKeywordReference(reference, start, end);
		}

		/// <summary>
		/// Writes a keyword, and all specials up to
		/// </summary>
		protected virtual void WriteKeyword(TokenRole tokenRole, AstNode node = null)
		{
			WriteKeywordIdentifier(tokenRole.Token, tokenRole, node, false);
		}

		protected virtual void WriteKeyword(string token, Role tokenRole = null, AstNode node = null)
		{
			WriteKeywordIdentifier(token, tokenRole, node, false);
		}

		void WriteKeywordIdentifier(TokenRole tokenRole)
		{
			WriteKeywordIdentifier(tokenRole.Token, tokenRole, null, true);
		}

		void WriteKeywordIdentifier(string token, Role tokenRole, AstNode node = null, bool isId = true)
		{
			if (node != null)
				DebugStart(node);
			if (isId)
				writer.WriteIdentifier(Identifier.Create(token), BoxedTextColor.Keyword);
			else
				writer.WriteKeyword(tokenRole, token);
			isAtStartOfLine = false;
			isAfterSpace = false;
		}

		protected virtual void WriteIdentifier(Identifier identifier)
		{
			WriteIdentifier(identifier, (identifier.AnnotationVT<TextColor>() ?? TextColor.Text).Box());
		}

		void WriteIdentifier(Identifier identifier, object data)
		{
			writer.WriteIdentifier(identifier, data);
			isAtStartOfLine = false;
			isAfterSpace = false;
		}

		protected virtual void WriteIdentifier(string identifier, object data)
		{
			AstType.Create(identifier, data).AcceptVisitor(this);
			isAtStartOfLine = false;
			isAfterSpace = false;
		}

		protected void WriteTokenOperatorOrKeyword(string token, Role tokenRole)
		{
			object data = char.IsLetter(token[0]) ? BoxedTextColor.Keyword : BoxedTextColor.Operator;
			WriteToken(token, tokenRole, data);
		}

		protected virtual void WriteToken(TokenRole tokenRole, object data)
		{
			WriteToken(tokenRole.Token, tokenRole, data);
		}

		protected virtual void WriteToken(string token, Role tokenRole, object data)
		{
			writer.WriteToken(tokenRole, token, data);
			isAtStartOfLine = false;
			isAfterSpace = false;
		}

		/// <summary>
		/// Marks the end of a statement
		/// </summary>
		/// <param name="node">Statement node or null</param>
		protected virtual void Semicolon(AstNode node = null)
		{
			// get the role of the current node
			Role role = containerStack.Peek().Role;

			if (!SkipToken()) {
				WriteToken(Roles.Semicolon, BoxedTextColor.Punctuation);
				if (node != null)
					DebugEnd(node);
				if (!SkipNewLine())
					NewLine();
				else
					Space();
			}
			else if (node != null)
				DebugEnd(node);

			bool SkipToken()
			{
				return role == ForStatement.InitializerRole
					   || role == ForStatement.IteratorRole
					   || role == UsingStatement.ResourceAcquisitionRole;
			}

			bool SkipNewLine()
			{
				if (containerStack.Peek() is not Accessor accessor)
					return false;
				if (!(role == PropertyDeclaration.GetterRole || role == PropertyDeclaration.SetterRole))
					return false;
				bool isAutoProperty = accessor.Body.IsNull
									  && !accessor.Attributes.Any()
									  && policy.AutoPropertyFormatting == PropertyFormatting.SingleLine;
				return isAutoProperty;
			}
		}

		/// <summary>
		/// Writes a space depending on policy.
		/// </summary>
		protected virtual void Space(bool addSpace = true)
		{
			if (addSpace && !isAfterSpace) {
				writer.Space();
				isAfterSpace = true;
			}
		}

		protected virtual void NewLine()
		{
			writer.NewLine();
			isAtStartOfLine = true;
			isAfterSpace = false;
		}

		int GetCallChainLengthLimited(MemberReferenceExpression expr)
		{
			int callChainLength = 0;
			var node = expr;

			while (node.Target is InvocationExpression invocation && invocation.Target is MemberReferenceExpression mre && callChainLength < 4) {
				node = mre;
				callChainLength++;
			}
			return callChainLength;
		}

		int ShouldInsertNewLineWhenInMethodCallChain(MemberReferenceExpression expr)
		{
			int callChainLength = GetCallChainLengthLimited(expr);
			return callChainLength < 3 ? 0 : callChainLength;
		}

		protected virtual bool InsertNewLineWhenInMethodCallChain(MemberReferenceExpression expr)
		{
			int callChainLength = ShouldInsertNewLineWhenInMethodCallChain(expr);
			if (callChainLength == 0)
				return false;
			if (callChainLength == 3)
				writer.Indent();
			writer.NewLine();

			isAtStartOfLine = true;
			isAfterSpace = false;
			return true;
		}

		BraceHelper OpenBrace(BraceStyle style, CodeBracesRangeFlags flags, bool newLine = true)
		{
			int? start, end;
			return OpenBrace(style, flags, out start, out end, newLine);
		}

		void CloseBrace(BraceStyle style, BraceHelper braceHelper, bool saveDeclOffset, bool unindent = true)
		{
			int? start, end;
			CloseBrace(style, braceHelper, out start, out end, saveDeclOffset, unindent);
		}

		BraceHelper OpenBrace(BraceStyle style, CodeBracesRangeFlags flags, out int? start, out int? end, bool newLine = true)
		{
			BraceHelper braceHelper;
			switch (style) {
			case BraceStyle.EndOfLine:
				case BraceStyle.BannerStyle:
					if (!isAtStartOfLine)
						Space();
					start = writer.GetLocation();
					braceHelper = BraceHelper.LeftBrace(this, flags);
					end = writer.GetLocation();
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					start = writer.GetLocation();
					braceHelper = BraceHelper.LeftBrace(this, flags);
					end = writer.GetLocation();
					break;
				case BraceStyle.NextLine:
					if (!isAtStartOfLine)
						NewLine();
					start = writer.GetLocation();
					braceHelper = BraceHelper.LeftBrace(this, flags);
					end = writer.GetLocation();
					break;
				case BraceStyle.NextLineShifted:
					NewLine();
					writer.Indent();
					start = writer.GetLocation();
					braceHelper = BraceHelper.LeftBrace(this, flags);
					end = writer.GetLocation();
					NewLine();
					return braceHelper;
				case BraceStyle.NextLineShifted2:
					NewLine();
					writer.Indent();
					start = writer.GetLocation();
					braceHelper = BraceHelper.LeftBrace(this, flags);
					end = writer.GetLocation();
					break;
				default:
					throw new ArgumentOutOfRangeException ();
			}
			if (newLine) {
				writer.Indent();
				NewLine();
			}
			return braceHelper;
		}

		void CloseBrace(BraceStyle style, BraceHelper braceHelper, out int? start, out int? end, bool saveDeclOffset, bool unindent = true)
		{
			switch (style) {
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
					if (unindent)
						writer.Unindent();
					start = writer.GetLocation();
					braceHelper.RightBrace();
					if (saveDeclOffset)
						SaveDeclarationOffset();
					end = writer.GetLocation();
					isAtStartOfLine = false;
					break;
				case BraceStyle.BannerStyle:
				case BraceStyle.NextLineShifted:
					start = writer.GetLocation();
					braceHelper.RightBrace();
					if (saveDeclOffset)
						SaveDeclarationOffset();
					end = writer.GetLocation();
					isAtStartOfLine = false;
					if (unindent)
						writer.Unindent();
					break;
				case BraceStyle.NextLineShifted2:
					if (unindent)
						writer.Unindent();
					start = writer.GetLocation();
					braceHelper.RightBrace();
					if (saveDeclOffset)
						SaveDeclarationOffset();
					end = writer.GetLocation();
					isAtStartOfLine = false;
					if (unindent)
						writer.Unindent();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region IsKeyword Test
		static readonly HashSet<string> unconditionalKeywords = new HashSet<string> {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
			"char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
			"do", "double", "else", "enum", "event", "explicit", "extern", "false",
			"finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
			"in", "int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params", "private",
			"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
			"using", "virtual", "void", "volatile", "while"
		};
		static readonly HashSet<string> queryKeywords = new HashSet<string> {
			"from", "where", "join", "on", "equals", "into", "let", "orderby",
			"ascending", "descending", "select", "group", "by"
		};
		static readonly int maxKeywordLength = unconditionalKeywords.Concat(queryKeywords).Max(s => s.Length);

		/// <summary>
		/// Determines whether the specified identifier is a keyword in the given context.
		/// </summary>
		public static bool IsKeyword(string identifier, AstNode context)
		{
			// only 2-10 char lower-case identifiers can be keywords
			if (identifier.Length > maxKeywordLength || identifier.Length < 2 || identifier[0] < 'a') {
				return false;
			}
			if (unconditionalKeywords.Contains(identifier)) {
				return true;
			}
			if (queryKeywords.Contains(identifier)) {
				return context.Ancestors.Any(ancestor => ancestor is QueryExpression);
			}
			if (identifier == "await") {
				foreach (AstNode ancestor in context.Ancestors) {
					// with lambdas/anonymous methods,
					if (ancestor is LambdaExpression) {
						return ((LambdaExpression)ancestor).IsAsync;
					}
					if (ancestor is AnonymousMethodExpression) {
						return ((AnonymousMethodExpression)ancestor).IsAsync;
					}
					if (ancestor is EntityDeclaration) {
						return (((EntityDeclaration)ancestor).Modifiers & Modifiers.Async) == Modifiers.Async;
					}
				}
			}
			return false;
		}
		#endregion

		#region Write constructs
		protected virtual void WriteTypeArguments(IEnumerable<AstType> typeArguments, CodeBracesRangeFlags flags)
		{
			if (typeArguments.Any()) {
				var braceHelper = BraceHelper.LeftChevron(this, flags);
				WriteCommaSeparatedList(typeArguments);
				braceHelper.RightChevron();
			}
		}

		public virtual void WriteTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters, CodeBracesRangeFlags flags)
		{
			if (typeParameters.Any()) {
				var braceHelper = BraceHelper.LeftChevron(this, flags);
				WriteCommaSeparatedList(typeParameters);
				braceHelper.RightChevron();
			}
		}

		protected virtual void WriteModifiers(IEnumerable<CSharpModifierToken> modifierTokens, AstNode nextNode)
		{
			int count = 0;
			foreach (CSharpModifierToken modifier in modifierTokens) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				modifier.AcceptVisitor(this);
				Space();
			}
			// Needed if there are no modifiers so eg. a keyword (such as 'class') isn't written
			// before the comment.
			if (nextNode != null)
				writer.WriteSpecialsUpToNode(nextNode);
		}

		protected virtual void WriteQualifiedIdentifier(IEnumerable<Identifier> identifiers)
		{
			bool first = true;
			int count = 0;
			foreach (Identifier ident in identifiers) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (first) {
					first = false;
				} else {
					writer.WriteTokenOperator(Roles.Dot, ".");
				}
				writer.WriteIdentifier(ident, CSharpMetadataTextColorProvider.Instance.GetColor(ident.Annotation<object>()));
			}
		}

		/// <summary>
		/// Writes an embedded statement.
		/// </summary>
		/// <param name="embeddedStatement">The statement to write.</param>
		/// <param name="nlp">Determines whether a trailing newline should be written following a block.
		/// Non-blocks always write a trailing newline.</param>
		/// <remarks>
		/// Blocks may or may not write a leading newline depending on StatementBraceStyle.
		/// Non-blocks always write a leading newline.
		/// </remarks>
		protected virtual void WriteEmbeddedStatement(Statement embeddedStatement, NewLinePlacement nlp = NewLinePlacement.NewLine)
		{
			if (embeddedStatement.IsNull) {
				NewLine();
				return;
			}
			BlockStatement block = embeddedStatement as BlockStatement;
			if (block != null) {
				WriteBlock(block, policy.StatementBraceStyle);
				if (nlp == NewLinePlacement.SameLine) {
					Space(); // if not a trailing newline, then at least a trailing space
				} else {
					NewLine();
				}
			} else {
				NewLine();
				writer.Indent();
				embeddedStatement.AcceptVisitor(this);
				writer.Unindent();
			}
		}

		protected virtual void WriteMethodBody(BlockStatement body, BraceStyle style, bool newLine = true)
		{
			if (body.IsNull) {
				SaveDeclarationOffset();
				Semicolon();
			} else {
				WriteBlock(body, style);
				NewLine();
				SaveDeclarationOffset(lastBlockStatementEndOffset);
			}
		}

		protected virtual void WriteAttributes(IEnumerable<AttributeSection> attributes)
		{
			int count = 0;
			foreach (AttributeSection attr in attributes) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				attr.AcceptVisitor(this);
			}
		}

		protected virtual void WritePrivateImplementationType(AstType privateImplementationType)
		{
			if (!privateImplementationType.IsNull) {
				privateImplementationType.AcceptVisitor(this);
				WriteToken(Roles.Dot, BoxedTextColor.Operator);
			}
		}

		#endregion

		#region Expressions
		public virtual void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			DebugExpression(anonymousMethodExpression);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			StartNode(anonymousMethodExpression);
			var builder = anonymousMethodExpression.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			if (anonymousMethodExpression.IsAsync) {
				int start = writer.GetLocation() ?? 0;
				WriteKeyword(AnonymousMethodExpression.AsyncModifierRole);
				writer.AddHighlightedKeywordReference(currentMethodRefs.AwaitReference, start, writer.GetLocation() ?? 0);
				Space();
			}
			WriteKeyword(AnonymousMethodExpression.DelegateKeywordRole);
			if (anonymousMethodExpression.HasParameterList) {
				Space(policy.SpaceBeforeMethodDeclarationParentheses);
				WriteCommaSeparatedListInParenthesis(anonymousMethodExpression.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			}

			WriteBlock(anonymousMethodExpression.Body, policy.AnonymousMethodBraceStyle);
			if (builder != null && builder.EndPosition == null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(anonymousMethodExpression);
		}

		public virtual void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
		{
			DebugExpression(undocumentedExpression);
			StartNode(undocumentedExpression);
			switch (undocumentedExpression.UndocumentedExpressionType) {
				case UndocumentedExpressionType.ArgList:
				case UndocumentedExpressionType.ArgListAccess:
					WriteKeyword(UndocumentedExpression.ArglistKeywordRole);
					break;
				case UndocumentedExpressionType.MakeRef:
					WriteKeyword(UndocumentedExpression.MakerefKeywordRole);
					break;
				case UndocumentedExpressionType.RefType:
					WriteKeyword(UndocumentedExpression.ReftypeKeywordRole);
					break;
				case UndocumentedExpressionType.RefValue:
					WriteKeyword(UndocumentedExpression.RefvalueKeywordRole);
					break;
			}
			if (undocumentedExpression.UndocumentedExpressionType != UndocumentedExpressionType.ArgListAccess) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(undocumentedExpression.Arguments, policy.SpaceWithinMethodCallParentheses, CodeBracesRangeFlags.Parentheses);
			}
			EndNode(undocumentedExpression);
		}

		public virtual void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
		{
			DebugExpression(arrayCreateExpression);
			StartNode(arrayCreateExpression);
			WriteKeyword(ArrayCreateExpression.NewKeywordRole);
			arrayCreateExpression.Type.AcceptVisitor(this);
			if (arrayCreateExpression.Arguments.Count > 0) {
				WriteCommaSeparatedListInBrackets(arrayCreateExpression.Arguments, CodeBracesRangeFlags.SquareBrackets);
			}
			int count = 0;
			foreach (var specifier in arrayCreateExpression.AdditionalArraySpecifiers) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				specifier.AcceptVisitor(this);
			}
			arrayCreateExpression.Initializer.AcceptVisitor(this);
			EndNode(arrayCreateExpression);
		}

		public virtual void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			DebugExpression(arrayInitializerExpression);
			StartNode(arrayInitializerExpression);
			// "new List<int> { { 1 } }" and "new List<int> { 1 }" are the same semantically.
			// We also use the same AST for both: we always use two nested ArrayInitializerExpressions
			// for collection initializers, even if the user did not write nested brackets.
			// The output visitor will output nested braces only if they are necessary,
			// or if the braces tokens exist in the AST.
			bool bracesAreOptional = arrayInitializerExpression.Elements.Count == 1
				&& IsObjectOrCollectionInitializer(arrayInitializerExpression.Parent)
				&& !CanBeConfusedWithObjectInitializer(arrayInitializerExpression.Elements.Single());
			if (bracesAreOptional && arrayInitializerExpression.LBraceToken.IsNull) {
				arrayInitializerExpression.Elements.Single().AcceptVisitor(this);
			} else {
				PrintInitializerElements(arrayInitializerExpression.Elements, CodeBracesRangeFlags.OtherBlockBraces);
			}
			EndNode(arrayInitializerExpression);
		}

		protected bool CanBeConfusedWithObjectInitializer(Expression expr)
		{
			// "int a; new List<int> { a = 1 };" is an object initalizers and invalid, but
			// "int a; new List<int> { { a = 1 } };" is a valid collection initializer.
			AssignmentExpression ae = expr as AssignmentExpression;
			return ae != null && ae.Operator == AssignmentOperatorType.Assign;
		}

		protected bool IsObjectOrCollectionInitializer(AstNode node)
		{
			if (!(node is ArrayInitializerExpression)) {
				return false;
			}
			if (node.Parent is ObjectCreateExpression) {
				return node.Role == ObjectCreateExpression.InitializerRole;
			}
			if (node.Parent is NamedExpression) {
				return node.Role == Roles.Expression;
			}
			return false;
		}

		protected virtual void PrintInitializerElements(AstNodeCollection<Expression> elements, CodeBracesRangeFlags flags)
		{
			bool wrapAlways = policy.ArrayInitializerWrapping == Wrapping.WrapAlways
							 || (elements.Count > 1 && elements.Any(e => !IsSimpleExpression(e)))
							 || elements.Any(IsComplexExpression);
			bool wrap = wrapAlways
						|| elements.Count > 10;
			BraceStyle style = wrap ? policy.ArrayInitializerBraceStyle : BraceStyle.EndOfLine;
			var braceHelper = OpenBrace(style, flags, newLine: wrap);
			if (!wrap)
				Space();

			AstNode last = null;
			int count = 0;
			foreach (var (idx, node) in elements.WithIndex()) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (idx > 0) {
					Comma(node, noSpaceAfterComma: true);
					if (wrapAlways || idx % 10 == 0)
						NewLine();
					else
						Space();
				}
				last = node;
				node.AcceptVisitor(this);
			}
			if (last != null)
				OptionalComma(last.NextSibling);
			if (wrap)
				NewLine();
			else
				Space();
			CloseBrace(style, braceHelper, false, unindent: wrap);

			bool IsSimpleExpression(Expression ex)
			{
				switch (ex)
				{
				case NullReferenceExpression _:
				case ThisReferenceExpression _:
				case PrimitiveExpression _:
				case IdentifierExpression _:
				case MemberReferenceExpression { Target: ThisReferenceExpression or IdentifierExpression or BaseReferenceExpression }:
					return true;
				case MemberReferenceExpression memberRef when memberRef.Target is TypeReferenceExpression:
					return memberRef.MemberName == "MinValue" || memberRef.MemberName == "MaxValue" ||
						   memberRef.MemberName == "NaN" || memberRef.MemberName == "PositiveInfinity" ||
						   memberRef.MemberName == "NegativeInfinity" || memberRef.MemberName == "Epsilon";
				default:
					return false;
				}
			}

			bool IsComplexExpression(Expression ex)
			{
				switch (ex)
				{
				case AnonymousMethodExpression _:
				case LambdaExpression _:
				case AnonymousTypeCreateExpression _:
				case ObjectCreateExpression _:
				case NamedExpression _:
					return true;
				default:
					return false;
				}
			}
		}

		public virtual void VisitAsExpression(AsExpression asExpression)
		{
			DebugExpression(asExpression);
			StartNode(asExpression);
			asExpression.Expression.AcceptVisitor(this);
			Space();
			WriteKeyword(AsExpression.AsKeywordRole);
			Space();
			asExpression.Type.AcceptVisitor(this);
			EndNode(asExpression);
		}

		public virtual void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			DebugExpression(assignmentExpression);
			StartNode(assignmentExpression);
			assignmentExpression.Left.AcceptVisitor(this);
			Space(policy.SpaceAroundAssignment);
			WriteToken(AssignmentExpression.GetOperatorRole(assignmentExpression.Operator), BoxedTextColor.Operator);
			Space(policy.SpaceAroundAssignment);
			assignmentExpression.Right.AcceptVisitor(this);
			EndNode(assignmentExpression);
		}

		public virtual void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			DebugExpression(baseReferenceExpression);
			StartNode(baseReferenceExpression);
			WriteKeyword("base", baseReferenceExpression.Role);
			EndNode(baseReferenceExpression);
		}

		public virtual void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			DebugExpression(binaryOperatorExpression);
			StartNode(binaryOperatorExpression);
			binaryOperatorExpression.Left.AcceptVisitor(this);
			bool spacePolicy;
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ExclusiveOr:
					spacePolicy = policy.SpaceAroundBitwiseOperator;
					break;
				case BinaryOperatorType.ConditionalAnd:
				case BinaryOperatorType.ConditionalOr:
					spacePolicy = policy.SpaceAroundLogicalOperator;
					break;
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.LessThan:
					spacePolicy = policy.SpaceAroundRelationalOperator;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					spacePolicy = policy.SpaceAroundEqualityOperator;
					break;
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
					spacePolicy = policy.SpaceAroundAdditiveOperator;
					break;
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
					spacePolicy = policy.SpaceAroundMultiplicativeOperator;
					break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					spacePolicy = policy.SpaceAroundShiftOperator;
					break;
				case BinaryOperatorType.NullCoalescing:
					spacePolicy = true;
					break;
				default:
					throw new NotSupportedException ("Invalid value for BinaryOperatorType");
			}
			Space(spacePolicy);
			WriteToken(BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator), BoxedTextColor.Operator);
			Space(spacePolicy);
			binaryOperatorExpression.Right.AcceptVisitor(this);
			EndNode(binaryOperatorExpression);
		}

		public virtual void VisitCastExpression(CastExpression castExpression)
		{
			DebugExpression(castExpression);
			StartNode(castExpression);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinCastParentheses);
			castExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinCastParentheses);
			braceHelper.RightParen();
			Space(policy.SpaceAfterTypecast);
			castExpression.Expression.AcceptVisitor(this);
			EndNode(castExpression);
		}

		public virtual void VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			DebugExpression(checkedExpression);
			StartNode(checkedExpression);
			WriteKeywordReference(CheckedExpression.CheckedKeywordRole);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			checkedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			braceHelper.RightParen();
			EndNode(checkedExpression);
		}

		public virtual void VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			DebugExpression(conditionalExpression);
			StartNode(conditionalExpression);

			if (conditionalExpression.TrueExpression is DirectionExpression) {
				WriteKeyword(DirectionExpression.RefKeywordRole);
				Space();
			}

			conditionalExpression.Condition.AcceptVisitor(this);

			Space(policy.SpaceBeforeConditionalOperatorCondition);
			WriteToken(ConditionalExpression.QuestionMarkRole, BoxedTextColor.Operator);
			Space(policy.SpaceAfterConditionalOperatorCondition);

			conditionalExpression.TrueExpression.AcceptVisitor(this);

			Space(policy.SpaceBeforeConditionalOperatorSeparator);
			WriteToken(ConditionalExpression.ColonRole, BoxedTextColor.Operator);
			Space(policy.SpaceAfterConditionalOperatorSeparator);

			conditionalExpression.FalseExpression.AcceptVisitor(this);

			EndNode(conditionalExpression);
		}

		public virtual void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
		{
			DebugExpression(defaultValueExpression);
			StartNode(defaultValueExpression);

			WriteKeyword(DefaultValueExpression.DefaultKeywordRole);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinTypeOfParentheses);
			defaultValueExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinTypeOfParentheses);
			braceHelper.RightParen();

			EndNode(defaultValueExpression);
		}

		public virtual void VisitDirectionExpression(DirectionExpression directionExpression)
		{
			DebugExpression(directionExpression);
			StartNode(directionExpression);

			switch (directionExpression.FieldDirection) {
				case FieldDirection.In:
					break;
				case FieldDirection.Out:
					WriteKeyword(DirectionExpression.OutKeywordRole);
					Space();
					break;
				case FieldDirection.Ref:
					WriteKeyword(DirectionExpression.RefKeywordRole);
					Space();
					break;
				default:
					throw new NotSupportedException ("Invalid value for FieldDirection");
			}
			directionExpression.Expression.AcceptVisitor(this);

			EndNode(directionExpression);
		}

		public virtual void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			DebugExpression(identifierExpression);
			StartNode(identifierExpression);
			WriteIdentifier(identifierExpression.IdentifierToken, CSharpMetadataTextColorProvider.Instance.GetColor(identifierExpression.IdentifierToken.Annotation<object>()));
			WriteTypeArguments(identifierExpression.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			EndNode(identifierExpression);
		}

		public virtual void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			DebugExpression(indexerExpression);
			StartNode(indexerExpression);
			indexerExpression.Target.AcceptVisitor(this);
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInBrackets(indexerExpression.Arguments, CodeBracesRangeFlags.SquareBrackets);
			EndNode(indexerExpression);
		}

		public virtual void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			DebugExpression(invocationExpression);
			StartNode(invocationExpression);
			invocationExpression.Target.AcceptVisitor(this);
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInParenthesis(invocationExpression.Arguments, policy.SpaceWithinMethodCallParentheses, CodeBracesRangeFlags.Parentheses);
			if (!(invocationExpression.Parent is MemberReferenceExpression)) {
				if (invocationExpression.Target is MemberReferenceExpression mre) {
					if (ShouldInsertNewLineWhenInMethodCallChain(mre) >= 3)
						writer.Unindent();
				}
			}
			EndNode(invocationExpression);
		}

		public virtual void VisitIsExpression(IsExpression isExpression)
		{
			DebugExpression(isExpression);
			StartNode(isExpression);
			isExpression.Expression.AcceptVisitor(this);
			Space();
			WriteKeyword(IsExpression.IsKeywordRole);
			isExpression.Type.AcceptVisitor(this);
			EndNode(isExpression);
		}

		public virtual void VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			DebugExpression(lambdaExpression);
			StartNode(lambdaExpression);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			var builder = lambdaExpression.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			if (lambdaExpression.IsAsync) {
				int start = writer.GetLocation() ?? 0;
				WriteKeyword(LambdaExpression.AsyncModifierRole);
				writer.AddHighlightedKeywordReference(currentMethodRefs.AwaitReference, start, writer.GetLocation() ?? 0);
				Space();
			}
			if (LambdaNeedsParenthesis(lambdaExpression)) {
				WriteCommaSeparatedListInParenthesis(lambdaExpression.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			} else {
				lambdaExpression.Parameters.Single().AcceptVisitor(this);
			}
			Space();
			WriteToken(LambdaExpression.ArrowRole, BoxedTextColor.Operator);

			if (lambdaExpression.Body is BlockStatement) {
				StartNode(lambdaExpression.Body);
				DebugStart(lambdaExpression.Body);
				WriteBlock((BlockStatement)lambdaExpression.Body, policy.AnonymousMethodBraceStyle);
			} else {
				Space();
				StartNode(lambdaExpression.Body);
				DebugStart(lambdaExpression.Body);
				lambdaExpression.Body.AcceptVisitor(this);
			}

			DebugEnd(lambdaExpression.Body);
			EndNode(lambdaExpression.Body);

			if (builder != null && builder.EndPosition == null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(lambdaExpression);
		}

		protected bool LambdaNeedsParenthesis(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1) {
				return true;
			}
			var p = lambdaExpression.Parameters.Single();
			return !(p.Type.IsNull && p.ParameterModifier == ParameterModifier.None);
		}

		public virtual void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			DebugExpression(memberReferenceExpression);
			StartNode(memberReferenceExpression);
			memberReferenceExpression.Target.AcceptVisitor(this);
			bool insertedNewLine = InsertNewLineWhenInMethodCallChain(memberReferenceExpression);
			WriteToken(Roles.Dot, BoxedTextColor.Operator);
			WriteIdentifier(memberReferenceExpression.MemberNameToken, CSharpMetadataTextColorProvider.Instance.GetColor(memberReferenceExpression.MemberNameToken.Annotation<object>() ?? memberReferenceExpression.Annotation<object>()));
			WriteTypeArguments(memberReferenceExpression.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			if (insertedNewLine && !(memberReferenceExpression.Parent is InvocationExpression && memberReferenceExpression.Role == Roles.TargetExpression)) {
				writer.Unindent();
			}
			EndNode(memberReferenceExpression);
		}

		public virtual void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			DebugExpression(namedArgumentExpression);
			StartNode(namedArgumentExpression);
			WriteIdentifier(namedArgumentExpression.NameToken);
			WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			Space();
			namedArgumentExpression.Expression.AcceptVisitor(this);
			EndNode(namedArgumentExpression);
		}

		public virtual void VisitNamedExpression(NamedExpression namedExpression)
		{
			DebugExpression(namedExpression);
			StartNode(namedExpression);
			WriteIdentifier(namedExpression.NameToken);
			Space();
			WriteToken(Roles.Assign, BoxedTextColor.Operator);
			Space();
			namedExpression.Expression.AcceptVisitor(this);
			EndNode(namedExpression);
		}

		public virtual void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
		{
			DebugExpression(nullReferenceExpression);
			StartNode(nullReferenceExpression);
			writer.WritePrimitiveValue(null);
			isAfterSpace = false;
			EndNode(nullReferenceExpression);
		}

		public virtual void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			DebugExpression(objectCreateExpression);
			StartNode(objectCreateExpression);
			WriteKeyword(ObjectCreateExpression.NewKeywordRole);
			objectCreateExpression.Type.AcceptVisitor(this);
			bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
			// also use parenthesis if there is an '(' token
			if (!objectCreateExpression.LParToken.IsNull) {
				useParenthesis = true;
			}
			if (useParenthesis) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(objectCreateExpression.Arguments, policy.SpaceWithinMethodCallParentheses, CodeBracesRangeFlags.Parentheses);
			}
			objectCreateExpression.Initializer.AcceptVisitor(this);
			EndNode(objectCreateExpression);
		}

		public virtual void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			DebugExpression(anonymousTypeCreateExpression);
			StartNode(anonymousTypeCreateExpression);
			WriteKeyword(AnonymousTypeCreateExpression.NewKeywordRole);
			PrintInitializerElements(anonymousTypeCreateExpression.Initializers, CodeBracesRangeFlags.OtherBlockBraces);
			EndNode(anonymousTypeCreateExpression);
		}

		public virtual void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			DebugExpression(parenthesizedExpression);
			StartNode(parenthesizedExpression);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinParentheses);
			parenthesizedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinParentheses);
			braceHelper.RightParen();
			EndNode(parenthesizedExpression);
		}

		public virtual void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			DebugExpression(pointerReferenceExpression);
			StartNode(pointerReferenceExpression);
			pointerReferenceExpression.Target.AcceptVisitor(this);
			WriteToken(PointerReferenceExpression.ArrowRole, BoxedTextColor.Operator);
			WriteIdentifier(pointerReferenceExpression.MemberNameToken, CSharpMetadataTextColorProvider.Instance.GetColor(pointerReferenceExpression.MemberNameToken.Annotation<object>()));
			WriteTypeArguments(pointerReferenceExpression.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			EndNode(pointerReferenceExpression);
		}

		#region VisitPrimitiveExpression
		public virtual void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			DebugExpression(primitiveExpression);
			StartNode(primitiveExpression);
			writer.WritePrimitiveValue(primitiveExpression.Value, BoxedTextColor.Text, primitiveExpression.UnsafeLiteralValue);
			isAfterSpace = false;
			EndNode(primitiveExpression);
		}
		#endregion

		public virtual void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			DebugExpression(sizeOfExpression);
			StartNode(sizeOfExpression);

			WriteKeyword(SizeOfExpression.SizeofKeywordRole);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinSizeOfParentheses);
			sizeOfExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinSizeOfParentheses);
			braceHelper.RightParen();

			EndNode(sizeOfExpression);
		}

		public virtual void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			DebugExpression(stackAllocExpression);
			StartNode(stackAllocExpression);
			WriteKeyword(StackAllocExpression.StackallocKeywordRole);
			stackAllocExpression.Type.AcceptVisitor(this);
			WriteCommaSeparatedListInBrackets(new[] { stackAllocExpression.CountExpression }, CodeBracesRangeFlags.SquareBrackets);
			EndNode(stackAllocExpression);
		}

		public virtual void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			DebugExpression(thisReferenceExpression);
			StartNode(thisReferenceExpression);
			WriteKeyword("this", thisReferenceExpression.Role);
			EndNode(thisReferenceExpression);
		}

		public virtual void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			DebugExpression(typeOfExpression);
			StartNode(typeOfExpression);

			WriteKeyword(TypeOfExpression.TypeofKeywordRole);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinTypeOfParentheses);
			typeOfExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinTypeOfParentheses);
			braceHelper.RightParen();

			EndNode(typeOfExpression);
		}

		public virtual void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			DebugExpression(typeReferenceExpression);
			StartNode(typeReferenceExpression);
			typeReferenceExpression.Type.AcceptVisitor(this);
			EndNode(typeReferenceExpression);
		}

		public virtual void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			DebugExpression(unaryOperatorExpression);
			StartNode(unaryOperatorExpression);
			UnaryOperatorType opType = unaryOperatorExpression.Operator;
			var opSymbol = UnaryOperatorExpression.GetOperatorRole(opType);
			if (opType == UnaryOperatorType.Await) {
				int start = writer.GetLocation() ?? 0;
				WriteKeyword(opSymbol);
				writer.AddHighlightedKeywordReference(currentMethodRefs.AwaitReference, start, writer.GetLocation() ?? 0);
				Space();
			} else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)) {
				WriteToken(opSymbol, BoxedTextColor.Operator);
			}
			unaryOperatorExpression.Expression.AcceptVisitor(this);
			if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement) {
				WriteToken(opSymbol, BoxedTextColor.Operator);
			}
			EndNode(unaryOperatorExpression);
		}

		public virtual void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			DebugExpression(uncheckedExpression);
			StartNode(uncheckedExpression);
			WriteKeywordReference(UncheckedExpression.UncheckedKeywordRole);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			uncheckedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			braceHelper.RightParen();
			EndNode(uncheckedExpression);
		}

		#endregion

		#region Query Expressions
		public virtual void VisitQueryExpression(QueryExpression queryExpression)
		{
			DebugExpression(queryExpression);
			StartNode(queryExpression);
			if (queryExpression.Role != QueryContinuationClause.PrecedingQueryRole)
				writer.Indent();
			bool first = true;
			int count = 0;
			foreach (var clause in queryExpression.Clauses) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (first) {
					first = false;
				} else {
					if (!(clause is QueryContinuationClause)) {
						NewLine();
					}
				}
				clause.AcceptVisitor(this);
			}
			if (queryExpression.Role != QueryContinuationClause.PrecedingQueryRole)
				writer.Unindent();
			EndNode(queryExpression);
		}

		public virtual void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			DebugExpression(queryContinuationClause);
			StartNode(queryContinuationClause);
			queryContinuationClause.PrecedingQuery.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryContinuationClause.IntoKeywordRole);
			Space();
			WriteIdentifier(queryContinuationClause.IdentifierToken);
			EndNode(queryContinuationClause);
		}

		public virtual void VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			DebugExpression(queryFromClause);
			StartNode(queryFromClause);
			WriteKeyword(QueryFromClause.FromKeywordRole);
			queryFromClause.Type.AcceptVisitor(this);
			Space();
			WriteIdentifier(queryFromClause.IdentifierToken);
			Space();
			WriteKeyword(QueryFromClause.InKeywordRole);
			Space();
			queryFromClause.Expression.AcceptVisitor(this);
			EndNode(queryFromClause);
		}

		public virtual void VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			DebugExpression(queryLetClause);
			StartNode(queryLetClause);
			WriteKeyword(QueryLetClause.LetKeywordRole);
			Space();
			WriteIdentifier(queryLetClause.IdentifierToken);
			Space(policy.SpaceAroundAssignment);
			WriteToken(Roles.Assign, BoxedTextColor.Operator);
			Space(policy.SpaceAroundAssignment);
			queryLetClause.Expression.AcceptVisitor(this);
			EndNode(queryLetClause);
		}

		public virtual void VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			DebugExpression(queryWhereClause);
			StartNode(queryWhereClause);
			WriteKeyword(QueryWhereClause.WhereKeywordRole);
			Space();
			queryWhereClause.Condition.AcceptVisitor(this);
			EndNode(queryWhereClause);
		}

		public virtual void VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			DebugExpression(queryJoinClause);
			StartNode(queryJoinClause);
			WriteKeyword(QueryJoinClause.JoinKeywordRole);
			queryJoinClause.Type.AcceptVisitor(this);
			Space();
			WriteIdentifier(queryJoinClause.JoinIdentifierToken, CSharpMetadataTextColorProvider.Instance.GetColor(queryJoinClause.JoinIdentifierToken.Annotation<object>()));
			Space();
			WriteKeyword(QueryJoinClause.InKeywordRole);
			Space();
			queryJoinClause.InExpression.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryJoinClause.OnKeywordRole);
			Space();
			queryJoinClause.OnExpression.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryJoinClause.EqualsKeywordRole);
			Space();
			queryJoinClause.EqualsExpression.AcceptVisitor(this);
			if (queryJoinClause.IsGroupJoin) {
				Space();
				WriteKeyword(QueryJoinClause.IntoKeywordRole);
				WriteIdentifier(queryJoinClause.IntoIdentifierToken, CSharpMetadataTextColorProvider.Instance.GetColor(queryJoinClause.IntoIdentifierToken.Annotation<object>()));
			}
			EndNode(queryJoinClause);
		}

		public virtual void VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			DebugExpression(queryOrderClause);
			StartNode(queryOrderClause);
			WriteKeyword(QueryOrderClause.OrderbyKeywordRole);
			Space();
			WriteCommaSeparatedList(queryOrderClause.Orderings);
			EndNode(queryOrderClause);
		}

		public virtual void VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			DebugExpression(queryOrdering);
			StartNode(queryOrdering);
			queryOrdering.Expression.AcceptVisitor(this);
			switch (queryOrdering.Direction) {
				case QueryOrderingDirection.Ascending:
					Space();
					WriteKeyword(QueryOrdering.AscendingKeywordRole);
					break;
				case QueryOrderingDirection.Descending:
					Space();
					WriteKeyword(QueryOrdering.DescendingKeywordRole);
					break;
			}
			EndNode(queryOrdering);
		}

		public virtual void VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			DebugExpression(querySelectClause);
			StartNode(querySelectClause);
			WriteKeyword(QuerySelectClause.SelectKeywordRole);
			Space();
			querySelectClause.Expression.AcceptVisitor(this);
			EndNode(querySelectClause);
		}

		public virtual void VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			DebugExpression(queryGroupClause);
			StartNode(queryGroupClause);
			WriteKeyword(QueryGroupClause.GroupKeywordRole);
			Space();
			queryGroupClause.Projection.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryGroupClause.ByKeywordRole);
			Space();
			queryGroupClause.Key.AcceptVisitor(this);
			EndNode(queryGroupClause);
		}

		#endregion

		#region GeneralScope
		public virtual void VisitAttribute(Attribute attribute)
		{
			StartNode(attribute);
			attribute.Type.AcceptVisitor(this);
			if (attribute.Arguments.Count != 0 || attribute.HasArgumentList) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(attribute.Arguments, policy.SpaceWithinMethodCallParentheses, CodeBracesRangeFlags.Parentheses);
			}
			EndNode(attribute);
		}

		public virtual void VisitAttributeSection(AttributeSection attributeSection)
		{
			StartNode(attributeSection);
			var braceHelper = BraceHelper.LeftBracket(this, CodeBracesRangeFlags.SquareBrackets);
			if (!string.IsNullOrEmpty(attributeSection.AttributeTarget)) {
				WriteKeyword(attributeSection.AttributeTarget, Roles.Identifier);
				WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
				Space();
			}
			WriteCommaSeparatedList(attributeSection.Attributes);
			braceHelper.RightBracket();
			switch (attributeSection.Parent) {
			case ParameterDeclaration _:
			case TypeParameterDeclaration _:
			case ComposedType _:
				Space();
				break;
			default:
				NewLine();
				break;
			}
			EndNode(attributeSection);
		}

		public virtual void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			StartNode(delegateDeclaration);
			WriteAttributes(delegateDeclaration.Attributes);
			WriteModifiers(delegateDeclaration.ModifierTokens, delegateDeclaration.ReturnType);
			WriteKeyword(Roles.DelegateKeyword);
			delegateDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteIdentifier(delegateDeclaration.NameToken);
			WriteTypeParameters(delegateDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			Space(policy.SpaceBeforeDelegateDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(delegateDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			int count = 0;
			foreach (Constraint constraint in delegateDeclaration.Constraints) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				constraint.AcceptVisitor(this);
			}
			SaveDeclarationOffset();
			Semicolon();
			EndNode(delegateDeclaration);
		}

		public virtual void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			StartNode(namespaceDeclaration);
			WriteKeyword(Roles.NamespaceKeyword);
			namespaceDeclaration.NamespaceName.AcceptVisitor (this);
			var braceHelper = OpenBrace(policy.NamespaceBraceStyle, CodeBracesRangeFlags.NamespaceBraces);
			int count = 0;
			int total = -1;
			foreach (var member in namespaceDeclaration.Members) {
				total++;
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (total > 0)
					writer.AddLineSeparator(Math.Max(lastBraceOffset, lastDeclarationOffset));
				member.AcceptVisitor(this);
				MaybeNewLinesAfterUsings(member);
			}
			CloseBrace(policy.NamespaceBraceStyle, braceHelper, true);
			OptionalSemicolon(namespaceDeclaration.LastChild);
			NewLine();
			EndNode(namespaceDeclaration);
		}

		public virtual void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			StartNode(typeDeclaration);
			WriteAttributes(typeDeclaration.Attributes);
			WriteModifiers(typeDeclaration.ModifierTokens, typeDeclaration.NameToken);
			BraceStyle braceStyle;
			switch (typeDeclaration.ClassType) {
				case ClassType.Enum:
					WriteKeyword(Roles.EnumKeyword);
					braceStyle = policy.EnumBraceStyle;
					break;
				case ClassType.Interface:
					WriteKeyword(Roles.InterfaceKeyword);
					braceStyle = policy.InterfaceBraceStyle;
					break;
				case ClassType.Struct:
					WriteKeyword(Roles.StructKeyword);
					braceStyle = policy.StructBraceStyle;
					break;
				default:
					WriteKeyword(Roles.ClassKeyword);
					braceStyle = policy.ClassBraceStyle;
					break;
			}
			WriteIdentifier(typeDeclaration.NameToken);
			WriteTypeParameters(typeDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			if (typeDeclaration.BaseTypes.Any()) {
				Space();
				WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
				Space();
				WriteCommaSeparatedList(typeDeclaration.BaseTypes);
			}
			int count = 0;
			foreach (Constraint constraint in typeDeclaration.Constraints) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				constraint.AcceptVisitor(this);
			}
			var braceHelper = OpenBrace(braceStyle, GetTypeBlockKind(typeDeclaration));
			if (typeDeclaration.ClassType == ClassType.Enum) {
				bool first = true;
				AstNode last = null;
				count = 0;
				foreach (var member in typeDeclaration.Members) {
					if (count-- <= 0) {
						cancellationToken.ThrowIfCancellationRequested();
						count = CANCEL_CHECK_LOOP_COUNT;
					}
					if (first) {
						first = false;
					} else {
						Comma(member, noSpaceAfterComma: true);
						NewLine();
					}
					last = member;
					member.AcceptVisitor(this);
				}
				if (last != null)
					OptionalComma(last.NextSibling);
				NewLine();
			} else {
				bool first = true;
				count = 0;
				AstNode lastMember = null;
				foreach (var member in typeDeclaration.Members) {
					if (count-- <= 0) {
						cancellationToken.ThrowIfCancellationRequested();
						count = CANCEL_CHECK_LOOP_COUNT;
					}
					if (!first) {
						for (int i = 0; i < policy.MinimumBlankLinesBetweenMembers; i++)
							NewLine();
					}
					first = false;
					if (!IsSameGroup(lastMember, member))
						writer.AddLineSeparator(Math.Max(lastBraceOffset, lastDeclarationOffset));
					member.AcceptVisitor(this);
					lastMember = member;
				}
			}
			CloseBrace(braceStyle, braceHelper, true);
			OptionalSemicolon(typeDeclaration.LastChild);
			NewLine();
			EndNode(typeDeclaration);
		}

		bool IsSameGroup(AstNode a, AstNode b) {
			if (a == null)
				return true;
			if (a is FieldDeclaration)
				return b is FieldDeclaration;
			return false;
		}

		public virtual void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
		{
			StartNode(usingAliasDeclaration);
			WriteKeyword(UsingAliasDeclaration.UsingKeywordRole);
			WriteIdentifier(usingAliasDeclaration.GetChildByRole(UsingAliasDeclaration.AliasRole), BoxedTextColor.Text);
			Space(policy.SpaceAroundEqualityOperator);
			WriteToken(Roles.Assign, BoxedTextColor.Operator);
			Space(policy.SpaceAroundEqualityOperator);
			usingAliasDeclaration.Import.AcceptVisitor(this);
			SaveDeclarationOffset();
			Semicolon();
			EndNode(usingAliasDeclaration);
		}

		public virtual void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			StartNode(usingDeclaration);
			WriteKeyword(UsingDeclaration.UsingKeywordRole);
			usingDeclaration.Import.AcceptVisitor(this);
			SaveDeclarationOffset();
			Semicolon();
			EndNode(usingDeclaration);
		}

		public virtual void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			StartNode(externAliasDeclaration);
			WriteKeyword(Roles.ExternKeyword);
			Space();
			WriteKeyword(Roles.AliasKeyword);
			Space();
			WriteIdentifier(externAliasDeclaration.NameToken);
			SaveDeclarationOffset();
			Semicolon();
			EndNode(externAliasDeclaration);
		}

		#endregion

		#region Statements
		public virtual void VisitBlockStatement(BlockStatement blockStatement)
		{
			WriteBlock(blockStatement, policy.StatementBraceStyle);
			NewLine();
		}

		/// <summary>
		/// Writes a block statement.
		/// Similar to VisitBlockStatement() except that:
		/// 1) it allows customizing the BraceStyle
		/// 2) it does not write a trailing newline after the '}' (this job is left to the caller)
		/// </summary>
		protected virtual void WriteBlock(BlockStatement blockStatement, BraceStyle style)
		{
			StartNode(blockStatement);
			CodeBracesRangeFlags flags;
			MethodDebugInfoBuilder builder = null;
			if (blockStatement.Parent is AnonymousMethodExpression || blockStatement.Parent is LambdaExpression) {
				flags = CodeBracesRangeFlags.AnonymousMethodBraces;
				builder = blockStatement.Parent.Annotation<MethodDebugInfoBuilder>();
			} else if (blockStatement.Parent is ConstructorDeclaration) {
				flags = CodeBracesRangeFlags.ConstructorBraces;
			} else if (blockStatement.Parent is DestructorDeclaration) {
				flags = CodeBracesRangeFlags.DestructorBraces;
			} else if (blockStatement.Parent is OperatorDeclaration) {
				flags = CodeBracesRangeFlags.OperatorBraces;
			} else if (blockStatement.Parent is MethodDeclaration) {
				flags = CodeBracesRangeFlags.MethodBraces;
			} else if (blockStatement.Parent is Accessor) {
				flags = CodeBracesRangeFlags.AccessorBraces;
			} else if (blockStatement.Parent is ForeachStatement || blockStatement.Parent is ForStatement ||
					   blockStatement.Parent is DoWhileStatement || blockStatement.Parent is WhileStatement) {
				flags = CodeBracesRangeFlags.LoopBraces;
			} else if (blockStatement.Parent is IfElseStatement) {
				flags = CodeBracesRangeFlags.ConditionalBraces;
			} else if (blockStatement.Parent is TryCatchStatement) {
				var stmt = (TryCatchStatement)blockStatement.Parent;
				if (stmt.TryBlock == blockStatement)
					flags = CodeBracesRangeFlags.TryBraces;
				else if (stmt.FinallyBlock == blockStatement)
					flags = CodeBracesRangeFlags.FinallyBraces;
				else
					flags = CodeBracesRangeFlags.OtherBlockBraces;
			} else if (blockStatement.Parent is CatchClause) {
				flags = CodeBracesRangeFlags.CatchBraces;
			} else if (blockStatement.Parent is LockStatement) {
				flags = CodeBracesRangeFlags.LockBraces;
			} else if (blockStatement.Parent is UsingStatement) {
				flags = CodeBracesRangeFlags.UsingBraces;
			} else if (blockStatement.Parent is FixedStatement) {
				flags = CodeBracesRangeFlags.FixedBraces;
			} else if (blockStatement.Parent is SwitchSection) {
				flags = CodeBracesRangeFlags.CaseBraces;
			} else {
				flags = CodeBracesRangeFlags.OtherBlockBraces;
			}
			int? start, end;
			var braceHelper = OpenBrace(style, flags, out start, out end);
			if (blockStatement.HiddenStart != null) {
				DebugStart(blockStatement, start);
				DebugHidden(blockStatement.HiddenStart);
				DebugEnd(blockStatement, end);
			}
			int count = 0;
			foreach (var node in blockStatement.Statements) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				node.AcceptVisitor(this);
			}
			CloseBrace(style, braceHelper, out start, out end, false);
			EndNode(blockStatement);
			lastBlockStatementEndOffset = writer.GetLocation() ?? 0;
			if (builder != null)
				builder.EndPosition = end;
			if (blockStatement.HiddenEnd != null) {
				DebugStart(blockStatement, start);
				DebugHidden(blockStatement.HiddenEnd);
				DebugEnd(blockStatement, end);
			}
		}
		int lastBlockStatementEndOffset;

		public virtual void VisitBreakStatement(BreakStatement breakStatement)
		{
			StartNode(breakStatement);
			DebugStart(breakStatement);
			WriteKeywordReference(BreakStatement.BreakKeywordRole, currentBreakReference);
			SemicolonDebugEnd(breakStatement);
			EndNode(breakStatement);
		}

		public virtual void VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			DebugExpression(checkedStatement);
			StartNode(checkedStatement);
			WriteKeywordReference(CheckedStatement.CheckedKeywordRole);
			checkedStatement.Body.AcceptVisitor(this);
			EndNode(checkedStatement);
		}

		public virtual void VisitContinueStatement(ContinueStatement continueStatement)
		{
			StartNode(continueStatement);
			DebugStart(continueStatement);
			WriteKeywordReference(ContinueStatement.ContinueKeywordRole, currentLoopReference);
			SemicolonDebugEnd(continueStatement);
			EndNode(continueStatement);
		}

		public virtual void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			StartNode(doWhileStatement);
			var oldRef = currentLoopReference;
			currentLoopReference = new object();
			var oldBreakRef = currentBreakReference;
			currentBreakReference = currentLoopReference;
			WriteKeywordReference(DoWhileStatement.DoKeywordRole, currentLoopReference);
			WriteEmbeddedStatement(doWhileStatement.EmbeddedStatement, policy.WhileNewLinePlacement);
			DebugStart(doWhileStatement);
			WriteKeywordReference(DoWhileStatement.WhileKeywordRole, currentLoopReference);
			Space(policy.SpaceBeforeWhileParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinWhileParentheses);
			doWhileStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinWhileParentheses);
			braceHelper.RightParen();
			SemicolonDebugEnd(doWhileStatement);
			currentLoopReference = oldRef;
			currentBreakReference = oldBreakRef;
			EndNode(doWhileStatement);
		}

		public virtual void VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			DebugExpression(emptyStatement);
			StartNode(emptyStatement);
			Semicolon();
			EndNode(emptyStatement);
		}

		public virtual void VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			StartNode(expressionStatement);
			DebugStart(expressionStatement);
			expressionStatement.Expression.AcceptVisitor(this);
			SemicolonDebugEnd(expressionStatement);
			EndNode(expressionStatement);
		}

		public virtual void VisitFixedStatement(FixedStatement fixedStatement)
		{
			StartNode(fixedStatement);
			WriteKeyword(FixedStatement.FixedKeywordRole);
			Space(policy.SpaceBeforeUsingParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinUsingParentheses);
			DebugStart(fixedStatement);
			fixedStatement.Type.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fixedStatement.Variables);
			DebugEnd(fixedStatement);
			Space(policy.SpacesWithinUsingParentheses);
			braceHelper.RightParen();
			WriteEmbeddedStatement(fixedStatement.EmbeddedStatement);
			EndNode(fixedStatement);
		}

		public virtual void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			StartNode(foreachStatement);
			var oldRef = currentLoopReference;
			currentLoopReference = new object();
			var oldBreakRef = currentBreakReference;
			currentBreakReference = currentLoopReference;
			DebugStart(foreachStatement);
			WriteKeywordReference(ForeachStatement.ForeachKeywordRole, currentLoopReference);
			DebugHidden(foreachStatement.HiddenInitializer);
			DebugEnd(foreachStatement, false);
			Space(policy.SpaceBeforeForeachParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinForeachParentheses);
			DebugStart(foreachStatement);
			foreachStatement.VariableType.AcceptVisitor(this);
			Space();
			WriteIdentifier(foreachStatement.VariableNameToken);
			DebugHidden(foreachStatement.HiddenGetCurrentNode);
			DebugEnd(foreachStatement, false);
			Space();
			DebugStart(foreachStatement);
			WriteKeyword(ForeachStatement.InKeywordRole);
			DebugHidden(foreachStatement.HiddenMoveNextNode);
			DebugEnd(foreachStatement, false);
			Space();
			DebugStart(foreachStatement);
			foreachStatement.InExpression.AcceptVisitor(this);
			DebugHidden(foreachStatement.HiddenGetEnumeratorNode);
			DebugEnd(foreachStatement, false);
			Space(policy.SpacesWithinForeachParentheses);
			braceHelper.RightParen();
			WriteEmbeddedStatement(foreachStatement.EmbeddedStatement);
			currentLoopReference = oldRef;
			currentBreakReference = oldBreakRef;
			EndNode(foreachStatement);
		}

		public virtual void VisitForStatement(ForStatement forStatement)
		{
			StartNode(forStatement);
			var oldRef = currentLoopReference;
			currentLoopReference = new object();
			var oldBreakRef = currentBreakReference;
			currentBreakReference = currentLoopReference;
			WriteKeywordReference(ForStatement.ForKeywordRole, currentLoopReference);
			Space(policy.SpaceBeforeForParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinForParentheses);

			bool emptyForList = !forStatement.Initializers.Any() && !forStatement.Iterators.Any();

			DebugStart(forStatement);
			WriteCommaSeparatedList(forStatement.Initializers);
			if (!emptyForList)
				Space(policy.SpaceBeforeForSemicolon);
			WriteToken(Roles.Semicolon, BoxedTextColor.Punctuation);
			DebugEnd(forStatement, false);
			if (!emptyForList)
				Space(policy.SpaceAfterForSemicolon);

			DebugStart(forStatement);
			forStatement.Condition.AcceptVisitor(this);
			DebugEnd(forStatement, false);
			if (!emptyForList)
				Space(policy.SpaceBeforeForSemicolon);
			WriteToken(Roles.Semicolon, BoxedTextColor.Punctuation);
			if (forStatement.Iterators.Any()) {
				Space(policy.SpaceAfterForSemicolon);
				DebugStart(forStatement);
				WriteCommaSeparatedList(forStatement.Iterators);
				DebugEnd(forStatement, false);
			}

			Space(policy.SpacesWithinForParentheses);
			braceHelper.RightParen();
			WriteEmbeddedStatement(forStatement.EmbeddedStatement);
			currentLoopReference = oldRef;
			currentBreakReference = oldBreakRef;
			EndNode(forStatement);
		}

		public virtual void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
		{
			StartNode(gotoCaseStatement);
			DebugStart(gotoCaseStatement);
			WriteKeywordReferences(GotoCaseStatement.GotoKeywordRole, GotoCaseStatement.CaseKeywordRole, currentSwitchReference);
			Space();
			gotoCaseStatement.LabelExpression.AcceptVisitor(this);
			SemicolonDebugEnd(gotoCaseStatement);
			EndNode(gotoCaseStatement);
		}

		public virtual void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
		{
			StartNode(gotoDefaultStatement);
			DebugStart(gotoDefaultStatement);
			WriteKeywordReferences(GotoDefaultStatement.GotoKeywordRole, GotoDefaultStatement.DefaultKeywordRole, currentSwitchReference);
			SemicolonDebugEnd(gotoDefaultStatement);
			EndNode(gotoDefaultStatement);
		}

		public virtual void VisitGotoStatement(GotoStatement gotoStatement)
		{
			StartNode(gotoStatement);
			DebugStart(gotoStatement);
			WriteKeyword(GotoStatement.GotoKeywordRole);
			WriteIdentifier(gotoStatement.GetChildByRole(Roles.Identifier), BoxedTextColor.Label);
			SemicolonDebugEnd(gotoStatement);
			EndNode(gotoStatement);
		}

		public virtual void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			StartNode(ifElseStatement);
			var oldRef = currentIfReference;
			if (elseIfStart < 0)
				currentIfReference = new object();
			DebugStartReference(ifElseStatement, IfElseStatement.IfKeywordRole, currentIfReference, ref elseIfStart);
			Space(policy.SpaceBeforeIfParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinIfParentheses);
			ifElseStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinIfParentheses);
			braceHelper.RightParen();
			DebugEnd(ifElseStatement);

			if (ifElseStatement.FalseStatement.IsNull) {
				WriteEmbeddedStatement(ifElseStatement.TrueStatement);
			} else {
				WriteEmbeddedStatement(ifElseStatement.TrueStatement, policy.ElseNewLinePlacement);
				if (ifElseStatement.FalseStatement is IfElseStatement) {
					elseIfStart = writer.GetLocation() ?? 0;
					WriteKeyword(IfElseStatement.ElseKeywordRole);
					Space();
					// don't put newline between 'else' and 'if'
					ifElseStatement.FalseStatement.AcceptVisitor(this);
				} else {
					WriteKeywordReference(IfElseStatement.ElseKeywordRole, currentIfReference);
					WriteEmbeddedStatement(ifElseStatement.FalseStatement);
				}
			}
			currentIfReference = oldRef;
			EndNode(ifElseStatement);
		}

		public virtual void VisitLabelStatement(LabelStatement labelStatement)
		{
			DebugExpression(labelStatement);
			StartNode(labelStatement);
			WriteIdentifier(labelStatement.GetChildByRole(Roles.Identifier), BoxedTextColor.Label);
			WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			bool foundLabelledStatement = false;
			for (AstNode tmp = labelStatement.NextSibling; tmp != null; tmp = tmp.NextSibling) {
				if (tmp.Role == labelStatement.Role) {
					foundLabelledStatement = true;
				}
			}
			if (!foundLabelledStatement) {
				// introduce an EmptyStatement so that the output becomes syntactically valid
				WriteToken(Roles.Semicolon, BoxedTextColor.Punctuation);
			}
			NewLine();
			EndNode(labelStatement);
		}

		public virtual void VisitLockStatement(LockStatement lockStatement)
		{
			StartNode(lockStatement);
			DebugStart(lockStatement);
			WriteKeywordReference(LockStatement.LockKeywordRole);
			Space(policy.SpaceBeforeLockParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinLockParentheses);
			lockStatement.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinLockParentheses);
			braceHelper.RightParen();
			DebugEnd(lockStatement);
			WriteEmbeddedStatement(lockStatement.EmbeddedStatement);
			EndNode(lockStatement);
		}

		public virtual void VisitReturnStatement(ReturnStatement returnStatement)
		{
			StartNode(returnStatement);
			DebugStart(returnStatement);
			WriteKeywordReference(ReturnStatement.ReturnKeywordRole, currentMethodRefs.MethodReference);
			if (!returnStatement.Expression.IsNull) {
				Space();
				returnStatement.Expression.AcceptVisitor(this);
			}
			SemicolonDebugEnd(returnStatement);
			EndNode(returnStatement);
		}

		public virtual void VisitSwitchStatement(SwitchStatement switchStatement)
		{
			StartNode(switchStatement);
			DebugStart(switchStatement);
			var oldRef = currentSwitchReference;
			currentSwitchReference = new object();
			var oldBreakRef = currentBreakReference;
			currentBreakReference = currentSwitchReference;
			WriteKeywordReference(SwitchStatement.SwitchKeywordRole, currentSwitchReference);
			Space(policy.SpaceBeforeSwitchParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinSwitchParentheses);
			switchStatement.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinSwitchParentheses);
			braceHelper.RightParen();
			DebugEnd(switchStatement);
			// We don't use CodeBracesRangeFlags.SwitchBraces if we are not indenting the switch body as the indent guide would interfere with the case labels.
			braceHelper = OpenBrace(policy.StatementBraceStyle, policy.IndentSwitchBody ? CodeBracesRangeFlags.SwitchBraces : CodeBracesRangeFlags.BraceKind_CurlyBraces);
			if (!policy.IndentSwitchBody) {
				writer.Unindent();
			}

			int count = 0;
			foreach (var section in switchStatement.SwitchSections) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				section.AcceptVisitor(this);
			}

			if (!policy.IndentSwitchBody) {
				writer.Indent();
			}
			int? start, end;
			CloseBrace(policy.StatementBraceStyle, braceHelper, out start, out end, false);
			if (switchStatement.HiddenEnd != null) {
				DebugStart(switchStatement, start);
				DebugHidden(switchStatement.HiddenEnd);
				DebugEnd(switchStatement, end);
			}
			currentSwitchReference = oldRef;
			currentBreakReference = oldBreakRef;
			NewLine();
			EndNode(switchStatement);
		}

		public virtual void VisitSwitchSection(SwitchSection switchSection)
		{
			StartNode(switchSection);
			bool first = true;
			int count = 0;
			foreach (var label in switchSection.CaseLabels) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (!first) {
					NewLine();
				}
				label.AcceptVisitor(this);
				first = false;
			}
			bool isBlock = switchSection.Statements.Count == 1 && switchSection.Statements.Single() is BlockStatement;
			if (policy.IndentCaseBody && !isBlock) {
				writer.Indent();
			}

			if (!isBlock)
				NewLine();

			count = 0;
			foreach (var statement in switchSection.Statements) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				statement.AcceptVisitor(this);
			}

			if (policy.IndentCaseBody && !isBlock) {
				writer.Unindent();
			}

			EndNode(switchSection);
		}

		public virtual void VisitCaseLabel(CaseLabel caseLabel)
		{
			DebugExpression(caseLabel);
			StartNode(caseLabel);
			if (caseLabel.Expression.IsNull) {
				WriteKeywordReference(CaseLabel.DefaultKeywordRole, currentSwitchReference);
			} else {
				WriteKeywordReference(CaseLabel.CaseKeywordRole, currentSwitchReference);
				Space();
				caseLabel.Expression.AcceptVisitor(this);
			}
			WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			EndNode(caseLabel);
		}

		public virtual void VisitThrowStatement(ThrowStatement throwStatement)
		{
			StartNode(throwStatement);
			DebugStart(throwStatement);
			WriteKeyword(ThrowStatement.ThrowKeywordRole);
			if (!throwStatement.Expression.IsNull) {
				Space();
				throwStatement.Expression.AcceptVisitor(this);
			}
			SemicolonDebugEnd(throwStatement);
			EndNode(throwStatement);
		}

		public virtual void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			StartNode(tryCatchStatement);
			var oldRef = currentTryReference;
			currentTryReference = new object();
			WriteKeywordReference(TryCatchStatement.TryKeywordRole, currentTryReference);
			WriteBlock(tryCatchStatement.TryBlock, policy.StatementBraceStyle);
			int count = 0;
			foreach (var catchClause in tryCatchStatement.CatchClauses) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (policy.CatchNewLinePlacement == NewLinePlacement.SameLine)
					Space();
				else
					NewLine();
				catchClause.AcceptVisitor(this);
			}
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				if (policy.FinallyNewLinePlacement == NewLinePlacement.SameLine)
					Space();
				else
					NewLine();
				WriteKeywordReference(TryCatchStatement.FinallyKeywordRole, currentTryReference);
				WriteBlock(tryCatchStatement.FinallyBlock, policy.StatementBraceStyle);
			}
			NewLine();
			currentTryReference = oldRef;
			EndNode(tryCatchStatement);
		}

		public virtual void VisitCatchClause(CatchClause catchClause)
		{
			StartNode(catchClause);
			bool hasWhen = !catchClause.Condition.IsNull;
			DebugStart(catchClause);
			WriteKeywordReference(CatchClause.CatchKeywordRole, currentTryReference);
			if (!catchClause.Type.IsNull) {
				Space(policy.SpaceBeforeCatchParentheses);
				var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
				Space(policy.SpacesWithinCatchParentheses);
				catchClause.Type.AcceptVisitor(this);
				if (!string.IsNullOrEmpty(catchClause.VariableName)) {
					Space();
					WriteIdentifier(catchClause.VariableNameToken);
				}
				Space(policy.SpacesWithinCatchParentheses);
				braceHelper.RightParen();
			}
			DebugEnd(catchClause);
			if (hasWhen) {
				Space();
				DebugStart(catchClause.Condition);
				WriteKeywordReference(CatchClause.WhenKeywordRole, currentTryReference);
				Space(policy.SpaceBeforeIfParentheses);
				var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
				Space(policy.SpacesWithinIfParentheses);
				catchClause.Condition.AcceptVisitor(this);
				Space(policy.SpacesWithinIfParentheses);
				braceHelper.RightParen();
				DebugEnd(catchClause.Condition);
			}
			WriteBlock(catchClause.Body, policy.StatementBraceStyle);
			EndNode(catchClause);
		}

		public virtual void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			DebugExpression(uncheckedStatement);
			StartNode(uncheckedStatement);
			WriteKeywordReference(UncheckedStatement.UncheckedKeywordRole);
			uncheckedStatement.Body.AcceptVisitor(this);
			EndNode(uncheckedStatement);
		}

		public virtual void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			DebugExpression(unsafeStatement);
			StartNode(unsafeStatement);
			WriteKeyword(UnsafeStatement.UnsafeKeywordRole);
			unsafeStatement.Body.AcceptVisitor(this);
			EndNode(unsafeStatement);
		}

		public virtual void VisitUsingStatement(UsingStatement usingStatement)
		{
			StartNode(usingStatement);
			WriteKeywordReference(UsingStatement.UsingKeywordRole);
			Space(policy.SpaceBeforeUsingParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinUsingParentheses);

			DebugStart(usingStatement);
			usingStatement.ResourceAcquisition.AcceptVisitor(this);
			DebugEnd(usingStatement);

			Space(policy.SpacesWithinUsingParentheses);
			braceHelper.RightParen();

			WriteEmbeddedStatement(usingStatement.EmbeddedStatement);

			EndNode(usingStatement);
		}

		public virtual void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			StartNode(variableDeclarationStatement);
			DebugStart(variableDeclarationStatement);
			WriteModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole), variableDeclarationStatement.Type);
			variableDeclarationStatement.Type.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(variableDeclarationStatement.Variables);
			SemicolonDebugEnd(variableDeclarationStatement);
			EndNode(variableDeclarationStatement);
		}

		public virtual void VisitWhileStatement(WhileStatement whileStatement)
		{
			StartNode(whileStatement);
			DebugStart(whileStatement);
			var oldRef = currentLoopReference;
			currentLoopReference = new object();
			var oldBreakRef = currentBreakReference;
			currentBreakReference = currentLoopReference;
			WriteKeywordReference(WhileStatement.WhileKeywordRole, currentLoopReference);
			Space(policy.SpaceBeforeWhileParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			Space(policy.SpacesWithinWhileParentheses);
			whileStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinWhileParentheses);
			braceHelper.RightParen();
			DebugEnd(whileStatement);
			WriteEmbeddedStatement(whileStatement.EmbeddedStatement);
			currentLoopReference = oldRef;
			currentBreakReference = oldBreakRef;
			EndNode(whileStatement);
		}

		public virtual void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			StartNode(yieldBreakStatement);
			DebugStart(yieldBreakStatement);
			WriteKeywordReferences(YieldBreakStatement.YieldKeywordRole, YieldBreakStatement.BreakKeywordRole, currentMethodRefs.MethodReference);
			SemicolonDebugEnd(yieldBreakStatement);
			EndNode(yieldBreakStatement);
		}

		public virtual void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
		{
			StartNode(yieldReturnStatement);
			DebugStart(yieldReturnStatement);
			WriteKeywordReferences(YieldReturnStatement.YieldKeywordRole, YieldReturnStatement.ReturnKeywordRole, currentMethodRefs.MethodReference);
			Space();
			yieldReturnStatement.Expression.AcceptVisitor(this);
			SemicolonDebugEnd(yieldReturnStatement);
			EndNode(yieldReturnStatement);
		}

		#endregion

		#region TypeMembers
		public virtual void VisitAccessor(Accessor accessor)
		{
			StartNode(accessor);
			var builder = accessor.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			WriteAttributes(accessor.Attributes);
			WriteModifiers(accessor.ModifierTokens, accessor.Body);

			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			bool isDefault = accessor.Body.IsNull;
			if (isDefault)
				DebugStart(accessor);
			var style = policy.StatementBraceStyle;
			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeywordIdentifier(PropertyDeclaration.GetKeywordRole);
				style = policy.PropertyGetBraceStyle;
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeywordIdentifier(PropertyDeclaration.SetKeywordRole);
				style = policy.PropertySetBraceStyle;
			} else if (accessor.Role == CustomEventDeclaration.AddAccessorRole) {
				WriteKeywordIdentifier(CustomEventDeclaration.AddKeywordRole);
				style = policy.EventAddBraceStyle;
			} else if (accessor.Role == CustomEventDeclaration.RemoveAccessorRole) {
				WriteKeywordIdentifier(CustomEventDeclaration.RemoveKeywordRole);
				style = policy.EventRemoveBraceStyle;
			}
			if (isDefault) {
				SaveDeclarationOffset();
				SemicolonDebugEnd(accessor);
			}
			else
				WriteMethodBody(accessor.Body, style);
			if (builder != null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(accessor);
		}

		public virtual void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			StartNode(constructorDeclaration);
			var builder = constructorDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			WriteAttributes(constructorDeclaration.Attributes);
			WriteModifiers(constructorDeclaration.ModifierTokens, constructorDeclaration.NameToken);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			TypeDeclaration type = constructorDeclaration.Parent as TypeDeclaration;
			var method = constructorDeclaration.Annotation<dnlib.DotNet.MethodDef>();
			var textToken = method == null ? BoxedTextColor.Type : CSharpMetadataTextColorProvider.Instance.GetColor(method.DeclaringType);
			if (type != null && type.Name != constructorDeclaration.Name)
				WriteIdentifier((Identifier)type.NameToken.Clone(), textToken);
			else
				WriteIdentifier(constructorDeclaration.NameToken);
			Space(policy.SpaceBeforeConstructorDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(constructorDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			if (!constructorDeclaration.Initializer.IsNull) {
				NewLine();
				writer.Indent();
				constructorDeclaration.Initializer.AcceptVisitor(this);
				writer.Unindent();
			}
			WriteMethodBody(constructorDeclaration.Body, policy.ConstructorBraceStyle);
			if (builder != null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(constructorDeclaration);
		}

		public virtual void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			StartNode(constructorInitializer);
			WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			Space();
			DebugStart(constructorInitializer);
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This) {
				WriteKeyword(ConstructorInitializer.ThisKeywordRole);
			} else {
				WriteKeyword(ConstructorInitializer.BaseKeywordRole);
			}
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInParenthesis(constructorInitializer.Arguments, policy.SpaceWithinMethodCallParentheses, CodeBracesRangeFlags.Parentheses);
			DebugEnd(constructorInitializer);
			EndNode(constructorInitializer);
		}

		public virtual void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			StartNode(destructorDeclaration);
			var builder = destructorDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			WriteAttributes(destructorDeclaration.Attributes);
			WriteModifiers(destructorDeclaration.ModifierTokens, destructorDeclaration.NameToken);
			if (destructorDeclaration.ModifierTokens.Any()) {
				Space();
			}
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();

			WriteToken(DestructorDeclaration.TildeRole, BoxedTextColor.Operator);
			TypeDeclaration type = destructorDeclaration.Parent as TypeDeclaration;
			var method = destructorDeclaration.Annotation<dnlib.DotNet.MethodDef>();
			var textToken = method == null ? BoxedTextColor.Type : CSharpMetadataTextColorProvider.Instance.GetColor(method.DeclaringType);
			if (type != null && type.Name != destructorDeclaration.Name)
				WriteIdentifier((Identifier)type.NameToken.Clone(), textToken);
			else
				WriteIdentifier(destructorDeclaration.NameToken, textToken);
			Space(policy.SpaceBeforeConstructorDeclarationParentheses);
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			braceHelper.RightParen();
			WriteMethodBody(destructorDeclaration.Body, policy.DestructorBraceStyle);
			if (builder != null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(destructorDeclaration);
		}

		public virtual void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			StartNode(enumMemberDeclaration);
			WriteAttributes(enumMemberDeclaration.Attributes);
			WriteModifiers(enumMemberDeclaration.ModifierTokens, enumMemberDeclaration.NameToken);
			WriteIdentifier(enumMemberDeclaration.NameToken);
			if (!enumMemberDeclaration.Initializer.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign, BoxedTextColor.Operator);
				Space(policy.SpaceAroundAssignment);
				enumMemberDeclaration.Initializer.AcceptVisitor(this);
			}
			SaveDeclarationOffset();
			EndNode(enumMemberDeclaration);
		}

		public virtual void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			StartNode(eventDeclaration);
			WriteAttributes(eventDeclaration.Attributes);
			WriteModifiers(eventDeclaration.ModifierTokens, eventDeclaration.ReturnType);

			WriteKeyword(EventDeclaration.EventKeywordRole);
			eventDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(eventDeclaration.Variables);
			SaveDeclarationOffset();
			Semicolon();
			EndNode(eventDeclaration);
		}

		public virtual void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
		{
			StartNode(customEventDeclaration);
			WriteAttributes(customEventDeclaration.Attributes);
			WriteModifiers(customEventDeclaration.ModifierTokens, customEventDeclaration.ReturnType);
			WriteKeyword(CustomEventDeclaration.EventKeywordRole);
			customEventDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(customEventDeclaration.PrivateImplementationType);
			WriteIdentifier(customEventDeclaration.NameToken);
			var braceHelper = OpenBrace(policy.EventBraceStyle, CodeBracesRangeFlags.EventBraces);
			// output add/remove in their original order
			int count = 0;
			foreach (AstNode node in customEventDeclaration.Children) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (node.Role == CustomEventDeclaration.AddAccessorRole || node.Role == CustomEventDeclaration.RemoveAccessorRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(policy.EventBraceStyle, braceHelper, true);
			NewLine();
			EndNode(customEventDeclaration);
		}

		public virtual void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			StartNode(fieldDeclaration);
			WriteAttributes(fieldDeclaration.Attributes);
			WriteModifiers(fieldDeclaration.ModifierTokens, fieldDeclaration.ReturnType);
			DebugStart(fieldDeclaration);
			fieldDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fieldDeclaration.Variables);
			SaveDeclarationOffset();
			SemicolonDebugEnd(fieldDeclaration);
			EndNode(fieldDeclaration);
		}

		public virtual void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			StartNode(fixedFieldDeclaration);
			WriteAttributes(fixedFieldDeclaration.Attributes);
			WriteModifiers(fixedFieldDeclaration.ModifierTokens, fixedFieldDeclaration.ReturnType);
			WriteKeyword(FixedFieldDeclaration.FixedKeywordRole);
			Space();
			fixedFieldDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fixedFieldDeclaration.Variables);
			SaveDeclarationOffset();
			Semicolon();
			EndNode(fixedFieldDeclaration);
		}

		public virtual void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			DebugExpression(fixedVariableInitializer);
			StartNode(fixedVariableInitializer);
			WriteIdentifier(fixedVariableInitializer.NameToken);
			if (!fixedVariableInitializer.CountExpression.IsNull) {
				var braceHelper = BraceHelper.LeftBracket(this, CodeBracesRangeFlags.SquareBrackets);
				Space(policy.SpacesWithinBrackets);
				fixedVariableInitializer.CountExpression.AcceptVisitor(this);
				Space(policy.SpacesWithinBrackets);
				braceHelper.RightBracket();
			}
			EndNode(fixedVariableInitializer);
		}

		public virtual void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			StartNode(indexerDeclaration);
			WriteAttributes(indexerDeclaration.Attributes);
			WriteModifiers(indexerDeclaration.ModifierTokens, indexerDeclaration.ReturnType);
			indexerDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(indexerDeclaration.PrivateImplementationType);
			WriteKeyword(IndexerDeclaration.ThisKeywordRole);
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInBrackets(indexerDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.SquareBrackets);
			bool isSingleLine =
				(policy.AutoPropertyFormatting == PropertyFormatting.SingleLine)
				&& (indexerDeclaration.Getter.IsNull || indexerDeclaration.Getter.Body.IsNull)
				&& (indexerDeclaration.Setter.IsNull || indexerDeclaration.Setter.Body.IsNull)
				&& !indexerDeclaration.Getter.Attributes.Any()
				&& !indexerDeclaration.Setter.Attributes.Any();

			var braceHelper = OpenBrace(isSingleLine ? BraceStyle.EndOfLine : policy.PropertyBraceStyle, CodeBracesRangeFlags.PropertyBraces, newLine: !isSingleLine);
			if (isSingleLine)
				Space();
			// output get/set in their original order
			int count = 0;
			foreach (AstNode node in indexerDeclaration.Children) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(isSingleLine ? BraceStyle.EndOfLine : policy.PropertyBraceStyle, braceHelper, true, unindent: !isSingleLine);
			NewLine();
			EndNode(indexerDeclaration);
		}

		public virtual void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			StartNode(methodDeclaration);
			var builder = methodDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			WriteAttributes(methodDeclaration.Attributes);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			WriteModifiers(methodDeclaration.ModifierTokens, methodDeclaration.ReturnType);
			methodDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(methodDeclaration.PrivateImplementationType);
			WriteIdentifier(methodDeclaration.NameToken);
			WriteTypeParameters(methodDeclaration.TypeParameters, CodeBracesRangeFlags.AngleBrackets);
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			int count = 0;
			foreach (Constraint constraint in methodDeclaration.Constraints) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				constraint.AcceptVisitor(this);
			}
			WriteMethodBody(methodDeclaration.Body, policy.MethodBraceStyle);
			if (builder != null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(methodDeclaration);
		}

		public virtual void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			StartNode(operatorDeclaration);
			var builder = operatorDeclaration.Annotation<MethodDebugInfoBuilder>();
			if (builder != null)
				builder.StartPosition = writer.GetLocation();
			WriteAttributes(operatorDeclaration.Attributes);
			WriteModifiers(operatorDeclaration.ModifierTokens, operatorDeclaration.ReturnType);
			var oldRef = currentMethodRefs;
			currentMethodRefs = MethodRefs.Create();
			if (operatorDeclaration.OperatorType == OperatorType.Explicit) {
				WriteKeyword(OperatorDeclaration.ExplicitRole);
			} else if (operatorDeclaration.OperatorType == OperatorType.Implicit) {
				WriteKeyword(OperatorDeclaration.ImplicitRole);
			} else {
				operatorDeclaration.ReturnType.AcceptVisitor(this);
			}
			WriteKeywordIdentifier(OperatorDeclaration.OperatorKeywordRole);
			Space();
			if (operatorDeclaration.OperatorType == OperatorType.Explicit
			    || operatorDeclaration.OperatorType == OperatorType.Implicit) {
				operatorDeclaration.ReturnType.AcceptVisitor(this);
			} else {
				WriteTokenOperatorOrKeyword(OperatorDeclaration.GetToken(operatorDeclaration.OperatorType), OperatorDeclaration.GetRole(operatorDeclaration.OperatorType));
			}
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(operatorDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
			WriteMethodBody(operatorDeclaration.Body, policy.MethodBraceStyle);
			if (builder != null)
				builder.EndPosition = writer.GetLocation();
			currentMethodRefs = oldRef;
			EndNode(operatorDeclaration);
		}

		public virtual void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			StartNode(parameterDeclaration);
			WriteAttributes(parameterDeclaration.Attributes);
			switch (parameterDeclaration.ParameterModifier) {
				case ParameterModifier.In:
					WriteKeyword(ParameterDeclaration.InModifierRole);
					break;
				case ParameterModifier.Ref:
					WriteKeyword(ParameterDeclaration.RefModifierRole);
					break;
				case ParameterModifier.Out:
					WriteKeyword(ParameterDeclaration.OutModifierRole);
					break;
				case ParameterModifier.Params:
					WriteKeyword(ParameterDeclaration.ParamsModifierRole);
					break;
				case ParameterModifier.This:
					WriteKeyword(ParameterDeclaration.ThisModifierRole);
					break;
			}
			parameterDeclaration.Type.AcceptVisitor(this);
			if (!parameterDeclaration.Type.IsNull && !string.IsNullOrEmpty(parameterDeclaration.Name)) {
				Space();
			}
			if (!string.IsNullOrEmpty(parameterDeclaration.Name)) {
				WriteIdentifier(parameterDeclaration.NameToken);
			}
			if (!parameterDeclaration.DefaultExpression.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign, BoxedTextColor.Operator);
				Space(policy.SpaceAroundAssignment);
				parameterDeclaration.DefaultExpression.AcceptVisitor(this);
			}
			SaveDeclarationOffset();
			EndNode(parameterDeclaration);
		}

		public virtual void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			StartNode(propertyDeclaration);
			WriteAttributes(propertyDeclaration.Attributes);
			WriteModifiers(propertyDeclaration.ModifierTokens, propertyDeclaration.ReturnType);
			propertyDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(propertyDeclaration.PrivateImplementationType);
			WriteIdentifier(propertyDeclaration.NameToken);

			bool isSingleLine =
				(policy.AutoPropertyFormatting == PropertyFormatting.SingleLine)
				&& (propertyDeclaration.Getter.IsNull || propertyDeclaration.Getter.Body.IsNull)
				&& (propertyDeclaration.Setter.IsNull || propertyDeclaration.Setter.Body.IsNull)
				&& !propertyDeclaration.Getter.Attributes.Any()
				&& !propertyDeclaration.Setter.Attributes.Any();

			var braceStyle = isSingleLine ? BraceStyle.EndOfLine : policy.PropertyBraceStyle;
			var braceHelper = OpenBrace(braceStyle, CodeBracesRangeFlags.PropertyBraces, newLine: !isSingleLine);
			if (isSingleLine)
				Space();

			// output get/set in their original order
			int count = 0;
			foreach (var node in propertyDeclaration.Children) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				if (node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(braceStyle, braceHelper, true, unindent: !isSingleLine);
			if (propertyDeclaration.Variables.Any()) {
				propertyDeclaration.Variables.AcceptVisitor(this);
				WriteToken(Roles.Semicolon, BoxedTextColor.Punctuation);
			}
			NewLine();
			EndNode(propertyDeclaration);
		}

		#endregion

		#region Other nodes
		public virtual void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			StartNode(variableInitializer);
			bool ownerIsProp = variableInitializer.Parent is PropertyDeclaration;
			if (!ownerIsProp)
				WriteIdentifier(variableInitializer.NameToken);
			if (!variableInitializer.Initializer.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign, BoxedTextColor.Operator);
				Space(policy.SpaceAroundAssignment);
				if (ownerIsProp)
					DebugStart(variableInitializer);
				WriteModifiers(variableInitializer.GetChildrenByRole(VariableInitializer.ModifierRole), null);
				variableInitializer.Initializer.AcceptVisitor(this);
				if (ownerIsProp)
					DebugEnd(variableInitializer);
			}
			EndNode(variableInitializer);
		}

		bool MaybeNewLinesAfterUsings(AstNode node)
		{
			var nextSibling = node.NextSibling;
			while (nextSibling is WhitespaceNode || nextSibling is NewLineNode)
				nextSibling = nextSibling.NextSibling;

			if ((node is UsingDeclaration || node is UsingAliasDeclaration) && !(nextSibling is UsingDeclaration || nextSibling is UsingAliasDeclaration)) {
				for (int i = 0; i < policy.MinimumBlankLinesAfterUsings; i++)
					NewLine();
				return true;
			}

			return false;
		}

		public virtual void VisitSyntaxTree(SyntaxTree syntaxTree)
		{
			// don't do node tracking as we visit all children directly
			int count = 0;
			bool addedLastLineSep = false;
			int totalCount = 0;
			int lastLineSepOffset = 0;
			foreach (AstNode node in syntaxTree.Children) {
				totalCount++;
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				node.AcceptVisitor(this);
				bool addLineSep = MaybeNewLinesAfterUsings(node) || node is NamespaceDeclaration;
				if (addLineSep) {
					lastLineSepOffset = Math.Max(lastBraceOffset, lastDeclarationOffset);
					writer.AddLineSeparator(lastLineSepOffset);
					addedLastLineSep = true;
				}
				else
					addedLastLineSep = false;
			}
			if (!addedLastLineSep && totalCount > 0) {
				int newOffs = Math.Max(lastBraceOffset, lastDeclarationOffset);
				if (newOffs != lastLineSepOffset && newOffs != 0)
					writer.AddLineSeparator(newOffs);
			}
		}

		public virtual void VisitSimpleType(SimpleType simpleType)
		{
			StartNode(simpleType);
			if (simpleType.Identifier.Length == 0 && SimpleType.DummyTypeGenericParam.Equals(simpleType.Annotation<string>(), StringComparison.Ordinal)) {
				// It's the empty string. Don't call WriteIdentifier() since it will write "<<EMPTY_NAME>>"
			}
			else
				WriteIdentifier(simpleType.IdentifierToken, CSharpMetadataTextColorProvider.Instance.GetColor(simpleType.IdentifierToken.Annotation<object>() ?? simpleType.Annotation<object>()));
			WriteTypeArguments(simpleType.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			EndNode(simpleType);
		}

		public virtual void VisitMemberType(MemberType memberType)
		{
			StartNode(memberType);
			memberType.Target.AcceptVisitor(this);
			if (memberType.IsDoubleColon) {
				WriteToken(Roles.DoubleColon, BoxedTextColor.Operator);
			} else {
				WriteToken(Roles.Dot, BoxedTextColor.Operator);
			}
			WriteIdentifier(memberType.MemberNameToken, CSharpMetadataTextColorProvider.Instance.GetColor(memberType.MemberNameToken.Annotation<object>() ?? memberType.Annotation<object>()));
			WriteTypeArguments(memberType.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			EndNode(memberType);
		}

		public virtual void VisitComposedType(ComposedType composedType)
		{
			StartNode(composedType);
			if (composedType.HasRefSpecifier) {
				WriteKeyword(ComposedType.RefRole);
			}
			composedType.BaseType.AcceptVisitor(this);
			if (composedType.HasNullableSpecifier) {
				WriteToken(ComposedType.NullableRole, BoxedTextColor.Operator);
			}
			int count = 0;
			for (int i = 0; i < composedType.PointerRank; i++) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				WriteToken(ComposedType.PointerRole, BoxedTextColor.Operator);
			}
			count = 0;
			foreach (var node in composedType.ArraySpecifiers) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				node.AcceptVisitor(this);
			}
			EndNode(composedType);
		}

		public virtual void VisitArraySpecifier(ArraySpecifier arraySpecifier)
		{
			StartNode(arraySpecifier);
			var braceHelper = BraceHelper.LeftBracket(this, CodeBracesRangeFlags.SquareBrackets);
			int count = 0;
			foreach (var comma in arraySpecifier.GetChildrenByRole(Roles.Comma)) {
				if (count-- <= 0) {
					cancellationToken.ThrowIfCancellationRequested();
					count = CANCEL_CHECK_LOOP_COUNT;
				}
				writer.WriteTokenPunctuation(Roles.Comma, ",");
			}
			braceHelper.RightBracket();
			EndNode(arraySpecifier);
		}

		public virtual void VisitPrimitiveType(PrimitiveType primitiveType)
		{
			StartNode(primitiveType);
			writer.WritePrimitiveType(primitiveType.Keyword);
			isAfterSpace = false;
			EndNode(primitiveType);
		}

		public virtual void VisitComment(Comment comment)
		{
			writer.StartNode(comment);
			writer.WriteComment(comment.CommentType, comment.Content, comment.References);
			writer.EndNode(comment);
		}

		public virtual void VisitNewLine(NewLineNode newLineNode)
		{
//			formatter.StartNode(newLineNode);
//			formatter.NewLine();
//			formatter.EndNode(newLineNode);
		}

		public virtual void VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			// unused
		}

		public virtual void VisitText(TextNode textNode)
		{
			// unused
		}

		public virtual void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
		{
			writer.StartNode(preProcessorDirective);
			writer.WritePreProcessorDirective(preProcessorDirective.Type, preProcessorDirective.Argument);
			writer.EndNode(preProcessorDirective);
		}

		public virtual void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
		{
			StartNode(typeParameterDeclaration);
			WriteAttributes(typeParameterDeclaration.Attributes);
			switch (typeParameterDeclaration.Variance) {
				case VarianceModifier.Invariant:
					break;
				case VarianceModifier.Covariant:
					WriteKeyword(TypeParameterDeclaration.OutVarianceKeywordRole);
					break;
				case VarianceModifier.Contravariant:
					WriteKeyword(TypeParameterDeclaration.InVarianceKeywordRole);
					break;
				default:
					throw new NotSupportedException ("Invalid value for VarianceModifier");
			}
			WriteIdentifier(typeParameterDeclaration.NameToken);
			SaveDeclarationOffset();
			EndNode(typeParameterDeclaration);
		}

		public virtual void VisitConstraint(Constraint constraint)
		{
			StartNode(constraint);
			Space();
			WriteKeyword(Roles.WhereKeyword);
			constraint.TypeParameter.AcceptVisitor(this);
			Space();
			WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			Space();
			WriteCommaSeparatedList(constraint.BaseTypes);
			EndNode(constraint);
		}

		public virtual void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
		{
			CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
			if (mod != null) {
				if (mod.Modifier == Modifiers.Async) {
					// Needed or comments could be written by WriteKeyword() and the comments
					// would be highlighted when clicking on 'async'
					writer.WriteSpecialsUpToNode(cSharpTokenNode);
				}
				int start = writer.GetLocation() ?? 0;
				// ITokenWriter assumes that each node processed between a
				// StartNode(parentNode)-EndNode(parentNode)-pair is a child of parentNode.
				WriteKeyword(CSharpModifierToken.GetModifierName(mod.Modifier), cSharpTokenNode.Role);
				if (mod.Modifier == Modifiers.Async)
					writer.AddHighlightedKeywordReference(currentMethodRefs.AwaitReference, start, writer.GetLocation() ?? 0);
			} else {
				throw new NotSupportedException ("Should never visit individual tokens");
			}
		}

		public virtual void VisitIdentifier(Identifier identifier)
		{
			// Do not call StartNode and EndNode for Identifier, because they are handled by the ITokenWriter.
			// ITokenWriter assumes that each node processed between a
			// StartNode(parentNode)-EndNode(parentNode)-pair is a child of parentNode.
			WriteIdentifier(identifier, CSharpMetadataTextColorProvider.Instance.GetColor(identifier.Annotation<object>()));
		}

		void IAstVisitor.VisitNullNode(AstNode nullNode)
		{
		}

		void IAstVisitor.VisitErrorNode(AstNode errorNode)
		{
			StartNode(errorNode);
			EndNode(errorNode);
		}
		#endregion

		#region Pattern Nodes
		public virtual void VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern)
		{
			StartNode(placeholder);
			VisitNodeInPattern(pattern);
			EndNode(placeholder);
		}

		void VisitAnyNode(AnyNode anyNode)
		{
			if (!string.IsNullOrEmpty(anyNode.GroupName)) {
				WriteIdentifier(anyNode.GroupName, BoxedTextColor.Text);
				WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			}
		}

		void VisitBackreference(Backreference backreference)
		{
			WriteKeyword("backreference");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			WriteIdentifier(backreference.ReferencedGroupName, BoxedTextColor.Text);
			braceHelper.RightParen();
		}

		void VisitIdentifierExpressionBackreference(IdentifierExpressionBackreference identifierExpressionBackreference)
		{
			WriteKeyword("identifierBackreference");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			WriteIdentifier(identifierExpressionBackreference.ReferencedGroupName, BoxedTextColor.Text);
			braceHelper.RightParen();
		}

		void VisitChoice(Choice choice)
		{
			WriteKeyword("choice");
			Space();
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			NewLine();
			writer.Indent();
			foreach (INode alternative in choice) {
				VisitNodeInPattern(alternative);
				if (alternative != choice.Last()) {
					WriteToken(Roles.Comma, BoxedTextColor.Punctuation);
				}
				NewLine();
			}
			writer.Unindent();
			braceHelper.RightParen();
		}

		void VisitNamedNode(NamedNode namedNode)
		{
			if (!string.IsNullOrEmpty(namedNode.GroupName)) {
				WriteIdentifier(namedNode.GroupName, BoxedTextColor.Text);
				WriteToken(Roles.Colon, BoxedTextColor.Punctuation);
			}
			VisitNodeInPattern(namedNode.ChildNode);
		}

		void VisitRepeat(Repeat repeat)
		{
			WriteKeyword("repeat");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			if (repeat.MinCount != 0 || repeat.MaxCount != int.MaxValue) {
				WriteIdentifier(repeat.MinCount.ToString(), BoxedTextColor.Number);
				WriteToken(Roles.Comma, BoxedTextColor.Punctuation);
				WriteIdentifier(repeat.MaxCount.ToString(), BoxedTextColor.Number);
				WriteToken(Roles.Comma, BoxedTextColor.Punctuation);
			}
			VisitNodeInPattern(repeat.ChildNode);
			braceHelper.RightParen();
		}

		void VisitOptionalNode(OptionalNode optionalNode)
		{
			WriteKeyword("optional");
			var braceHelper = BraceHelper.LeftParen(this, CodeBracesRangeFlags.Parentheses);
			VisitNodeInPattern(optionalNode.ChildNode);
			braceHelper.RightParen();
		}

		void VisitNodeInPattern(INode childNode)
		{
			if (childNode is AstNode) {
				((AstNode)childNode).AcceptVisitor(this);
			} else if (childNode is IdentifierExpressionBackreference) {
				VisitIdentifierExpressionBackreference((IdentifierExpressionBackreference)childNode);
			} else if (childNode is Choice) {
				VisitChoice((Choice)childNode);
			} else if (childNode is AnyNode) {
				VisitAnyNode((AnyNode)childNode);
			} else if (childNode is Backreference) {
				VisitBackreference((Backreference)childNode);
			} else if (childNode is NamedNode) {
				VisitNamedNode((NamedNode)childNode);
			} else if (childNode is OptionalNode) {
				VisitOptionalNode((OptionalNode)childNode);
			} else if (childNode is Repeat) {
				VisitRepeat((Repeat)childNode);
			} else {
				writer.WritePrimitiveValue(childNode);
			}
		}
		#endregion

		#region Documentation Reference
		public virtual void VisitDocumentationReference(DocumentationReference documentationReference)
		{
			StartNode(documentationReference);
			if (!documentationReference.DeclaringType.IsNull) {
				documentationReference.DeclaringType.AcceptVisitor(this);
				if (documentationReference.SymbolKind != SymbolKind.TypeDefinition) {
					WriteToken(Roles.Dot, BoxedTextColor.Operator);
				}
			}
			switch (documentationReference.SymbolKind) {
				case SymbolKind.TypeDefinition:
					// we already printed the DeclaringType
					break;
				case SymbolKind.Indexer:
					WriteKeyword(IndexerDeclaration.ThisKeywordRole);
					break;
				case SymbolKind.Operator:
					var opType = documentationReference.OperatorType;
					if (opType == OperatorType.Explicit) {
						WriteKeyword(OperatorDeclaration.ExplicitRole);
					} else if (opType == OperatorType.Implicit) {
						WriteKeyword(OperatorDeclaration.ImplicitRole);
					}
					WriteKeyword(OperatorDeclaration.OperatorKeywordRole);
					Space();
					if (opType == OperatorType.Explicit || opType == OperatorType.Implicit) {
						documentationReference.ConversionOperatorReturnType.AcceptVisitor(this);
					} else {
						WriteTokenOperatorOrKeyword(OperatorDeclaration.GetToken(opType), OperatorDeclaration.GetRole(opType));
					}
					break;
				default:
					WriteIdentifier(documentationReference.GetChildByRole(Roles.Identifier), BoxedTextColor.Text);
					break;
			}
			WriteTypeArguments(documentationReference.TypeArguments, CodeBracesRangeFlags.AngleBrackets);
			if (documentationReference.HasParameterList) {
				Space(policy.SpaceBeforeMethodDeclarationParentheses);
				if (documentationReference.SymbolKind == SymbolKind.Indexer) {
					WriteCommaSeparatedListInBrackets(documentationReference.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.SquareBrackets);
				} else {
					WriteCommaSeparatedListInParenthesis(documentationReference.Parameters, policy.SpaceWithinMethodDeclarationParentheses, CodeBracesRangeFlags.Parentheses);
				}
			}
			EndNode(documentationReference);
		}
		#endregion
	}
}
