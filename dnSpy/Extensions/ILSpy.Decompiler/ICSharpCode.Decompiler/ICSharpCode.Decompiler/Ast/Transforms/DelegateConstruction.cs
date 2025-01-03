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
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Converts "new Action(obj, ldftn(func))" into "new Action(obj.func)".
	/// For anonymous methods, creates an AnonymousMethodExpression.
	/// Also gets rid of any "Display Classes" left over after inlining an anonymous method.
	/// </summary>
	public class DelegateConstruction : ContextTrackingVisitor<object>, IAstTransformPoolObject
	{
		internal sealed class Annotation
		{
			public static readonly Annotation True = new Annotation(true);
			public static readonly Annotation False = new Annotation(false);

			/// <summary>
			/// ldftn or ldvirtftn?
			/// </summary>
			public readonly bool IsVirtual;

			Annotation(bool isVirtual)
			{
				this.IsVirtual = isVirtual;
			}
		}

		internal sealed class CapturedVariableAnnotation
		{
			public static readonly CapturedVariableAnnotation Instance = new CapturedVariableAnnotation();
			CapturedVariableAnnotation() { }
		}

		readonly List<string> currentlyUsedVariableNames = new List<string>();
		readonly StringBuilder stringBuilder;
		readonly AutoPropertyProvider autoPropertyProvider;

		public DelegateConstruction(DecompilerContext context) : base(context)
		{
			stringBuilder = new StringBuilder();
			autoPropertyProvider = new AutoPropertyProvider();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
			currentlyUsedVariableNames.Clear();
			autoPropertyProvider.Reset();
		}

		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (objectCreateExpression.Arguments.Count == 2) {
				Expression obj = objectCreateExpression.Arguments.First();
				Expression func = objectCreateExpression.Arguments.Last();
				Annotation annotation = func.Annotation<Annotation>();
				if (annotation != null) {
					IdentifierExpression methodIdent = (IdentifierExpression)((InvocationExpression)func).Arguments.Single();
					IMethod method = methodIdent.Annotation<IMethod>();
					if (method != null) {
						if (HandleAnonymousMethod(objectCreateExpression, obj, method))
							return null;
						var ilSpans = context.CalculateILSpans ? objectCreateExpression.GetAllRecursiveILSpans() : null;
						// Perform the transformation to "new Action(obj.func)".
						obj.Remove();
						methodIdent.Remove();
						if (!annotation.IsVirtual && obj is ThisReferenceExpression) {
							// maybe it's getting the pointer of a base method?
							if (method.DeclaringType.ResolveTypeDef() != context.CurrentType) {
								obj = new BaseReferenceExpression().WithAnnotation(method.DeclaringType);
							}
						}
						if (!annotation.IsVirtual && obj is NullReferenceExpression && method.MethodSig != null && !method.MethodSig.HasThis) {
							// We're loading a static method.
							// However it is possible to load extension methods with an instance, so we compare the number of arguments:
							bool isExtensionMethod = false;
							ITypeDefOrRef delegateType = objectCreateExpression.Type.Annotation<ITypeDefOrRef>();
							if (delegateType != null) {
								TypeDef delegateTypeDef = delegateType.ResolveTypeDef();
								if (delegateTypeDef != null) {
									MethodDef invokeMethod = delegateTypeDef.Methods.FirstOrDefault(m => m.Name == "Invoke");
									if (invokeMethod != null) {
										isExtensionMethod = (invokeMethod.Parameters.GetNumberOfNormalParameters() + 1 == method.MethodSig.GetParameters().Count);
									}
								}
							}
							if (!isExtensionMethod) {
								obj = new TypeReferenceExpression { Type = AstBuilder.ConvertType(method.DeclaringType, stringBuilder) };
							}
						}
						// now transform the identifier into a member reference
						MemberReferenceExpression mre = new MemberReferenceExpression();
						mre.Target = obj;
						mre.MemberNameToken = (Identifier)methodIdent.IdentifierToken.Clone();
						methodIdent.TypeArguments.MoveTo(mre.TypeArguments);
						mre.AddAnnotation(method);

						// Replace 'new DelegateClass(<method>)' with '<method>' if it's an event adder/remover
						var parent = objectCreateExpression.Parent as AssignmentExpression;
						if (context.Settings.RemoveNewDelegateClass && parent != null && (parent.Operator == AssignmentOperatorType.Add || parent.Operator == AssignmentOperatorType.Subtract)) {
							var delType = objectCreateExpression.Annotation<IMethod>()?.DeclaringType;
							var invokeMethod = delType.ResolveTypeDef()?.FindMethod(nameInvoke);
							if (invokeMethod != null) {
								var invokeSig = GetMethodBaseSig(delType, invokeMethod.MethodSig);
								var msig = GetMethodBaseSig(method.DeclaringType, method.MethodSig, (method as MethodSpec)?.GenericInstMethodSig.GenericArguments);
								invokeSig = new MethodSig((invokeSig.CallingConvention | CallingConvention.HasThis) & ~CallingConvention.Generic, 0, invokeSig.RetType, invokeSig.Params);
								msig = new MethodSig((msig.CallingConvention | CallingConvention.HasThis) & ~CallingConvention.Generic, 0, msig.RetType, msig.Params);
								if (new SigComparer().Equals(invokeSig, msig)) {
									objectCreateExpression.ReplaceWith(mre);
									mre.AddAnnotation(ilSpans);
									return null;
								}
							}
						}

						objectCreateExpression.Arguments.Clear();
						objectCreateExpression.Arguments.Add(mre);
						objectCreateExpression.AddAnnotation(ilSpans);
						return null;
					}
				}
			}
			return base.VisitObjectCreateExpression(objectCreateExpression, data);
		}
		static readonly UTF8String nameInvoke = new UTF8String("Invoke");

		static MethodBaseSig GetMethodBaseSig(ITypeDefOrRef type, MethodBaseSig msig, IList<TypeSig> methodGenArgs = null)
		{
			IList<TypeSig> typeGenArgs = null;
			var ts = type as TypeSpec;
			if (ts != null) {
				var genSig = ts.TypeSig.ToGenericInstSig();
				if (genSig != null)
					typeGenArgs = genSig.GenericArguments;
			}
			if (typeGenArgs == null && methodGenArgs == null)
				return msig;
			return GenericArgumentResolver.Resolve(msig, typeGenArgs, methodGenArgs);
		}

		internal static bool IsAnonymousMethod(DecompilerContext context, MethodDef method)
		{
			if (method is null)
				return false;
			if (method.IsLocalFunction())
				return false;
			if (!(method.HasGeneratedName()
				  || method.Name.Contains("$"))
				  || !(method.IsCompilerGeneratedOrIsInCompilerGeneratedClass()
				  || IsPotentialClosure(context, method.DeclaringType)
				  || ContainsAnonymousType(method)))
			{
				return false;
			}
			return true;
		}

		static bool ContainsAnonymousType(MethodDef method)
		{
			if (method.ReturnType.ContainsAnonymousType())
				return true;
			foreach (var p in method.Parameters)
			{
				if (p.Type.ContainsAnonymousType())
					return true;
			}
			return false;
		}

		bool HandleAnonymousMethod(ObjectCreateExpression objectCreateExpression, Expression target, IMethod methodRef)
		{
			if (!context.Settings.AnonymousMethods)
				return false; // anonymous method decompilation is disabled
			if (target != null && !(target is IdentifierExpression || target is ThisReferenceExpression || target is NullReferenceExpression || target is MemberReferenceExpression))
				return false; // don't copy arbitrary expressions, deal with identifiers only

			// Anonymous methods are defined in the same assembly
			MethodDef method = methodRef.ResolveMethodWithinSameModule();
			if (!IsAnonymousMethod(context, method))
				return false;

			var ilSpans = context.CalculateILSpans ? objectCreateExpression.GetAllRecursiveILSpans() : null;

			// Create AnonymousMethodExpression and prepare parameters
			AnonymousMethodExpression ame = new AnonymousMethodExpression();
			ame.CopyAnnotationsFrom(objectCreateExpression); // copy ILSpans etc.
			ame.RemoveAnnotations<IMethod>(); // remove reference to delegate ctor
			ame.AddAnnotation(method); // add reference to anonymous method
			ame.Parameters.AddRange(AstBuilder.MakeParameters(context.MetadataTextColorProvider, method, context.Settings, stringBuilder, isLambda: true));
			ame.HasParameterList = true;

			// rename variables so that they don't conflict with the parameters:
			foreach (ParameterDeclaration pd in ame.Parameters) {
				EnsureVariableNameIsAvailable(objectCreateExpression, pd.Name);
			}

			// Decompile the anonymous method:

			DecompilerContext subContext = context.CloneDontUse();
			subContext.CurrentMethod = method;
			subContext.CurrentMethodIsAsync = false;
			subContext.CurrentMethodIsYieldReturn = false;
			subContext.ReservedVariableNames.AddRange(currentlyUsedVariableNames);
			subContext.CalculateILSpans = true;
			MethodDebugInfoBuilder builder;
			BlockStatement body;
			try {
				body = AstMethodBodyBuilder.CreateMethodBody(method, subContext, autoPropertyProvider, ame.Parameters, false,
					stringBuilder, out builder);
			}
			catch (OperationCanceledException) {
				throw;
			}
			catch (Exception ex) {
				AstBuilder.CreateBadMethod(subContext, method, ex, out body, out builder);
			}
			TransformationPipeline.RunTransformationsUntil(body, v => v is DelegateConstruction, subContext);
			body.AcceptVisitor(this, null);
			ame.IsAsync = subContext.CurrentMethodIsAsync;

			bool isLambda = false;
			if (ame.Parameters.All(p => p.ParameterModifier == ParameterModifier.None)) {
				isLambda = body.Statements.Count == 1 && body.Statements.Single() is ReturnStatement &&
						body.HiddenStart == null && body.HiddenEnd == null;
			}
			// Remove the parameter list from an AnonymousMethodExpression if the original method had no names,
			// and the parameters are not used in the method body
			if (!isLambda && method.Parameters.SkipNonNormal().All(p => string.IsNullOrEmpty(p.Name) || (p.Name.StartsWith("<", StringComparison.Ordinal) && p.Name.EndsWith(">", StringComparison.Ordinal)))) {
				var parameterReferencingIdentifiers =
					from ident in body.Descendants.OfType<IdentifierExpression>()
					let v = ident.Annotation<ILVariable>()
					where v != null && v.IsParameter && method.Parameters.Contains(v.OriginalParameter)
					select ident;
				if (!parameterReferencingIdentifiers.Any()) {
					ame.AddAnnotation(ame.Parameters.GetAllRecursiveILSpans());
					ame.Parameters.Clear();
					ame.HasParameterList = false;
				}
			}

			// Replace all occurrences of 'this' in the method body with the delegate's target:
			foreach (AstNode node in body.Descendants) {
				if (node is ThisReferenceExpression) {
					var newTarget = target.Clone();
					if (context.CalculateILSpans) {
						newTarget.RemoveAllILSpansRecursive();
						newTarget.AddAnnotation(node.GetAllRecursiveILSpans());
					}
					node.ReplaceWith(newTarget);
				}
			}
			Expression replacement;
			if (isLambda) {
				LambdaExpression lambda = new LambdaExpression();
				lambda.CopyAnnotationsFrom(ame);
				ame.Parameters.MoveTo(lambda.Parameters);
				var stmtILSpans = body.Statements.Single().GetAllILSpans();
				Expression returnExpr = ((ReturnStatement)body.Statements.Single()).Expression;
				if (stmtILSpans.Count > 0)
					returnExpr.AddAnnotation(stmtILSpans);
				returnExpr.Remove();
				returnExpr.AddAnnotation(builder);
				lambda.Body = returnExpr;
				lambda.IsAsync = subContext.CurrentMethodIsAsync;
				replacement = lambda;
			} else {
				ame.AddAnnotation(builder);
				ame.Body = body;
				replacement = ame;
			}
			var expectedType = objectCreateExpression.Annotation<TypeInformation>()?.ExpectedType?.Resolve();
			if (expectedType != null && !expectedType.IsDelegate) {
				var simplifiedDelegateCreation = (ObjectCreateExpression)objectCreateExpression.Clone();
				simplifiedDelegateCreation.Arguments.Clear();
				simplifiedDelegateCreation.Arguments.Add(replacement);
				replacement = simplifiedDelegateCreation;
			}
			objectCreateExpression.ReplaceWith(replacement);
			replacement.AddAnnotation(ilSpans);
			return true;
		}

		internal static bool IsPotentialClosure(DecompilerContext context, TypeDef potentialDisplayClass)
		{
			if (potentialDisplayClass == null || !potentialDisplayClass.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return false;
			// check that methodContainingType is within containingType
			while (potentialDisplayClass != context.CurrentType) {
				potentialDisplayClass = potentialDisplayClass.DeclaringType;
				if (potentialDisplayClass == null)
					return false;
			}
			return true;
		}

		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (context.Settings.ExpressionTrees && ExpressionTreeConverter.CouldBeExpressionTree(invocationExpression)) {
				Expression converted = ExpressionTreeConverter.TryConvert(context, invocationExpression, this.stringBuilder);
				if (converted != null) {
					//TODO: Do we need to preserve ILSpans or is it taken care of by TryConvert?
					converted.RemoveAllILSpansRecursive();
					invocationExpression.AddAllRecursiveILSpansTo(converted);
					invocationExpression.ReplaceWith(converted);
					return converted.AcceptVisitor(this, data);
				}
			}
			return base.VisitInvocationExpression(invocationExpression, data);
		}

		#region Track current variables
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(methodDeclaration.Parameters.Select(p => p.Name));
				return base.VisitMethodDeclaration(methodDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}

		public override object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(operatorDeclaration.Parameters.Select(p => p.Name));
				return base.VisitOperatorDeclaration(operatorDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}

		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(constructorDeclaration.Parameters.Select(p => p.Name));
				return base.VisitConstructorDeclaration(constructorDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}

		public override object VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(indexerDeclaration.Parameters.Select(p => p.Name));
				return base.VisitIndexerDeclaration(indexerDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}

		public override object VisitAccessor(Accessor accessor, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				if (accessor.Role != PropertyDeclaration.GetterRole)
					currentlyUsedVariableNames.Add("value");
				return base.VisitAccessor(accessor, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}

		public override object VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			foreach (VariableInitializer v in variableDeclarationStatement.Variables)
				currentlyUsedVariableNames.Add(v.Name);
			return base.VisitVariableDeclarationStatement(variableDeclarationStatement, data);
		}

		public override object VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			foreach (VariableInitializer v in fixedStatement.Variables)
				currentlyUsedVariableNames.Add(v.Name);
			return base.VisitFixedStatement(fixedStatement, data);
		}
		#endregion

		static readonly ExpressionStatement displayClassAssignmentPattern =
			new ExpressionStatement(new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression(Pattern.AnyString)),
				new ObjectCreateExpression { Type = new AnyNode("type") }
			));

		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			base.VisitBlockStatement(blockStatement, data);
			foreach (ExpressionStatement stmt in blockStatement.Statements.OfType<ExpressionStatement>().ToArray()) {
				Match displayClassAssignmentMatch = displayClassAssignmentPattern.Match(stmt);
				if (!displayClassAssignmentMatch.Success)
					continue;

				ILVariable variable = displayClassAssignmentMatch.Get<AstNode>("variable").Single().Annotation<ILVariable>();
				if (variable == null)
					continue;
				TypeDef type = variable.Type.ToTypeDefOrRef().ResolveWithinSameModule();
				if (!IsPotentialClosure(context, type))
					continue;
				if (displayClassAssignmentMatch.Get<AstType>("type").Single().Annotation<ITypeDefOrRef>().ResolveWithinSameModule() != type)
					continue;

				// Looks like we found a display class creation. Now let's verify that the variable is used only for field accesses:
				bool ok = true;
				INode nodeVar = null;
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name && identExpr != (nodeVar ?? (nodeVar = displayClassAssignmentMatch.Get("variable").Single()))) {
						if (!(identExpr.Parent is MemberReferenceExpression && identExpr.Parent.Annotation<IField>()?.IsField == true)) {
							ok = false;
							break;
						}
					}
				}
				if (!ok)
					continue;
				Dictionary<IField, AstNode> dict = new Dictionary<IField, AstNode>();

				// Delete the variable declaration statement:
				VariableDeclarationStatement displayClassVarDecl = PatternStatementTransform.FindVariableDeclaration(stmt, variable.Name);
				if (displayClassVarDecl != null)
					displayClassVarDecl.Remove();

				// Delete the assignment statement:
				AstNode cur = stmt.NextSibling;
				stmt.Remove();

				// Delete any following statements as long as they assign parameters to the display class
				BlockStatement rootBlock = blockStatement.Ancestors.OfType<BlockStatement>().LastOrDefault() ?? blockStatement;
				List<ILVariable> parameterOccurrances = rootBlock.Descendants.OfType<IdentifierExpression>()
					.Select(n => n.Annotation<ILVariable>()).Where(p => p != null && p.IsParameter).ToList();
				AstNode next;
				FieldDef thisField = null;
				for (; cur != null; cur = next) {
					next = cur.NextSibling;

					// Test for the pattern:
					// "variableName.MemberName = right;"
					ExpressionStatement closureFieldAssignmentPattern = new ExpressionStatement(
						new AssignmentExpression(
							new NamedNode("left", new MemberReferenceExpression {
							              	Target = IdentifierExpression.Create(variable.Name, variable.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local),
							              	MemberName = Pattern.AnyString
							              }),
							new AnyNode("right")
						)
					);
					Match m = closureFieldAssignmentPattern.Match(cur);
					if (m.Success) {
						var leftStmt = m.Get<MemberReferenceExpression>("left").Single();
						FieldDef fieldDef = leftStmt.Annotation<IField>().ResolveFieldWithinSameModule();
						AstNode right = m.Get<AstNode>("right").Single();
						bool isParameter = false;
						bool isDisplayClassParentPointerAssignment = false;
						if (right is ThisReferenceExpression) {
							isParameter = true;
							thisField = fieldDef;
						} else if (right is IdentifierExpression) {
							// handle parameters only if the whole method contains no other occurrence except for 'right'
							ILVariable v = right.Annotation<ILVariable>();
							isParameter = v.IsParameter && parameterOccurrances.Count(c => c == v) == 1;
							if (!isParameter && IsPotentialClosure(context, v.Type.ToTypeDefOrRef().ResolveWithinSameModule())) {
								// parent display class within the same method
								// (closure2.localsX = closure1;)
								isDisplayClassParentPointerAssignment = true;
							}
						} else if (right is MemberReferenceExpression) {
							// copy of parent display class reference from an outer lambda
							// closure2.localsX = this.localsY
							MemberReferenceExpression mre = m.Get<MemberReferenceExpression>("right").Single();
							do {
								// descend into the targets of the mre as long as the field types are closures
								FieldDef fieldDef2 = mre.Annotation<FieldDef>().ResolveFieldWithinSameModule();
								if (fieldDef2 == null || !IsPotentialClosure(context, fieldDef2.FieldType.ToTypeDefOrRef().ResolveWithinSameModule())) {
									break;
								}
								// if we finally get to a this reference, it's copying a display class parent pointer
								if (mre.Target is ThisReferenceExpression) {
									isDisplayClassParentPointerAssignment = true;
								}
								mre = mre.Target as MemberReferenceExpression;
							} while (mre != null);
						}
						if (isParameter || isDisplayClassParentPointerAssignment) {
							if (fieldDef != null)
								dict[fieldDef] = right;
							cur.Remove();
						} else {
							break;
						}
					} else {
						break;
					}
				}

				// Now create variables for all fields of the display class (except for those that we already handled as parameters)
				List<Tuple<AstType, ILVariable>> variablesToDeclare = new List<Tuple<AstType, ILVariable>>();
				foreach (FieldDef field in type.Fields) {
					if (field.IsStatic)
						continue; // skip static fields
					if (dict.ContainsKey(field)) // skip field if it already was handled as parameter
						continue;
					string capturedVariableName = field.Name;
					if (capturedVariableName.StartsWith("$VB$Local_", StringComparison.Ordinal) && capturedVariableName.Length > 10)
						capturedVariableName = capturedVariableName.Substring(10);
					EnsureVariableNameIsAvailable(blockStatement, capturedVariableName);
					currentlyUsedVariableNames.Add(capturedVariableName);
					ILVariable ilVar = new ILVariable(capturedVariableName)
					{
						GeneratedByDecompiler = true,
						Type = field.FieldType,
					};
					variablesToDeclare.Add(Tuple.Create(AstBuilder.ConvertType(field.FieldType, stringBuilder, field), ilVar));
					dict[field] = IdentifierExpression.Create(capturedVariableName, BoxedTextColor.Local).WithAnnotation(ilVar);
				}

				// Now figure out where the closure was accessed and use the simpler replacement expression there:
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name) {
						AstNode mre = identExpr.Parent;
						AstNode replacement;
						var fieldDef = mre.Annotation<IField>().ResolveFieldWithinSameModule();
						if (fieldDef != null && dict.TryGetValue(fieldDef, out replacement)) {
							var newReplacement = replacement.Clone();
							if (context.CalculateILSpans) {
								newReplacement.RemoveAllILSpansRecursive();
								newReplacement.AddAnnotation(mre.GetAllRecursiveILSpans());
							}
							mre.ReplaceWith(newReplacement);
							// mcs: this.$this.field
							if (fieldDef == thisField && newReplacement.Parent is MemberReferenceExpression parentMre && parentMre.MemberName == "$this" && parentMre.Parent is MemberReferenceExpression parentParentMre) {
								var oldMemberToken = parentMre.MemberNameToken.Detach();
								parentMre.MemberNameToken = parentParentMre.MemberNameToken.Detach();
								if (context.CalculateILSpans)
									parentMre.AddAnnotation(oldMemberToken.GetAllRecursiveILSpans());
								parentParentMre.ReplaceWith(parentMre.Detach());
								parentMre.RemoveAnnotations<IField>();//'$this' field
								parentMre.AddAnnotationsFrom(parentParentMre);//'field' field
							}
						}
					}
				}
				// Now insert the variable declarations (we can do this after the replacements only so that the scope detection works):
				Statement insertionPoint = blockStatement.Statements.FirstOrDefault();
				foreach (var tuple in variablesToDeclare) {
					var newVarDecl = new VariableDeclarationStatement(tuple.Item2.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local, tuple.Item1, tuple.Item2.Name);
					newVarDecl.Variables.Single().AddAnnotation(CapturedVariableAnnotation.Instance);
					newVarDecl.Variables.Single().AddAnnotation(tuple.Item2);
					blockStatement.Statements.InsertBefore(insertionPoint, newVarDecl);
				}
			}
			return null;
		}

		void EnsureVariableNameIsAvailable(AstNode currentNode, string name)
		{
			int pos = currentlyUsedVariableNames.IndexOf(name);
			if (pos < 0) {
				// name is still available
				return;
			}
			// Naming conflict. Let's rename the existing variable so that the field keeps the name from metadata.
			NameVariables nv = new NameVariables(stringBuilder);
			// Add currently used variable and parameter names
			foreach (string nameInUse in currentlyUsedVariableNames)
				nv.AddExistingName(nameInUse);
			// variables declared in child nodes of this block
			foreach (VariableInitializer vi in currentNode.Descendants.OfType<VariableInitializer>())
				nv.AddExistingName(vi.Name);
			// parameters in child lambdas
			foreach (ParameterDeclaration pd in currentNode.Descendants.OfType<ParameterDeclaration>())
				nv.AddExistingName(pd.Name);

			string newName = nv.GetAlternativeName(name);
			currentlyUsedVariableNames[pos] = newName;

			// find top-most block
			AstNode topMostBlock = currentNode.Ancestors.OfType<BlockStatement>().LastOrDefault() ?? currentNode;

			// rename identifiers
			foreach (IdentifierExpression ident in topMostBlock.Descendants.OfType<IdentifierExpression>()) {
				if (ident.Identifier == name) {
					var id = Identifier.Create(newName);
					id.AddAnnotationsFrom(ident.IdentifierToken);
					ident.IdentifierToken = id;
					ILVariable v = ident.Annotation<ILVariable>();
					if (v != null)
						v.Name = newName;
				}
			}
			// rename variable declarations
			foreach (VariableInitializer vi in topMostBlock.Descendants.OfType<VariableInitializer>()) {
				if (vi.Name == name) {
					var id = Identifier.Create(newName);
					id.AddAnnotationsFrom(vi.NameToken);
					vi.NameToken = id;
					ILVariable v = vi.Annotation<ILVariable>();
					if (v != null)
						v.Name = newName;
				}
			}
		}
	}
}
