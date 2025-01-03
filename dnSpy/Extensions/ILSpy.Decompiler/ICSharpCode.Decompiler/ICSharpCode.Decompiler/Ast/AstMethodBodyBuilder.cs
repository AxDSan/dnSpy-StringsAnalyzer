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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast {
	using dnSpy.Contracts.Decompiler;
	using dnSpy.Contracts.Text;
	using Ast = ICSharpCode.NRefactory.CSharp;

	public class AstMethodBodyBuilder
	{
		StringBuilder stringBuilder;
		MethodDef methodDef;
		ICorLibTypes corLib;
		DecompilerContext context;
		bool valueParameterIsKeyword;
		AutoPropertyProvider autoPropertyProvider;
		readonly HashSet<ILVariable> localVariablesToDefine = new HashSet<ILVariable>(); // local variables that are missing a definition
		readonly List<ILNode> ILNode_List = new List<ILNode>();

		public void Reset()
		{
			autoPropertyProvider = null;
			localVariablesToDefine.Clear();
			ILNode_List.Clear();
		}

		/// <summary>
		/// Creates the body for the method definition.
		/// </summary>
		/// <param name="methodDef">Method definition to decompile.</param>
		/// <param name="context">Decompilation context.</param>
		/// <param name="parameters">Parameter declarations of the method being decompiled.
		/// These are used to update the parameter names when the decompiler generates names for the parameters.</param>
		/// <returns>Block for the method body</returns>
		internal static BlockStatement CreateMethodBody(MethodDef methodDef,
		                                              DecompilerContext context,
													  AutoPropertyProvider autoPropertyProvider,
													  IEnumerable<ParameterDeclaration> parameters,
													  bool valueParameterIsKeyword,
													  StringBuilder sb,
													  out MethodDebugInfoBuilder stmtsBuilder)
		{
			MethodDef oldCurrentMethod = context.CurrentMethod;
			Debug.Assert(oldCurrentMethod == null || oldCurrentMethod == methodDef);
			context.CurrentMethod = methodDef;
			context.CurrentMethodIsAsync = false;
			context.CurrentMethodIsYieldReturn = false;
			var builder = context.Cache.GetAstMethodBodyBuilder();
			try {
				builder.stringBuilder = sb;
				builder.methodDef = methodDef;
				builder.context = context;
				builder.corLib = methodDef.Module.CorLibTypes;
				builder.valueParameterIsKeyword = valueParameterIsKeyword;
				builder.autoPropertyProvider = autoPropertyProvider;
				if (Debugger.IsAttached) {
					return builder.CreateMethodBody(parameters, out stmtsBuilder);
				} else {
					try {
						return builder.CreateMethodBody(parameters, out stmtsBuilder);
					} catch (OperationCanceledException) {
						throw;
					} catch (Exception ex) {
						throw new ICSharpCode.Decompiler.DecompilerException(methodDef, ex);
					}
				}
			} finally {
				context.CurrentMethod = oldCurrentMethod;
				context.Cache.Return(builder);
			}
		}

		BlockStatement CreateMethodBody(IEnumerable<ParameterDeclaration> parameters, out MethodDebugInfoBuilder builder)
		{
			if (methodDef.Body == null) {
				builder = null;
				return null;
			}

			context.CancellationToken.ThrowIfCancellationRequested();
			ILBlock ilMethod = new ILBlock(CodeBracesRangeFlags.MethodBraces);
			var astBuilder = context.Cache.GetILAstBuilder();
			StateMachineKind stateMachineKind;
			MethodDef inlinedMethod = null;
			HashSet<ILVariable> localVariables;
			AsyncMethodDebugInfo asyncInfo;
			string compilerName;
			try {
				ilMethod.Body = astBuilder.Build(methodDef, true, context);

				context.CancellationToken.ThrowIfCancellationRequested();
				var optimizer = context.Cache.GetILAstOptimizer();
				try {
					int variableMapVersion = context.variableMap?.Version ?? -1;
					optimizer.Optimize(context, ilMethod, autoPropertyProvider, out stateMachineKind, out inlinedMethod, out asyncInfo);
					// Only do it if Optimize() didn't do it already
					if (context.variableMap != null && context.variableMap.Version == variableMapVersion)
						YieldReturnDecompiler.TranslateFieldsToLocalAccess(ilMethod.Body, context.variableMap, null, context.CalculateILSpans, false);
					compilerName = optimizer.CompilerName;
				}
				finally {
					context.Cache.Return(optimizer);
				}
				context.CancellationToken.ThrowIfCancellationRequested();

				localVariables = new HashSet<ILVariable>(GetVariables(ilMethod));
				Debug.Assert(context.CurrentMethod == methodDef);
				NameVariables.AssignNamesToVariables(context, astBuilder.Parameters, localVariables, ilMethod, stringBuilder);

				if (parameters != null) {
					foreach (var pair in parameters.Join(astBuilder.Parameters, p => p.Annotation<Parameter>(),
								 v => v.OriginalParameter, (p, v) => (p, v))) {
						pair.p.NameToken = Identifier.Create(pair.v.Name).WithAnnotation(GetParameterColor(pair.v)).WithAnnotation(pair.v);
					}
				}
			}
			finally {
				context.Cache.Return(astBuilder);
			}

			context.CancellationToken.ThrowIfCancellationRequested();
			Ast.BlockStatement astBlock = TransformBlock(ilMethod);
			CommentStatement.ReplaceAll(astBlock); // convert CommentStatements to Comments

			Statement insertionPoint = astBlock.Statements.FirstOrDefault();
			foreach (ILVariable v in localVariablesToDefine) {
				if (v.Declared)
					continue;
				v.Declared = true;
				AstType type;
				if (v.Type.ContainsAnonymousType())
					type = new SimpleType("var").WithAnnotation(BoxedTextColor.Keyword);
				else
					type = AstBuilder.ConvertType(v.Type, stringBuilder);
				bool isRefType = v.Type.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef && AstBuilder.UndoByRefToPointer(type);
				var newVarDecl = new VariableDeclarationStatement(GetParameterColor(v), type, v.Name);
				if (isRefType)
					newVarDecl.Modifiers |= Modifiers.Ref;
				newVarDecl.Variables.Single().AddAnnotation(v);
				astBlock.Statements.InsertBefore(insertionPoint, newVarDecl);
			}

			builder = new MethodDebugInfoBuilder(context.SettingsVersion, stateMachineKind, inlinedMethod ?? methodDef, inlinedMethod != null ? methodDef : null, CreateSourceLocals(localVariables), CreateSourceParameters(astBuilder.Parameters), asyncInfo);
			builder.CompilerName = compilerName;

			return astBlock;
		}

		IEnumerable<ILVariable> GetVariables(ILBlock ilMethod) {
			var ns = ilMethod.GetSelfAndChildrenRecursive(ILNode_List);
			for (int i = 0; i < ns.Count; i++) {
				var n = ns[i];
				if (n is ILExpression expr) {
					if (expr.Operand is ILVariable v && !v.IsParameter)
						yield return v;
					continue;
				}

				if (n is ILTryCatchBlock.CatchBlockBase cb && cb.ExceptionVariable != null)
					yield return cb.ExceptionVariable;
			}
		}

		readonly List<SourceLocal> sourceLocalsList = new List<SourceLocal>();
		SourceLocal[] CreateSourceLocals(HashSet<ILVariable> variables) {
			foreach (var v in variables) {
				if (v.IsParameter)
					continue;
				sourceLocalsList.Add(v.GetSourceLocal());
			}
			var array = sourceLocalsList.ToArray();
			sourceLocalsList.Clear();
			return array;
		}

		readonly List<SourceParameter> sourceParametersList = new List<SourceParameter>();
		SourceParameter[] CreateSourceParameters(List<ILVariable> variables) {
			for (int i = 0; i < variables.Count; i++) {
				var v = variables[i];
				if (!v.IsParameter)
					continue;
				sourceParametersList.Add(v.GetSourceParameter());
			}

			var array = sourceParametersList.ToArray();
			sourceParametersList.Clear();
			return array;
		}

		Ast.Expression TransformBlockExpression(ILTryCatchBlock.FilterILBlock block) {
			if (block == null)
				return null;
			var expr = TryTransformBlockExpression(block);
			if (expr != null) {
				if (context.CalculateILSpans && block.StlocILSpans.Count != 0)
					expr.AddAnnotation(block.StlocILSpans);
				return expr;
			}
			// Show something...
			var body = TransformBlock(block);
			body.InsertChildAfter(null, new Comment(" Failed to create a 'catch-when' expression"), Roles.Comment);
			return new AnonymousMethodExpression { Body = body };
		}

		Ast.Expression TryTransformBlockExpression(ILTryCatchBlock.FilterILBlock block)
		{
			var body = block.Body;
			if (body.Count != 1)
				return null;
			var expr = body[0] as ILExpression;
			if (expr == null)
				return null;

			if (context.CalculateILSpans)
				expr.ILSpans.AddRange(body[0].ILSpans);

			return TransformExpression(expr) as Ast.Expression;
		}

		Ast.BlockStatement TransformBlock(ILBlock block)
		{
			Ast.BlockStatement astBlock = new BlockStatement();
			if (block != null) {
				astBlock.HiddenStart = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompact(block.ILSpans), astBlock.HiddenStart);
				astBlock.HiddenEnd = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompact(block.EndILSpans), astBlock.HiddenEnd);
				foreach(ILNode node in block.GetChildren()) {
					var stmt = TransformNode(node);
					if (stmt != null)
						astBlock.Statements.Add(stmt);
				}
			}
			return astBlock;
		}

		Statement TransformNode(ILNode node)
		{
			if (node is ILLabel) {
				var lbl = new Ast.LabelStatement { Label = ((ILLabel)node).Name };
				if (context.CalculateILSpans)
					lbl.AddAnnotation(node.ILSpans);
				return lbl;
			} else if (node is ILExpression) {
				AstNode codeExpr = TransformExpression((ILExpression)node);
				if (codeExpr != null) {
					if (codeExpr is Ast.Expression) {
						return new Ast.ExpressionStatement { Expression = (Ast.Expression)codeExpr };
					} else if (codeExpr is Ast.Statement) {
						return (Ast.Statement)codeExpr;
					} else {
						throw new Exception();
					}
				}
				return null;
			} else if (node is ILWhileLoop) {
				ILWhileLoop ilLoop = (ILWhileLoop)node;
				Expression expr;
				WhileStatement whileStmt = new WhileStatement() {
					Condition = expr = ilLoop.Condition != null ? (Expression)TransformExpression(ilLoop.Condition) : new PrimitiveExpression(true),
					EmbeddedStatement = TransformBlock(ilLoop.BodyBlock)
				};
				if (context.CalculateILSpans)
					expr.AddAnnotation(ilLoop.ILSpans);
				return whileStmt;
			} else if (node is ILCondition) {
				ILCondition conditionalNode = (ILCondition)node;
				bool hasFalseBlock = conditionalNode.FalseBlock.EntryGoto != null || conditionalNode.FalseBlock.Body.Count > 0;
				BlockStatement trueStmt;
				var ifElseStmt = new Ast.IfElseStatement {
					Condition = (Expression)TransformExpression(conditionalNode.Condition),
					TrueStatement = trueStmt = TransformBlock(conditionalNode.TrueBlock),
					FalseStatement = hasFalseBlock ? TransformBlock(conditionalNode.FalseBlock) : null
				};
				if (context.CalculateILSpans)
					ifElseStmt.Condition.AddAnnotation(conditionalNode.ILSpans);
				if (ifElseStmt.FalseStatement == null)
					trueStmt.HiddenEnd = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : conditionalNode.FalseBlock.GetSelfAndChildrenRecursiveILSpans_OrderAndJoin(), trueStmt.HiddenEnd);
				return ifElseStmt;
			} else if (node is ILSwitch) {
				ILSwitch ilSwitch = (ILSwitch)node;
				if (ilSwitch.Condition.InferredType.GetElementType() == ElementType.Boolean && (
					from cb in ilSwitch.CaseBlocks
					where cb.Values != null
					from val in cb.Values
					select val
				).Any(val => val != 0 && val != 1))
				{
					// If switch cases contain values other then 0 and 1, force the condition to be non-boolean
					ilSwitch.Condition.ExpectedType = corLib.Int32;
				}
				SwitchStatement switchStmt = new SwitchStatement() { Expression = (Expression)TransformExpression(ilSwitch.Condition) };
				if (context.CalculateILSpans)
					switchStmt.Expression.AddAnnotation(ilSwitch.ILSpans);
				switchStmt.HiddenEnd = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompact(ilSwitch.EndILSpans), switchStmt.HiddenEnd);
				for (int i = 0; i < ilSwitch.CaseBlocks.Count; i++) {
					var caseBlock = ilSwitch.CaseBlocks[i];
					SwitchSection section = new SwitchSection();
					if (caseBlock.Values != null) {
						section.CaseLabels.AddRange(caseBlock.Values.Select(v => new CaseLabel() { Expression = AstBuilder.MakePrimitive(v, (ilSwitch.Condition.ExpectedType ?? ilSwitch.Condition.InferredType).ToTypeDefOrRef(), stringBuilder) }));
					} else {
						section.CaseLabels.Add(new CaseLabel());
					}
					section.Statements.Add(TransformBlock(caseBlock));
					switchStmt.SwitchSections.Add(section);
				}
				return switchStmt;
			} else if (node is ILTryCatchBlock) {
				ILTryCatchBlock tryCatchNode = ((ILTryCatchBlock)node);
				var tryCatchStmt = new Ast.TryCatchStatement();
				tryCatchStmt.TryBlock = TransformBlock(tryCatchNode.TryBlock);
				tryCatchStmt.TryBlock.HiddenStart = NRefactoryExtensions.CreateHidden(!context.CalculateILSpans ? null : ILSpan.OrderAndCompact(tryCatchNode.ILSpans), tryCatchStmt.TryBlock.HiddenStart);
				for (int i = 0; i < tryCatchNode.CatchBlocks.Count; i++) {
					var catchClause = tryCatchNode.CatchBlocks[i];
					if (catchClause.ExceptionVariable == null
					    && (catchClause.ExceptionType == null || catchClause.ExceptionType.GetElementType() == ElementType.Object))
					{
						tryCatchStmt.CatchClauses.Add(new Ast.CatchClause {
							Body = TransformBlock(catchClause),
							Condition = TransformBlockExpression(catchClause.FilterBlock),
						}.WithAnnotation(!context.CalculateILSpans ? null : catchClause.StlocILSpans));
					} else {
						tryCatchStmt.CatchClauses.Add(
							new Ast.CatchClause {
								Type = AstBuilder.ConvertType(catchClause.ExceptionType, stringBuilder),
								VariableNameToken = catchClause.ExceptionVariable == null ? null : Identifier.Create(catchClause.ExceptionVariable.Name).WithAnnotation(GetParameterColor(catchClause.ExceptionVariable)),
								Body = TransformBlock(catchClause),
								Condition = TransformBlockExpression(catchClause.FilterBlock),
							}.WithAnnotation(catchClause.ExceptionVariable).WithAnnotation(!context.CalculateILSpans ? null : catchClause.StlocILSpans));
					}
				}

				if (tryCatchNode.FinallyBlock != null) {
					tryCatchStmt.FinallyBlock = TransformBlock(tryCatchNode.FinallyBlock);
					if (tryCatchNode.InlinedFinallyMethod is not null) {
						var finallyBlockDebugInfoBuilder = new MethodDebugInfoBuilder(context.SettingsVersion,
							StateMachineKind.None, tryCatchNode.InlinedFinallyMethod, null,
							CreateSourceLocals(GetVariables(tryCatchNode.FinallyBlock).ToHashSet()), null, null);
						tryCatchStmt.FinallyBlock.AddAnnotation(finallyBlockDebugInfoBuilder);
					}
				}

				if (tryCatchNode.FaultBlock != null) {
					CatchClause cc = new CatchClause();
					cc.Body = TransformBlock(tryCatchNode.FaultBlock);
					cc.Body.Add(new ThrowStatement()); // rethrow
					cc.InsertChildAfter(new Comment(" This is a fault block"), null, Roles.Comment);
					tryCatchStmt.CatchClauses.Add(cc);
				}
				return tryCatchStmt;
			} else if (node is ILFixedStatement) {
				ILFixedStatement fixedNode = (ILFixedStatement)node;
				FixedStatement fixedStatement = new FixedStatement();
				for (int i = 0; i < fixedNode.Initializers.Count; i++) {
					var initializer = fixedNode.Initializers[i];
					Debug.Assert(initializer.Code == ILCode.Stloc);
					ILVariable v = (ILVariable)initializer.Operand;
					VariableInitializer vi;
					fixedStatement.Variables.Add(vi =
						new VariableInitializer {
							NameToken = Identifier.Create(v.Name).WithAnnotation(GetParameterColor(v)),
							Initializer = (Expression)TransformExpression(initializer.Arguments[0])
						}.WithAnnotation(v));
					if (context.CalculateILSpans) {
						vi.AddAnnotation(initializer.GetSelfAndChildrenRecursiveILSpans_OrderAndJoin());
						if (i == 0)
							vi.AddAnnotation(ILSpan.OrderAndCompact(fixedNode.ILSpans));
					}
				}
				fixedStatement.Type = AstBuilder.ConvertType(((ILVariable)fixedNode.Initializers[0].Operand).Type, stringBuilder);
				fixedStatement.EmbeddedStatement = TransformBlock(fixedNode.BodyBlock);
				return fixedStatement;
			} else if (node is ILBlock) {
				return TransformBlock((ILBlock)node);
			} else {
				throw new Exception("Unknown node type");
			}
		}

		AstNode TransformExpression(ILExpression expr)
		{
			List<ILSpan> ilSpans = !context.CalculateILSpans ? null : expr.GetSelfAndChildrenRecursiveILSpans_OrderAndJoin();

			AstNode node = TransformByteCode(expr);
			Expression astExpr = node as Expression;

			AstNode result;

			if (astExpr != null)
				result = Convert(astExpr, expr.InferredType, expr.ExpectedType);
			else
				result = node;

			if (result != null)
				result = result.WithAnnotation(new TypeInformation(expr.InferredType, expr.ExpectedType));

			if (context.CalculateILSpans && result != null)
				return result.WithAnnotation(ilSpans);

			return result;
		}

		IMDTokenProvider Create_SystemArray_get_Length()
		{
			if (Create_SystemArray_get_Length_result_initd)
				return Create_SystemArray_get_Length_result;
			Create_SystemArray_get_Length_result_initd = true;

			const string propName = "Length";
			var type = corLib.GetTypeRef("System", "Array");
			var retType = corLib.Int32;
			var mr = new MemberRefUser(methodDef.Module, "get_" + propName, MethodSig.CreateInstance(retType), type);
			Create_SystemArray_get_Length_result = mr;
			var md = mr.ResolveMethod();
			if (md == null || md.DeclaringType == null)
				return mr;
			var prop = md.DeclaringType.FindProperty(propName);
			if (prop == null)
				return mr;

			Create_SystemArray_get_Length_result = prop;
			return prop;
		}
		IMDTokenProvider Create_SystemArray_get_Length_result;
		bool Create_SystemArray_get_Length_result_initd;

		IMDTokenProvider Create_SystemType_get_TypeHandle()
		{
			if (Create_SystemType_get_TypeHandle_initd)
				return Create_SystemType_get_TypeHandle_result;
			Create_SystemType_get_TypeHandle_initd = true;

			const string propName = "TypeHandle";
			var type = corLib.GetTypeRef("System", "Type");
			var retType = new ValueTypeSig(corLib.GetTypeRef("System", "RuntimeTypeHandle"));
			var mr = new MemberRefUser(methodDef.Module, "get_" + propName, MethodSig.CreateInstance(retType), type);
			Create_SystemType_get_TypeHandle_result = mr;
			var md = mr.ResolveMethod();
			if (md == null || md.DeclaringType == null)
				return mr;
			var prop = md.DeclaringType.FindProperty(propName);
			if (prop == null)
				return mr;

			Create_SystemType_get_TypeHandle_result = prop;
			return prop;
		}
		IMDTokenProvider Create_SystemType_get_TypeHandle_result;
		bool Create_SystemType_get_TypeHandle_initd;

		object GetParameterColor(ILVariable ilv)
		{
			if (valueParameterIsKeyword && ilv.OriginalParameter?.Name == "value" && methodDef.Parameters.Count > 0 && methodDef.Parameters[methodDef.Parameters.Count - 1] == ilv.OriginalParameter)
				return BoxedTextColor.Keyword;
			return ilv.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local;
		}

		AstNode TransformByteCode(ILExpression byteCode)
		{
			object operand = byteCode.Operand;
			AstType operandAsTypeRef = AstBuilder.ConvertType(operand as ITypeDefOrRef, stringBuilder);

			List<Ast.Expression> args = new List<Expression>(byteCode.Arguments.Count);
			for (int i = 0; i < byteCode.Arguments.Count; i++)
				args.Add((Ast.Expression)TransformExpression(byteCode.Arguments[i]));

			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Count >= 3 ? args[2] : null;

			switch (byteCode.Code) {
					#region Arithmetic
				case ILCode.Add:
				case ILCode.Add_Ovf:
				case ILCode.Add_Ovf_Un:
					{
						BinaryOperatorExpression boe;
						if (byteCode.InferredType is PtrSig) {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
							if (byteCode.Arguments[0].ExpectedType is PtrSig ||
								byteCode.Arguments[1].ExpectedType is PtrSig) {
								boe.AddAnnotation(IntroduceUnsafeModifier.PointerArithmeticAnnotation);
							}
						} else {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
						}
						boe.AddAnnotation(byteCode.Code == ILCode.Add ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return boe;
					}
				case ILCode.Sub:
				case ILCode.Sub_Ovf:
				case ILCode.Sub_Ovf_Un:
					{
						BinaryOperatorExpression boe;
						if (byteCode.InferredType is PtrSig) {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
							if (byteCode.Arguments[0].ExpectedType is PtrSig) {
								boe.WithAnnotation(IntroduceUnsafeModifier.PointerArithmeticAnnotation);
							}
						} else {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
						}
						boe.AddAnnotation(byteCode.Code == ILCode.Sub ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return boe;
					}
					case ILCode.Div:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case ILCode.Div_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case ILCode.Mul:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.UncheckedAnnotation);
					case ILCode.Mul_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.CheckedAnnotation);
					case ILCode.Mul_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.CheckedAnnotation);
					case ILCode.Rem:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case ILCode.Rem_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case ILCode.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
					case ILCode.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
					case ILCode.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case ILCode.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case ILCode.Neg:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.Minus, arg1).WithAnnotation(AddCheckedBlocks.UncheckedAnnotation);
					case ILCode.Not:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.BitNot, arg1);

					case ILCode.And:
						// Roslyn replaces && and || with & and | if allowed; undo it
						if (IsBooleanLocalOrParam(byteCode.Arguments[1]))
							return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalAnd, arg2);
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseAnd, arg2);
					case ILCode.Or:
						// Roslyn replaces && and || with & and | if allowed; undo it
						if (IsBooleanLocalOrParam(byteCode.Arguments[1]))
							return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalOr, arg2);
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseOr, arg2);

				case ILCode.PostIncrement:
				case ILCode.PostIncrement_Ovf:
				case ILCode.PostIncrement_Ovf_Un:
					{
						if (arg1 is DirectionExpression)
							arg1 = ((DirectionExpression)arg1).Expression.Detach();
						var uoe = new Ast.UnaryOperatorExpression(
							(int)byteCode.Operand > 0 ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement, arg1);
						uoe.AddAnnotation((byteCode.Code == ILCode.PostIncrement) ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return uoe;
					}
					#endregion
					#region Arrays
					case ILCode.Newarr: {
						var ace = new Ast.ArrayCreateExpression();
						ace.Type = operandAsTypeRef;
						ComposedType ct = operandAsTypeRef as ComposedType;
						if (ct != null) {
							// change "new (int[,])[10] to new int[10][,]"
							ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
						}
						if (byteCode.Code == ILCode.InitArray) {
							ace.Initializer = new ArrayInitializerExpression();
							ace.Initializer.Elements.AddRange(args);
						} else {
							ace.Arguments.Add(arg1);
						}
						return ace;
					}
					case ILCode.InitArray: {
						var ace = new Ast.ArrayCreateExpression();
						ace.Type = operandAsTypeRef;
						ComposedType ct = operandAsTypeRef as ComposedType;
						if (ct != null)
						{
							// change "new (int[,])[10] to new int[10][,]"
							ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
							ace.Initializer = new ArrayInitializerExpression();
						}
						var arySig = ((TypeSpec)operand).TypeSig.RemovePinnedAndModifiers() as ArraySigBase;
						if (arySig == null) {
						}
						else if (arySig.IsSingleDimensional)
						{
							ace.Initializer.Elements.AddRange(args);
						}
						else
						{
							var newArgs = new List<Expression>();
							foreach (var length in arySig.GetLengths().Skip(1).Reverse())
							{
								for (int j = 0; j < args.Count; j += length)
								{
									var child = new ArrayInitializerExpression();
									child.Elements.AddRange(args.GetRange(j, length));
									newArgs.Add(child);
								}
								(args, newArgs) = (newArgs, args);
								newArgs.Clear();
							}
							ace.Initializer.Elements.AddRange(args);
						}
						return ace;
					}
					case ILCode.Ldlen: return arg1.Member("Length", BoxedTextColor.InstanceProperty).WithAnnotation(Create_SystemArray_get_Length());
				case ILCode.Ldelem_I:
				case ILCode.Ldelem_I1:
				case ILCode.Ldelem_I2:
				case ILCode.Ldelem_I4:
				case ILCode.Ldelem_I8:
				case ILCode.Ldelem_U1:
				case ILCode.Ldelem_U2:
				case ILCode.Ldelem_U4:
				case ILCode.Ldelem_R4:
				case ILCode.Ldelem_R8:
				case ILCode.Ldelem_Ref:
				case ILCode.Ldelem:
					return arg1.Indexer(arg2);
				case ILCode.Ldelema:
					return MakeRef(arg1.Indexer(arg2));
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
				case ILCode.Stelem:
					return new Ast.AssignmentExpression(arg1.Indexer(arg2), arg3);
				case ILCode.CompoundAssignment:
					{
						CastExpression cast = arg1 as CastExpression;
						var boe = cast != null ? (BinaryOperatorExpression)cast.Expression : arg1 as BinaryOperatorExpression;
						// AssignmentExpression doesn't support overloaded operators so they have to be processed to BinaryOperatorExpression
						if (boe == null) {
							var tmp = new ParenthesizedExpression(arg1);
							ReplaceMethodCallsWithOperators.ProcessInvocationExpression((InvocationExpression)arg1, stringBuilder);
							boe = (BinaryOperatorExpression)tmp.Expression;
						}
						var assignment = new Ast.AssignmentExpression {
							Left = boe.Left.Detach(),
							Operator = ReplaceMethodCallsWithOperators.GetAssignmentOperatorForBinaryOperator(boe.Operator),
							Right = boe.Right.Detach()
						}.CopyAnnotationsFrom(boe);
						// We do not mark the resulting assignment as RestoreOriginalAssignOperatorAnnotation, because
						// the operator cannot be translated back to the expanded form (as the left-hand expression
						// would be evaluated twice, and might have side-effects)
						if (cast != null) {
							cast.Expression = assignment;
							return cast;
						} else {
							return assignment;
						}
					}
					#endregion
					#region Comparison
					case ILCode.Cnull: return new Ast.BinaryOperatorExpression(UnpackDirectionExpression(arg1), BinaryOperatorType.Equality, new NullReferenceExpression());
					case ILCode.Cnotnull: return new Ast.BinaryOperatorExpression(UnpackDirectionExpression(arg1), BinaryOperatorType.InEquality, new NullReferenceExpression());
					case ILCode.Ceq: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
					case ILCode.Cne: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
					case ILCode.Cgt: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case ILCode.Cgt_Un: {
						// can also mean Inequality, when used with object references
						TypeSig arg1Type = byteCode.Arguments[0].InferredType;
						if (arg1Type != null && !DnlibExtensions.IsValueType(arg1Type)) goto case ILCode.Cne;

						// when comparing signed integral values using Cgt_Un with 0
						// the Ast should actually contain InEquality since "(uint)a > 0u" is identical to "a != 0"
						if (arg1Type.IsSignedIntegralType())
						{
							var p = arg2 as Ast.PrimitiveExpression;
							if (p != null && p.Value.IsZero()) goto case ILCode.Cne;
						}

						goto case ILCode.Cgt;
					}
					case ILCode.Cle_Un: {
						// can also mean Equality, when used with object references
						TypeSig arg1Type = byteCode.Arguments[0].InferredType;
						if (arg1Type != null && !DnlibExtensions.IsValueType(arg1Type)) goto case ILCode.Ceq;

						// when comparing signed integral values using Cle_Un with 0
						// the Ast should actually contain Equality since "(uint)a <= 0u" is identical to "a == 0"
						if (arg1Type.IsSignedIntegralType())
						{
							var p = arg2 as Ast.PrimitiveExpression;
							if (p != null && p.Value.IsZero()) goto case ILCode.Ceq;
						}

						goto case ILCode.Cle;
					}
					case ILCode.Cle: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case ILCode.Cge_Un:
					case ILCode.Cge: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case ILCode.Clt_Un:
					case ILCode.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					#endregion
					#region Logical
					case ILCode.LogicNot:   return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
					case ILCode.LogicAnd:   return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalAnd, arg2);
					case ILCode.LogicOr:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalOr, arg2);
					case ILCode.TernaryOp:  return new Ast.ConditionalExpression() { Condition = arg1, TrueExpression = arg2, FalseExpression = arg3 };
					case ILCode.NullCoalescing: 	return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.NullCoalescing, arg2);
					#endregion
					#region Branch
					case ILCode.Br:         return new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name);
				case ILCode.Brtrue:
					return new Ast.IfElseStatement() {
						Condition = arg1,
						TrueStatement = new BlockStatement() {
							new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name)
						}
					};
					case ILCode.LoopOrSwitchBreak: return new Ast.BreakStatement();
					case ILCode.LoopContinue:      return new Ast.ContinueStatement();
					#endregion
					#region Conversions
				case ILCode.Conv_I1:
				case ILCode.Conv_I2:
				case ILCode.Conv_I4:
				case ILCode.Conv_I8:
				case ILCode.Conv_U1:
				case ILCode.Conv_U2:
				case ILCode.Conv_U4:
				case ILCode.Conv_U8:
				case ILCode.Conv_I:
				case ILCode.Conv_U:
					{
						// conversion was handled by Convert() function using the info from type analysis
						CastExpression cast = arg1 as CastExpression;
						if (cast != null) {
							cast.AddAnnotation(AddCheckedBlocks.UncheckedAnnotation);
						}
						return arg1;
					}
				case ILCode.Conv_R4:
				case ILCode.Conv_R8:
				case ILCode.Conv_R_Un: // TODO
					return arg1;
				case ILCode.Conv_Ovf_I1:
				case ILCode.Conv_Ovf_I2:
				case ILCode.Conv_Ovf_I4:
				case ILCode.Conv_Ovf_I8:
				case ILCode.Conv_Ovf_U1:
				case ILCode.Conv_Ovf_U2:
				case ILCode.Conv_Ovf_U4:
				case ILCode.Conv_Ovf_U8:
				case ILCode.Conv_Ovf_I1_Un:
				case ILCode.Conv_Ovf_I2_Un:
				case ILCode.Conv_Ovf_I4_Un:
				case ILCode.Conv_Ovf_I8_Un:
				case ILCode.Conv_Ovf_U1_Un:
				case ILCode.Conv_Ovf_U2_Un:
				case ILCode.Conv_Ovf_U4_Un:
				case ILCode.Conv_Ovf_U8_Un:
				case ILCode.Conv_Ovf_I:
				case ILCode.Conv_Ovf_U:
				case ILCode.Conv_Ovf_I_Un:
				case ILCode.Conv_Ovf_U_Un:
					{
						// conversion was handled by Convert() function using the info from type analysis
						CastExpression cast = arg1 as CastExpression;
						if (cast != null) {
							cast.AddAnnotation(AddCheckedBlocks.CheckedAnnotation);
						}
						return arg1;
					}
				case ILCode.Unbox_Any:
					// unboxing does not require a cast if the argument was an isinst instruction
					if (arg1 is AsExpression && byteCode.Arguments[0].Code == ILCode.Isinst && TypeAnalysis.IsSameType(operand as ITypeDefOrRef, byteCode.Arguments[0].Operand as ITypeDefOrRef))
						return arg1;
					else
						goto case ILCode.Castclass;
				case ILCode.Castclass:
					if ((byteCode.Arguments[0].InferredType != null && byteCode.Arguments[0].InferredType.IsGenericParameter) || (operand as ITypeDefOrRef).TryGetGenericSig() != null)
						return arg1.CastTo(new PrimitiveType("object")).CastTo(operandAsTypeRef);
					else
						return arg1.CastTo(operandAsTypeRef);
				case ILCode.Isinst:
					return arg1.CastAs(operandAsTypeRef);
				case ILCode.Box:
					return arg1;
				case ILCode.Unbox:
					return MakeRef(arg1.CastTo(operandAsTypeRef));
					#endregion
					#region Indirect
				case ILCode.Ldind_Ref:
				case ILCode.Ldobj:
					if (arg1 is DirectionExpression)
						return ((DirectionExpression)arg1).Expression.Detach();
					else
						return new UnaryOperatorExpression(UnaryOperatorType.Dereference, arg1);
				case ILCode.Stind_Ref:
				case ILCode.Stobj:
					if (arg1 is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)arg1).Expression.Detach(), arg2);
					else
						return new AssignmentExpression(new UnaryOperatorExpression(UnaryOperatorType.Dereference, arg1), arg2);
					#endregion
				case ILCode.Arglist:
					return new UndocumentedExpression { UndocumentedExpressionType = UndocumentedExpressionType.ArgListAccess };
					case ILCode.Break:    return InlineAssembly(byteCode, args);
				case ILCode.Call:
				case ILCode.CallGetter:
				case ILCode.CallSetter:
					return TransformCall(false, byteCode, args);
				case ILCode.CallReadOnlySetter:// Never virtual since it's really a store to a field
					return TransformCall(false, byteCode, args, MethodSemanticsAttributes.Setter);
				case ILCode.Callvirt:
				case ILCode.CallvirtGetter:
				case ILCode.CallvirtSetter:
					return TransformCall(true, byteCode,  args);
					case ILCode.Ldftn: {
						IMethod method = (IMethod)operand;
						var expr = Ast.IdentifierExpression.Create(method.Name, method);
						expr.TypeArguments.AddRange(ConvertTypeArguments(method));
						expr.AddAnnotation(method);
						return IdentifierExpression.Create("ldftn", BoxedTextColor.OpCode).Invoke(expr)
							.WithAnnotation(Transforms.DelegateConstruction.Annotation.False);
					}
					case ILCode.Ldvirtftn: {
						IMethod method = (IMethod)operand;
						var expr = Ast.IdentifierExpression.Create(method.Name, method);
						expr.TypeArguments.AddRange(ConvertTypeArguments(method));
						expr.AddAnnotation(method);
						return IdentifierExpression.Create("ldvirtftn", BoxedTextColor.OpCode).Invoke(expr)
							.WithAnnotation(Transforms.DelegateConstruction.Annotation.True);
					}
					case ILCode.Calli:		 return context.Settings.EmitCalliAsInvocationExpression ? TransformCalli(byteCode, args) : InlineAssembly(byteCode, args);
					case ILCode.Ckfinite:    return InlineAssembly(byteCode, args);
					case ILCode.Constrained: return InlineAssembly(byteCode, args);
					case ILCode.Cpblk:       return InlineAssembly(byteCode, args);
					case ILCode.Cpobj:       return InlineAssembly(byteCode, args);
					case ILCode.Dup:         return arg1;
					case ILCode.Endfilter:   return InlineAssembly(byteCode, args);
					case ILCode.Endfinally:  return null;
					case ILCode.Initblk:     return InlineAssembly(byteCode, args);
					case ILCode.Initobj:     return InlineAssembly(byteCode, args);
				case ILCode.DefaultValue:
					return MakeDefaultValue((operand as ITypeDefOrRef).ToTypeSig());
					case ILCode.Jmp: {
						var method = (IMethod)operand;
						var expr = IdentifierExpression.Create(method.Name + "()", method, true);
						expr.TypeArguments.AddRange(ConvertTypeArguments(method));
						return IdentifierExpression.Create("jmp", BoxedTextColor.OpCode).Invoke(expr);
					}
				case ILCode.Ldc_I4:
						return AstBuilder.MakePrimitive((int)operand, byteCode.InferredType.ToTypeDefOrRef(), stringBuilder);
				case ILCode.Ldc_I8:
						return AstBuilder.MakePrimitive((long)operand, byteCode.InferredType.ToTypeDefOrRef(), stringBuilder);
				case ILCode.Ldc_R4:
				case ILCode.Ldc_R8:
				case ILCode.Ldc_Decimal:
					return new Ast.PrimitiveExpression(operand);
				case ILCode.Ldfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return arg1.Member(((IField) operand).Name, operand).WithAnnotation(operand);
				case ILCode.Ldsfld:
					return AstBuilder.ConvertType(((IField)operand).DeclaringType, stringBuilder)
						.Member(((IField)operand).Name, operand).WithAnnotation(operand);
				case ILCode.Stfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return new AssignmentExpression(arg1.Member(((IField) operand).Name, operand).WithAnnotation(operand), arg2);
				case ILCode.Stsfld:
					return new AssignmentExpression(
						AstBuilder.ConvertType(((IField)operand).DeclaringType, stringBuilder)
						.Member(((IField)operand).Name, operand).WithAnnotation(operand),
						arg1);
				case ILCode.Ldflda:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return MakeRef(arg1.Member(((IField) operand).Name, operand).WithAnnotation(operand));
				case ILCode.Ldsflda:
					return MakeRef(
						AstBuilder.ConvertType(((IField)operand).DeclaringType, stringBuilder)
						.Member(((IField)operand).Name, operand).WithAnnotation(operand));
					case ILCode.Ldloc: {
						ILVariable v = (ILVariable)operand;
						if (!v.IsParameter)
							localVariablesToDefine.Add((ILVariable)operand);
						Expression expr;
						if (v.IsParameter && v.OriginalParameter.IsHiddenThisParameter)
							expr = new ThisReferenceExpression().WithAnnotation(methodDef.DeclaringType);
						else {
							var ide = Ast.IdentifierExpression.Create(((ILVariable)operand).Name, GetParameterColor((ILVariable)operand)).WithAnnotation(operand);
							ide.IdentifierToken.AddAnnotation(operand);
							expr = ide;
						}
						return v.Type.RemovePinnedAndModifiers() is ByRefSig ? MakeRef(expr) : expr;
					}
					case ILCode.Ldloca: {
						ILVariable v = (ILVariable)operand;
						if (v.IsParameter && v.OriginalParameter.IsHiddenThisParameter)
							return MakeRef(new ThisReferenceExpression().WithAnnotation(methodDef.DeclaringType));
						if (!v.IsParameter)
							localVariablesToDefine.Add((ILVariable)operand);
						var ide = Ast.IdentifierExpression.Create(((ILVariable)operand).Name, GetParameterColor((ILVariable)operand)).WithAnnotation(operand);
						ide.IdentifierToken.AddAnnotation(operand);
						return MakeRef(ide);
					}
					case ILCode.Ldnull: return new Ast.NullReferenceExpression();
					case ILCode.Ldstr:  return new Ast.PrimitiveExpression(operand);
				case ILCode.Ldtoken:
					if (operand is ITypeDefOrRef) {
						var th = Create_SystemType_get_TypeHandle();
						return AstBuilder.CreateTypeOfExpression((ITypeDefOrRef)operand, stringBuilder).Member("TypeHandle", BoxedTextColor.InstanceProperty).WithAnnotation(th);
					} else {
						Expression referencedEntity;
						string loadName;
						string handleName;
						if (operand is IField && ((IField)operand).FieldSig != null) {
							loadName = "fieldof";
							handleName = "FieldHandle";
							IField fr = (IField)operand;
							referencedEntity = AstBuilder.ConvertType(fr.DeclaringType, stringBuilder).Member(fr.Name, fr).WithAnnotation(fr);
						} else if (operand is IMethod) {
							loadName = "methodof";
							handleName = "MethodHandle";
							IMethod mr = (IMethod)operand;
							var methodParameters = mr.MethodSig.GetParameters().Select(p => new TypeReferenceExpression(AstBuilder.ConvertType(p, stringBuilder)));
							referencedEntity = AstBuilder.ConvertType(mr.DeclaringType, stringBuilder).Invoke(mr, mr.Name, methodParameters).WithAnnotation(mr);
						} else {
							loadName = "ldtoken";
							handleName = "Handle";
							var ie = IdentifierExpression.Create(FormatByteCodeOperand(byteCode.Operand), byteCode.Operand);
							ie.IdentifierToken.AddAnnotation(IdentifierFormatted.Instance);
							referencedEntity = ie;
						}
						return IdentifierExpression.Create(loadName, BoxedTextColor.Keyword).Invoke(referencedEntity).WithAnnotation(new LdTokenAnnotation()).Member(handleName, BoxedTextColor.InstanceProperty);
					}
					case ILCode.Leave:    return new GotoStatement() { Label = ((ILLabel)operand).Name };
				case ILCode.Localloc:
					{
						PtrSig ptrType = byteCode.InferredType as PtrSig;
						TypeSig type;
						if (ptrType != null && ptrType.Next.GetElementType() != ElementType.Void) {
							type = ptrType.Next;
						} else {
							type = corLib.Byte;
						}
						return new StackAllocExpression {
							Type = AstBuilder.ConvertType(type, stringBuilder),
                            CountExpression = arg1
						};
					}
				case ILCode.Mkrefany:
					{
						DirectionExpression dir = arg1 as DirectionExpression;
						if (dir != null) {
							return new UndocumentedExpression {
								UndocumentedExpressionType = UndocumentedExpressionType.MakeRef,
								Arguments = { dir.Expression.Detach() }
							};
						} else {
							return InlineAssembly(byteCode, args);
						}
					}
				case ILCode.Refanytype:
					return new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.RefType,
						Arguments = { arg1 }
					}.Member("TypeHandle", BoxedTextColor.InstanceProperty).WithAnnotation(Create_SystemType_get_TypeHandle());
				case ILCode.Refanyval:
					return MakeRef(
						new UndocumentedExpression {
							UndocumentedExpressionType = UndocumentedExpressionType.RefValue,
							Arguments = { arg1, new TypeReferenceExpression(operandAsTypeRef) }
						});
					case ILCode.Newobj: {
						ITypeDefOrRef declaringType = ((IMethod)operand).DeclaringType;
						if ((declaringType as TypeSpec)?.TypeSig.RemovePinnedAndModifiers() is ArraySigBase) {
							ComposedType ct = AstBuilder.ConvertType(declaringType, stringBuilder) as ComposedType;
							if (ct != null && ct.ArraySpecifiers.Count >= 1) {
								var ace = new Ast.ArrayCreateExpression();
								ct.ArraySpecifiers.First().Remove();
								ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
								ace.Type = ct;
								ace.Arguments.AddRange(args);
								return ace;
							}
						}
						MethodDef ctor = ((IMethod)operand).Resolve();
						if (declaringType.IsAnonymousType() && ctor != null) {
							AnonymousTypeCreateExpression atce = new AnonymousTypeCreateExpression();
							if (CanInferAnonymousTypePropertyNamesFromArguments(args, ctor.Parameters)) {
								atce.Initializers.AddRange(args);
							} else {
								int skip = ctor.Parameters.GetParametersSkip();
								for (int i = 0; i < args.Count; i++) {
									atce.Initializers.Add(
										new NamedExpression {
											NameToken = Identifier.Create(ctor.Parameters[i + skip].Name).WithAnnotation(ctor.Parameters[i + skip]),
											Expression = args[i]
										});
								}
							}
							return atce;
						}
						var oce = new Ast.ObjectCreateExpression();
						oce.Type = AstBuilder.ConvertType(declaringType, stringBuilder);
						// seems like IsIn/IsOut information for parameters is only correct on the ctor's MethodDefinition
						if (ctor != null) {
							AdjustArgumentsForMethodCall(ctor, args);
						}
						oce.Arguments.AddRange(args);
						return oce.WithAnnotation(operand);
					}
					case ILCode.No: return InlineAssembly(byteCode, args);
					case ILCode.Nop: return null;
					case ILCode.Pop: return arg1;
					case ILCode.Readonly: return InlineAssembly(byteCode, args);
				case ILCode.Ret:
					if (methodDef.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void) {
						return new Ast.ReturnStatement { Expression = arg1 };
					} else {
						return new Ast.ReturnStatement();
					}
					case ILCode.Rethrow: return new Ast.ThrowStatement();
					case ILCode.Sizeof:  return new Ast.SizeOfExpression { Type = operandAsTypeRef };
					case ILCode.Stloc: {
						ILVariable locVar = (ILVariable)operand;
						if (!locVar.IsParameter)
							localVariablesToDefine.Add(locVar);
						var ide = Ast.IdentifierExpression.Create(locVar.Name, GetParameterColor(locVar)).WithAnnotation(locVar);
						ide.IdentifierToken.AddAnnotation(locVar);
						return new Ast.AssignmentExpression(ide, arg1);
					}
					case ILCode.Switch: return InlineAssembly(byteCode, args);
					case ILCode.Tailcall: return InlineAssembly(byteCode, args);
					case ILCode.Throw: return new Ast.ThrowStatement { Expression = arg1 };
					case ILCode.Unaligned: return InlineAssembly(byteCode, args);
					case ILCode.Volatile: return InlineAssembly(byteCode, args);
				case ILCode.YieldBreak:
					return new Ast.YieldBreakStatement();
				case ILCode.YieldReturn:
					return new Ast.YieldReturnStatement { Expression = arg1 };
				case ILCode.InitObject:
				case ILCode.InitCollection:
					{
						ArrayInitializerExpression initializer = new ArrayInitializerExpression();
						for (int i = 1; i < args.Count; i++) {
							Match m = objectInitializerPattern.Match(args[i]);
							if (m.Success) {
								MemberReferenceExpression mre = m.Get<MemberReferenceExpression>("left").Single();
								initializer.Elements.Add(
									new NamedExpression {
										NameToken = (Identifier)mre.MemberNameToken.Clone(),
										Expression = m.Get<Expression>("right").Single().Detach()
									}.CopyAnnotationsFrom(mre));
							} else {
								m = collectionInitializerPattern.Match(args[i]);
								if (!m.Success)
									m = staticCollectionInitializerPattern.Match(args[i]);
								if (m.Success) {
									if (m.Get("arg").Count() == 1) {
										initializer.Elements.Add(m.Get<Expression>("arg").Single().Detach());
									} else {
										ArrayInitializerExpression argList = new ArrayInitializerExpression();
										foreach (var expr in m.Get<Expression>("arg")) {
											argList.Elements.Add(expr.Detach());
										}
										initializer.Elements.Add(argList);
									}
								} else {
									initializer.Elements.Add(args[i]);
								}
							}
						}
						ObjectCreateExpression oce = arg1 as ObjectCreateExpression;
						if (oce != null) {
							oce.Initializer = initializer;
							return oce;
						}
						DefaultValueExpression dve = arg1 as DefaultValueExpression;
						if (dve != null) {
							oce = new ObjectCreateExpression(dve.Type.Detach());
							oce.CopyAnnotationsFrom(dve);
							oce.Initializer = initializer;
							return oce;
						} else {
							return new AssignmentExpression(arg1, initializer);
						}
					}
				case ILCode.InitializedObject:
					return new InitializedObjectExpression();
				case ILCode.Wrap:
					return arg1.WithAnnotation(PushNegation.LiftedOperatorAnnotation);
				case ILCode.AddressOf:
					return MakeRef(arg1);
				case ILCode.ExpressionTreeParameterDeclarations:
					args[args.Count - 1].AddAnnotation(new ParameterDeclarationAnnotation(byteCode, stringBuilder));
					return args[args.Count - 1];
				case ILCode.Await:
					return new UnaryOperatorExpression(UnaryOperatorType.Await, UnpackDirectionExpression(arg1));
				case ILCode.NullableOf:
				case ILCode.ValueOf:
					return arg1;
				default:
					throw new Exception("Unknown OpCode: " + byteCode.Code);
			}
		}

		bool IsBooleanLocalOrParam(ILExpression expr) {
			if (expr.Code != ILCode.Ldloc)
				return false;
			if (expr.ExpectedType != null)
				return expr.ExpectedType.GetElementType() == ElementType.Boolean;
			var v = (ILVariable)expr.Operand;
			return (v.Type ?? v.OriginalParameter?.Type ?? v.OriginalVariable?.Type).GetElementType() == ElementType.Boolean;
		}

		internal static bool CanInferAnonymousTypePropertyNamesFromArguments(IList<Expression> args, IList<Parameter> parameters)
		{
			int skip = parameters.GetParametersSkip();
			for (int i = 0; i < args.Count; i++) {
				string inferredName;
				if (args[i] is IdentifierExpression)
					inferredName = ((IdentifierExpression)args[i]).Identifier;
				else if (args[i] is MemberReferenceExpression)
					inferredName = ((MemberReferenceExpression)args[i]).MemberName;
				else
					inferredName = null;

				if (i + skip >= parameters.Count || inferredName != parameters[i + skip].Name) {
					return false;
				}
			}
			return true;
		}

		static readonly AstNode objectInitializerPattern = new AssignmentExpression(
			new MemberReferenceExpression {
				Target = new InitializedObjectExpression(),
				MemberName = Pattern.AnyString
			}.WithName("left"),
			new AnyNode("right")
		);

		static readonly AstNode collectionInitializerPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new InitializedObjectExpression(),
				TypeArguments = { new Repeat(new AnyNode()) },
				MemberName = "Add"
			},
			Arguments = { new Repeat(new AnyNode("arg")) }
		};

		static readonly AstNode staticCollectionInitializerPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new TypeReferenceExpression {
					Type = new AnyNode(),
				},
				TypeArguments = { new Repeat(new AnyNode()) },
				MemberName = "Add"
			},
			Arguments = { new AnyNode(), new Repeat(new AnyNode("arg")) }
		};

		sealed class InitializedObjectExpression : IdentifierExpression
		{
			public InitializedObjectExpression() : base("__initialized_object__") {}

			protected override bool DoMatch(AstNode other, Match match)
			{
				return other is InitializedObjectExpression;
			}
		}

		Expression MakeDefaultValue(TypeSig type)
		{
			TypeDef typeDef = type.Resolve();
			if (typeDef != null) {
				if (TypeAnalysis.IsIntegerOrEnum(type))
					return AstBuilder.MakePrimitive(0, typeDef, stringBuilder);
				else if (!DnlibExtensions.IsValueType(typeDef))
					return new NullReferenceExpression();
				switch (typeDef.FullName) {
					case "System.Nullable`1":
						return new NullReferenceExpression();
					case "System.Single":
						return new PrimitiveExpression(0f);
					case "System.Double":
						return new PrimitiveExpression(0.0);
					case "System.Decimal":
						return new PrimitiveExpression(0m);
				}
			}
			return new DefaultValueExpression { Type = AstBuilder.ConvertType(type, stringBuilder) };
		}

		AstNode TransformCall(bool isVirtual, ILExpression byteCode, List<Ast.Expression> args, MethodSemanticsAttributes? forceSemAttr = null)
		{
			IMethod method = (IMethod)byteCode.Operand;
			MethodDef methodDef = method.Resolve();
			Ast.Expression target;
			List<Ast.Expression> methodArgs = new List<Ast.Expression>(args);
			if (method.MethodSig != null && method.MethodSig.HasThis) {
				target = methodArgs[0];
				methodArgs.RemoveAt(0);

				// Unpack any DirectionExpression that is used as target for the call
				// (calling methods on value types implicitly passes the first argument by reference)
				target = UnpackDirectionExpression(target);

				if (methodDef != null) {
					// convert null.ToLower() to ((string)null).ToLower()
					if (target is NullReferenceExpression)
						target = target.CastTo(AstBuilder.ConvertType(method.DeclaringType, stringBuilder));

					if (methodDef.DeclaringType.IsInterface) {
						TypeSig tr = byteCode.Arguments[0].InferredType;
						if (tr != null) {
							TypeDef td = tr.Resolve();
							if (td != null && !td.IsInterface) {
								// Calling an interface method on a non-interface object:
								// we need to introduce an explicit cast
								target = target.CastTo(AstBuilder.ConvertType(method.DeclaringType, stringBuilder));
							}
						}
					}
				}
			} else {
				target = new TypeReferenceExpression { Type = AstBuilder.ConvertType(method.DeclaringType, stringBuilder) };
			}
			if (target is ThisReferenceExpression && !isVirtual) {
				// a non-virtual call on "this" might be a "base"-call.
				if (method.DeclaringType != null && method.DeclaringType.ScopeType.ResolveTypeDef() != context.CurrentType) {
					// If we're not calling a method in the current class; we must be calling one in the base class.
					target = new BaseReferenceExpression();
					target.AddAnnotation(method.DeclaringType);
				}
			}

			if (method.Name == ".ctor" && DnlibExtensions.IsValueType(method.DeclaringType)) {
				// On value types, the constructor can be called.
				// This is equivalent to 'target = new ValueType(args);'.
				ObjectCreateExpression oce = new ObjectCreateExpression();
				oce.Type = AstBuilder.ConvertType(method.DeclaringType, stringBuilder);
				oce.AddAnnotation(method);
				AdjustArgumentsForMethodCall(method, methodArgs);
				oce.Arguments.AddRange(methodArgs);
				return new AssignmentExpression(target, oce);
			}

			if (method.Name == "Get" && (method.DeclaringType.TryGetArraySig() != null || method.DeclaringType.TryGetSZArraySig() != null) && methodArgs.Count > 1) {
				return target.Indexer(methodArgs);
			} else if (method.Name == "Set" && (method.DeclaringType.TryGetArraySig() != null || method.DeclaringType.TryGetSZArraySig() != null) && methodArgs.Count > 2) {
				return new AssignmentExpression(target.Indexer(methodArgs.GetRange(0, methodArgs.Count - 1)), methodArgs.Last());
			}

			// Test whether the method is an accessor:
			var semAttr = forceSemAttr ?? methodDef?.SemanticsAttributes ?? GetMethodSemanticsAttributes(method);
			if (semAttr != MethodSemanticsAttributes.None) {
				if (methodArgs.Count == 0 && (semAttr & MethodSemanticsAttributes.Getter) != 0) {
					if (methodDef == null)
						return target.Member(method.Name.Substring(4), method).WithAnnotation(method);
					for (int i = 0; i < methodDef.DeclaringType.Properties.Count; i++) {
						var prop = methodDef.DeclaringType.Properties[i];
						if (prop.GetMethod == methodDef)
							return target.Member(prop.Name, prop).WithAnnotation(prop).WithAnnotation(method);
					}
				} else if ((semAttr & MethodSemanticsAttributes.Getter) != 0) { // with parameters
					if (methodDef == null && method.Name == "get_Item")
						return target.Indexer(methodArgs).WithAnnotation(method);
					PropertyDef indexer = GetIndexer(methodDef);
					if (indexer != null)
						return target.Indexer(methodArgs).WithAnnotation(indexer).WithAnnotation(method);
				} else if (methodArgs.Count == 1 && (semAttr & MethodSemanticsAttributes.Setter) != 0) {
					if (methodDef == null)
						return new Ast.AssignmentExpression(target.Member(method.Name.Substring(4), method).WithAnnotation(method), methodArgs[0]);
					if (forceSemAttr != null) {
						// read-only property, the method is actually the getter since there's no setter
						for (int i = 0; i < methodDef.DeclaringType.Properties.Count; i++) {
							var prop = methodDef.DeclaringType.Properties[i];
							if (prop.GetMethod == methodDef)
								return new Ast.AssignmentExpression(target.Member(prop.Name, prop).WithAnnotation(prop).WithAnnotation(method), methodArgs[0]);
						}
					}
					else {
						for (int i = 0; i < methodDef.DeclaringType.Properties.Count; i++) {
							var prop = methodDef.DeclaringType.Properties[i];
							if (prop.SetMethod == methodDef)
								return new Ast.AssignmentExpression(target.Member(prop.Name, prop).WithAnnotation(prop).WithAnnotation(method), methodArgs[0]);
						}
					}
				} else if (methodArgs.Count > 1 && (semAttr & MethodSemanticsAttributes.Setter) != 0) {
					PropertyDef indexer = GetIndexer(methodDef);
					if (indexer != null || (methodDef == null && method.Name == "set_Item"))
						return new AssignmentExpression(
							target.Indexer(methodArgs.GetRange(0, methodArgs.Count - 1)).WithAnnotation(indexer).WithAnnotation(method),
							methodArgs[methodArgs.Count - 1]
						);
				} else if (methodArgs.Count == 1 && (semAttr & MethodSemanticsAttributes.AddOn) != 0) {
					if (methodDef == null) {
						return new Ast.AssignmentExpression {
							Left = target.Member(method.Name.Substring(4), method).WithAnnotation(method),
							Operator = AssignmentOperatorType.Add,
							Right = methodArgs[0]
						};
					}
					for (int i = 0; i < methodDef.DeclaringType.Events.Count; i++) {
						var ev = methodDef.DeclaringType.Events[i];
						if (ev.AddMethod == methodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name, ev).WithAnnotation(ev).WithAnnotation(method),
								Operator = AssignmentOperatorType.Add,
								Right = methodArgs[0]
							};
						}
					}
				} else if (methodArgs.Count == 1 && (semAttr & MethodSemanticsAttributes.RemoveOn) != 0) {
					if (methodDef == null) {
						return new Ast.AssignmentExpression {
							Left = target.Member(method.Name.Substring(7), method).WithAnnotation(method),
							Operator = AssignmentOperatorType.Subtract,
							Right = methodArgs[0]
						};
					}
					for (int i = 0; i < methodDef.DeclaringType.Events.Count; i++) {
						var ev = methodDef.DeclaringType.Events[i];
						if (ev.RemoveMethod == methodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name, ev).WithAnnotation(ev).WithAnnotation(method),
								Operator = AssignmentOperatorType.Subtract,
								Right = methodArgs[0]
							};
						}
					}
				}
			} else if (methodDef != null && methodDef.Name == nameInvoke && methodDef.DeclaringType.BaseType != null && methodDef.DeclaringType.BaseType.FullName == "System.MulticastDelegate") {
				AdjustArgumentsForMethodCall(method, methodArgs);
				return target.Invoke(methodArgs).WithAnnotation(method);
			}
			// Default invocation
			AdjustArgumentsForMethodCall(methodDef ?? method, methodArgs);

			if (method.MethodSig is not null && method.MethodSig.IsVarArg) {
				var argListArg = new UndocumentedExpression { UndocumentedExpressionType = UndocumentedExpressionType.ArgList };
				for (int i = 0; i < methodArgs.Count; i++) {
					if (i < method.MethodSig.Params.Count)
						continue;
					argListArg.Arguments.Add(methodArgs[i]);
					methodArgs.RemoveAt(i);
					i--;
				}
				methodArgs.Add(argListArg);
			}

			return target.Invoke(methodDef ?? method, method.Name, ConvertTypeArguments(method), methodArgs).WithAnnotation(method);
		}
		static readonly UTF8String nameInvoke = new UTF8String("Invoke");

		AstNode TransformCalli(ILExpression byteCode, List<Expression> args) {
			var methodSig = (MethodSig)byteCode.Operand;
			var methodArgs = new List<Expression>(args);

			var target = methodArgs.Last();
			methodArgs.RemoveAt(methodArgs.Count - 1);

			// Unpack any DirectionExpression that is used as target for the call
			// (calling methods on value types implicitly passes the first argument by reference)
			target = UnpackDirectionExpression(target);

			target.AddChild(new Comment($"calli[{methodSig.CallingConvention.ToString().ToLower()}]", CommentType.MultiLine), Roles.Comment);

			return target.Invoke(methodArgs).CastTo(AstBuilder.ConvertType(methodSig.RetType, stringBuilder));
		}

		static MethodSemanticsAttributes GetMethodSemanticsAttributes(dnlib.DotNet.IMethod method) {
			if (method == null)
				return MethodSemanticsAttributes.None;
			string name = method.Name;
			if (name.StartsWith("get_"))
				return MethodSemanticsAttributes.Getter;
			if (name.StartsWith("set_"))
				return MethodSemanticsAttributes.Setter;
			if (name.StartsWith("add_"))
				return MethodSemanticsAttributes.AddOn;
			if (name.StartsWith("remove_"))
				return MethodSemanticsAttributes.RemoveOn;
			return MethodSemanticsAttributes.None;
		}

		Expression UnpackDirectionExpression(Expression target)
		{
			if (target is DirectionExpression) {
				var expr = ((DirectionExpression)target).Expression.Detach();
				if (context.CalculateILSpans)
					target.AddAllRecursiveILSpansTo(expr);
				return expr;
			} else {
				return target;
			}
		}

		static void AdjustArgumentsForMethodCall(IMethod method, List<Expression> methodArgs)
		{
			MethodDef methodDef = method.Resolve();
			if (methodDef == null)
				return;
			int skip = methodDef.Parameters.GetParametersSkip();
			// Convert 'ref' into 'out' where necessary
			for (int i = 0; i < methodArgs.Count && i < methodDef.Parameters.Count - skip; i++) {
				DirectionExpression dir = methodArgs[i] as DirectionExpression;
				Parameter p = methodDef.Parameters[i + skip];
				if (dir != null && p.HasParamDef) {
					if (p.ParamDef.IsOut && !p.ParamDef.IsIn)
						dir.FieldDirection = FieldDirection.Out;
					else if (DnlibExtensions.HasIsReadOnlyAttribute(p.ParamDef))
						dir.FieldDirection = FieldDirection.In;
				}
			}
		}

		static readonly UTF8String systemReflectionString = new UTF8String("System.Reflection");
		static readonly UTF8String defaultMemberAttributeString = new UTF8String("DefaultMemberAttribute");
		internal static PropertyDef GetIndexer(MethodDef method)
		{
			if (method == null)
				return null;
			TypeDef typeDef = method.DeclaringType;
			UTF8String indexerName = null;
			for (int i = 0; i < typeDef.CustomAttributes.Count; i++) {
				var ca = typeDef.CustomAttributes[i];
				if (ca.ConstructorArguments.Count != 1)
					continue;
				var ctor = ca.Constructor;
				if (ctor == null)
					continue;
				var sig = ctor.MethodSig;
				if (sig == null || sig.Params.Count != 1 || sig.Params[0].GetElementType() != ElementType.String)
					continue;
				var type = ctor.DeclaringType;
				if (!type.Compare(systemReflectionString, defaultMemberAttributeString))
					continue;
				object caValue = ca.ConstructorArguments[0].Value;
				indexerName = caValue as UTF8String;
				if (UTF8String.IsNull(indexerName)) {
					if (caValue is not string str)
						continue;
					indexerName = str;
				}
				break;
			}

			if (UTF8String.IsNull(indexerName))
				return null;
			for (int i = 0; i < typeDef.Properties.Count; i++) {
				var prop = typeDef.Properties[i];
				if (prop.Name == indexerName && (prop.GetMethod == method || prop.SetMethod == method))
					return prop;
			}

			return null;
		}

		#if DEBUG
		static readonly ConcurrentDictionary<ILCode, int> unhandledOpcodes = new ConcurrentDictionary<ILCode, int>();
		#endif

		[Conditional("DEBUG")]
		public static void ClearUnhandledOpcodes()
		{
			#if DEBUG
			unhandledOpcodes.Clear();
			#endif
		}

		[Conditional("DEBUG")]
		public static void PrintNumberOfUnhandledOpcodes()
		{
			#if DEBUG
			foreach (var pair in unhandledOpcodes) {
				Debug.WriteLine("AddMethodBodyBuilder unhandled opcode: {1}x {0}", pair.Key, pair.Value);
			}
			#endif
		}

		static Expression InlineAssembly(ILExpression byteCode, List<Ast.Expression> args)
		{
			#if DEBUG
			unhandledOpcodes.AddOrUpdate(byteCode.Code, c => 1, (c, n) => n+1);
			#endif
			// Output the operand of the unknown IL code as well
			if (byteCode.Operand != null) {
				var ie = IdentifierExpression.Create(FormatByteCodeOperand(byteCode.Operand), byteCode.Operand);
				ie.IdentifierToken.AddAnnotation(IdentifierFormatted.Instance);
				args.Insert(0, ie);
			}
			return IdentifierExpression.Create(byteCode.Code.GetName(), BoxedTextColor.OpCode).Invoke(args);
		}

		static string FormatByteCodeOperand(object operand)
		{
			if (operand == null) {
				return string.Empty;
				//} else if (operand is ILExpression) {
				//	return string.Format("IL_{0:X2}", ((ILExpression)operand).Offset);
			} else if (operand is IMethod && ((IMethod)operand).MethodSig != null) {
				return IdentifierEscaper.Escape(((IMethod)operand).Name) + "()";
			} else if (operand is ITypeDefOrRef) {
				return IdentifierEscaper.Escape(((ITypeDefOrRef)operand).FullName);
			} else if (operand is Local) {
				return IdentifierEscaper.Escape(((Local)operand).Name);
			} else if (operand is Parameter) {
				return IdentifierEscaper.Escape(((Parameter)operand).Name);
			} else if (operand is IField) {
				return IdentifierEscaper.Escape(((IField)operand).Name);
			} else if (operand is string) {
				return "\"" + Escape((string)operand) + "\"";
			} else if (operand is int) {
				return operand.ToString();
			} else if (operand is MethodSig) {
				var msig = (MethodSig)operand;
				return Escape(DnlibExtensions.GetMethodSigFullName(msig));
			} else {
				return Escape(operand.ToString());
			}
		}

		static string Escape(string s)
		{
			if (s.IndexOfAny(newLineChars) < 0)
				return s;
			s = s.Replace("\r", @"\u000D");
			s = s.Replace("\n", @"\u000A");
			s = s.Replace("\u0085", @"\u0085");
			s = s.Replace("\u2028", @"\u2028");
			s = s.Replace("\u2029", @"\u2029");
			return s;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		IEnumerable<AstType> ConvertTypeArguments(IMethod method)
		{
			MethodSpec g = method as MethodSpec;
			if (g == null || g.GenericInstMethodSig == null)
				return null;
			if (g.GenericInstMethodSig.GenericArguments.Any(ta => ta.ContainsAnonymousType()))
				return null;
			return g.GenericInstMethodSig.GenericArguments.Select(t => AstBuilder.ConvertType(t, stringBuilder));
		}

		static Ast.DirectionExpression MakeRef(Ast.Expression expr)
		{
			return new DirectionExpression { Expression = expr, FieldDirection = FieldDirection.Ref };
		}

		Ast.Expression Convert(Ast.Expression expr, TypeSig actualType, TypeSig reqType)
		{
			if (actualType == null || reqType == null || TypeAnalysis.IsSameType(actualType, reqType)) {
				return expr;
			} else if (actualType is ByRefSig && reqType is PtrSig && expr is DirectionExpression) {
				return Convert(
					new UnaryOperatorExpression(UnaryOperatorType.AddressOf, ((DirectionExpression)expr).Expression.Detach()),
					new PtrSig(((ByRefSig)actualType).Next),
					reqType);
			} else if (actualType is PtrSig && reqType is ByRefSig) {
				expr = Convert(expr, actualType, new PtrSig(reqType.Next));
				return new DirectionExpression {
					FieldDirection = FieldDirection.Ref,
					Expression = new UnaryOperatorExpression(UnaryOperatorType.Dereference, expr)
				};
			} else if (actualType is PtrSig && reqType is PtrSig) {
				if (actualType.FullName != reqType.FullName)
					return expr.CastTo(AstBuilder.ConvertType(reqType, stringBuilder));
				else
					return expr;
			} else {
				if (reqType.GetElementType() == ElementType.Boolean) {
					if (actualType.GetElementType() == ElementType.Boolean)
						return expr;
					if (TypeAnalysis.IsIntegerOrEnum(actualType)) {
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, AstBuilder.MakePrimitive(0, actualType.ToTypeDefOrRef(), stringBuilder));
					} else {
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, new NullReferenceExpression());
					}
				}
				bool requiredIsIntegerOrEnum = TypeAnalysis.IsIntegerOrEnum(reqType);
				if (actualType.GetElementType() == ElementType.Boolean && requiredIsIntegerOrEnum) {
					return new ConditionalExpression {
						Condition = expr,
						TrueExpression = AstBuilder.MakePrimitive(1, reqType.ToTypeDefOrRef(), stringBuilder),
						FalseExpression = AstBuilder.MakePrimitive(0, reqType.ToTypeDefOrRef(), stringBuilder)
					};
				}

				if (expr is PrimitiveExpression && !requiredIsIntegerOrEnum && TypeAnalysis.IsEnum(actualType))
				{
					return expr.CastTo(AstBuilder.ConvertType(actualType, stringBuilder));
				}

				bool actualIsPrimitiveType = TypeAnalysis.IsIntegerOrEnum(actualType)
					|| actualType.GetElementType() == ElementType.R4 || actualType.GetElementType() == ElementType.R8;
				bool requiredIsPrimitiveType = requiredIsIntegerOrEnum
					|| reqType.GetElementType() == ElementType.R4 || reqType.GetElementType() == ElementType.R8;
				if (actualIsPrimitiveType && requiredIsPrimitiveType) {
					return expr.CastTo(AstBuilder.ConvertType(reqType, stringBuilder));
				}
				return expr;
			}
		}
	}
}
