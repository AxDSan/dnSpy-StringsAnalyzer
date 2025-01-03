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
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Ast;
using CSharp2 = ICSharpCode.NRefactory.CSharp;

namespace dnSpy.Decompiler.ILSpy.Core.VisualBasic {
	sealed class VBTextOutputFormatter : IOutputFormatter {
		readonly IDecompilerOutput output;
		readonly DecompilerContext context;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();

		public VBTextOutputFormatter(IDecompilerOutput output, DecompilerContext context) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.context = context ?? throw new ArgumentNullException(nameof(context));
		}

		MethodDebugInfoBuilder? currentMethodDebugInfoBuilder;
		Stack<MethodDebugInfoBuilder?> parentMethodDebugInfoBuilder = new Stack<MethodDebugInfoBuilder?>();
		List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>>? multiMappings;

		public void StartNode(AstNode node) {
			nodeStack.Push(node);

			MethodDebugInfoBuilder mapping = node.Annotation<MethodDebugInfoBuilder>();
			if (mapping is not null) {
				parentMethodDebugInfoBuilder.Push(currentMethodDebugInfoBuilder);
				currentMethodDebugInfoBuilder = mapping;
			}
			// For ctor/cctor field initializers
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>>>();
			if (mms is not null) {
				Debug2.Assert(multiMappings is null);
				multiMappings = mms;
			}
		}

		public void EndNode(AstNode node) {
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();

			if (node.Annotation<MethodDebugInfoBuilder>() is not null) {
				Debug2.Assert(currentMethodDebugInfoBuilder is not null);
				if (context.CalculateILSpans) {
					foreach (var ns in context.UsingNamespaces)
						currentMethodDebugInfoBuilder.Scope.Imports.Add(ImportInfo.CreateNamespace(ns));
				}
				output.AddDebugInfo(currentMethodDebugInfoBuilder.Create());
				currentMethodDebugInfoBuilder = parentMethodDebugInfoBuilder.Pop();
			}
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<ILSpan>>>>();
			if (mms is not null) {
				Debug.Assert(mms == multiMappings);
				if (mms == multiMappings) {
					foreach (var mm in mms)
						output.AddDebugInfo(mm.Item1.Create());
					multiMappings = null;
				}
			}
		}

		public void WriteIdentifier(string identifier, object data, object extraData) {
			var definition = GetCurrentDefinition();
			if (definition is not null) {
				output.Write(IdentifierEscaper.Escape(identifier), definition, DecompilerReferenceFlags.Definition, data);
				return;
			}

			var memberRef = GetCurrentMemberReference() ?? (object?)(extraData as NamespaceReference);
			if (memberRef is not null) {
				output.Write(IdentifierEscaper.Escape(identifier), memberRef, DecompilerReferenceFlags.None, data);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition is not null) {
				output.Write(IdentifierEscaper.Escape(identifier), definition, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, data);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef is not null) {
				output.Write(IdentifierEscaper.Escape(identifier), memberRef, DecompilerReferenceFlags.Local, data);
				return;
			}

			output.Write(IdentifierEscaper.Escape(identifier), data);
		}

