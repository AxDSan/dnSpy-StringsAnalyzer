// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast {
	public class TextTokenWriter : TokenWriter
	{
		readonly IDecompilerOutput output;
		readonly DecompilerContext context;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		int braceLevelWithinType = -1;

		public bool FoldBraces = false;

		public TextTokenWriter(IDecompilerOutput output, DecompilerContext context)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			if (context == null)
				throw new ArgumentNullException("context");
			this.output = output;
			this.context = context;
		}

		public override void WriteIdentifier(Identifier identifier, object data)
		{
			if (BoxedTextColor.Text.Equals(data))
				data = context.MetadataTextColorProvider.GetColor(identifier.AnnotationVT<TextColor>() ?? identifier.Annotation<object>());

			var escapedName = IdentifierEscaper.Escape(identifier.Name);
			if (!BoxedTextColor.Keyword.Equals(data) && (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier))) {
				escapedName = "@" + escapedName;
			}

			var definition = GetCurrentDefinition(identifier);
			if (definition != null) {
				output.Write(escapedName, definition, DecompilerReferenceFlags.Definition, data);
				return;
			}

			object memberRef = GetCurrentMemberReference() ?? (object)identifier.Annotation<NamespaceReference>();
			if (memberRef != null) {
				output.Write(escapedName, memberRef, DecompilerReferenceFlags.None, data);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition != null) {
				output.Write(escapedName, definition, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, data);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef != null) {
				output.Write(escapedName, memberRef, DecompilerReferenceFlags.Local, data);
				return;
			}

			if (identifier.Annotation<IdentifierFormatted>() != null)
				escapedName = identifier.Name;
			output.Write(escapedName, data);
		}

		IMemberRef GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			IMemberRef memberRef = node.Annotation<IMemberRef>();
			if (node is IndexerDeclaration)
				memberRef = null;
			if ((node is SimpleType || node is MemberType) && node.Parent is ObjectCreateExpression) {
				var td = (memberRef as IType).Resolve();
				if (td == null || !td.IsDelegate)
					memberRef = node.Parent.Annotation<IMemberRef>() ?? memberRef;
			}
			if (memberRef == null && node.Role == Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreateExpression)) {
				memberRef = node.Parent.Annotation<IMemberRef>();
			}
			if (node is IdentifierExpression && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression && memberRef != null) {
				var declaringType = memberRef.DeclaringType.Resolve();
				if (declaringType != null && declaringType.IsDelegate)
					return null;
			}
			return FilterMemberReference(memberRef);
		}

		IMemberRef FilterMemberReference(IMemberRef memberRef)
		{
			if (memberRef == null)
				return null;

			if (context.Settings.AutomaticEvents && memberRef is FieldDef) {
				var field = (FieldDef)memberRef;
				return field.DeclaringType.FindEvent(field.Name) ?? memberRef;
			}

			return memberRef;
		}

		object GetCurrentLocalReference()
		{
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariable>();
			if (variable != null)
				return variable.GetTextReferenceObject();

			var gotoStatement = node as GotoStatement;
			if (gotoStatement != null)
			{
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr != null && mr.IsMethod);
				if (method != null)
					return method.ToString() + gotoStatement.Label;
			}

			return null;
		}

		object GetCurrentLocalDefinition()
		{
			AstNode node = nodeStack.Peek();
			if (node is Identifier && node.Parent != null)
				node = node.Parent;

			var parameterDef = node.Annotation<Parameter>();
			if (parameterDef != null)
				return parameterDef;

			if (node is VariableInitializer || node is CatchClause || node is ForeachStatement) {
				var variable = node.Annotation<ILVariable>();
				if (variable != null)
					return variable.GetTextReferenceObject();
			}

			var label = node as LabelStatement;
			if (label != null) {
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr != null && mr.IsMethod);
				if (method != null)
					return method.ToString() + label.Label;
			}

			return null;
		}

		object GetCurrentDefinition(Identifier identifier)
		{
			if (nodeStack != null && nodeStack.Count != 0) {
				var data = GetDefinition(nodeStack.Peek());
				if (data != null)
					return data;
			}
			return GetDefinition(identifier);
		}

		object GetDefinition(AstNode node)
		{
			if (node is Identifier) {
				node = node.Parent;
				if (node is VariableInitializer)
					node = node.Parent;		// get FieldDeclaration / EventDeclaration
			}
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();

			return null;
		}

		public override void WriteKeyword(Role role, string keyword)
		{
			WriteKeyword(keyword);
		}

		void WriteKeyword(string keyword)
		{
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (node is IndexerDeclaration)
				memberRef = node.Annotation<PropertyDef>();
			if (keyword != "async" && memberRef != null && (node is PrimitiveType || node is ConstructorInitializer || node is BaseReferenceExpression || node is ThisReferenceExpression || node is ObjectCreateExpression || node is AnonymousMethodExpression))
				output.Write(keyword, memberRef, keyword == "new" ? DecompilerReferenceFlags.Hidden : DecompilerReferenceFlags.None, BoxedTextColor.Keyword);
			else if (memberRef != null && node is IndexerDeclaration && keyword == "this")
				output.Write(keyword, memberRef, DecompilerReferenceFlags.Definition, BoxedTextColor.Keyword);
			else
				output.Write(keyword, BoxedTextColor.Keyword);
		}

		public override void WriteToken(Role role, string token, object data)
		{
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();

			bool addRef = memberRef != null &&
					(node is BinaryOperatorExpression ||
					node is UnaryOperatorExpression ||
					node is AssignmentExpression ||
					node is IndexerExpression);

			// Add a ref to the method if it's a delegate call
			if (!addRef && node is InvocationExpression && memberRef is IMethod) {
				var md = (memberRef as IMethod).Resolve();
				if (md != null && md.DeclaringType != null && md.DeclaringType.IsDelegate)
					addRef = true;
			}

			if (addRef)
				output.Write(token, memberRef, DecompilerReferenceFlags.None, data);
			else
				output.Write(token, data);
		}

		public override void Space()
		{
			output.Write(" ", BoxedTextColor.Text);
		}

		public void OpenBrace(BraceStyle style, out int? start, out int? end)
		{
			if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
				braceLevelWithinType++;
			output.WriteLine();
			start = output.NextPosition;
			output.Write("{", BoxedTextColor.Punctuation);
			end = output.NextPosition;
			output.WriteLine();
			output.IncreaseIndent();
		}

		public void CloseBrace(BraceStyle style, out int? start, out int? end)
		{
			output.DecreaseIndent();
			start = output.NextPosition;
			output.Write("}", BoxedTextColor.Punctuation);
			end = output.NextPosition;
			if (braceLevelWithinType >= 0)
				braceLevelWithinType--;
		}

		public override void Indent()
		{
			output.IncreaseIndent();
		}

		public override void Unindent()
		{
			output.DecreaseIndent();
		}

		public override void NewLine()
		{
			output.WriteLine();
		}

		public override void WriteComment(CommentType commentType, string content, CommentReference[] refs)
		{
			switch (commentType) {
				case CommentType.SingleLine:
					output.Write("//", BoxedTextColor.Comment);
					Write(content, refs);
					output.WriteLine();
					break;
				case CommentType.MultiLine:
					output.Write("/*", BoxedTextColor.Comment);
					Write(content, refs);
					output.Write("*/", BoxedTextColor.Comment);
					break;
				case CommentType.Documentation:
					output.Write("///", BoxedTextColor.XmlDocCommentDelimiter);
					Debug.Assert(refs == null);
					output.WriteXmlDoc(content);
					output.WriteLine();
					break;
				default:
					Write(content, refs);
					break;
			}
		}

		void Write(string content, CommentReference[] refs)
		{
			if (refs == null) {
				output.Write(content, BoxedTextColor.Comment);
				return;
			}

			int offs = 0;
			for (int i = 0; i < refs.Length; i++) {
				var @ref = refs[i];
				var s = content.Substring(offs, @ref.Length);
				offs += @ref.Length;
				if (@ref.Reference == null)
					output.Write(s, BoxedTextColor.Comment);
				else
					output.Write(s, @ref.Reference, @ref.IsLocal ? DecompilerReferenceFlags.Local : DecompilerReferenceFlags.None, BoxedTextColor.Comment);
			}
			Debug.Assert(offs == content.Length);
		}

		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			output.Write("#", BoxedTextColor.PreprocessorKeyword);
			output.Write(type.ToString().ToLowerInvariant(), BoxedTextColor.PreprocessorKeyword);
			if (!string.IsNullOrEmpty(argument)) {
				output.Write(" ", BoxedTextColor.Text);
				output.Write(argument, BoxedTextColor.PreprocessorText);
			}
			output.WriteLine();
		}

		public override void WritePrimitiveValue(object value, object data = null, string literalValue = null)
		{
			int column = 0;
			var numberFormatter = NumberFormatter.GetCSharpInstance(context.Settings.HexadecimalNumbers, upper: true);
			TextWriterTokenWriter.WritePrimitiveValue(value, data, literalValue, context.Settings.MaxStringLength, ref column, numberFormatter, WritePrimitiveValueCore, WriteToken);
		}

		void WritePrimitiveValueCore(string text, object reference, object color)
		{
			if (color == BoxedTextColor.String || color == BoxedTextColor.Char) {
				int start = output.NextPosition;
				output.Write(text, color);
				int end = output.NextPosition;
				output.AddBracePair(new TextSpan(start, 1), new TextSpan(end - 1, 1), CodeBracesRangeFlags.SingleQuotes);
			}
			else if (reference != null)
				output.Write(text, reference, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Hidden | DecompilerReferenceFlags.NoFollow, color);
			else
				output.Write(text, color);
		}

		public override void WritePrimitiveType(string type)
		{
			WriteKeyword(type);
			if (type == "new") {
				int startPos1 = output.NextPosition;
				output.Write("(", BoxedTextColor.Punctuation);
				int startPos2 = output.NextPosition;
				output.Write(")", BoxedTextColor.Punctuation);
				output.AddBracePair(new TextSpan(startPos1, 1), new TextSpan(startPos2, 1), CodeBracesRangeFlags.Parentheses);
			}
		}

		MethodDebugInfoBuilder currentMethodDebugInfoBuilder;
		Stack<MethodDebugInfoBuilder> parentMethodDebugInfoBuilder = new Stack<MethodDebugInfoBuilder>();
		List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>> multiMappings;

		public override void StartNode(AstNode node)
		{
			nodeStack.Push(node);

			MethodDebugInfoBuilder mapping = node.Annotation<MethodDebugInfoBuilder>();
			if (mapping != null) {
				parentMethodDebugInfoBuilder.Push(currentMethodDebugInfoBuilder);
				currentMethodDebugInfoBuilder = mapping;
			}
			// For ctor/cctor field initializers
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>>>();
			if (mms != null) {
				Debug.Assert(multiMappings == null);
				multiMappings = mms;
			}
		}

		public override void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();

			if (node.Annotation<MethodDebugInfoBuilder>() != null) {
				if (context.CalculateILSpans) {
					foreach (var ns in context.UsingNamespaces)
						currentMethodDebugInfoBuilder.Scope.Imports.Add(ImportInfo.CreateNamespace(ns));
				}
				if (parentMethodDebugInfoBuilder.Peek() != currentMethodDebugInfoBuilder)
					output.AddDebugInfo(currentMethodDebugInfoBuilder.Create());
				currentMethodDebugInfoBuilder = parentMethodDebugInfoBuilder.Pop();
			}
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>>>();
			if (mms != null) {
				Debug.Assert(mms == multiMappings);
				if (mms == multiMappings) {
					foreach (var mm in mms)
						output.AddDebugInfo(mm.Item1.Create());
					multiMappings = null;
				}
			}
		}

		private static bool IsDefinition(AstNode node)
		{
			return node is EntityDeclaration
				|| (node is VariableInitializer && node.Parent is FieldDeclaration)
				|| node is FixedVariableInitializer
				|| node is TypeParameterDeclaration;
		}

		class DebugState
		{
			public List<AstNode> Nodes = new List<AstNode>();
			public int StartSpan;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public override void DebugStart(AstNode node, int? start)
		{
			debugStack.Push(new DebugState { StartSpan = start ?? output.NextPosition });
		}

		public override void DebugHidden(AstNode hiddenNode)
		{
			if (hiddenNode == null || hiddenNode.IsNull)
				return;
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.AddRange(hiddenNode.DescendantsAndSelf);
		}

		public override void DebugExpression(AstNode node)
		{
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		public override void DebugEnd(AstNode node, int? end)
		{
			var state = debugStack.Pop();
			if (currentMethodDebugInfoBuilder != null) {
				foreach (var ilSpan in ILSpan.OrderAndCompact(GetILSpans(state)))
					currentMethodDebugInfoBuilder.Add(new SourceStatement(ilSpan, new TextSpan(state.StartSpan, (end ?? output.NextPosition) - state.StartSpan)));
			}
			else if (multiMappings != null) {
				foreach (var mm in multiMappings) {
					foreach (var ilSpan in ILSpan.OrderAndCompact(mm.Item2))
						mm.Item1.Add(new SourceStatement(ilSpan, new TextSpan(state.StartSpan, (end ?? output.NextPosition) - state.StartSpan)));
				}
			}
		}

		static IEnumerable<ILSpan> GetILSpans(DebugState state)
		{
			foreach (var node in state.Nodes) {
				foreach (var ann in node.Annotations) {
					var list = ann as IList<ILSpan>;
					if (list == null)
						continue;
					foreach (var ilSpan in list)
						yield return ilSpan;
				}
			}
		}

		public override int? GetLocation()
		{
			return output.NextPosition;
		}

		public override void AddHighlightedKeywordReference(object reference, int start, int end) {
			Debug.Assert(reference != null);
			if (reference != null)
				output.AddSpanReference(reference, start, end, PredefinedSpanReferenceIds.HighlightRelatedKeywords);
		}

		public override void AddBracePair(int leftStart, int leftEnd, int rightStart, int rightEnd, CodeBracesRangeFlags flags) =>
			output.AddBracePair(TextSpan.FromBounds(leftStart, leftEnd), TextSpan.FromBounds(rightStart, rightEnd), flags);

		public override void AddLineSeparator(int position) => output.AddLineSeparator(position);
	}
}
