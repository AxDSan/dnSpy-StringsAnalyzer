﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Finds the expanded form of using statements using pattern matching and replaces it with a UsingStatement.
	/// </summary>
	public sealed class PatternStatementTransform : ContextTrackingVisitor<AstNode>, IAstTransformPoolObject
	{
		readonly StringBuilder stringBuilder;

		public PatternStatementTransform(DecompilerContext context) : base(context)
		{
			this.stringBuilder = new StringBuilder();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
		}

		#region Visitor Overrides
		protected override AstNode VisitChildren(AstNode node, object data)
		{
			// Go through the children, and keep visiting a node as long as it changes.
			// Because some transforms delete/replace nodes before and after the node being transformed, we rely
			// on the transform's return value to know where we need to keep iterating.
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				AstNode oldChild;
				do {
					oldChild = child;
					child = child.AcceptVisitor(this, data);
					Debug.Assert(child != null && child.Parent == node);
				} while (child != oldChild);
			}
			return node;
		}

		public override AstNode VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			AstNode result;
			if (context.Settings.UsingStatement)
			{
				result = TransformNonGenericForEach(expressionStatement);
				if (result != null)
					return result;
				result = TransformUsings(expressionStatement);
				if (result != null)
					return result;
			}
			result = TransformForeachArrayOrString(expressionStatement);
			if (result != null)
				return result;
			result = TransformFor(expressionStatement);
			if (result != null)
				return result;
			if (context.Settings.LockStatement) {
				result = TransformLock(expressionStatement);
				if (result != null)
					return result;
			}
			return base.VisitExpressionStatement(expressionStatement, data);
		}

		public override AstNode VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			if (context.Settings.ForEachStatement) {
				AstNode result = TransformForeach(usingStatement);
				if (result != null)
					return result;
			}
			return base.VisitUsingStatement(usingStatement, data);
		}

		public override AstNode VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			return TransformDoWhile(whileStatement) ??
				TransformWhileTrueToForLoop(whileStatement) ??
				base.VisitWhileStatement(whileStatement, data);
		}

		public override AstNode VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			if (context.Settings.SwitchStatementOnString) {
				AstNode result = TransformSwitchOnString(ifElseStatement);
				if (result != null)
					return result;
			}
			AstNode simplifiedIfElse = SimplifyCascadingIfElseStatements(ifElseStatement);
			if (simplifiedIfElse != null)
				return simplifiedIfElse;
			return base.VisitIfElseStatement(ifElseStatement, data);
		}

		public override AstNode VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (context.Settings.AutomaticProperties && !context.Settings.ForceShowAllMembers) {
				AstNode result = TransformAutomaticProperties(propertyDeclaration);
				if (result != null)
					return result;
			}
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}

		public override AstNode VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			// first apply transforms to the accessor bodies
			base.VisitCustomEventDeclaration(eventDeclaration, data);
			if (context.Settings.AutomaticEvents && !context.Settings.ForceShowAllMembers) {
				AstNode result = TransformAutomaticEvents(eventDeclaration);
				if (result != null)
					return result;
			}
			return eventDeclaration;
		}

		public override AstNode VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			return TransformDestructor(methodDeclaration) ?? base.VisitMethodDeclaration(methodDeclaration, data);
		}

		public override AstNode VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			return TransformTryCatchFinally(tryCatchStatement) ?? base.VisitTryCatchStatement(tryCatchStatement, data);
		}
		#endregion

		/// <summary>
		/// $variable = $initializer;
		/// </summary>
		static readonly AstNode variableAssignPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression(Pattern.AnyString)),
				new AnyNode("initializer")
			));

		#region using
		static Expression InvokeDispose(Expression identifier)
		{
			return new Choice {
				identifier.Invoke("Dispose"),
				identifier.Clone().CastTo(new TypePattern(typeof(IDisposable))).Invoke("Dispose")
			};
		}

		static readonly AstNode usingTryCatchPattern = new Choice {
			{ "c#/vb",
			new TryCatchStatement {
			TryBlock = new AnyNode(),
			FinallyBlock = new BlockStatement {
				new Choice {
					{ "valueType",
						new ExpressionStatement(InvokeDispose(new NamedNode("ident", new IdentifierExpression(Pattern.AnyString))))
					},
					{ "referenceType",
						new IfElseStatement {
							Condition = new BinaryOperatorExpression(
								new NamedNode("ident", new IdentifierExpression(Pattern.AnyString)),
								BinaryOperatorType.InEquality,
								new NullReferenceExpression()
							),
							TrueStatement = new BlockStatement {
								new ExpressionStatement(InvokeDispose(new Backreference("ident")))
							}
						}
					}
				}.ToStatement()
			}
		}
		},
		{ "f#",
			new TryCatchStatement {
			TryBlock = new AnyNode(),
			FinallyBlock =
					new BlockStatement {
						new ExpressionStatement(
							new AssignmentExpression(left: new NamedNode("disposable", new IdentifierExpression(Pattern.AnyString)),
														right: new AsExpression(expression: new NamedNode("ident", new IdentifierExpression(Pattern.AnyString)),
																				type: new TypePattern(typeof(IDisposable))
																				)
							)
						),
						new IfElseStatement {
							Condition = new BinaryOperatorExpression(
								new Backreference("disposable"),
								BinaryOperatorType.InEquality,
								new NullReferenceExpression()
							),
							TrueStatement = new BlockStatement {
								new ExpressionStatement(InvokeDispose(new Backreference("disposable")))
							}
						}
					}
				}
			}
		};

		public UsingStatement TransformUsings(ExpressionStatement node)
		{
			Match m1 = variableAssignPattern.Match(node);
			if (!m1.Success) return null;
			TryCatchStatement tryCatch = node.NextSibling as TryCatchStatement;
			Match m2 = usingTryCatchPattern.Match(tryCatch);
			if (!m2.Success) return null;
			string variableName = m1.Get<IdentifierExpression>("variable").Single().Identifier;
			if (variableName != m2.Get<IdentifierExpression>("ident").Single().Identifier)
				return null;
			if (m2.Has("valueType")) {
				// if there's no if(x!=null), then it must be a value type
				ILVariable v = m1.Get<AstNode>("variable").Single().Annotation<ILVariable>();
				if (v == null || v.Type == null || !DnlibExtensions.IsValueType(v.Type))
					return null;
			}

			// There are two variants of the using statement:
			// "using (var a = init)" and "using (expr)".
			// The former declares a read-only variable 'a', and the latter declares an unnamed read-only variable
			// to store the original value of 'expr'.
			// This means that in order to introduce a using statement, in both cases we need to detect a read-only
			// variable that is used only within that block.

			if (HasAssignment(tryCatch, variableName))
				return null;

			VariableDeclarationStatement varDecl = FindVariableDeclaration(node, variableName);
			if (varDecl == null || !(varDecl.Parent is BlockStatement))
				return null;

			// Validate that the variable is not used after the using statement:
			if (!IsVariableValueUnused(varDecl, tryCatch))
				return null;

			if (m2.Has("f#")) {
				string variableNameDisposable = m2.Get<IdentifierExpression>("disposable").Single().Identifier;
				VariableDeclarationStatement varDeclDisposable = FindVariableDeclaration(node, variableNameDisposable);
				if (varDeclDisposable == null || !(varDeclDisposable.Parent is BlockStatement))
					return null;

				// Validate that the variable is not used after the using statement:
				if (!IsVariableValueUnused(varDeclDisposable, tryCatch))
					return null;
			}

			node.Remove();

			UsingStatement usingStatement = new UsingStatement();
			tryCatch.TryBlock.HiddenEnd = tryCatch.FinallyBlock.Detach();
			usingStatement.EmbeddedStatement = tryCatch.TryBlock.Detach();
			tryCatch.ReplaceWith(usingStatement);
			tryCatch.AddAllRecursiveILSpansTo(usingStatement);

			// If possible, we'll eliminate the variable completely:
			if (usingStatement.EmbeddedStatement.Descendants.OfType<IdentifierExpression>().Any(ident => ident.Identifier == variableName)) {
				// variable is used, so we'll create a variable declaration
				usingStatement.ResourceAcquisition = new VariableDeclarationStatement {
					Type = (AstType)varDecl.Type.Clone(),
					Variables = {
						new VariableInitializer {
							NameToken = Identifier.Create(variableName).WithAnnotation(BoxedTextColor.Local),
							Initializer = m1.Get<Expression>("initializer").Single().Detach()
						}.CopyAnnotationsFrom(node.Expression)
							.WithAnnotation(m1.Get<AstNode>("variable").Single().Annotation<ILVariable>())
					}
				}.CopyAnnotationsFrom(node).WithAnnotation(node.Expression.GetAllRecursiveILSpans());
			} else {
				// the variable is never used; eliminate it:
				usingStatement.ResourceAcquisition = m1.Get<Expression>("initializer").Single().Detach();
				usingStatement.ResourceAcquisition.AddAnnotation(node.Expression.GetAllRecursiveILSpans());
			}
			return usingStatement;
		}

		internal static VariableDeclarationStatement FindVariableDeclaration(AstNode node, string identifier)
		{
			while (node != null) {
				while (node.PrevSibling != null) {
					node = node.PrevSibling;
					VariableDeclarationStatement varDecl = node as VariableDeclarationStatement;
					if (varDecl != null && varDecl.Variables.Count == 1 && varDecl.Variables.Single().Name == identifier) {
						return varDecl;
					}
				}
				node = node.Parent;
			}
			return null;
		}

		static AstType GetParameterOrVariableType(AstNode node, string identifier) {
			while (node != null) {
				while (node.PrevSibling != null) {
					node = node.PrevSibling;
					var varDecl = node as VariableDeclarationStatement;
					if (varDecl != null && varDecl.Variables.Count == 1 && varDecl.Variables.Single().Name == identifier)
						return varDecl.Type;
					var paramDecl = node as ParameterDeclaration;
					if (paramDecl != null && paramDecl.Name == identifier)
						return paramDecl.Type;
				}
				node = node.Parent;
			}
			return null;
		}

		/// <summary>
		/// Gets whether the old variable value (assigned inside 'targetStatement' or earlier)
		/// is read anywhere in the remaining scope of the variable declaration.
		/// </summary>
		bool IsVariableValueUnused(VariableDeclarationStatement varDecl, Statement targetStatement)
		{
			Debug.Assert(targetStatement.Ancestors.Contains(varDecl.Parent));
			BlockStatement block = (BlockStatement)varDecl.Parent;
			DefiniteAssignmentAnalysis daa = new DefiniteAssignmentAnalysis(block, context.CancellationToken);
			daa.SetAnalyzedRange(targetStatement, block, startInclusive: false);
			daa.Analyze(varDecl.Variables.Single().Name, context.CancellationToken);
			return daa.UnassignedVariableUses.Count == 0;
		}

		// I used this in the first implementation of the using-statement transform, but now no longer
		// because there were problems when multiple using statements were using the same variable
		// - no single using statement could be transformed without making the C# code invalid,
		// but transforming both would work.
		// We now use 'IsVariableValueUnused' which will perform the transform
		// even if it results in two variables with the same name and overlapping scopes.
		// (this issue could be fixed later by renaming one of the variables)

		// I'm not sure whether the other consumers of 'CanMoveVariableDeclarationIntoStatement' should be changed the same way.
		bool CanMoveVariableDeclarationIntoStatement(VariableDeclarationStatement varDecl, Statement targetStatement, out Statement declarationPoint)
		{
			Debug.Assert(targetStatement.Ancestors.Contains(varDecl.Parent));
			// Find all blocks between targetStatement and varDecl.Parent
			List<BlockStatement> blocks = targetStatement.Ancestors.TakeWhile(block => block != varDecl.Parent).OfType<BlockStatement>().ToList();
			blocks.Add((BlockStatement)varDecl.Parent); // also handle the varDecl.Parent block itself
			blocks.Reverse(); // go from parent blocks to child blocks
			DefiniteAssignmentAnalysis daa = new DefiniteAssignmentAnalysis(blocks[0], context.CancellationToken);
			declarationPoint = null;
			foreach (BlockStatement block in blocks) {
				if (!DeclareVariables.FindDeclarationPoint(daa, varDecl, block, out declarationPoint, context.CancellationToken)) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets whether there is an assignment to 'variableName' anywhere within the given node.
		/// </summary>
		bool HasAssignment(AstNode root, string variableName)
		{
			foreach (AstNode node in root.DescendantsAndSelf) {
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null && ident.Identifier == variableName) {
					if (ident.Parent is AssignmentExpression && ident.Role == AssignmentExpression.LeftRole
					    || ident.Parent is DirectionExpression)
					{
						return true;
					}
				}
			}
			return false;
		}
		#endregion

		#region foreach (generic)
		static readonly UsingStatement genericForeachPattern = new UsingStatement {
			ResourceAcquisition = new VariableDeclarationStatement {
				Type = new AnyNode("enumeratorType"),
				Variables = {
					new NamedNode(
						"enumeratorVariable",
						new VariableInitializer {
							Name = Pattern.AnyString,
							Initializer = new AnyNode("collection").ToExpression().Invoke("GetEnumerator")
						}
					)
				}
			},
			EmbeddedStatement = new BlockStatement {
				new Repeat(
					new VariableDeclarationStatement { Type = new AnyNode(), Variables = { new VariableInitializer(null, Pattern.AnyString) } }.WithName("variablesOutsideLoop")
				).ToStatement(),
				new WhileStatement {
					Condition = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Invoke("MoveNext"),
					EmbeddedStatement = new BlockStatement {
						new Repeat(
							new VariableDeclarationStatement {
								Type = new AnyNode(),
								Variables = { new VariableInitializer(null, Pattern.AnyString) }
							}.WithName("variablesInsideLoop")
						).ToStatement(),
						new AssignmentExpression {
							Left = new IdentifierExpression(Pattern.AnyString).WithName("itemVariable"),
							Operator = AssignmentOperatorType.Assign,
							Right = new IdentifierExpressionBackreference("enumeratorVariable").ToExpression().Member("Current", BoxedTextColor.InstanceProperty)
						}.WithName("getCurrent"),
						new Repeat(new AnyNode("statement")).ToStatement()
					}
				}.WithName("loop")
			}};

		public ForeachStatement TransformForeach(UsingStatement node)
		{
			Match m = genericForeachPattern.Match(node);
			if (!m.Success)
				return null;
			if (!(node.Parent is BlockStatement) && m.Has("variablesOutsideLoop")) {
				// if there are variables outside the loop, we need to put those into the parent block, and that won't work if the direct parent isn't a block
				return null;
			}
			VariableInitializer enumeratorVar = m.Get<VariableInitializer>("enumeratorVariable").Single();
			IdentifierExpression itemVar = m.Get<IdentifierExpression>("itemVariable").Single();
			WhileStatement loop = m.Get<WhileStatement>("loop").Single();

			// Find the declaration of the item variable:
			// Because we look only outside the loop, we won't make the mistake of moving a captured variable across the loop boundary
			VariableDeclarationStatement itemVarDecl = FindVariableDeclaration(loop, itemVar.Identifier);
			if (itemVarDecl == null || !(itemVarDecl.Parent is BlockStatement))
				return null;

			// Now verify that we can move the variable declaration in front of the loop:
			Statement declarationPoint;
			CanMoveVariableDeclarationIntoStatement(itemVarDecl, loop, out declarationPoint);
			// We ignore the return value because we don't care whether we can move the variable into the loop
			// (that is possible only with non-captured variables).
			// We just care that we can move it in front of the loop:
			if (declarationPoint != loop)
				return null;

			// Make sure that the enumerator variable is not used inside the body
			var enumeratorId = Identifier.Create(enumeratorVar.Name);
			foreach (Statement stmt in m.Get<Statement>("statement")) {
				if (stmt.Descendants.OfType<Identifier>().Any(id => enumeratorId.IsMatch(id)))
					return null;
			}

			BlockStatement newBody = new BlockStatement();
			foreach (Statement stmt in m.Get<Statement>("variablesInsideLoop"))
				newBody.Add(stmt.Detach());
			foreach (Statement stmt in m.Get<Statement>("statement"))
				newBody.Add(stmt.Detach());

			var oldBody = node.EmbeddedStatement as BlockStatement;
			if (oldBody != null) {
				newBody.HiddenStart = oldBody.HiddenStart;
				newBody.HiddenEnd = oldBody.HiddenEnd;
			}

			ForeachStatement foreachStatement = new ForeachStatement {
				VariableType = (AstType)itemVarDecl.Type.Clone(),
				VariableNameToken = (Identifier)itemVar.IdentifierToken.Clone(),
				InExpression = m.Get<Expression>("collection").Single().Detach(),
				EmbeddedStatement = newBody
			}.WithAnnotation(itemVarDecl.Variables.Single().Annotation<ILVariable>());
			if (foreachStatement.InExpression is BaseReferenceExpression) {
				foreachStatement.InExpression = new ThisReferenceExpression().CopyAnnotationsFrom(foreachStatement.InExpression);
			}
			foreachStatement.HiddenGetEnumeratorNode = enumeratorVar;
			foreachStatement.HiddenGetCurrentNode = m.Get<AstNode>("getCurrent").Single();
			foreachStatement.HiddenMoveNextNode = loop.Condition;
			node.ReplaceWith(foreachStatement);
			foreach (Statement stmt in m.Get<Statement>("variablesOutsideLoop")) {
				((BlockStatement)foreachStatement.Parent).Statements.InsertAfter(null, stmt.Detach());
			}
			return foreachStatement;
		}
		#endregion

		#region foreach (non-generic)
		static readonly ExpressionStatement getEnumeratorPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("left", new IdentifierExpression(Pattern.AnyString)),
				new AnyNode("collection").ToExpression().Invoke("GetEnumerator")
			).WithName("getEnumeratorAssignment"));

		static readonly TryCatchStatement nonGenericForeachPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new WhileStatement {
					Condition = new IdentifierExpression(Pattern.AnyString).WithName("enumerator").Invoke("MoveNext"),
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(
							new IdentifierExpression(Pattern.AnyString).WithName("itemVar"),
							new Choice {
								new Backreference("enumerator").ToExpression().Member("Current", BoxedTextColor.InstanceProperty),
								new CastExpression {
									Type = new AnyNode("castType"),
									Expression = new Backreference("enumerator").ToExpression().Member("Current", BoxedTextColor.InstanceProperty)
								}
							}
						).WithName("getCurrent"),
						new Repeat(new AnyNode("stmt")).ToStatement()
					}
				}.WithName("loop")
			},
			FinallyBlock = new BlockStatement {
				new AssignmentExpression(
					new IdentifierExpression(Pattern.AnyString).WithName("disposable"),
					new Backreference("enumerator").ToExpression().CastAs(new TypePattern(typeof(IDisposable)))
				),
				new IfElseStatement {
					Condition = new BinaryOperatorExpression {
						Left = new Backreference("disposable"),
						Operator = BinaryOperatorType.InEquality,
						Right = new NullReferenceExpression()
					},
					TrueStatement = new BlockStatement {
						new Backreference("disposable").ToExpression().Invoke("Dispose")
					}
				}
			}};
		// There's no finally block if it's a sealed class (or a struct) that doesn't implement IDisposable
		static readonly Statement nonGenericForeachPatternNoFinallyBlock = new WhileStatement {
			Condition = new IdentifierExpression(Pattern.AnyString).WithName("enumerator").Invoke("MoveNext"),
			EmbeddedStatement = new BlockStatement {
				new AssignmentExpression(
					new IdentifierExpression(Pattern.AnyString).WithName("itemVar"),
					new Choice {
						new Backreference("enumerator").ToExpression().Member("Current", BoxedTextColor.InstanceProperty),
						new CastExpression {
							Type = new AnyNode("castType"),
							Expression = new Backreference("enumerator").ToExpression().Member("Current", BoxedTextColor.InstanceProperty)
						}
					}
				).WithName("getCurrent"),
				new Repeat(new AnyNode("stmt")).ToStatement()
			}
		}.WithName("loop");

		public ForeachStatement TransformNonGenericForEach(ExpressionStatement node)
		{
			Match m1 = getEnumeratorPattern.Match(node);
			if (!m1.Success) return null;
			AstNode tryCatch = node.NextSibling;
			Match m2 = nonGenericForeachPattern.Match(tryCatch);
			if (!m2.Success)
				m2 = nonGenericForeachPatternNoFinallyBlock.Match(tryCatch);
			if (!m2.Success) return null;

			IdentifierExpression enumeratorVar = m2.Get<IdentifierExpression>("enumerator").Single();
			IdentifierExpression itemVar = m2.Get<IdentifierExpression>("itemVar").Single();
			WhileStatement loop = m2.Get<WhileStatement>("loop").Single();

			// verify that the getEnumeratorPattern assigns to the same variable as the nonGenericForeachPattern is reading from
			if (!enumeratorVar.IsMatch(m1.Get("left").Single()))
				return null;

			VariableDeclarationStatement enumeratorVarDecl = FindVariableDeclaration(loop, enumeratorVar.Identifier);
			if (enumeratorVarDecl == null || !(enumeratorVarDecl.Parent is BlockStatement))
				return null;

			// Find the declaration of the item variable:
			// Because we look only outside the loop, we won't make the mistake of moving a captured variable across the loop boundary
			VariableDeclarationStatement itemVarDecl = FindVariableDeclaration(loop, itemVar.Identifier);
			if (itemVarDecl == null || !(itemVarDecl.Parent is BlockStatement))
				return null;

			// Now verify that we can move the variable declaration in front of the loop:
			Statement declarationPoint;
			CanMoveVariableDeclarationIntoStatement(itemVarDecl, loop, out declarationPoint);
			// We ignore the return value because we don't care whether we can move the variable into the loop
			// (that is possible only with non-captured variables).
			// We just care that we can move it in front of the loop:
			if (declarationPoint != loop)
				return null;

			ForeachStatement foreachStatement = new ForeachStatement
			{
				VariableType = itemVarDecl.Type.Clone(),
				VariableNameToken = (Identifier)itemVar.IdentifierToken.Clone(),
			}.WithAnnotation(itemVarDecl.Variables.Single().Annotation<ILVariable>());
			BlockStatement body = new BlockStatement();
			foreachStatement.EmbeddedStatement = body;
			((BlockStatement)node.Parent).Statements.InsertBefore(node, foreachStatement);

			body.Add(node.Detach());
			body.Add((Statement)tryCatch.Detach());

			// Now that we moved the whole try-catch into the foreach loop; verify that we can
			// move the enumerator into the foreach loop:
			if (!IsVariableValueUnused(enumeratorVarDecl, foreachStatement)) {
				// oops, the enumerator variable can't be moved into the foreach loop
				// Undo our AST changes:
				((BlockStatement)foreachStatement.Parent).Statements.InsertBefore(foreachStatement, node.Detach());
				foreachStatement.ReplaceWith(tryCatch);
				return null;
			}

			var tc = tryCatch as TryCatchStatement;
			if (tc != null) {
				foreachStatement.HiddenGetEnumeratorNode = !context.CalculateILSpans ? tc.TryBlock.HiddenStart : NRefactoryExtensions.CreateHidden(tc.TryBlock.HiddenStart, m1.Get<AssignmentExpression>("getEnumeratorAssignment").Single());
				foreachStatement.HiddenGetEnumeratorNode = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompactList(tc.TryBlock.GetAllILSpans()), foreachStatement.HiddenGetEnumeratorNode);
			}
			foreachStatement.HiddenMoveNextNode = loop.Condition;
			foreachStatement.HiddenGetCurrentNode = m2.Get<AstNode>("getCurrent").Single();
			var oldBody = loop.EmbeddedStatement as BlockStatement;
			if (oldBody != null) {
				body.HiddenStart = oldBody.HiddenStart;
				body.HiddenEnd = oldBody.HiddenEnd;
			}
			if (context.CalculateILSpans && tc != null)
				body.HiddenEnd = NRefactoryExtensions.CreateHidden(body.HiddenEnd, tc.TryBlock.HiddenEnd, tc.FinallyBlock);

			// Now create the correct body for the foreach statement:
			foreachStatement.InExpression = m1.Get<Expression>("collection").Single().Detach();
			if (foreachStatement.InExpression is BaseReferenceExpression) {
				foreachStatement.InExpression = new ThisReferenceExpression().CopyAnnotationsFrom(foreachStatement.InExpression).WithAnnotation(foreachStatement.InExpression.GetAllRecursiveILSpans());
			}
			body.Statements.Clear();
			body.Statements.AddRange(m2.Get<Statement>("stmt").Select(stmt => stmt.Detach()));

			return foreachStatement;
		}
		#endregion

		/// <summary>
		/// $variable = 0;
		/// </summary>
		static readonly AstNode variableZeroAssignPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("initializer", new IdentifierExpression(Pattern.AnyString)),
				AssignmentOperatorType.Assign,
				new PrimitiveExpression(0)
			));
		static readonly WhileStatement foreachStringOrArrayPattern = new WhileStatement {
			// $i < $loopArray.Length
			Condition = new BinaryOperatorExpression {
				Left = new NamedNode("i", new IdentifierExpression(Pattern.AnyString)),
				Operator = BinaryOperatorType.LessThan,
				Right = new NamedNode("endExpr",
					new MemberReferenceExpression {
						Target = new NamedNode("loopArray", new IdentifierExpression(Pattern.AnyString)),
						MemberName = "Length"
					})
			},
			EmbeddedStatement = new BlockStatement {
				Statements = {
					// $loopVar = $loopArray[$i]
					// $loopVar = (TYPE)$loopArray[$i]
					new NamedNode(
						"variable",
						new ExpressionStatement(
							new AssignmentExpression {
								Left = new NamedNode("loopVar", new IdentifierExpression(Pattern.AnyString)),
								Operator = AssignmentOperatorType.Assign,
								Right = new Choice {
									new IndexerExpression {
										Target = new Backreference("loopArray"),
										Arguments = { new Backreference("i") }
									},
									new IndexerExpression {
										Target = new Backreference("loopArray"),
										Arguments = { new Backreference("i") }
									}.CastTo(new AnyNode())
								}
							})),
					new Repeat(new AnyNode("statement")),
					// $i = $i + 1
					new NamedNode(
						"increment",
						new ExpressionStatement(
							new AssignmentExpression {
								Left = new Backreference("i"),
								Operator = AssignmentOperatorType.Assign,
								Right = new BinaryOperatorExpression {
									Left = new Backreference("i"),
									Operator = BinaryOperatorType.Add,
									Right = new PrimitiveExpression(1)
								}
							}))
				}
			}};

		public ForeachStatement TransformForeachArrayOrString(ExpressionStatement node)
		{
			ExpressionStatement loopFieldNode = null;
			AstNode next = node;
			// If it's a loop over an array or string field, or if there's a cast, the expression is first stored in a local.
			var loopFieldMatch = variableAssignPattern.Match(next);
			if (loopFieldMatch.Success) {
				loopFieldNode = (ExpressionStatement)next;
				next = next.NextSibling;
			}
			var loopVarMatch = variableZeroAssignPattern.Match(next);
			if (!loopVarMatch.Success) {
				loopVarMatch = variableZeroAssignPattern.Match(loopFieldNode);
				if (!loopVarMatch.Success)
					return null;
				next = loopFieldNode;
				loopFieldNode = null;
				loopFieldMatch = default(Match);
			}
			var loopIndexVarNode = (ExpressionStatement)next;
			var whileMatch = foreachStringOrArrayPattern.Match(loopIndexVarNode.NextSibling);
			if (!whileMatch.Success)
				return null;
			var whileStmt = (WhileStatement)loopIndexVarNode.NextSibling;

			var loopArray = whileMatch.Get<IdentifierExpression>("loopArray").Single();
			var typeInfo = loopArray.Annotation<TypeInformation>();
			var type = (typeInfo?.InferredType ?? typeInfo?.ExpectedType).RemovePinnedAndModifiers();
			if (type.GetElementType() != ElementType.SZArray && type.GetElementType() != ElementType.String)
				return null;

			// Verify that the local where the field is stored is used in the loop
			if (loopFieldMatch.Success && !loopArray.IsMatch(loopFieldMatch.Get("variable").Single())) {
				// It wasn't a temp local
				loopFieldNode = null;
			}

			// Verify that the same local ("i") is used outside and inside the loop
			var loopInit = loopVarMatch.Get<IdentifierExpression>("initializer").Single();
			var loopInit2 = whileMatch.Get("i").Single();
			if (!loopInit.IsMatch(loopInit2))
				return null;

			var varDecl = FindVariableDeclaration(node, loopInit.Identifier);
			if (varDecl == null || !IsVariableValueUnused(varDecl, whileStmt))
				return null;

			// Make sure the loop variable, or compiler generated array local, isn't used by any of the other statements
			var stmts = whileMatch.Get("statement");
			if (loopFieldNode != null) {
				var id = ((IdentifierExpression)((AssignmentExpression)(loopFieldNode).Expression).Left).Identifier;
				if (stmts.Cast<AstNode>().Any(a => a.DescendantsAndSelf.OfType<IdentifierExpression>().Any(i => i.Identifier == loopInit.Identifier || i.Identifier == id)))
					return null;
			}
			else {
				if (stmts.Cast<AstNode>().Any(a => a.DescendantsAndSelf.OfType<IdentifierExpression>().Any(i => i.Identifier == loopInit.Identifier)))
					return null;
			}

			var arrayType = GetParameterOrVariableType(whileStmt, loopArray.Identifier);
			AstType elemType;
			if (arrayType is ComposedType) {
				var composedType = (ComposedType)arrayType;
				if (!composedType.ArraySpecifiers.Any())
					return null;
				if (composedType.ArraySpecifiers.Count <= 1)
					elemType = composedType.BaseType;
				else
					elemType = AstBuilder.ConvertType((type as ArraySigBase).Next, stringBuilder);
			}
			else if (arrayType is PrimitiveType) {
				var primType = (PrimitiveType)arrayType;
				if (primType.KnownTypeCode != NRefactory.TypeSystem.KnownTypeCode.String)
					return null;
				elemType = new PrimitiveType("char").WithAnnotation(context.CurrentModule.CorLibTypes.Char.TypeDefOrRef);
			}
			else
				return null;

			// First statement in while loop, eg.: c = array[i];
			var loadFromArrayStmt = whileMatch.Get<ExpressionStatement>("variable").Single();
			var loadFromArrayExpr = ((AssignmentExpression)loadFromArrayStmt.Expression).Right;
			if (loadFromArrayExpr is CastExpression)
				elemType = ((CastExpression)loadFromArrayExpr).Type;

			var whileBlock = (BlockStatement)whileStmt.EmbeddedStatement;

			var body = new BlockStatement();
			body.Statements.AddRange(stmts.Cast<Statement>().Select(a => a.Detach()));

			Expression inExpr;
			if (loopFieldNode != null)
				inExpr = ((AssignmentExpression)(loopFieldNode).Expression).Right.Clone();
			else
				inExpr = loopArray.Clone();

			loopFieldNode?.Detach();
			loopIndexVarNode.Detach();

			var foreachStatement = new ForeachStatement {
				VariableType = elemType.Clone().Detach(),
				VariableNameToken = whileMatch.Get<IdentifierExpression>("loopVar").Single().IdentifierToken.Detach(),
				InExpression = inExpr,
				EmbeddedStatement = body,
			};
			foreachStatement.WithAnnotation(((AssignmentExpression)loadFromArrayStmt.Expression).Left.Annotation<ILVariable>());

			if (context.CalculateILSpans) {
				var incStmt = whileMatch.Get<ExpressionStatement>("increment").Single();
				inExpr.RemoveAllILSpansRecursive();
				body.HiddenStart = whileBlock.HiddenStart;
				body.HiddenEnd = whileBlock.HiddenEnd;
				// Temp local (if source is a field or a cast, otherwise the statement doesn't exist)
				// array = (int[])args;
				foreachStatement.HiddenInitializer = loopFieldNode;         // |foreach| (var c in args)
				// Compiler generated index
				// i = 0;
				foreachStatement.HiddenGetEnumeratorNode = loopIndexVarNode;// foreach (var c in |args|)
				// Condition and increment get to share the same location, there's not enough space left
				// i < array.Length
				// i = i + 1;
				foreachStatement.HiddenMoveNextNode = incStmt;              // foreach (var c |in| args)
				whileStmt.Condition.AddAllRecursiveILSpansTo(incStmt);
				// Store value in local
				// c = array[i];
				foreachStatement.HiddenGetCurrentNode = loadFromArrayStmt;  // foreach (|var c| in args)
			}

			whileStmt.ReplaceWith(foreachStatement);

			return foreachStatement;
		}

		#region for
		static readonly WhileStatement forPattern = new WhileStatement {
			Condition = new BinaryOperatorExpression {
				Left = new NamedNode("ident", new IdentifierExpression(Pattern.AnyString)),
				Operator = BinaryOperatorType.Any,
				Right = new AnyNode("endExpr")
			},
			EmbeddedStatement = new BlockStatement {
				Statements = {
					new Repeat(new AnyNode("statement")),
					new NamedNode(
						"increment",
						new ExpressionStatement(
							new AssignmentExpression {
								Left = new Backreference("ident"),
								Operator = AssignmentOperatorType.Any,
								Right = new AnyNode()
							}))
				}
			}};

		public ForStatement TransformFor(ExpressionStatement node)
		{
			Match m1 = variableAssignPattern.Match(node);
			if (!m1.Success) return null;
			AstNode next = node.NextSibling;
			Match m2 = forPattern.Match(next);
			if (!m2.Success) return null;
			// ensure the variable in the for pattern is the same as in the declaration
			if (m1.Get<IdentifierExpression>("variable").Single().Identifier != m2.Get<IdentifierExpression>("ident").Single().Identifier)
				return null;
			WhileStatement loop = (WhileStatement)next;
			node.Remove();
			BlockStatement newBody = new BlockStatement();
			foreach (Statement stmt in m2.Get<Statement>("statement"))
				newBody.Add(stmt.Detach());
			ForStatement forStatement = new ForStatement();
			forStatement.Initializers.Add(node);
			forStatement.Condition = loop.Condition.Detach();
			forStatement.Iterators.Add(m2.Get<Statement>("increment").Single().Detach());
			forStatement.EmbeddedStatement = newBody;
			loop.ReplaceWith(forStatement);
			var oldBody = loop.EmbeddedStatement as BlockStatement;
			if (oldBody != null) {
				newBody.HiddenStart = oldBody.HiddenStart;
				newBody.HiddenEnd = oldBody.HiddenEnd;
			}
			return forStatement;
		}
		#endregion

		static readonly WhileStatement whileTrueLoopPattern = new WhileStatement {
				Condition = new PrimitiveExpression(true),
				EmbeddedStatement = new BlockStatement {
					Statements = {
						new Repeat(new AnyNode("statement")),
					}
				}
			};
		ForStatement TransformWhileTrueToForLoop(WhileStatement whileLoop) {
			var m = whileTrueLoopPattern.Match(whileLoop);
			if (!m.Success)
				return null;

			var forStatement = new ForStatement();
			forStatement.EmbeddedStatement = whileLoop.EmbeddedStatement.Detach();
			if (context.CalculateILSpans) {
				var blockStmt = (BlockStatement)forStatement.EmbeddedStatement;
				if (blockStmt.HiddenStart == null)
					blockStmt.HiddenStart = whileLoop.Condition;
				else {
					var node = new EmptyStatement();
					blockStmt.HiddenStart.AddAllRecursiveILSpansTo(node);
					whileLoop.Condition.AddAllRecursiveILSpansTo(node);
					blockStmt.HiddenStart = node;
				}
			}
			whileLoop.ReplaceWith(forStatement);
			return forStatement;
		}

		#region doWhile
		static readonly WhileStatement doWhilePattern = new WhileStatement {
			Condition = new PrimitiveExpression(true),
			EmbeddedStatement = new BlockStatement {
				Statements = {
					new Repeat(new AnyNode("statement")),
					new IfElseStatement {
						Condition = new AnyNode("condition"),
						TrueStatement = new BlockStatement { new BreakStatement() }
					}
				}
			}};

		public DoWhileStatement TransformDoWhile(WhileStatement whileLoop)
		{
			Match m = doWhilePattern.Match(whileLoop);
			if (m.Success) {
				DoWhileStatement doLoop = new DoWhileStatement();
				doLoop.Condition = new UnaryOperatorExpression(UnaryOperatorType.Not, m.Get<Expression>("condition").Single().Detach());
				doLoop.Condition.AcceptVisitor(new PushNegation(), null);
				BlockStatement block = (BlockStatement)whileLoop.EmbeddedStatement;
				var ifStmt = block.Statements.Last();
				ifStmt.Remove();
				ifStmt.AddAllRecursiveILSpansTo(doLoop.Condition);
				doLoop.EmbeddedStatement = block.Detach();
				whileLoop.ReplaceWith(doLoop);
				block.HiddenStart = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompactList(whileLoop.Condition.GetAllRecursiveILSpans()), block.HiddenStart);

				// we may have to extract variable definitions out of the loop if they were used in the condition:
				foreach (var varDecl in block.Statements.OfType<VariableDeclarationStatement>()) {
					VariableInitializer v = varDecl.Variables.Single();
					if (doLoop.Condition.DescendantsAndSelf.OfType<IdentifierExpression>().Any(i => i.Identifier == v.Name)) {
						object tokenKind = null;
						var ilv = v.Annotation<ILVariable>();
						if (ilv != null)
							tokenKind = ilv.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local;
						var locParam = v.Annotation<IVariable>();
						if (tokenKind == null && locParam != null)
							tokenKind = context.MetadataTextColorProvider.GetColor(locParam);
						AssignmentExpression assign = new AssignmentExpression(IdentifierExpression.Create(v.Name, tokenKind ?? BoxedTextColor.Local), v.Initializer.Detach());
						// move annotations from v to assign:
						assign.CopyAnnotationsFrom(v);
						v.RemoveAnnotations<object>();
						// remove varDecl with assignment; and move annotations from varDecl to the ExpressionStatement:
						varDecl.ReplaceWith(new ExpressionStatement(assign).CopyAnnotationsFrom(varDecl).WithAnnotation(varDecl.GetAllRecursiveILSpans()));
						varDecl.RemoveAnnotations<object>();

						// insert the varDecl above the do-while loop:
						doLoop.Parent.InsertChildBefore(doLoop, varDecl, BlockStatement.StatementRole);
					}
				}
				return doLoop;
			}
			return null;
		}
		#endregion

		#region lock
		static readonly AstNode lockFlagInitPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression(Pattern.AnyString)),
				new PrimitiveExpression(false)
			));

		static readonly AstNode lockTryCatchPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new OptionalNode(new VariableDeclarationStatement()).ToStatement(),
				new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke2(
					BoxedTextColor.StaticMethod,
					"Enter", new AnyNode("enter"),
					new DirectionExpression {
						FieldDirection = FieldDirection.Ref,
						Expression = new NamedNode("flag", new IdentifierExpression(Pattern.AnyString))
					}),
				new Repeat(new AnyNode()).ToStatement()
			},
			FinallyBlock = new BlockStatement {
				new IfElseStatement {
					Condition = new Backreference("flag"),
					TrueStatement = new BlockStatement {
						new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke2(BoxedTextColor.StaticMethod, "Exit", new AnyNode("exit"))
					}
				}
			}};

		static readonly AstNode oldMonitorCallPattern = new ExpressionStatement(
			new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke("Enter", new AnyNode("enter"))
		);

		static readonly AstNode oldLockTryCatchPattern = new TryCatchStatement
		{
			TryBlock = new BlockStatement {
				new Repeat(new AnyNode()).ToStatement()
			},
			FinallyBlock = new BlockStatement {
				new TypePattern(typeof(System.Threading.Monitor)).ToType().Invoke("Exit", new AnyNode("exit"))
			}
		};

		bool AnalyzeLockV2(ExpressionStatement node, out Expression enter, out Expression exit)
		{
			enter = null;
			exit = null;
			Match m1 = oldMonitorCallPattern.Match(node);
			if (!m1.Success) return false;
			Match m2 = oldLockTryCatchPattern.Match(node.NextSibling);
			if (!m2.Success) return false;
			enter = m1.Get<Expression>("enter").Single();
			exit = m2.Get<Expression>("exit").Single();
			return true;
		}

		bool AnalyzeLockV4(ExpressionStatement node, out Expression enter, out Expression exit)
		{
			enter = null;
			exit = null;
			Match m1 = lockFlagInitPattern.Match(node);
			if (!m1.Success) return false;
			Match m2 = lockTryCatchPattern.Match(node.NextSibling);
			if (!m2.Success) return false;
			enter = m2.Get<Expression>("enter").Single();
			exit = m2.Get<Expression>("exit").Single();
			return m1.Get<IdentifierExpression>("variable").Single().Identifier == m2.Get<IdentifierExpression>("flag").Single().Identifier;
		}

		public LockStatement TransformLock(ExpressionStatement node)
		{
			Expression enter, exit;
			bool isV2 = AnalyzeLockV2(node, out enter, out exit);
			if (isV2 || AnalyzeLockV4(node, out enter, out exit)) {
				TryCatchStatement tryCatch = (TryCatchStatement)node.NextSibling;
				if (!exit.IsMatch(enter)) {
					// If exit and enter are not the same, then enter must be "exit = ..."
					AssignmentExpression assign = enter as AssignmentExpression;
					if (assign == null)
						return null;
					if (!exit.IsMatch(assign.Left))
						return null;
					enter = assign.Right;
					// TODO: verify that 'obj' variable can be removed
				}
				// TODO: verify that 'flag' variable can be removed
				// transform the code into a lock statement:
				LockStatement l = new LockStatement();
				l.Expression = enter.Detach();
				l.EmbeddedStatement = tryCatch.TryBlock.Detach();
				var block = (BlockStatement)l.EmbeddedStatement;
				if (block.HiddenStart != null) {
					block.HiddenStart.AddAllRecursiveILSpansTo(l.Expression);
					block.HiddenStart = null;
				}
				if (!isV2) { // Remove 'Enter()' call
					var enterCall = block.Statements.First();
					enterCall.Remove();
					enterCall.AddAllRecursiveILSpansTo(l.Expression);
				}
				tryCatch.ReplaceWith(l);
				if (context.CalculateILSpans)
					block.HiddenEnd = NRefactoryExtensions.CreateHidden(block.HiddenEnd, tryCatch.FinallyBlock);
				node.AddAllRecursiveILSpansTo(l.Expression);
				node.Remove(); // remove flag variable
				return l;
			}
			return null;
		}
		#endregion

		#region switch on strings
		static readonly IfElseStatement switchOnStringPattern = new IfElseStatement {
			Condition = new BinaryOperatorExpression {
				Left = new AnyNode("switchExpr"),
				Operator = BinaryOperatorType.InEquality,
				Right = new NullReferenceExpression()
			},
			TrueStatement = new BlockStatement {
				new IfElseStatement {
					Condition = new BinaryOperatorExpression {
						Left = new AnyNode("cachedDict"),
						Operator = BinaryOperatorType.Equality,
						Right = new NullReferenceExpression()
					},
					TrueStatement = new AnyNode("dictCreation")
				},
				new IfElseStatement {
					Condition = new Backreference("cachedDict").ToExpression().Invoke(
						"TryGetValue",
						new NamedNode("switchVar", new IdentifierExpression(Pattern.AnyString)),
						new DirectionExpression {
							FieldDirection = FieldDirection.Out,
							Expression = new IdentifierExpression(Pattern.AnyString).WithName("intVar")
						}),
					TrueStatement = new BlockStatement {
						Statements = {
							new NamedNode(
								"switch", new SwitchStatement {
									Expression = new IdentifierExpressionBackreference("intVar"),
									SwitchSections = { new Repeat(new AnyNode()) }
								})
						}
					}
				},
				new Repeat(new AnyNode("nonNullDefaultStmt")).ToStatement()
			},
			FalseStatement = new OptionalNode("nullStmt", new BlockStatement { Statements = { new Repeat(new AnyNode()) } })
		};

		public SwitchStatement TransformSwitchOnString(IfElseStatement node)
		{
			Match m = switchOnStringPattern.Match(node);
			if (!m.Success)
				return null;
			// switchVar must be the same as switchExpr; or switchExpr must be an assignment and switchVar the left side of that assignment
			if (!m.Get("switchVar").Single().IsMatch(m.Get("switchExpr").Single())) {
				AssignmentExpression assign = m.Get("switchExpr").Single() as AssignmentExpression;
				if (!(assign != null && m.Get("switchVar").Single().IsMatch(assign.Left)))
					return null;
			}
			IField cachedDictField = m.Get<AstNode>("cachedDict").Single().Annotation<IField>();
			if (cachedDictField == null)
				return null;
			List<Statement> dictCreation = m.Get<BlockStatement>("dictCreation").Single().Statements.ToList();
			List<KeyValuePair<string, int>> dict = BuildDictionary(dictCreation);
			SwitchStatement sw = m.Get<SwitchStatement>("switch").Single();
			var oldExpr = sw.Expression;
			sw.Expression = m.Get<Expression>("switchExpr").Single().Detach();
			oldExpr.AddAllRecursiveILSpansTo(sw.Expression);
			foreach (SwitchSection section in sw.SwitchSections) {
				List<CaseLabel> labels = section.CaseLabels.ToList();
				section.CaseLabels.Clear();
				foreach (CaseLabel label in labels) {
					PrimitiveExpression expr = label.Expression as PrimitiveExpression;
					if (expr == null || !(expr.Value is int))
						continue;
					int val = (int)expr.Value;
					foreach (var pair in dict) {
						if (pair.Value == val)
							section.CaseLabels.Add(new CaseLabel { Expression = new PrimitiveExpression(pair.Key) });
					}
				}
			}
			if (m.Has("nullStmt")) {
				SwitchSection section = new SwitchSection();
				section.CaseLabels.Add(new CaseLabel { Expression = new NullReferenceExpression() });
				BlockStatement block = m.Get<BlockStatement>("nullStmt").Single();
				block.Statements.Add(new BreakStatement());
				section.Statements.Add(block.Detach());
				sw.SwitchSections.Add(section);
			} else if (m.Has("nonNullDefaultStmt")) {
				sw.SwitchSections.Add(
					new SwitchSection {
						CaseLabels = { new CaseLabel { Expression = new NullReferenceExpression() } },
						Statements = { new BlockStatement { new BreakStatement() } }
					});
			}
			if (m.Has("nonNullDefaultStmt")) {
				SwitchSection section = new SwitchSection();
				section.CaseLabels.Add(new CaseLabel());
				BlockStatement block = new BlockStatement();
				block.Statements.AddRange(m.Get<Statement>("nonNullDefaultStmt").Select(s => s.Detach()));
				block.Add(new BreakStatement());
				section.Statements.Add(block);
				sw.SwitchSections.Add(section);
			}
			node.ReplaceWith(sw);
			node.AddAllRecursiveILSpansTo(sw.Expression);
			return sw;
		}

		List<KeyValuePair<string, int>> BuildDictionary(List<Statement> dictCreation)
		{
			if (context.Settings.ObjectOrCollectionInitializers && dictCreation.Count == 1)
				return BuildDictionaryFromInitializer(dictCreation[0]);

			return BuildDictionaryFromAddMethodCalls(dictCreation);
		}

		static readonly Statement assignInitializedDictionary = new ExpressionStatement {
			Expression = new AssignmentExpression {
				Left = new AnyNode().ToExpression(),
				Right = new ObjectCreateExpression {
					Type = new AnyNode(),
					Arguments = { new Repeat(new AnyNode()) },
					Initializer = new ArrayInitializerExpression {
						Elements = { new Repeat(new AnyNode("dictJumpTable")) }
					}
				},
			},
		};

		private List<KeyValuePair<string, int>> BuildDictionaryFromInitializer(Statement statement)
		{
			List<KeyValuePair<string, int>> dict = new List<KeyValuePair<string, int>>();
			Match m = assignInitializedDictionary.Match(statement);
			if (!m.Success)
				return dict;

			foreach (ArrayInitializerExpression initializer in m.Get<ArrayInitializerExpression>("dictJumpTable")) {
				KeyValuePair<string, int> pair;
				if (TryGetPairFrom(initializer.Elements, out pair))
					dict.Add(pair);
			}

			return dict;
		}

		private static List<KeyValuePair<string, int>> BuildDictionaryFromAddMethodCalls(List<Statement> dictCreation)
		{
			List<KeyValuePair<string, int>> dict = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < dictCreation.Count; i++) {
				ExpressionStatement es = dictCreation[i] as ExpressionStatement;
				if (es == null)
					continue;
				InvocationExpression ie = es.Expression as InvocationExpression;
				if (ie == null)
					continue;

				KeyValuePair<string, int> pair;
				if (TryGetPairFrom(ie.Arguments, out pair))
					dict.Add(pair);
			}
			return dict;
		}

		private static bool TryGetPairFrom(AstNodeCollection<Expression> expressions, out KeyValuePair<string, int> pair)
		{
			PrimitiveExpression arg1 = expressions.ElementAtOrDefault(0) as PrimitiveExpression;
			PrimitiveExpression arg2 = expressions.ElementAtOrDefault(1) as PrimitiveExpression;
			if (arg1 != null && arg2 != null && arg1.Value is string && arg2.Value is int) {
				pair = new KeyValuePair<string, int>((string)arg1.Value, (int)arg2.Value);
				return true;
			}

			pair = default(KeyValuePair<string, int>);
			return false;
		}

		#endregion

		#region Automatic Properties
		static readonly PropertyDeclaration automaticPropertyPattern = new PropertyDeclaration {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			ReturnType = new AnyNode(),
			PrivateImplementationType = new OptionalNode(new AnyNode()),
			Name = Pattern.AnyString,
			Getter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new ReturnStatement {
						Expression = new AnyNode("fieldReference")
					}
				}
			},
			Setter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new AssignmentExpression {
						Left = new Backreference("fieldReference"),
						Right = IdentifierExpression.Create("value", BoxedTextColor.Keyword)
					}
				}}};
		static readonly PropertyDeclaration automaticReadOnlyPropertyPattern = new PropertyDeclaration {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			ReturnType = new AnyNode(),
			PrivateImplementationType = new OptionalNode(new AnyNode()),
			Name = Pattern.AnyString,
			Getter = new Accessor {
				Attributes = { new Repeat(new AnyNode()) },
				Modifiers = Modifiers.Any,
				Body = new BlockStatement {
					new ReturnStatement {
						Expression = new AnyNode("fieldReference")
					}
				}
			}};

		PropertyDeclaration TransformAutomaticProperties(PropertyDeclaration property)
		{
			PropertyDef prop = property.Annotation<PropertyDef>();
			if (prop == null || prop.GetMethod == null || prop.HasOtherMethods)
				return null;
			if (!prop.GetMethod.IsCompilerGenerated())
				return null;
			if (prop.SetMethod != null && !prop.SetMethod.IsCompilerGenerated())
				return null;
			Match m = automaticPropertyPattern.Match(property);
			if (!m.Success)
				m = automaticReadOnlyPropertyPattern.Match(property);
			if (m.Success) {
				FieldDef field = m.Get<AstNode>("fieldReference").Single().Annotation<IField>().ResolveFieldWithinSameModule();
				if (field != null && field.IsCompilerGenerated() && field.DeclaringType == prop.DeclaringType) {
					RemoveCompilerGeneratedAttribute(property.Getter.Attributes);
					RemoveCompilerGeneratedAttribute(property.Setter.Attributes);
					var getterMM = property.Getter.Body.Annotation<MethodDebugInfoBuilder>();
					var setterMM = property.Setter.Body.Annotation<MethodDebugInfoBuilder>();
					if (getterMM != null)
						property.Getter.AddAnnotation(getterMM);
					if (setterMM != null)
						property.Setter.AddAnnotation(setterMM);
					property.Getter.Body = null;
					property.Setter.Body = null;
					if (prop.GetMethod.Body != null)
						property.Getter.AddAnnotation(new List<ILSpan>(1) { new ILSpan(0, (uint)prop.GetMethod.Body.GetCodeSize()) });
					if (prop.SetMethod?.Body != null)
						property.Setter.AddAnnotation(new List<ILSpan>(1) { new ILSpan(0, (uint)prop.SetMethod.Body.GetCodeSize()) });
				}
			}
			// Since the event instance is not changed, we can continue in the visitor as usual, so return null
			return null;
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String compilerGeneratedAttributeString = new UTF8String("CompilerGeneratedAttribute");
		static readonly UTF8String methodImplAttributeString = new UTF8String("MethodImplAttribute");
		static readonly KeyValuePair<UTF8String, UTF8String>[] compilerGeneratedAttributeNames = new KeyValuePair<UTF8String, UTF8String>[] {
			new KeyValuePair<UTF8String, UTF8String>(systemRuntimeCompilerServicesString, compilerGeneratedAttributeString),
		};
		static readonly KeyValuePair<UTF8String, UTF8String>[] eventAttributesToRemove = new KeyValuePair<UTF8String, UTF8String>[] {
			new KeyValuePair<UTF8String, UTF8String>(systemRuntimeCompilerServicesString, compilerGeneratedAttributeString),
			new KeyValuePair<UTF8String, UTF8String>(systemRuntimeCompilerServicesString, methodImplAttributeString),
		};
		void RemoveCompilerGeneratedAttribute(AstNodeCollection<AttributeSection> attributeSections) =>
			RemoveAttribuets(attributeSections, compilerGeneratedAttributeNames);
		void RemoveEventAttributes(AstNodeCollection<AttributeSection> attributeSections) =>
			RemoveAttribuets(attributeSections, eventAttributesToRemove);
		void RemoveAttribuets(AstNodeCollection<AttributeSection> attributeSections, KeyValuePair<UTF8String, UTF8String>[] attrNames) {
			foreach (AttributeSection section in attributeSections) {
				foreach (var attr in section.Attributes) {
					ITypeDefOrRef tr = attr.Type.Annotation<ITypeDefOrRef>();
					if (tr == null)
						continue;
					foreach (var kv in attrNames) {
						if (tr.Compare(kv.Key, kv.Value)) {
							attr.Remove();
							break;
						}
					}
				}
				if (section.Attributes.Count == 0)
					section.Remove();
			}
		}
		#endregion

		#region Automatic Events
		static readonly Accessor automaticEventPatternV4 = new Accessor {
			Attributes = { new Repeat(new AnyNode()) },
			Body = new BlockStatement {
				new VariableDeclarationStatement { Type = new AnyNode("type"), Variables = { new AnyNode() } },
				new VariableDeclarationStatement { Type = new Backreference("type"), Variables = { new AnyNode() } },
				new VariableDeclarationStatement { Type = new Backreference("type"), Variables = { new AnyNode() } },
				new AssignmentExpression {
					Left = new NamedNode("var1", new IdentifierExpression(Pattern.AnyString)),
					Operator = AssignmentOperatorType.Assign,
					Right = new NamedNode(
						"field",
						new MemberReferenceExpression {
							Target = new Choice { new ThisReferenceExpression(), new TypeReferenceExpression { Type = new AnyNode() } },
							MemberName = Pattern.AnyString
						})
				},
				new DoWhileStatement {
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(new NamedNode("var2", new IdentifierExpression(Pattern.AnyString)), new IdentifierExpressionBackreference("var1")),
						new AssignmentExpression {
							Left = new NamedNode("var3", new IdentifierExpression(Pattern.AnyString)),
							Operator = AssignmentOperatorType.Assign,
							Right = new AnyNode("delegateCombine").ToExpression().Invoke(
								new IdentifierExpressionBackreference("var2"),
								IdentifierExpression.Create("value", BoxedTextColor.Keyword)
							).CastTo(new Backreference("type"))
						},
						new AssignmentExpression {
							Left = new IdentifierExpressionBackreference("var1"),
							Right = new TypePattern(typeof(System.Threading.Interlocked)).ToType().Invoke(
								BoxedTextColor.StaticMethod,
								"CompareExchange",
								new AstType[] { new Backreference("type") }, // type argument
								new Expression[] { // arguments
									new DirectionExpression { FieldDirection = FieldDirection.Ref, Expression = new Backreference("field") },
									new IdentifierExpressionBackreference("var3"),
									new IdentifierExpressionBackreference("var2")
								}
							)}
					},
					Condition = new BinaryOperatorExpression {
						Left = new IdentifierExpressionBackreference("var1"),
						Operator = BinaryOperatorType.InEquality,
						Right = new IdentifierExpressionBackreference("var2")
					}}
			}};
		// mcs, Mono 4.6.2
		static readonly Accessor automaticEventPatternMcs46 = new Accessor {
			Body = new BlockStatement {
				new VariableDeclarationStatement { Type = new AnyNode("type"), Variables = { new AnyNode() } },
				new VariableDeclarationStatement { Type = new Backreference("type"), Variables = { new AnyNode() } },
				new ExpressionStatement {
					Expression = new AssignmentExpression {
						Left = new NamedNode("var1", new IdentifierExpression(Pattern.AnyString)),
						Operator = AssignmentOperatorType.Assign,
						Right = new NamedNode(
							"field",
							new MemberReferenceExpression {
								Target = new Choice { new ThisReferenceExpression(), new TypeReferenceExpression { Type = new AnyNode() } },
								MemberName = Pattern.AnyString
							})
					},
				},
				new DoWhileStatement {
					EmbeddedStatement = new BlockStatement {
						new AssignmentExpression(new NamedNode("var2", new IdentifierExpression(Pattern.AnyString)), new IdentifierExpressionBackreference("var1")),
						new AssignmentExpression {
							Left = new IdentifierExpressionBackreference("var1"),
							Right = new TypePattern(typeof(System.Threading.Interlocked)).ToType().Invoke(
								BoxedTextColor.StaticMethod,
								"CompareExchange",
								new AstType[] { new Backreference("type") }, // type argument
								new Expression[] { // arguments
									new DirectionExpression { FieldDirection = FieldDirection.Ref, Expression = new Backreference("field") },
									new AnyNode("delegateCombine").ToExpression().Invoke(
										new IdentifierExpressionBackreference("var2"),
										IdentifierExpression.Create("value", BoxedTextColor.Keyword)
									).CastTo(new Backreference("type")),
									new IdentifierExpressionBackreference("var1")
								}
							)}
					},
					Condition = new BinaryOperatorExpression {
						Left = new IdentifierExpressionBackreference("var1"),
						Operator = BinaryOperatorType.InEquality,
						Right = new IdentifierExpressionBackreference("var2")
					}}
			}};
		// csc 2.0-3.5
		// Accessors have MethodImplOptions.Synchronized set
		// this.SomeEvent = (EventHandler)Delegate.Combine(this.SomeEvent, value);
		static readonly Accessor automaticEventPatternV35 = new Accessor {
			Attributes = { new Repeat(new AnyNode()) },
			Body = new BlockStatement {
				new ExpressionStatement {
					Expression = new AssignmentExpression {
						Left = new NamedNode("field", new MemberReferenceExpression {
							Target = new Choice { new ThisReferenceExpression(), new TypeReferenceExpression { Type = new AnyNode() } },
							MemberName = Pattern.AnyString
						}),
						Operator = AssignmentOperatorType.Assign,
						Right = new AnyNode("delegateCombine").ToExpression().Invoke(
								new Backreference("field"),
								IdentifierExpression.Create("value", BoxedTextColor.Keyword)
							).CastTo(new AnyNode())
					},
				},
			}};

		bool CheckAutomaticEventV4Match(Match m, CustomEventDeclaration ev, bool isAddAccessor, bool hasType)
		{
			if (!m.Success)
				return false;
			if (!AstBuilder.IsEventBackingFieldName(m.Get<MemberReferenceExpression>("field").Single().MemberName, ev.Name))
				return false;
			if (hasType && !ev.ReturnType.IsMatch(m.Get("type").Single()))
				return false; // variable types must match event type
			var combineMethod = m.Get<AstNode>("delegateCombine").Single().Parent.Annotation<IMethod>();
			if (combineMethod == null || combineMethod.Name != (isAddAccessor ? "Combine" : "Remove"))
				return false;
			return combineMethod.DeclaringType != null && combineMethod.DeclaringType.FullName == "System.Delegate";
		}

		EventDeclaration TransformAutomaticEvents(CustomEventDeclaration ev)
		{
			var pat = automaticEventPatternV4;
			bool hasType = true;
			Match m1 = pat.Match(ev.AddAccessor);
			if (!m1.Success)
				m1 = (pat = automaticEventPatternMcs46).Match(ev.AddAccessor);
			if (!m1.Success) {
				m1 = (pat = automaticEventPatternV35).Match(ev.AddAccessor);
				hasType = false;
			}
			if (!CheckAutomaticEventV4Match(m1, ev, true, hasType))
				return null;
			Match m2 = pat.Match(ev.RemoveAccessor);
			if (!CheckAutomaticEventV4Match(m2, ev, false, hasType))
				return null;
			EventDeclaration ed = new EventDeclaration();
			ev.Attributes.MoveTo(ed.Attributes);
			foreach (var attr in ev.AddAccessor.Attributes) {
				attr.AttributeTarget = "method";
				ed.Attributes.Add(attr.Detach());
			}
			ed.ReturnType = ev.ReturnType.Detach();
			ed.Modifiers = ev.Modifiers;
			ed.Variables.Add(new VariableInitializer(context.MetadataTextColorProvider.GetColor(ev.Annotation<EventDef>()), ev.Name));
			ed.CopyAnnotationsFrom(ev);

			// Keep the token comments
			foreach (var child in ev.Children.Reverse().ToArray()) {
				var cmt = child as Comment;
				if (cmt != null) {
					ed.InsertChildAfter(null, cmt.Detach(), Roles.Comment);
					continue;
				}

				var acc = child as Accessor;
				if (acc != null) {
					foreach (var accChild in acc.Children.Reverse().ToArray()) {
						var accCmt = accChild as Comment;
						if (accCmt != null)
							ed.InsertChildAfter(null, accCmt.Detach(), Roles.Comment);
					}
					continue;
				}
			}

			EventDef eventDef = ev.Annotation<EventDef>();
			if (eventDef != null) {
				FieldDef field = eventDef.DeclaringType.Fields.FirstOrDefault(f => f.Name == ev.Name);
				if (field != null) {
					ed.AddAnnotation(field);
					AstBuilder.ConvertAttributes(context.MetadataTextColorProvider, ed, field, context.Settings, stringBuilder, "field");
				}
			}

			RemoveEventAttributes(ed.Attributes);
			ev.ReplaceWith(ed);
			ev.AddAllRecursiveILSpansTo(ev);
			return ed;
		}
		#endregion

		#region Destructor
		static readonly MethodDeclaration destructorPattern = new MethodDeclaration {
			Attributes = { new Repeat(new AnyNode()) },
			Modifiers = Modifiers.Any,
			ReturnType = new PrimitiveType("void"),
			Name = "Finalize",
			Body = new BlockStatement {
				new TryCatchStatement {
					TryBlock = new AnyNode("body"),
					FinallyBlock = new BlockStatement {
						new BaseReferenceExpression().Invoke("Finalize")
					}
				}
			}
		};

		DestructorDeclaration TransformDestructor(MethodDeclaration methodDef)
		{
			Match m = destructorPattern.Match(methodDef);
			if (m.Success) {
				DestructorDeclaration dd = new DestructorDeclaration();
				dd.AddAnnotation(methodDef.Annotation<MethodDef>());
				methodDef.Attributes.MoveTo(dd.Attributes);
				dd.Modifiers = methodDef.Modifiers & ~(Modifiers.Protected | Modifiers.Override);
				dd.Body = m.Get<BlockStatement>("body").Single().Detach();
				dd.AddAnnotation(methodDef.Annotation<MethodDebugInfoBuilder>());
				var tc = (TryCatchStatement)methodDef.Body.FirstChild;
				if (context.CalculateILSpans) {
					dd.Body.HiddenStart = NRefactoryExtensions.CreateHidden(dd.Body.HiddenStart, methodDef.Body.HiddenStart);
					dd.Body.HiddenEnd = NRefactoryExtensions.CreateHidden(dd.Body.HiddenEnd, methodDef.Body.HiddenEnd, tc.FinallyBlock);
				}
				dd.NameToken = Identifier.Create(AstBuilder.CleanName(context.CurrentType.Name)).WithAnnotation(context.CurrentType);
				methodDef.ReplaceWith(dd);
				foreach (var child in methodDef.Children.Reverse().ToArray()) {
					var cmt = child as Comment;
					if (cmt != null) {
						cmt.Detach();
						dd.InsertChildAfter(null, cmt, Roles.Comment);
					}
				}
				return dd;
			}
			return null;
		}
		#endregion

		#region Try-Catch-Finally
		static readonly TryCatchStatement tryCatchFinallyPattern = new TryCatchStatement {
			TryBlock = new BlockStatement {
				new TryCatchStatement {
					TryBlock = new AnyNode(),
					CatchClauses = { new Repeat(new AnyNode()) }
				}
			},
			FinallyBlock = new AnyNode()
		};

		/// <summary>
		/// Simplify nested 'try { try {} catch {} } finally {}'.
		/// This transformation must run after the using/lock tranformations.
		/// </summary>
		TryCatchStatement TransformTryCatchFinally(TryCatchStatement tryFinally)
		{
			if (tryCatchFinallyPattern.IsMatch(tryFinally)) {
				TryCatchStatement tryCatch = (TryCatchStatement)tryFinally.TryBlock.Statements.Single();
				if (context.CalculateILSpans) {
					tryCatch.TryBlock.HiddenStart = NRefactoryExtensions.CreateHidden(tryCatch.TryBlock.HiddenStart, tryFinally.TryBlock.HiddenStart);
					tryCatch.TryBlock.HiddenEnd = NRefactoryExtensions.CreateHidden(tryCatch.TryBlock.HiddenEnd, tryFinally.TryBlock.HiddenEnd);
				}
				tryFinally.TryBlock = tryCatch.TryBlock.Detach();
				tryCatch.CatchClauses.MoveTo(tryFinally.CatchClauses);
			}
			// Since the tryFinally instance is not changed, we can continue in the visitor as usual, so return null
			return null;
		}
		#endregion

		#region Simplify cascading if-else-if statements
		static readonly IfElseStatement cascadingIfElsePattern = new IfElseStatement
		{
			Condition = new AnyNode(),
			TrueStatement = new AnyNode(),
			FalseStatement = new BlockStatement {
				Statements = {
					new NamedNode(
						"nestedIfStatement",
						new IfElseStatement {
							Condition = new AnyNode(),
							TrueStatement = new AnyNode(),
							FalseStatement = new OptionalNode(new AnyNode())
						}
					)
				}
			}
		};

		AstNode SimplifyCascadingIfElseStatements(IfElseStatement node)
		{
			Match m = cascadingIfElsePattern.Match(node);
			if (m.Success) {
				IfElseStatement elseIf = m.Get<IfElseStatement>("nestedIfStatement").Single();
				var block = (BlockStatement)node.FalseStatement;
				node.FalseStatement = elseIf.Detach();
				block.HiddenStart.AddAllRecursiveILSpansTo(node.Condition);
				if (block.HiddenEnd != null) {
					var stmt = elseIf.FalseStatement.IsNull ? elseIf.TrueStatement : elseIf.FalseStatement;
					var block2 = stmt as BlockStatement;
					if (block2 != null) {
						if (context.CalculateILSpans)
							block2.HiddenEnd = NRefactoryExtensions.CreateHidden(block2.HiddenEnd, block.HiddenEnd);
					}
					else
						block.HiddenEnd.AddAllRecursiveILSpansTo(stmt);
				}
			}

			return null;
		}
		#endregion
	}
}
