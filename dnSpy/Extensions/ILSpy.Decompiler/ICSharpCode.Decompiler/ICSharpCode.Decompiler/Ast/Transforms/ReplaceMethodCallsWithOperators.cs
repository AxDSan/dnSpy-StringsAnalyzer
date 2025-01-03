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

using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Replaces method calls with the appropriate operator expressions.
	/// Also simplifies "x = x op y" into "x op= y" where possible.
	/// </summary>
	public class ReplaceMethodCallsWithOperators : DepthFirstAstVisitor<object, object>, IAstTransformPoolObject
	{
		static readonly MemberReferenceExpression typeHandleOnTypeOfPattern = new MemberReferenceExpression {
			Target = new Choice {
				new TypeOfExpression(new AnyNode()),
				new UndocumentedExpression { UndocumentedExpressionType = UndocumentedExpressionType.RefType, Arguments = { new AnyNode() } }
			},
			MemberName = "TypeHandle"
		};

		DecompilerContext context;
		readonly StringBuilder stringBuilder;

		public ReplaceMethodCallsWithOperators(DecompilerContext context)
		{
			this.stringBuilder = new StringBuilder();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
		}

		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			base.VisitInvocationExpression(invocationExpression, data);
			ProcessInvocationExpression(invocationExpression, stringBuilder);
			return null;
		}

		static bool CheckType(ITypeDefOrRef tdr, UTF8String expNs, UTF8String expName)
		{
			// PERF: Don't allocate a System.String by calling FullName etc.
			var tr = tdr as TypeRef;
			if (tr != null)
				return tr.Name == expName && tr.Namespace == expNs;
			var td = tdr as TypeDef;
			if (td != null)
				return td.Name == expName && td.Namespace == expNs;
			return false;
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String typeString = new UTF8String("Type");
		static readonly UTF8String decimalString = new UTF8String("Decimal");
		static readonly UTF8String activatortring = new UTF8String("Activator");
		static readonly UTF8String systemReflectionString = new UTF8String("System.Reflection");
		static readonly UTF8String fieldInfoString = new UTF8String("FieldInfo");

		internal static void ProcessInvocationExpression(InvocationExpression invocationExpression, StringBuilder sb)
		{
			IMethod methodRef = invocationExpression.Annotation<IMethod>();
			if (methodRef == null)
				return;
			var builder = invocationExpression.Annotation<MethodDebugInfoBuilder>();
			var arguments = invocationExpression.Arguments.ToArray();

			// Reduce "String.Concat(a, b)" to "a + b"
			if (methodRef.Name == "Concat" && methodRef.DeclaringType != null && arguments.Length >= 2 && methodRef.DeclaringType.FullName == "System.String")
			{
				invocationExpression.Arguments.Clear(); // detach arguments from invocationExpression
				Expression expr = arguments[0];
				for (int i = 1; i < arguments.Length; i++) {
					expr = new BinaryOperatorExpression(expr, BinaryOperatorType.Add, arguments[i]);
				}
				expr.CopyAnnotationsFrom(invocationExpression);
				invocationExpression.ReplaceWith(expr);
				expr.AddAnnotation(invocationExpression.GetAllRecursiveILSpans());
				expr.AddAnnotation(builder);
				return;
			}

			if (methodRef.Name == "CreateInstance" && CheckType(methodRef.DeclaringType, systemString, activatortring) &&
				arguments.Length == 0 && methodRef is MethodSpec spec && methodRef.NumberOfGenericParameters > 0 &&
				spec.GenericInstMethodSig.GenericArguments[0] is GenericSig genSig &&
				genSig.GenericParam.HasDefaultConstructorConstraint) {
				invocationExpression.ReplaceWith(
					new ObjectCreateExpression(AstBuilder.ConvertType(spec.GenericInstMethodSig.GenericArguments[0], sb)).WithAnnotation(invocationExpression
						.GetAllRecursiveILSpans()));
			}

			bool isSupportedType = CheckType(methodRef.DeclaringType, systemString, typeString) ||
									   CheckType(methodRef.DeclaringType, systemReflectionString, fieldInfoString);
			switch (isSupportedType ? methodRef.Name.String : string.Empty) {
				case "GetTypeFromHandle":
					if (arguments.Length == 1 && methodRef.FullName == "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)") {
						if (typeHandleOnTypeOfPattern.IsMatch(arguments[0])) {
							invocationExpression.ReplaceWith(((MemberReferenceExpression)arguments[0]).Target
								.WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder));
							return;
						}
					}
					break;
				case "GetFieldFromHandle":
					if (arguments.Length == 1 && methodRef.FullName == "System.Reflection.FieldInfo System.Reflection.FieldInfo::GetFieldFromHandle(System.RuntimeFieldHandle)") {
						MemberReferenceExpression mre = arguments[0] as MemberReferenceExpression;
						if (mre != null && mre.MemberName == "FieldHandle" && mre.Target.Annotation<LdTokenAnnotation>() != null) {
							invocationExpression.ReplaceWith(mre.Target
								.WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder));
							return;
						}
					}
					else if (arguments.Length == 2 && methodRef.FullName == "System.Reflection.FieldInfo System.Reflection.FieldInfo::GetFieldFromHandle(System.RuntimeFieldHandle,System.RuntimeTypeHandle)") {
						MemberReferenceExpression mre1 = arguments[0] as MemberReferenceExpression;
						MemberReferenceExpression mre2 = arguments[1] as MemberReferenceExpression;
						if (mre1 != null && mre1.MemberName == "FieldHandle" && mre1.Target.Annotation<LdTokenAnnotation>() != null) {
							if (mre2 != null && mre2.MemberName == "TypeHandle" && mre2.Target is TypeOfExpression) {
								Expression oldArg = ((InvocationExpression)mre1.Target).Arguments.Single();
								IField field = oldArg.Annotation<IField>();
								if (field != null) {
									var ilSpans = invocationExpression.GetAllRecursiveILSpans();
									AstType declaringType = ((TypeOfExpression)mre2.Target).Type.Detach();
									oldArg.ReplaceWith(declaringType.Member(field.Name, field).WithAnnotation(field));
									invocationExpression.ReplaceWith(mre1.Target.WithAnnotation(ilSpans).WithAnnotation(builder));
									return;
								}
							}
						}
					}

					break;
			}

			BinaryOperatorType? bop = GetBinaryOperatorTypeFromMetadataName(methodRef.Name);
			if (bop != null && arguments.Length == 2) {
				invocationExpression.Arguments.Clear(); // detach arguments from invocationExpression
				invocationExpression.ReplaceWith(
					new BinaryOperatorExpression(arguments[0], bop.Value, arguments[1]).WithAnnotation(methodRef)
							.WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder)
				);
				return;
			}
			UnaryOperatorType? uop = GetUnaryOperatorTypeFromMetadataName(methodRef.Name);
			if (uop != null && arguments.Length == 1) {
				if (uop == UnaryOperatorType.Increment || uop == UnaryOperatorType.Decrement) {
					// `op_Increment(a)` is not equivalent to `++a`,
					// because it doesn't assign the incremented value to a.
					if (CheckType(methodRef.DeclaringType, systemString, decimalString)) {
						// Legacy csc optimizes "d + 1m" to "op_Increment(d)",
						// so reverse that optimization here:
						invocationExpression.ReplaceWith(
							new BinaryOperatorExpression(
								arguments[0].Detach(),
								(uop == UnaryOperatorType.Increment ? BinaryOperatorType.Add : BinaryOperatorType.Subtract),
								new PrimitiveExpression(1m)
							).CopyAnnotationsFrom(invocationExpression)
						);
					}
					return;
				}
				arguments[0].Remove(); // detach argument
				invocationExpression.ReplaceWith(
					new UnaryOperatorExpression(uop.Value, arguments[0]).WithAnnotation(methodRef)
							.WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder)
				);
				return;
			}
			if (methodRef.Name == "op_Explicit" && arguments.Length == 1) {
				arguments[0].Remove(); // detach argument
				invocationExpression.ReplaceWith(
					arguments[0].CastTo(AstBuilder.ConvertType(methodRef.MethodSig.GetRetType(), sb))
					.WithAnnotation(methodRef)
					.WithAnnotation(invocationExpression.GetAllRecursiveILSpans())
					.WithAnnotation(builder)
				);
				return;
			}
			if (methodRef.Name == "op_Implicit" && arguments.Length == 1) {
				invocationExpression.ReplaceWith(arguments[0].WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder));
				return;
			}
			if (methodRef.Name == "op_True" && arguments.Length == 1 && invocationExpression.Role == Roles.Condition) {
				invocationExpression.ReplaceWith(arguments[0].WithAnnotation(invocationExpression.GetAllRecursiveILSpans()).WithAnnotation(builder));
				return;
			}

			return;
		}

		static BinaryOperatorType? GetBinaryOperatorTypeFromMetadataName(string name)
		{
			switch (name) {
				case "op_Addition":
					return BinaryOperatorType.Add;
				case "op_Subtraction":
					return BinaryOperatorType.Subtract;
				case "op_Multiply":
					return BinaryOperatorType.Multiply;
				case "op_Division":
					return BinaryOperatorType.Divide;
				case "op_Modulus":
					return BinaryOperatorType.Modulus;
				case "op_BitwiseAnd":
					return BinaryOperatorType.BitwiseAnd;
				case "op_BitwiseOr":
					return BinaryOperatorType.BitwiseOr;
				case "op_ExclusiveOr":
					return BinaryOperatorType.ExclusiveOr;
				case "op_LeftShift":
					return BinaryOperatorType.ShiftLeft;
				case "op_RightShift":
					return BinaryOperatorType.ShiftRight;
				case "op_Equality":
					return BinaryOperatorType.Equality;
				case "op_Inequality":
					return BinaryOperatorType.InEquality;
				case "op_LessThan":
					return BinaryOperatorType.LessThan;
				case "op_LessThanOrEqual":
					return BinaryOperatorType.LessThanOrEqual;
				case "op_GreaterThan":
					return BinaryOperatorType.GreaterThan;
				case "op_GreaterThanOrEqual":
					return BinaryOperatorType.GreaterThanOrEqual;
				default:
					return null;
			}
		}

		static UnaryOperatorType? GetUnaryOperatorTypeFromMetadataName(string name)
		{
			switch (name) {
				case "op_LogicalNot":
					return UnaryOperatorType.Not;
				case  "op_OnesComplement":
					return UnaryOperatorType.BitNot;
				case "op_UnaryNegation":
					return UnaryOperatorType.Minus;
				case "op_UnaryPlus":
					return UnaryOperatorType.Plus;
				case "op_Increment":
					return UnaryOperatorType.Increment;
				case "op_Decrement":
					return UnaryOperatorType.Decrement;
				default:
					return null;
			}
		}

		/// <summary>
		/// This annotation is used to convert a compound assignment "a += 2;" or increment operator "a++;"
		/// back to the original "a = a + 2;". This is sometimes necessary when the checked/unchecked semantics
		/// cannot be guaranteed otherwise (see CheckedUnchecked.ForWithCheckedInitializerAndUncheckedIterator test)
		/// </summary>
		public class RestoreOriginalAssignOperatorAnnotation
		{
			readonly BinaryOperatorExpression binaryOperatorExpression;

			public RestoreOriginalAssignOperatorAnnotation(BinaryOperatorExpression binaryOperatorExpression)
			{
				this.binaryOperatorExpression = binaryOperatorExpression;
			}

			public AssignmentExpression Restore(Expression expression)
			{
				var ilSpans = expression.GetAllRecursiveILSpans();
				expression.RemoveAnnotations<RestoreOriginalAssignOperatorAnnotation>();
				AssignmentExpression assign = expression as AssignmentExpression;
				if (assign == null) {
					UnaryOperatorExpression uoe = (UnaryOperatorExpression)expression;
					assign = new AssignmentExpression(uoe.Expression.Detach(), new PrimitiveExpression(1));
				} else {
					assign.Operator = AssignmentOperatorType.Assign;
				}
				binaryOperatorExpression.Right = assign.Right.Detach();
				assign.Right = binaryOperatorExpression;
				assign.AddAnnotation(ilSpans);
				return assign;
			}
		}

		public override object VisitAssignmentExpression(AssignmentExpression assignment, object data)
		{
			base.VisitAssignmentExpression(assignment, data);
			// Combine "x = x op y" into "x op= y"
			BinaryOperatorExpression binary = assignment.Right as BinaryOperatorExpression;
			if (binary != null && assignment.Operator == AssignmentOperatorType.Assign) {
				if (CanConvertToCompoundAssignment(assignment.Left) && assignment.Left.IsMatch(binary.Left)) {
					assignment.Operator = GetAssignmentOperatorForBinaryOperator(binary.Operator);
					if (assignment.Operator != AssignmentOperatorType.Assign) {
						// If we found a shorter operator, get rid of the BinaryOperatorExpression:
						assignment.CopyAnnotationsFrom(binary);
						assignment.Right = binary.Right.WithAnnotation(assignment.Right.GetAllRecursiveILSpans());
						assignment.AddAnnotation(new RestoreOriginalAssignOperatorAnnotation(binary));
					}
				}
			}
			if (context.Settings.IntroduceIncrementAndDecrement && (assignment.Operator == AssignmentOperatorType.Add || assignment.Operator == AssignmentOperatorType.Subtract)) {
				// detect increment/decrement
				if (assignment.Right.IsMatch(new PrimitiveExpression(1))) {
					// only if it's not a custom operator
					if (assignment.Annotation<IMethod>() == null) {
						UnaryOperatorType type;
						// When the parent is an expression statement, pre- or post-increment doesn't matter;
						// so we can pick post-increment which is more commonly used (for (int i = 0; i < x; i++))
						if (assignment.Parent is ExpressionStatement)
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement;
						else
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.Increment : UnaryOperatorType.Decrement;
						assignment.ReplaceWith(new UnaryOperatorExpression(type, assignment.Left.Detach()).CopyAnnotationsFrom(assignment).WithAnnotation(assignment.GetAllRecursiveILSpans()));
					}
				}
			}
			return null;
		}

		public static AssignmentOperatorType GetAssignmentOperatorForBinaryOperator(BinaryOperatorType bop)
		{
			switch (bop) {
				case BinaryOperatorType.Add:
					return AssignmentOperatorType.Add;
				case BinaryOperatorType.Subtract:
					return AssignmentOperatorType.Subtract;
				case BinaryOperatorType.Multiply:
					return AssignmentOperatorType.Multiply;
				case BinaryOperatorType.Divide:
					return AssignmentOperatorType.Divide;
				case BinaryOperatorType.Modulus:
					return AssignmentOperatorType.Modulus;
				case BinaryOperatorType.ShiftLeft:
					return AssignmentOperatorType.ShiftLeft;
				case BinaryOperatorType.ShiftRight:
					return AssignmentOperatorType.ShiftRight;
				case BinaryOperatorType.BitwiseAnd:
					return AssignmentOperatorType.BitwiseAnd;
				case BinaryOperatorType.BitwiseOr:
					return AssignmentOperatorType.BitwiseOr;
				case BinaryOperatorType.ExclusiveOr:
					return AssignmentOperatorType.ExclusiveOr;
				default:
					return AssignmentOperatorType.Assign;
			}
		}

		static bool CanConvertToCompoundAssignment(Expression left)
		{
			MemberReferenceExpression mre = left as MemberReferenceExpression;
			if (mre != null)
				return IsWithoutSideEffects(mre.Target);
			IndexerExpression ie = left as IndexerExpression;
			if (ie != null)
				return IsWithoutSideEffects(ie.Target) && ie.Arguments.All(IsWithoutSideEffects);
			UnaryOperatorExpression uoe = left as UnaryOperatorExpression;
			if (uoe != null && uoe.Operator == UnaryOperatorType.Dereference)
				return IsWithoutSideEffects(uoe.Expression);
			return IsWithoutSideEffects(left);
		}

		static bool IsWithoutSideEffects(Expression left)
		{
			return left is ThisReferenceExpression || left is IdentifierExpression || left is TypeReferenceExpression || left is BaseReferenceExpression;
		}

		static readonly Expression getMethodOrConstructorFromHandlePattern =
			new TypePattern(typeof(MethodBase)).ToType().Invoke2(
				BoxedTextColor.StaticMethod,
				"GetMethodFromHandle",
				new NamedNode("ldtokenNode", new LdTokenPattern("method")).ToExpression().Member("MethodHandle", BoxedTextColor.InstanceProperty),
				new OptionalNode(new TypeOfExpression(new AnyNode("declaringType")).Member("TypeHandle", BoxedTextColor.InstanceProperty))
			).CastTo(new Choice {
		         	new TypePattern(typeof(MethodInfo)),
		         	new TypePattern(typeof(ConstructorInfo))
		         });

		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			base.VisitCastExpression(castExpression, data);
			// Handle methodof
			Match m = getMethodOrConstructorFromHandlePattern.Match(castExpression);
			if (m.Success) {
				var ilSpans = castExpression.GetAllRecursiveILSpans();
				IMethod method = m.Get<AstNode>("method").Single().Annotation<IMethod>();
				if (method != null && m.Has("declaringType")) {
					Expression newNode = m.Get<AstType>("declaringType").Single().Detach().Member(method.Name, method);
					newNode = newNode.Invoke(method.MethodSig.GetParameters().Select(p => new TypeReferenceExpression(AstBuilder.ConvertType(p, stringBuilder))));
					newNode.AddAnnotation(method);
					m.Get<AstNode>("method").Single().ReplaceWith(newNode);
				}
				castExpression.ReplaceWith(m.Get<AstNode>("ldtokenNode").Single().WithAnnotation(ilSpans));
			}
			return null;
		}

		void IAstTransform.Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}