		IMemberRef? GetCurrentMemberReference() {
			AstNode node = nodeStack.Peek();
			if (node.Annotation<ILVariable>() is not null)
				return null;
			if (node.Role == AstNode.Roles.Type && node.Parent is ObjectCreationExpression)
				node = node.Parent;
			var memberRef = node.Annotation<IMemberRef>();
			if (memberRef is null && node is Identifier) {
				node = node.Parent ?? node;
				memberRef = node.Annotation<IMemberRef>();
			}
			if (memberRef is null && node.Role == AstNode.Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreationExpression)) {
				memberRef = node.Parent.Annotation<IMemberRef>();
			}
			return memberRef;
		}

		object? GetCurrentLocalReference() {
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariable>();
			if (variable is null && node.Parent is IdentifierExpression)
				variable = node.Parent.Annotation<ILVariable>();
			if (variable is not null)
				return variable.GetTextReferenceObject();
			var lbl = (node.Parent?.Parent as GoToStatement)?.Label ?? (node.Parent?.Parent as LabelDeclarationStatement)?.Label;
			if (lbl is not null) {
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr is not null && mr.IsMethod);
				if (method is not null)
					return method.ToString() + lbl;
			}
			return null;
		}

		object? GetCurrentLocalDefinition() {
			AstNode node = nodeStack.Peek();
			if (node is Identifier && node.Parent is CatchBlock)
				node = node.Parent;
			var parameterDef = node.Annotation<Parameter>();
			if (parameterDef is not null)
				return parameterDef;
			if (node is ParameterDeclaration) {
				node = ((ParameterDeclaration)node).Name;
				parameterDef = node.Annotation<Parameter>();
				if (parameterDef is not null)
					return parameterDef;
			}

			if (node is VariableIdentifier) {
				var variable = ((VariableIdentifier)node).Name.Annotation<ILVariable>();
				if (variable is not null)
					return variable.GetTextReferenceObject();
				node = node.Parent ?? node;
			}
			if (node is VariableDeclaratorWithTypeAndInitializer || node is VariableInitializer || node is CatchBlock || node is ForEachStatement) {
				var variable = node.Annotation<ILVariable>();
				if (variable is not null)
					return variable.GetTextReferenceObject();
			}

			if (node is LabelDeclarationStatement label) {
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr is not null && mr.IsMethod);
				if (method is not null)
					return method.ToString() + label.Label;
			}


			return null;
		}

		object? GetCurrentDefinition() {
			if (nodeStack is null || nodeStack.Count == 0)
				return null;

			var node = nodeStack.Peek();
			if (node is ParameterDeclaration)
				return null;
			if (node is VariableIdentifier)
				return ((VariableIdentifier)node).Name.Annotation<IMemberDef>();
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();

			if (node is Identifier) {
				node = node.Parent;
				if (IsDefinition(node))
					return node.Annotation<IMemberRef>();
			}

			return null;
		}

		public void WriteKeyword(string keyword) {
			var memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef is not null && (node is PrimitiveType || node is InstanceExpression))
				output.Write(keyword, memberRef, DecompilerReferenceFlags.None, BoxedTextColor.Keyword);
			else if (memberRef is not null && (node is ConstructorDeclaration && keyword == "New"))
				output.Write(keyword, memberRef, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Keyword);
			else if (memberRef is not null && (node is Accessor && (keyword == "Get" || keyword == "Set" || keyword == "AddHandler" || keyword == "RemoveHandler" || keyword == "RaiseEvent"))) {
				if (canPrintAccessor)
					output.Write(keyword, memberRef, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Keyword);
				else
					output.Write(keyword, BoxedTextColor.Keyword);
				canPrintAccessor = !canPrintAccessor;
			}
			else if (memberRef is not null && node is OperatorDeclaration && keyword == "Operator")
				output.Write(keyword, memberRef, DecompilerReferenceFlags.Definition, BoxedTextColor.Keyword);
			else
				output.Write(keyword, BoxedTextColor.Keyword);
		}
		bool canPrintAccessor = true;

		public void WriteToken(string token, object data, object? reference) {
			var memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();

			bool addRef = memberRef is not null &&
					(node is BinaryOperatorExpression ||
					node is UnaryOperatorExpression ||
					node is AssignmentExpression);

			// Add a ref to the method if it's a delegate call
			if (!addRef && node is InvocationExpression && memberRef is IMethod) {
				var md = Resolve(memberRef as IMethod);
				if (md is not null && md.DeclaringType is not null && md.DeclaringType.IsDelegate)
					addRef = true;
			}

			if (addRef)
				output.Write(token, memberRef, DecompilerReferenceFlags.None, data);
			else if (reference is not null)
				output.Write(token, reference, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Hidden | DecompilerReferenceFlags.NoFollow, data);
			else
				output.Write(token, data);
		}

		static MethodDef? Resolve(IMethod? method) {
			if (method is MethodSpec)
				method = ((MethodSpec)method).Method;
			if (method is MemberRef)
				return ((MemberRef)method).ResolveMethod();
			else
				return (MethodDef?)method;
		}

		public void Space() => output.Write(" ", BoxedTextColor.Text);
		public void Indent() => output.IncreaseIndent();
		public void Unindent() => output.DecreaseIndent();
		public void NewLine() => output.WriteLine();

		public void WriteComment(bool isDocumentation, string content, CSharp2.CommentReference[] refs) {
			if (isDocumentation) {
				Debug2.Assert(refs is null);
				output.Write("'''", BoxedTextColor.XmlDocCommentDelimiter);
				output.WriteXmlDoc(content);
				output.WriteLine();
			}
			else {
				output.Write("'", BoxedTextColor.Comment);
				Write(content, refs);
				output.WriteLine();
			}
		}

		void Write(string content, CSharp2.CommentReference[] refs)
		{
			if (refs is null) {
				output.Write(content, BoxedTextColor.Comment);
				return;
			}

			int offs = 0;
			for (int i = 0; i < refs.Length; i++) {
				var @ref = refs[i];
				var s = content.Substring(offs, @ref.Length);
				offs += @ref.Length;
				if (@ref.Reference is null)
					output.Write(s, BoxedTextColor.Comment);
				else
					output.Write(s, @ref.Reference, @ref.IsLocal ? DecompilerReferenceFlags.Local : DecompilerReferenceFlags.None, BoxedTextColor.Comment);
			}
			Debug.Assert(offs == content.Length);
		}

		static bool IsDefinition(AstNode node) =>
			node is FieldDeclaration ||
			node is ConstructorDeclaration ||
			node is EventDeclaration ||
			node is DelegateDeclaration ||
			node is OperatorDeclaration ||
			node is MemberDeclaration ||
			node is TypeDeclaration ||
			node is EnumDeclaration ||
			node is EnumMemberDeclaration ||
			node is TypeParameterDeclaration;

		class DebugState {
			public List<AstNode> Nodes = new List<AstNode>();
			public List<ILSpan> ExtraILSpans = new List<ILSpan>();
			public int StartLocation;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public void DebugStart(AstNode node) => debugStack.Push(new DebugState { StartLocation = output.NextPosition });

		public void DebugHidden(object hiddenILSpans) {
			if (hiddenILSpans is IList<ILSpan> list) {
				if (debugStack.Count > 0)
					debugStack.Peek().ExtraILSpans.AddRange(list);
			}
		}

		public void DebugExpression(AstNode node) {
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		public void DebugEnd(AstNode node) {
			var state = debugStack.Pop();
			if (currentMethodDebugInfoBuilder is not null) {
				foreach (var ilSpan in ILSpan.OrderAndCompact(GetILSpans(state)))
					currentMethodDebugInfoBuilder.Add(new SourceStatement(ilSpan, new TextSpan(state.StartLocation, output.NextPosition - state.StartLocation)));
			}
			else if (multiMappings is not null) {
				foreach (var mm in multiMappings) {
					foreach (var ilSpan in ILSpan.OrderAndCompact(mm.Item2))
						mm.Item1.Add(new SourceStatement(ilSpan, new TextSpan(state.StartLocation, output.NextPosition - state.StartLocation)));
				}
			}
		}

		static IEnumerable<ILSpan> GetILSpans(DebugState state) {
			foreach (var node in state.Nodes) {
				foreach (var ann in node.Annotations) {
					var list = ann as IList<ILSpan>;
					if (list is null)
						continue;
					foreach (var ilSpan in list)
						yield return ilSpan;
				}
			}
			foreach (var ilSpan in state.ExtraILSpans)
				yield return ilSpan;
		}

		public void AddHighlightedKeywordReference(object reference, int start, int end) {
			Debug2.Assert(reference is not null);
			if (reference is not null)
				output.AddSpanReference(reference, start, end, PredefinedSpanReferenceIds.HighlightRelatedKeywords);
		}

		public int NextPosition => output.NextPosition;

		public void AddBracePair(int leftStart, int leftEnd, int rightStart, int rightEnd, CodeBracesRangeFlags flags) =>
			output.AddBracePair(TextSpan.FromBounds(leftStart, leftEnd), TextSpan.FromBounds(rightStart, rightEnd), flags);

		public void AddBlock(int start, int end, CodeBracesRangeFlags flags) =>
			output.AddBracePair(new TextSpan(start, 0), new TextSpan(end, 0), flags);

		public void AddLineSeparator(int position) => output.AddLineSeparator(position);
	}
}
