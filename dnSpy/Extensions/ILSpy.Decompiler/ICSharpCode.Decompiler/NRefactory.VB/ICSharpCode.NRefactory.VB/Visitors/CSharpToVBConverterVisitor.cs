// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Visitors {
	public interface IEnvironmentProvider
	{
		string RootNamespace { get; }
		string GetTypeNameForAttribute(CSharp.Attribute attribute);
		TypeKind GetTypeKindForAstType(CSharp.AstType type);
		TypeCode ResolveExpression(CSharp.Expression expression);
		bool? IsReferenceType(CSharp.Expression expression);
		//ITypeResolveContext ResolveContext { get; }
		TypeSystem.IType ResolveType(AstType type, TypeDeclaration entity = null);
		bool IsMethodGroup(CSharp.Expression expression);
		bool HasEvent(Expression expression);
		CSharp.ParameterDeclaration[] GetParametersForProperty(CSharp.PropertyDeclaration property);
	}
	
	/// <summary>
	/// Description of CSharpToVBConverterVisitor.
	/// </summary>
	public class CSharpToVBConverterVisitor : CSharp.IAstVisitor<object, VB.AstNode>
	{
		IEnvironmentProvider provider;
		Stack<BlockStatement> blocks;
		Stack<TypeDeclaration> types;
		Stack<MemberInfo> members;
		readonly ModuleDef module;

		enum ConvertedStatementKind {
			None,
			While,
			For,
		}

		class MemberInfo
		{
			public bool inIterator;
		}
		
		public CSharpToVBConverterVisitor(ModuleDef module, IEnvironmentProvider provider)
		{
			this.module = module;
			this.provider = provider;
			this.blocks = new Stack<BlockStatement>();
			this.types = new Stack<TypeDeclaration>();
			this.members = new Stack<MemberInfo>();
		}
		
		public AstNode VisitAnonymousMethodExpression(CSharp.AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			members.Push(new MemberInfo());
			
			var expr = new MultiLineLambdaExpression() {
				Body = (BlockStatement)anonymousMethodExpression.Body.AcceptVisitor(this, data)
			};
			if (anonymousMethodExpression.IsAsync)
				expr.Modifiers |= LambdaExpressionModifiers.Async;
			var retStmt = anonymousMethodExpression.Body.Descendants.OfType<CSharp.ReturnStatement>().FirstOrDefault();
			expr.IsSub = retStmt == null || retStmt.Expression.IsNull;

			ConvertNodes(anonymousMethodExpression.Parameters, expr.Parameters);
			
			if (members.Pop().inIterator) {
				expr.Modifiers |= LambdaExpressionModifiers.Iterator;
			}
			
			return EndNode(anonymousMethodExpression, expr);
		}
		
		public AstNode VisitUndocumentedExpression(CSharp.UndocumentedExpression undocumentedExpression, object data)
		{
			var invocation = new InvocationExpression();
			
			switch (undocumentedExpression.UndocumentedExpressionType) {
				case CSharp.UndocumentedExpressionType.ArgListAccess:
				case CSharp.UndocumentedExpressionType.ArgList:
					invocation.Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__ArgList") };
					break;
				case CSharp.UndocumentedExpressionType.RefValue:
					invocation.Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__RefValue") };
					break;
				case CSharp.UndocumentedExpressionType.RefType:
					invocation.Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__RefType") };
					break;
				case CSharp.UndocumentedExpressionType.MakeRef:
					invocation.Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__MakeRef") };
					break;
				default:
					throw new Exception("Invalid value for UndocumentedExpressionType");
			}
			
			ConvertNodes(undocumentedExpression.Arguments, invocation.Arguments);
			
			return EndNode(undocumentedExpression, invocation);
		}
		
		public AstNode VisitArrayCreateExpression(CSharp.ArrayCreateExpression arrayCreateExpression, object data)
		{
			var expr = new ArrayCreateExpression() {
				Type = (AstType)arrayCreateExpression.Type.AcceptVisitor(this, data),
				Initializer = (ArrayInitializerExpression)arrayCreateExpression.Initializer.AcceptVisitor(this, data)
			};
			ConvertNodes(arrayCreateExpression.Arguments, expr.Arguments, ReduceArrayUpperBoundExpression);
			ConvertNodes(arrayCreateExpression.AdditionalArraySpecifiers, expr.AdditionalArraySpecifiers);
			
			return EndNode(arrayCreateExpression, expr);
		}

		Expression ReduceArrayUpperBoundExpression(Expression expression)
		{
			if (expression is PrimitiveExpression)
			{
				var numericLiteral = expression as PrimitiveExpression;
				int? upperBound = numericLiteral.Value as int?;
				if (upperBound.HasValue)
					return new PrimitiveExpression(upperBound.Value - 1);
			}
			return new BinaryOperatorExpression(expression, BinaryOperatorType.Subtract, new PrimitiveExpression(1));
		}
		
		public AstNode VisitArrayInitializerExpression(CSharp.ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			var expr = new ArrayInitializerExpression();
			ConvertNodes(arrayInitializerExpression.Elements, expr.Elements);
			
			return EndNode(arrayInitializerExpression, expr);
		}
		
		public AstNode VisitAsExpression(CSharp.AsExpression asExpression, object data)
		{
			return EndNode(asExpression, new CastExpression(CastType.TryCast, (AstType)asExpression.Type.AcceptVisitor(this, data), (Expression)asExpression.Expression.AcceptVisitor(this, data)));
		}
		
		public AstNode VisitAssignmentExpression(CSharp.AssignmentExpression assignmentExpression, object data)
		{
			var left = (Expression)assignmentExpression.Left.AcceptVisitor(this, data);
			var op = AssignmentOperatorType.None;
			var right = (Expression)assignmentExpression.Right.AcceptVisitor(this, data);
			
			switch (assignmentExpression.Operator) {
				case CSharp.AssignmentOperatorType.Assign:
					op = AssignmentOperatorType.Assign;
					break;
				case CSharp.AssignmentOperatorType.Add:
					if (provider.HasEvent(left)) {
						var addHandler = new AddRemoveHandlerStatement { IsAddHandler = true };
						addHandler.EventExpression = left;
						addHandler.DelegateExpression = right;
						return EndNode(assignmentExpression, addHandler);
					}
					op = AssignmentOperatorType.Add;
					break;
				case CSharp.AssignmentOperatorType.Subtract:
					if (provider.HasEvent(left)) {
						var addHandler = new AddRemoveHandlerStatement { IsAddHandler = false };
						addHandler.EventExpression = left;
						addHandler.DelegateExpression = right;
						return EndNode(assignmentExpression, addHandler);
					}
					op = AssignmentOperatorType.Subtract;
					break;
				case CSharp.AssignmentOperatorType.Multiply:
					op = AssignmentOperatorType.Multiply;
					break;
				case CSharp.AssignmentOperatorType.Divide:
					op = AssignmentOperatorType.Divide;
					break;
				case CSharp.AssignmentOperatorType.Modulus:
					op = AssignmentOperatorType.Assign;
					right = new BinaryOperatorExpression((Expression)left.Clone(), BinaryOperatorType.Modulus, right);
					break;
				case CSharp.AssignmentOperatorType.ShiftLeft:
					op = AssignmentOperatorType.ShiftLeft;
					break;
				case CSharp.AssignmentOperatorType.ShiftRight:
					op = AssignmentOperatorType.ShiftRight;
					break;
				case CSharp.AssignmentOperatorType.BitwiseAnd:
					op = AssignmentOperatorType.Assign;
					right = new BinaryOperatorExpression((Expression)left.Clone(), BinaryOperatorType.BitwiseAnd, right);
					break;
				case CSharp.AssignmentOperatorType.BitwiseOr:
					op = AssignmentOperatorType.Assign;
					right = new BinaryOperatorExpression((Expression)left.Clone(), BinaryOperatorType.BitwiseOr, right);
					break;
				case CSharp.AssignmentOperatorType.ExclusiveOr:
					op = AssignmentOperatorType.Assign;
					right = new BinaryOperatorExpression((Expression)left.Clone(), BinaryOperatorType.ExclusiveOr, right);
					break;
				default:
					throw new Exception("Invalid value for AssignmentOperatorType: " + assignmentExpression.Operator);
			}
			
			var expr = new AssignmentExpression(left, op, right);
			return EndNode(assignmentExpression, expr);
		}
		
		public AstNode VisitBaseReferenceExpression(CSharp.BaseReferenceExpression baseReferenceExpression, object data)
		{
			InstanceExpression result = new InstanceExpression(InstanceExpressionType.MyBase, baseReferenceExpression.StartLocation);
			
			return EndNode(baseReferenceExpression, result);
		}
		
		public AstNode VisitBinaryOperatorExpression(CSharp.BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			var left = (Expression)binaryOperatorExpression.Left.AcceptVisitor(this, data);
			var op = BinaryOperatorType.None;
			var right = (Expression)binaryOperatorExpression.Right.AcceptVisitor(this, data);
			
			switch (binaryOperatorExpression.Operator) {
				case CSharp.BinaryOperatorType.BitwiseAnd:
					op = BinaryOperatorType.BitwiseAnd;
					break;
				case CSharp.BinaryOperatorType.BitwiseOr:
					op = BinaryOperatorType.BitwiseOr;
					break;
				case CSharp.BinaryOperatorType.ConditionalAnd:
					op = BinaryOperatorType.LogicalAnd;
					break;
				case CSharp.BinaryOperatorType.ConditionalOr:
					op = BinaryOperatorType.LogicalOr;
					break;
				case CSharp.BinaryOperatorType.ExclusiveOr:
					op = BinaryOperatorType.ExclusiveOr;
					break;
				case CSharp.BinaryOperatorType.GreaterThan:
					op = BinaryOperatorType.GreaterThan;
					break;
				case CSharp.BinaryOperatorType.GreaterThanOrEqual:
					op = BinaryOperatorType.GreaterThanOrEqual;
					break;
				case CSharp.BinaryOperatorType.Equality:
					if (IsReferentialEquality(binaryOperatorExpression))
						op = BinaryOperatorType.ReferenceEquality;
					else
						op = BinaryOperatorType.Equality;
					break;
				case CSharp.BinaryOperatorType.InEquality:
					if (IsReferentialEquality(binaryOperatorExpression))
						op = BinaryOperatorType.ReferenceInequality;
					else
						op = BinaryOperatorType.InEquality;
					break;
				case CSharp.BinaryOperatorType.LessThan:
					op = BinaryOperatorType.LessThan;
					break;
				case CSharp.BinaryOperatorType.LessThanOrEqual:
					op = BinaryOperatorType.LessThanOrEqual;
					break;
				case CSharp.BinaryOperatorType.Add:
					// TODO might be string concatenation
					op = BinaryOperatorType.Add;
					break;
				case CSharp.BinaryOperatorType.Subtract:
					op = BinaryOperatorType.Subtract;
					break;
				case CSharp.BinaryOperatorType.Multiply:
					op = BinaryOperatorType.Multiply;
					break;
				case CSharp.BinaryOperatorType.Divide:
					op = BinaryOperatorType.Divide;
					break;
				case CSharp.BinaryOperatorType.Modulus:
					op = BinaryOperatorType.Modulus;
					break;
				case CSharp.BinaryOperatorType.ShiftLeft:
					op = BinaryOperatorType.ShiftLeft;
					break;
				case CSharp.BinaryOperatorType.ShiftRight:
					op = BinaryOperatorType.ShiftRight;
					break;
				case CSharp.BinaryOperatorType.NullCoalescing:
					var nullCoalescing = new ConditionalExpression {
						ConditionExpression = left,
						FalseExpression = right
					};
					return EndNode(binaryOperatorExpression, nullCoalescing);
				default:
					throw new Exception("Invalid value for BinaryOperatorType: " + binaryOperatorExpression.Operator);
			}
			
			return EndNode(binaryOperatorExpression, new BinaryOperatorExpression(left, op, right));
		}
		
		bool IsReferentialEquality(CSharp.BinaryOperatorExpression binaryOperatorExpression)
		{
			var left = provider.IsReferenceType(binaryOperatorExpression.Left);
			var right = provider.IsReferenceType(binaryOperatorExpression.Right);
			
			var leftCode = provider.ResolveExpression(binaryOperatorExpression.Left);
			var rightCode = provider.ResolveExpression(binaryOperatorExpression.Right);
			
			return (left == true || right == true) && (leftCode != TypeCode.String && rightCode != TypeCode.String);
		}
		
		public AstNode VisitCastExpression(CSharp.CastExpression castExpression, object data)
		{
			var expr = new CastExpression();
			
			expr.Type = (AstType)castExpression.Type.AcceptVisitor(this, data);
			// TODO read additional type information from annotation
			// (int)x is equivalent to CInt(Math.Truncate(x))
			expr.CastType = GetCastType(expr.Type, null);
			expr.Expression = (Expression)castExpression.Expression.AcceptVisitor(this, data);
			
			if (expr.CastType != CastType.CType)
				expr.Type = null;
			
			return EndNode(castExpression, expr);
		}
		
		CastType GetCastType(AstType type, object typeInformation)
		{
			var primType = type as PrimitiveType;
			if (primType == null)
				return CastType.CType;
			
			switch (primType.Keyword) {
				case "Boolean":
					return CastType.CBool;
				case "Byte":
					return CastType.CByte;
				case "Char":
					return CastType.CChar;
				case "Date":
					return CastType.CDate;
				case "Double":
					return CastType.CDbl;
				case "Decimal":
					return CastType.CDec;
				case "Integer":
					return CastType.CInt;
				case "Long":
					return CastType.CLng;
				case "Object":
					return CastType.CObj;
				case "SByte":
					return CastType.CSByte;
				case "Short":
					return CastType.CShort;
				case "Single":
					return CastType.CSng;
				case "String":
					return CastType.CStr;
				case "UInteger":
					return CastType.CUInt;
				case "ULong":
					return CastType.CULng;
				case "UShort":
					return CastType.CUShort;
			}
			
			return CastType.CType;
		}
		
		public AstNode VisitCheckedExpression(CSharp.CheckedExpression checkedExpression, object data)
		{
			if (blocks.Count > 0)
				blocks.Peek().AddChild(new Comment(" The following expression was wrapped in a checked-expression", false), AstNode.Roles.Comment);
			return EndNode(checkedExpression, checkedExpression.Expression.AcceptVisitor(this, data));
		}
		
		public AstNode VisitConditionalExpression(CSharp.ConditionalExpression conditionalExpression, object data)
		{
			var cond = new ConditionalExpression() {
				ConditionExpression = (Expression)conditionalExpression.Condition.AcceptVisitor(this, data),
				TrueExpression = (Expression)conditionalExpression.TrueExpression.AcceptVisitor(this, data),
				FalseExpression = (Expression)conditionalExpression.FalseExpression.AcceptVisitor(this, data)
			};
			
			return EndNode(conditionalExpression, cond);
		}
		
		public AstNode VisitDefaultValueExpression(CSharp.DefaultValueExpression defaultValueExpression, object data)
		{
			// Nothing is equivalent to default(T) for reference and value types.
			return EndNode(defaultValueExpression, new PrimitiveExpression(null));
		}
		
		public AstNode VisitDirectionExpression(CSharp.DirectionExpression directionExpression, object data)
		{
			return EndNode(directionExpression, (Expression)directionExpression.Expression.AcceptVisitor(this, data));
		}
		
		public AstNode VisitIdentifierExpression(CSharp.IdentifierExpression identifierExpression, object data)
		{
			var expr = new IdentifierExpression();
			expr.Identifier = Identifier.Create(identifierExpression.IdentifierToken.Annotations, identifierExpression.Identifier);
			ConvertNodes(identifierExpression.TypeArguments, expr.TypeArguments);
			if (provider.IsMethodGroup(identifierExpression)) {
				return EndNode(identifierExpression, new UnaryOperatorExpression(UnaryOperatorType.AddressOf, expr));
			}
			
			return EndNode(identifierExpression, expr);
		}
		
		public AstNode VisitIndexerExpression(CSharp.IndexerExpression indexerExpression, object data)
		{
			var expr = new InvocationExpression((Expression)indexerExpression.Target.AcceptVisitor(this, data));
			ConvertNodes(indexerExpression.Arguments, expr.Arguments);
			return EndNode(indexerExpression, expr);
		}
		
		public AstNode VisitInvocationExpression(CSharp.InvocationExpression invocationExpression, object data)
		{
			var expr = new InvocationExpression((Expression)invocationExpression.Target.AcceptVisitor(this, data));
			ConvertNodes(invocationExpression.Arguments, expr.Arguments);
			return EndNode(invocationExpression, expr);
		}
		
		public AstNode VisitIsExpression(CSharp.IsExpression isExpression, object data)
		{
			var expr = new TypeOfIsExpression() {
				Type = (AstType)isExpression.Type.AcceptVisitor(this, data),
				TypeOfExpression = (Expression)isExpression.Expression.AcceptVisitor(this, data)
			};
			
			return EndNode(isExpression, expr);
		}
		
		public AstNode VisitLambdaExpression(CSharp.LambdaExpression lambdaExpression, object data)
		{
			LambdaExpression expr = null;
			
			if (lambdaExpression.Body is CSharp.Expression) {
				var singleLine = new SingleLineFunctionLambdaExpression() {
					EmbeddedExpression = (Expression)lambdaExpression.Body.AcceptVisitor(this, data)
				};
				ConvertNodes(lambdaExpression.Parameters, singleLine.Parameters);
				expr = singleLine;
			} else
				throw new NotImplementedException();
			
			return EndNode(lambdaExpression, expr);
		}
		
		public AstNode VisitMemberReferenceExpression(CSharp.MemberReferenceExpression memberReferenceExpression, object data)
		{
			var memberAccessExpression = new MemberAccessExpression();
			
			memberAccessExpression.Target = (Expression)memberReferenceExpression.Target.AcceptVisitor(this, data);
			memberAccessExpression.MemberName = Identifier.Create(memberReferenceExpression.MemberNameToken.Annotations, memberReferenceExpression.MemberName);
			memberAccessExpression.MemberName.AddAnnotation(memberReferenceExpression.Annotation<IMemberRef>());
			bool isExtensionMethod = false;
			if (memberReferenceExpression.Parent is CSharp.InvocationExpression invocExpr) {
				var method = invocExpr.Annotation<dnlib.DotNet.IMethod>().ResolveMethodDef();
				isExtensionMethod = method?.CustomAttributes.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute") == true;
			}
			if (!isExtensionMethod)
				ConvertNodes(memberReferenceExpression.TypeArguments, memberAccessExpression.TypeArguments);
			if (provider.IsMethodGroup(memberReferenceExpression)) {
				return EndNode(memberReferenceExpression, new UnaryOperatorExpression(UnaryOperatorType.AddressOf, memberAccessExpression));
			}
			
			return EndNode(memberReferenceExpression, memberAccessExpression);
		}
		
		public AstNode VisitNamedArgumentExpression(CSharp.NamedArgumentExpression namedArgumentExpression, object data)
		{
			Expression expr = new NamedArgumentExpression {
				Identifier = Identifier.Create(namedArgumentExpression.NameToken.Annotations, namedArgumentExpression.Name),
				Expression = (Expression)namedArgumentExpression.Expression.AcceptVisitor(this, data)
			};
			
			return EndNode(namedArgumentExpression, expr);
		}
		
		public AstNode VisitNamedExpression(CSharp.NamedExpression namedExpression, object data)
		{
			Expression expr = new FieldInitializerExpression {
				IsKey = true,
				Identifier = Identifier.Create(namedExpression.NameToken.Annotations, namedExpression.Name),
				Expression = (Expression)namedExpression.Expression.AcceptVisitor(this, data)
			};
			return EndNode(namedExpression, expr);
		}
		
		public AstNode VisitNullReferenceExpression(CSharp.NullReferenceExpression nullReferenceExpression, object data)
		{
			return EndNode(nullReferenceExpression, new PrimitiveExpression(null));
		}
		
		public AstNode VisitObjectCreateExpression(CSharp.ObjectCreateExpression objectCreateExpression, object data)
		{
			var expr = new ObjectCreationExpression((AstType)objectCreateExpression.Type.AcceptVisitor(this, data));
			ConvertNodes(objectCreateExpression.Arguments, expr.Arguments);
			var arg1 = expr.Arguments.FirstOrDefault() as UnaryOperatorExpression;
			if (arg1 != null && arg1.Operator == UnaryOperatorType.AddressOf) {
				arg1.Remove();
				return EndNode(objectCreateExpression, arg1);
			}
			if (!objectCreateExpression.Initializer.IsNull)
				expr.Initializer = (ArrayInitializerExpression)objectCreateExpression.Initializer.AcceptVisitor(this, data);
			
			return EndNode(objectCreateExpression, expr);
		}
		
		public AstNode VisitAnonymousTypeCreateExpression(CSharp.AnonymousTypeCreateExpression anonymousTypeCreateExpression, object data)
		{
			var expr = new AnonymousObjectCreationExpression();
			
			ConvertNodes(anonymousTypeCreateExpression.Initializers, expr.Initializer);
			
			return EndNode(anonymousTypeCreateExpression, expr);
		}
		
		public AstNode VisitParenthesizedExpression(CSharp.ParenthesizedExpression parenthesizedExpression, object data)
		{
			var expr = parenthesizedExpression.Expression;
			if (expr is CSharp.CastExpression || expr is CSharp.AsExpression || expr is CSharp.TypeOfExpression || expr is CSharp.ConditionalExpression || expr is CSharp.ParenthesizedExpression)
				return expr.AcceptVisitor(this, data);

			var result = new ParenthesizedExpression();
			
			result.Expression = (Expression)expr.AcceptVisitor(this, data);
			
			return EndNode(parenthesizedExpression, result);
		}
		
		public AstNode VisitPointerReferenceExpression(CSharp.PointerReferenceExpression pointerReferenceExpression, object data)
		{
			return EndNode(pointerReferenceExpression, ((Expression)pointerReferenceExpression.Target.AcceptVisitor(this, data)).Invoke2(BoxedTextColor.InstanceMethod, "Dereference").Member(pointerReferenceExpression.MemberNameToken.Annotation<object>(), pointerReferenceExpression.MemberName));
		}
		
		public AstNode VisitPrimitiveExpression(CSharp.PrimitiveExpression primitiveExpression, object data)
		{
			Expression expr;
			
			if (!string.IsNullOrEmpty(primitiveExpression.Value as string))
				expr = ConvertToConcat(primitiveExpression.Value.ToString());
			else if (primitiveExpression.Value is char)
				expr = ConvertToSpecialChar((char)primitiveExpression.Value);
			else
				expr = new PrimitiveExpression(primitiveExpression.Value);
			
			return EndNode(primitiveExpression, expr);
		}
		
		Expression ConvertToConcat(string literal)
		{
			Stack<Expression> parts = new Stack<Expression>();
			int start = 0;
			
			for (int i = 0; i < literal.Length; i++) {
				string part;
				switch (literal[i]) {
					case '\0':
					case '\b':
					case '\f':
					case '\n':
					case '\t':
					case '\v':
					case '\u0085':
					case '\u2028':
					case '\u2029':
						part = literal.Substring(start, i - start);
						if (!string.IsNullOrEmpty(part))
							parts.Push(new PrimitiveExpression(part));
						parts.Push(ConvertToSpecialChar(literal[i]));
						start = i + 1;
						break;
					case '\r':
						part = literal.Substring(start, i - start);
						if (!string.IsNullOrEmpty(part))
							parts.Push(new PrimitiveExpression(part));
						if (i + 1 < literal.Length && literal[i + 1] == '\n') {
							i++;
							parts.Push(new IdentifierExpression(Identifier.CreateLiteralField("vbCrLf")));
						} else
							parts.Push(new IdentifierExpression(Identifier.CreateLiteralField("vbCr")));
						start = i + 1;
						break;
					default:
						if (char.IsControl(literal[i])) {
							part = literal.Substring(start, i - start);
							if (!string.IsNullOrEmpty(part))
								parts.Push(new PrimitiveExpression(part));
							parts.Push(new InvocationExpression(new IdentifierExpression(Identifier.Create(BoxedTextColor.StaticMethod, "ChrW")), new PrimitiveExpression((int)literal[i])));
						} else
							continue;
						start = i + 1;
						break;
				}
				
			}
			
			if (start < literal.Length) {
				string part = literal.Substring(start);
				parts.Push(new PrimitiveExpression(part));
			}
			
			Expression current = parts.Pop();
			
			while (parts.Any())
				current = new BinaryOperatorExpression(parts.Pop(), BinaryOperatorType.Concat, current);
			
			return current;
		}

		Expression ConvertToSpecialChar(char ch)
		{
			switch (ch) {
				case '\0':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbNullChar"));
				case '\b':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbBack"));
				case '\f':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbFormFeed"));
				case '\r':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbCr"));
				case '\n':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbLf"));
				case '\t':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbTab"));
				case '\v':
					return new IdentifierExpression(Identifier.CreateLiteralField("vbVerticalTab"));
				default:
					if (char.IsControl(ch))
						return new InvocationExpression(new IdentifierExpression(Identifier.Create(BoxedTextColor.StaticMethod, "ChrW")), new PrimitiveExpression((int)ch));
					return new PrimitiveExpression(ch);
			}
		}
		
		public AstNode VisitSizeOfExpression(CSharp.SizeOfExpression sizeOfExpression, object data)
		{
			return EndNode(
				sizeOfExpression,
				new InvocationExpression(
					new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__SizeOf") },
					new TypeReferenceExpression((AstType)sizeOfExpression.Type.AcceptVisitor(this, data))
				)
			);
		}
		
		public AstNode VisitStackAllocExpression(CSharp.StackAllocExpression stackAllocExpression, object data)
		{
			return EndNode(
				stackAllocExpression,
				new InvocationExpression(
					new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__StackAlloc") },
					new TypeReferenceExpression((AstType)stackAllocExpression.Type.AcceptVisitor(this, data)),
					(Expression)stackAllocExpression.CountExpression.AcceptVisitor(this, data)
				)
			);
		}
		
		public AstNode VisitThisReferenceExpression(CSharp.ThisReferenceExpression thisReferenceExpression, object data)
		{
			InstanceExpression result = new InstanceExpression(InstanceExpressionType.Me, thisReferenceExpression.StartLocation);
			return EndNode(thisReferenceExpression, result);
		}
		
		public AstNode VisitTypeOfExpression(CSharp.TypeOfExpression typeOfExpression, object data)
		{
			var expr = new GetTypeExpression();
			expr.Type = (AstType)typeOfExpression.Type.AcceptVisitor(this, data);
			return EndNode(typeOfExpression, expr);
		}
		
		public AstNode VisitTypeReferenceExpression(CSharp.TypeReferenceExpression typeReferenceExpression, object data)
		{
			var expr = new TypeReferenceExpression((AstType)typeReferenceExpression.Type.AcceptVisitor(this, data));
			return EndNode(typeReferenceExpression, expr);
		}
		
		public AstNode VisitUnaryOperatorExpression(CSharp.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			Expression expr;

			switch (unaryOperatorExpression.Operator) {
				case CSharp.UnaryOperatorType.Not:
				case CSharp.UnaryOperatorType.BitNot:
					expr = new UnaryOperatorExpression() {
						Expression = (Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data),
						Operator = UnaryOperatorType.Not
					};
					break;
				case CSharp.UnaryOperatorType.Minus:
					expr = new UnaryOperatorExpression() {
						Expression = (Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data),
						Operator = UnaryOperatorType.Minus
					};
					break;
				case CSharp.UnaryOperatorType.Plus:
					expr = new UnaryOperatorExpression() {
						Expression = (Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data),
						Operator = UnaryOperatorType.Plus
					};
					break;
				case CSharp.UnaryOperatorType.Increment:
					expr = new InvocationExpression();
					((InvocationExpression)expr).Target = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__Increment") };
					((InvocationExpression)expr).Arguments.Add((Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
					break;
				case CSharp.UnaryOperatorType.PostIncrement:
					expr = new InvocationExpression();
					((InvocationExpression)expr).Target = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__PostIncrement") };
					((InvocationExpression)expr).Arguments.Add((Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
					break;
				case CSharp.UnaryOperatorType.Decrement:
					expr = new InvocationExpression();
					((InvocationExpression)expr).Target = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__Decrement") };
					((InvocationExpression)expr).Arguments.Add((Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
					break;
				case CSharp.UnaryOperatorType.PostDecrement:
					expr = new InvocationExpression();
					((InvocationExpression)expr).Target = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__PostDecrement") };
					((InvocationExpression)expr).Arguments.Add((Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
					break;
				case CSharp.UnaryOperatorType.AddressOf:
					expr = new UnaryOperatorExpression() {
						Expression = (Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data),
						Operator = UnaryOperatorType.AddressOf
					};
					break;
				case CSharp.UnaryOperatorType.Dereference:
					expr = new InvocationExpression();
					((InvocationExpression)expr).Target = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Keyword, "__Dereference") };
					((InvocationExpression)expr).Arguments.Add((Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
					break;
				case CSharp.UnaryOperatorType.Await:
					expr = new UnaryOperatorExpression() {
						Expression = (Expression)unaryOperatorExpression.Expression.AcceptVisitor(this, data),
						Operator = UnaryOperatorType.Await
					};
					break;
				default:
					throw new Exception("Invalid value for UnaryOperatorType");
			}
			
			return EndNode(unaryOperatorExpression, expr);
		}
		
		public AstNode VisitUncheckedExpression(CSharp.UncheckedExpression uncheckedExpression, object data)
		{
			blocks.Peek().AddChild(new Comment(" The following expression was wrapped in a unchecked-expression", false), AstNode.Roles.Comment);
			return EndNode(uncheckedExpression, uncheckedExpression.Expression.AcceptVisitor(this, data));
		}
		
		public AstNode VisitQueryExpression(CSharp.QueryExpression queryExpression, object data)
		{
			var expr = new QueryExpression();
			ConvertNodes(queryExpression.Clauses, expr.QueryOperators);
			return EndNode(queryExpression, expr);
		}
		
		public AstNode VisitQueryContinuationClause(CSharp.QueryContinuationClause queryContinuationClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitQueryFromClause(CSharp.QueryFromClause queryFromClause, object data)
		{
			var op = new FromQueryOperator();
			op.Variables.Add(
				new CollectionRangeVariableDeclaration {
					Identifier = new VariableIdentifier { Name = Identifier.Create(queryFromClause.IdentifierToken.Annotations, queryFromClause.Identifier) },
					Type = (AstType)queryFromClause.Type.AcceptVisitor(this, data),
					Expression = (Expression)queryFromClause.Expression.AcceptVisitor(this, data)
				}
			);
			
			return EndNode(queryFromClause, op);
		}
		
		public AstNode VisitQueryLetClause(CSharp.QueryLetClause queryLetClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitQueryWhereClause(CSharp.QueryWhereClause queryWhereClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitQueryJoinClause(CSharp.QueryJoinClause queryJoinClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitQueryOrderClause(CSharp.QueryOrderClause queryOrderClause, object data)
		{
			var op = new OrderByQueryOperator();
			
			ConvertNodes(queryOrderClause.Orderings, op.Expressions);
			
			return EndNode(queryOrderClause, op);
		}
		
		public AstNode VisitQueryOrdering(CSharp.QueryOrdering queryOrdering, object data)
		{
			var expr = new OrderExpression();
			
			expr.Direction = (QueryOrderingDirection)queryOrdering.Direction;
			expr.Expression = (Expression)queryOrdering.Expression.AcceptVisitor(this, data);
			
			return EndNode(queryOrdering, expr);
		}
		
		int selectVarCount = 0;
		
		public AstNode VisitQuerySelectClause(CSharp.QuerySelectClause querySelectClause, object data)
		{
			var op = new SelectQueryOperator();
			
			op.Variables.Add(
				new VariableInitializer {
					Identifier = new VariableIdentifier { Name = Identifier.Create(BoxedTextColor.Local, "SelectVar" + selectVarCount) },
					Expression = (Expression)querySelectClause.Expression.AcceptVisitor(this, data)
				});
			
			return EndNode(querySelectClause, op);
		}
		
		public AstNode VisitQueryGroupClause(CSharp.QueryGroupClause queryGroupClause, object data)
		{
			var op = new GroupByQueryOperator();
			
			throw new NotImplementedException();
			
			//return EndNode(queryGroupClause, op);
		}
		
		public AstNode VisitAttribute(CSharp.Attribute attribute, object data)
		{
			var attr = new VB.Ast.Attribute();
			AttributeTarget target;
			Enum.TryParse(((CSharp.AttributeSection)attribute.Parent).AttributeTarget, true, out target);
			attr.Target = target;
			attr.Type = (AstType)attribute.Type.AcceptVisitor(this, data);
			ConvertNodes(attribute.Arguments, attr.Arguments);
			
			return EndNode(attribute, attr);
		}
		
		public AstNode VisitAttributeSection(CSharp.AttributeSection attributeSection, object data)
		{
			AttributeBlock block = new AttributeBlock();
			ConvertNodes(attributeSection.Attributes, block.Attributes);
			return EndNode(attributeSection, block);
		}
		
		public AstNode VisitDelegateDeclaration(CSharp.DelegateDeclaration delegateDeclaration, object data)
		{
			var result = new DelegateDeclaration();
			
			ConvertNodes(delegateDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), result.Attributes);
			ConvertNodes(delegateDeclaration.ModifierTokens, result.ModifierTokens);
			result.Name = Identifier.Create(delegateDeclaration.NameToken.Annotations, delegateDeclaration.Name);
			result.IsSub = IsSub(delegateDeclaration.ReturnType);
			ConvertNodes(delegateDeclaration.Parameters, result.Parameters);
			ConvertNodes(delegateDeclaration.TypeParameters, result.TypeParameters);
			ConvertNodes(delegateDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), result.ReturnTypeAttributes);
			if (!result.IsSub)
				result.ReturnType = (AstType)delegateDeclaration.ReturnType.AcceptVisitor(this, data);
			return EndNode(delegateDeclaration, result);
		}
		
		public AstNode VisitNamespaceDeclaration(CSharp.NamespaceDeclaration namespaceDeclaration, object data)
		{
			var newNamespace = new NamespaceDeclaration();

			foreach (var id in namespaceDeclaration.IdentifierTypes) {
				var newId = new Identifier(BoxedTextColor.Namespace, id.Name, TextLocation.Empty);
				CopyAnnotations(id, newId);
				newNamespace.Identifiers.Add(newId);
			}
			ConvertMembers(namespaceDeclaration, newNamespace, CSharp.NamespaceDeclaration.MemberRole, NamespaceDeclaration.MemberRole);
			
			return EndNode(namespaceDeclaration, newNamespace);
		}
		
		public AstNode VisitTypeDeclaration(CSharp.TypeDeclaration typeDeclaration, object data)
		{
			// TODO add missing features!
			
			if (typeDeclaration.ClassType == CSharp.ClassType.Enum) {
				var type = new EnumDeclaration();
				CopyAnnotations(typeDeclaration, type);
				
				ConvertNodes(typeDeclaration.Attributes, type.Attributes);
				ConvertNodes(typeDeclaration.ModifierTokens, type.ModifierTokens);
				
				if (typeDeclaration.BaseTypes.Any()) {
					var first = typeDeclaration.BaseTypes.First();
					
					type.UnderlyingType = (AstType)first.AcceptVisitor(this, data);
				}
				
				type.Name = Identifier.Create(typeDeclaration.NameToken.Annotations, typeDeclaration.Name);
				
				ConvertMembers(typeDeclaration, type, CSharp.Roles.TypeMemberRole, EnumDeclaration.MemberRole);
				
				return EndNode(typeDeclaration, type);
			} else {
				var type = new TypeDeclaration();
				CopyAnnotations(typeDeclaration, type);
				
				CSharp.Attribute stdModAttr;
				
				if (typeDeclaration.ClassType == CSharp.ClassType.Class && HasAttribute(typeDeclaration.Attributes, "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute", out stdModAttr)) {
					type.ClassType = ClassType.Module;
					// remove AttributeSection if only one attribute is present
					var attrSec = (CSharp.AttributeSection)stdModAttr.Parent;
					if (attrSec.Attributes.Count == 1)
						attrSec.Remove();
					else
						stdModAttr.Remove();
				} else {
					switch (typeDeclaration.ClassType) {
						case CSharp.ClassType.Class:
							type.ClassType = ClassType.Class;
							break;
						case CSharp.ClassType.Struct:
							type.ClassType = ClassType.Struct;
							break;
						case CSharp.ClassType.Interface:
							type.ClassType = ClassType.Interface;
							break;
						default:
							throw new InvalidOperationException("Invalid value for ClassType");
					}
				}
				
				if ((typeDeclaration.Modifiers & CSharp.Modifiers.Static) == CSharp.Modifiers.Static) {
					type.ClassType = ClassType.Module;
					typeDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
				}
				
				ConvertNodes(typeDeclaration.Attributes, type.Attributes);
				ConvertNodes(typeDeclaration.ModifierTokens, type.ModifierTokens);
				ConvertNodes(typeDeclaration.TypeParameters, type.TypeParameters);

				if (typeDeclaration.Parent is CSharp.TypeDeclaration && type.ClassType == ClassType.Module) {
					type.ClassType = ClassType.Class;
					type.Modifiers &= ~Modifiers.Static;
					type.Modifiers |= Modifiers.NotInheritable;
				}
				
				if (typeDeclaration.BaseTypes.Any()) {
					var first = typeDeclaration.BaseTypes.First();
					
					if (provider.GetTypeKindForAstType(first) != TypeKind.Interface) {
						ConvertNodes(typeDeclaration.BaseTypes.Skip(1), type.ImplementsTypes);
						type.InheritsType = (AstType)first.AcceptVisitor(this, data);
					} else
						ConvertNodes(typeDeclaration.BaseTypes, type.ImplementsTypes);
				}
				
				type.Name = Identifier.Create(typeDeclaration.NameToken.Annotations, typeDeclaration.Name);
				
				types.Push(type);
				ConvertMembers(typeDeclaration, type, CSharp.Roles.TypeMemberRole, TypeDeclaration.MemberRole);
				types.Pop();
				
				return EndNode(typeDeclaration, type);
			}
		}
		
		public AstNode VisitUsingAliasDeclaration(CSharp.UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			var imports = new ImportsStatement();
			
			var clause = new AliasImportsClause() {
				Name = Identifier.Create(usingAliasDeclaration.AliasToken.Annotations, usingAliasDeclaration.Alias),
				Alias = (AstType)usingAliasDeclaration.Import.AcceptVisitor(this, data)
			};
			
			imports.AddChild(clause, ImportsStatement.ImportsClauseRole);
			
			return EndNode(usingAliasDeclaration, imports);
		}
		
		public AstNode VisitUsingDeclaration(CSharp.UsingDeclaration usingDeclaration, object data)
		{
			var imports = new ImportsStatement();
			
			var clause = new MemberImportsClause() {
				Member = (AstType)usingDeclaration.Import.AcceptVisitor(this, data)
			};
			
			imports.AddChild(clause, ImportsStatement.ImportsClauseRole);
			
			return EndNode(usingDeclaration, imports);
		}
		
		public AstNode VisitExternAliasDeclaration(CSharp.ExternAliasDeclaration externAliasDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitBlockStatement(CSharp.BlockStatement blockStatement, object data)
		{
			var block = new BlockStatement();
			blocks.Push(block);
			block.HiddenStart = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(blockStatement.HiddenStart);
			block.HiddenEnd = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(blockStatement.HiddenEnd);
			ConvertNodes(blockStatement, block.Statements);
			blocks.Pop();
			return EndNode(blockStatement, block);
		}
		
		public AstNode VisitBreakStatement(CSharp.BreakStatement breakStatement, object data)
		{
			var exit = new ExitStatement(ExitKind.None);
			
			foreach (var stmt in breakStatement.Ancestors) {
				if (stmt is CSharp.MethodDeclaration) {
					exit.ExitKind = IsSub(((CSharp.MethodDeclaration)stmt).ReturnType) ? ExitKind.Sub : ExitKind.Function;
					break;
				}
				if (stmt is CSharp.PropertyDeclaration) {
					exit.ExitKind = ExitKind.Property;
					break;
				}
				if (stmt is CSharp.DoWhileStatement) {
					exit.ExitKind = ExitKind.Do;
					break;
				}
				if (stmt is CSharp.ForStatement || stmt is CSharp.ForeachStatement) {
					ConvertedStatementKind kind;
					if (!convertedKind.TryGetValue(stmt, out kind) || kind != ConvertedStatementKind.While)
						exit.ExitKind = ExitKind.For;
					else
						exit.ExitKind = ExitKind.While;
					break;
				}
				if (stmt is CSharp.WhileStatement) {
					exit.ExitKind = ExitKind.While;
					break;
				}
				if (stmt is CSharp.SwitchStatement) {
					exit.ExitKind = ExitKind.Select;
					break;
				}
				if (stmt is CSharp.TryCatchStatement) {
					exit.ExitKind = ExitKind.Try;
					break;
				}
			}
			
			return EndNode(breakStatement, exit);
		}
		
		public AstNode VisitCheckedStatement(CSharp.CheckedStatement checkedStatement, object data)
		{
			blocks.Peek().AddChild(new Comment(" The following expression was wrapped in a checked-statement", false), AstNode.Roles.Comment);
			var body = (BlockStatement)checkedStatement.Body.AcceptVisitor(this, data);
			
			foreach (var stmt in body) {
				stmt.Remove();
				blocks.Peek().Add(stmt);
			}
			
			return EndNode<AstNode>(checkedStatement, null);
		}
		
		public AstNode VisitContinueStatement(CSharp.ContinueStatement continueStatement, object data)
		{
			var @continue = new ContinueStatement(ContinueKind.None);
			
			foreach (var stmt in continueStatement.Ancestors) {
				if (stmt is CSharp.DoWhileStatement) {
					@continue.ContinueKind = ContinueKind.Do;
					break;
				}
				if (stmt is CSharp.ForStatement || stmt is CSharp.ForeachStatement) {
					ConvertedStatementKind kind;
					if (!convertedKind.TryGetValue(stmt, out kind) || kind != ConvertedStatementKind.While)
						@continue.ContinueKind = ContinueKind.For;
					else
						@continue.ContinueKind = ContinueKind.While;
					break;
				}
				if (stmt is CSharp.WhileStatement) {
					@continue.ContinueKind = ContinueKind.While;
					break;
				}
			}
			
			return EndNode(continueStatement, @continue);
		}
		
		public AstNode VisitDoWhileStatement(CSharp.DoWhileStatement doWhileStatement, object data)
		{
			var stmt = new DoLoopStatement();
			
			stmt.ConditionType = ConditionType.LoopWhile;
			stmt.Expression = (Expression)doWhileStatement.Condition.AcceptVisitor(this, data);
			stmt.Body = (BlockStatement)doWhileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			
			return EndNode(doWhileStatement, stmt);
		}
		
		public AstNode VisitEmptyStatement(CSharp.EmptyStatement emptyStatement, object data)
		{
			return EndNode<Statement>(emptyStatement, null);
		}
		
		public AstNode VisitExpressionStatement(CSharp.ExpressionStatement expressionStatement, object data)
		{
			var node = expressionStatement.Expression.AcceptVisitor(this, data);
			if (node is Expression)
				node = new ExpressionStatement((Expression)node);
			return EndNode(expressionStatement, node);
		}
		
		public AstNode VisitFixedStatement(CSharp.FixedStatement fixedStatement, object data)
		{
			var block = blocks.Peek();
			block.AddChild(new Comment(" Emulating fixed-Statement, might not be entirely correct!", false), AstNode.Roles.Comment);
			
			var variables = new LocalDeclarationStatement();
			variables.Modifiers = Modifiers.Dim;
			var stmt = new TryStatement();
			stmt.FinallyBlock = new BlockStatement();
			foreach (var decl in fixedStatement.Variables) {
				var v = new VariableDeclaratorWithTypeAndInitializer {
					Identifiers = { new VariableIdentifier { Name = Identifier.Create(decl.NameToken.Annotations, decl.Name) } },
					Type = SimpleType.CreateWithColor(BoxedTextColor.ValueType, "GCHandle"),
					Initializer = new InvocationExpression(
						new MemberAccessExpression { Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.ValueType, "GCHandle") }, MemberName = Identifier.Create(BoxedTextColor.StaticMethod, "Alloc") },
						(Expression)decl.Initializer.AcceptVisitor(this, data),
						new MemberAccessExpression { Target = new IdentifierExpression { Identifier = Identifier.Create(BoxedTextColor.Enum, "GCHandleType") }, MemberName = Identifier.Create(BoxedTextColor.EnumField, "Pinned") }
					)
				};
				variables.Variables.Add(v);
				stmt.FinallyBlock.Add(new IdentifierExpression { Identifier = Identifier.Create(decl.NameToken.Annotations, decl.Name) }.Invoke2(BoxedTextColor.InstanceMethod, "Free"));
			}
			
			block.Add(variables);
			
			stmt.Body = (BlockStatement)fixedStatement.EmbeddedStatement.AcceptVisitor(this, data);
			
			foreach (var ident in stmt.Body.Descendants.OfType<IdentifierExpression>()) {
				ident.ReplaceWith(expr => ((Expression)expr).Invoke2(BoxedTextColor.InstanceMethod, "AddrOfPinnedObject"));
			}
			
			return EndNode(fixedStatement, stmt);
		}
		
		public AstNode VisitForeachStatement(CSharp.ForeachStatement foreachStatement, object data)
		{
			var stmt = new ForEachStatement() {
				Body = (BlockStatement)foreachStatement.EmbeddedStatement.AcceptVisitor(this, data),
				InExpression = (Expression)foreachStatement.InExpression.AcceptVisitor(this, data),
				Variable = new VariableInitializer() {
					Identifier = new VariableIdentifier() { Name = Identifier.Create(foreachStatement.VariableNameToken.Annotations, foreachStatement.VariableName) },
					Type = (AstType)foreachStatement.VariableType.AcceptVisitor(this, data)
				}
			};
			stmt.HiddenInitializer = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(foreachStatement.HiddenInitializer);
			stmt.HiddenGetEnumeratorILSpans = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(foreachStatement.HiddenGetEnumeratorNode);
			stmt.HiddenMoveNextILSpans = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(foreachStatement.HiddenMoveNextNode);
			stmt.HiddenGetCurrentILSpans = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(foreachStatement.HiddenGetCurrentNode);
			
			return EndNode(foreachStatement, stmt);
		}
		
		public AstNode VisitForStatement(CSharp.ForStatement forStatement, object data)
		{
			// for (;;) ;
			if (!forStatement.Initializers.Any() && forStatement.Condition.IsNull && !forStatement.Iterators.Any())
				return EndNode(forStatement, new WhileStatement() { Condition = new PrimitiveExpression(true), Body = (BlockStatement)forStatement.EmbeddedStatement.AcceptVisitor(this, data) });
			
			CSharp.AstNode counterLoop = new CSharp.ForStatement() {
				Initializers = {
					new NamedNode(
						"iteratorVar",
						new Choice {
							new CSharp.VariableDeclarationStatement {
								Type = new Choice {
									new CSharp.PrimitiveType("long"),
									new CSharp.PrimitiveType("ulong"),
									new CSharp.PrimitiveType("int"),
									new CSharp.PrimitiveType("uint"),
									new CSharp.PrimitiveType("short"),
									new CSharp.PrimitiveType("ushort"),
									new CSharp.PrimitiveType("sbyte"),
									new CSharp.PrimitiveType("byte")
								},
								Variables = {
									new AnyNode()
								}
							},
							new CSharp.ExpressionStatement(
								new CSharp.AssignmentExpression()
							)
						})
				},
				Condition = new NamedNode(
					"condition",
					new CSharp.BinaryOperatorExpression {
						Left = new NamedNode("ident", new CSharp.IdentifierExpression(Pattern.AnyString)),
						Operator = CSharp.BinaryOperatorType.Any,
						Right = new AnyNode("endExpr")
					}),
				Iterators = {
					new CSharp.ExpressionStatement(
						new NamedNode(
							"increment",
							new CSharp.AssignmentExpression {
								Left = new Backreference("ident"),
								Operator = CSharp.AssignmentOperatorType.Any,
								Right = new NamedNode("factor", new AnyNode())
							}
						)
					)
				},
				EmbeddedStatement = new NamedNode("body", new AnyNode())
			};
			
			var match = counterLoop.Match(forStatement);
			
			if (match.Success) {
				var init = match.Get<CSharp.Statement>("iteratorVar").SingleOrDefault();
				
				AstNode iteratorVariable;
				
				if (init is CSharp.VariableDeclarationStatement) {
					var var = ((CSharp.VariableDeclarationStatement)init).Variables.First();
					iteratorVariable = new VariableInitializer() {
						Identifier = new VariableIdentifier { Name = Identifier.Create(var.NameToken.Annotations, var.Name) },
						Type = (AstType)((CSharp.VariableDeclarationStatement)init).Type.AcceptVisitor(this, data),
						Expression = (Expression)var.Initializer.AcceptVisitor(this, data)
					};
					CopyAnnotations(var, iteratorVariable);
				} else if (init is CSharp.ExpressionStatement) {
					iteratorVariable = init.AcceptVisitor(this, data);
				} else goto end;
				
				bool copiedCondAnns = false;
				bool copiedIncAnns = false;
				Expression toExpr = Expression.Null;
				
				var cond = match.Get<CSharp.BinaryOperatorExpression>("condition").SingleOrDefault();
				var endExpr = (Expression)match.Get<CSharp.Expression>("endExpr").SingleOrDefault().AcceptVisitor(this, data);
				
				if (cond.Operator == CSharp.BinaryOperatorType.LessThanOrEqual ||
				    cond.Operator == CSharp.BinaryOperatorType.GreaterThanOrEqual) {
					toExpr = endExpr;
				}
				
				if (cond.Operator == CSharp.BinaryOperatorType.LessThan)
					toExpr = new BinaryOperatorExpression(endExpr, BinaryOperatorType.Subtract, new PrimitiveExpression(1));
				if (cond.Operator == CSharp.BinaryOperatorType.GreaterThan)
					toExpr = new BinaryOperatorExpression(endExpr, BinaryOperatorType.Add, new PrimitiveExpression(1));
				
				Expression stepExpr = Expression.Null;
				
				var increment = match.Get<CSharp.AssignmentExpression>("increment").SingleOrDefault();
				var factorExpr = (Expression)match.Get<CSharp.Expression>("factor").SingleOrDefault().AcceptVisitor(this, data);
				
				if (increment.Operator == CSharp.AssignmentOperatorType.Add && (factorExpr is PrimitiveExpression && !IsEqual(((PrimitiveExpression)factorExpr).Value, 1)))
					stepExpr = factorExpr;
				if (increment.Operator == CSharp.AssignmentOperatorType.Subtract)
					stepExpr = new UnaryOperatorExpression(UnaryOperatorType.Minus, factorExpr);

				// Copy all IL ranges
				if (!toExpr.IsNull) {
					CopyAnnotations(cond, toExpr);
					copiedCondAnns = true;
				}
				if (!stepExpr.IsNull) {
					CopyAnnotations(increment, stepExpr);
					copiedIncAnns = true;
				}
				if (!copiedCondAnns) {
					if (!stepExpr.IsNull)
						CopyAnnotations(cond, stepExpr);
				}
				if (!copiedIncAnns) {
					if (!toExpr.IsNull)
						CopyAnnotations(increment, toExpr);
				}
				
				return new ForStatement() {
					Variable = iteratorVariable,
					ToExpression = toExpr,
					StepExpression = stepExpr,
					Body = (BlockStatement)match.Get<CSharp.Statement>("body").Single().AcceptVisitor(this, data)
				};
			}
			
		end:
			convertedKind[forStatement] = ConvertedStatementKind.While;
			var stmt = new WhileStatement() {
				Condition = (Expression)forStatement.Condition.AcceptVisitor(this, data),
				Body = (BlockStatement)forStatement.EmbeddedStatement.AcceptVisitor(this, data)
			};
			ConvertNodes(forStatement.Iterators, stmt.Body.Statements);
			foreach (var initializer in forStatement.Initializers)
				blocks.Peek().Statements.Add((Statement)initializer.AcceptVisitor(this, data));
			convertedKind.Remove(forStatement);
			
			return EndNode(forStatement, stmt);
		}
		readonly Dictionary<CSharp.AstNode, ConvertedStatementKind> convertedKind = new Dictionary<CSharp.AstNode, ConvertedStatementKind>();
		
		bool IsEqual(object value, int num)
		{
			if (value is byte)
				return (byte)value == num;
			if (value is sbyte)
				return (sbyte)value == num;
			if (value is short)
				return (short)value == num;
			if (value is ushort)
				return (ushort)value == num;
			if (value is int)
				return (int)value == num;
			if (value is uint)
				return (uint)value == num;
			if (value is long)
				return (long)value == num;
			if (value is ulong)
				return (ulong)value == (ulong)num;
			
			throw new InvalidCastException();
		}
		
		public AstNode VisitGotoCaseStatement(CSharp.GotoCaseStatement gotoCaseStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitGotoDefaultStatement(CSharp.GotoDefaultStatement gotoDefaultStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitGotoStatement(CSharp.GotoStatement gotoStatement, object data)
		{
			return EndNode(gotoStatement, new GoToStatement() { Label = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Label, gotoStatement.Label) } });
		}
		
		public AstNode VisitIfElseStatement(CSharp.IfElseStatement ifElseStatement, object data)
		{
			var stmt = new IfElseStatement();
			
			stmt.Condition = (Expression)ifElseStatement.Condition.AcceptVisitor(this, data);
			stmt.Body = (Statement)ifElseStatement.TrueStatement.AcceptVisitor(this, data);
			stmt.ElseBlock = (Statement)ifElseStatement.FalseStatement.AcceptVisitor(this, data);
			
			return EndNode(ifElseStatement, stmt);
		}
		
		public AstNode VisitLabelStatement(CSharp.LabelStatement labelStatement, object data)
		{
			return EndNode(labelStatement, new LabelDeclarationStatement() { Label = new IdentifierExpression() { Identifier = Identifier.Create(BoxedTextColor.Label, labelStatement.Label) } });
		}
		
		public AstNode VisitLockStatement(CSharp.LockStatement lockStatement, object data)
		{
			var stmt = new SyncLockStatement();
			
			stmt.Expression = (Expression)lockStatement.Expression.AcceptVisitor(this, data);
			stmt.Body = (BlockStatement)lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
			
			return EndNode(lockStatement, stmt);
		}
		
		public AstNode VisitReturnStatement(CSharp.ReturnStatement returnStatement, object data)
		{
			var stmt = new ReturnStatement((Expression)returnStatement.Expression.AcceptVisitor(this, data));
			
			return EndNode(returnStatement, stmt);
		}
		
		public AstNode VisitSwitchStatement(CSharp.SwitchStatement switchStatement, object data)
		{
			var stmt = new SelectStatement() { Expression = (Expression)switchStatement.Expression.AcceptVisitor(this, data) };
			ConvertNodes(switchStatement.SwitchSections, stmt.Cases);
			stmt.HiddenEnd = ICSharpCode.Decompiler.Ast.NRefactoryExtensions.GetAllRecursiveILSpans(switchStatement.HiddenEnd);
			
			return EndNode(switchStatement, stmt);
		}
		
		public AstNode VisitSwitchSection(CSharp.SwitchSection switchSection, object data)
		{
			var caseStmt = new CaseStatement();
			ConvertNodes(switchSection.CaseLabels, caseStmt.Clauses);
			if (switchSection.Statements.Count == 1 && switchSection.Statements.FirstOrDefault() is CSharp.BlockStatement)
				caseStmt.Body = (BlockStatement)switchSection.Statements.FirstOrDefault().AcceptVisitor(this, data);
			else {
				caseStmt.Body = new BlockStatement();
				ConvertNodes(switchSection.Statements, caseStmt.Body.Statements);
			}
			if (caseStmt.Body.LastOrDefault() is ExitStatement && ((ExitStatement)caseStmt.Body.LastOrDefault()).ExitKind == ExitKind.Select)
				caseStmt.Body.LastOrDefault().Remove();
			return EndNode(switchSection, caseStmt);
		}
		
		public AstNode VisitCaseLabel(CSharp.CaseLabel caseLabel, object data)
		{
			return EndNode(caseLabel, new SimpleCaseClause() { Expression = (Expression)caseLabel.Expression.AcceptVisitor(this, data) });
		}
		
		public AstNode VisitThrowStatement(CSharp.ThrowStatement throwStatement, object data)
		{
			return EndNode(throwStatement, new ThrowStatement((Expression)throwStatement.Expression.AcceptVisitor(this, data)));
		}
		
		public AstNode VisitTryCatchStatement(CSharp.TryCatchStatement tryCatchStatement, object data)
		{
			var stmt = new TryStatement();
			
			stmt.Body = (BlockStatement)tryCatchStatement.TryBlock.AcceptVisitor(this, data);
			stmt.FinallyBlock = (BlockStatement)tryCatchStatement.FinallyBlock.AcceptVisitor(this, data);
			ConvertNodes(tryCatchStatement.CatchClauses, stmt.CatchBlocks);
			
			return EndNode(tryCatchStatement, stmt);
		}
		
		public AstNode VisitCatchClause(CSharp.CatchClause catchClause, object data)
		{
			var clause = new CatchBlock();
			
			if (!catchClause.Type.IsNull)
				clause.ExceptionType = (AstType)catchClause.Type.AcceptVisitor(this, data);
			if (!catchClause.VariableNameToken.IsNull)
				clause.ExceptionVariable = Identifier.Create(catchClause.VariableNameToken.Annotations, catchClause.VariableName);
			if (!catchClause.Condition.IsNull)
				clause.WhenExpression = (Expression)catchClause.Condition.AcceptVisitor(this, data);
			ConvertNodes(catchClause.Body.Statements, clause.Statements);
			
			return EndNode(catchClause, clause);
		}
		
		public AstNode VisitUncheckedStatement(CSharp.UncheckedStatement uncheckedStatement, object data)
		{
			var body = uncheckedStatement.Body.AcceptVisitor(this, data);
			return EndNode<AstNode>(uncheckedStatement, body);
		}
		
		public AstNode VisitUnsafeStatement(CSharp.UnsafeStatement unsafeStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitUsingStatement(CSharp.UsingStatement usingStatement, object data)
		{
			var stmt = new UsingStatement();
			
			stmt.Resources.Add(usingStatement.ResourceAcquisition.AcceptVisitor(this, data));
			stmt.Body = (BlockStatement)usingStatement.EmbeddedStatement.AcceptVisitor(this, data);
			
			return EndNode(usingStatement, stmt);
		}
		
		public AstNode VisitVariableDeclarationStatement(CSharp.VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			var decl = new LocalDeclarationStatement();
			decl.Modifiers = Modifiers.Dim;
			ConvertNodes(variableDeclarationStatement.Variables, decl.Variables);
			
			return EndNode(variableDeclarationStatement, decl);
		}
		
		public AstNode VisitWhileStatement(CSharp.WhileStatement whileStatement, object data)
		{
			var stmt = new WhileStatement() {
				Condition = (Expression)whileStatement.Condition.AcceptVisitor(this, data),
				Body = (BlockStatement)whileStatement.EmbeddedStatement.AcceptVisitor(this, data)
			};
			
			return EndNode(whileStatement, stmt);
		}
		
		public AstNode VisitYieldBreakStatement(CSharp.YieldBreakStatement yieldBreakStatement, object data)
		{
			var frame = members.Peek();
			frame.inIterator = true;
			return EndNode(yieldBreakStatement, new ReturnStatement());
		}
		
		public AstNode VisitYieldReturnStatement(CSharp.YieldReturnStatement yieldReturnStatement, object data)
		{
			var frame = members.Peek();
			frame.inIterator = true;
			return EndNode(yieldReturnStatement, new YieldStatement((Expression)yieldReturnStatement.Expression.AcceptVisitor(this, data)));
		}
		
		public AstNode VisitAccessor(CSharp.Accessor accessor, object data)
		{
			var result = new Accessor();
			
			ConvertNodes(accessor.Attributes, result.Attributes);
			ConvertNodes(accessor.ModifierTokens, result.ModifierTokens);
			result.Body = (BlockStatement)accessor.Body.AcceptVisitor(this, data);
			
			return EndNode(accessor, result);
		}
		
		public AstNode VisitConstructorDeclaration(CSharp.ConstructorDeclaration constructorDeclaration, object data)
		{
			var result = new ConstructorDeclaration();
			
			ConvertNodes(constructorDeclaration.Attributes, result.Attributes);
			ConvertNodes(constructorDeclaration.ModifierTokens, result.ModifierTokens);
			ConvertNodes(constructorDeclaration.Parameters, result.Parameters);
			result.Body = (BlockStatement)constructorDeclaration.Body.AcceptVisitor(this, data);
			if (!constructorDeclaration.Initializer.IsNull)
				result.Body.Statements.InsertBefore(result.Body.FirstOrDefault(), (Statement)constructorDeclaration.Initializer.AcceptVisitor(this, data));
			
			return EndNode(constructorDeclaration, result);
		}
		
		public AstNode VisitConstructorInitializer(CSharp.ConstructorInitializer constructorInitializer, object data)
		{
			InstanceExpression instExpr;
			Identifier identifier;
			var result = new InvocationExpression(
				new MemberAccessExpression() {
					Target = instExpr = new InstanceExpression(constructorInitializer.ConstructorInitializerType == CSharp.ConstructorInitializerType.This ? InstanceExpressionType.Me : InstanceExpressionType.MyBase, TextLocation.Empty),
					MemberName = identifier = new Identifier(BoxedTextColor.Keyword, "New", TextLocation.Empty)
				}
			);
			var ctor = constructorInitializer.Annotation<dnlib.DotNet.IMethod>();
			instExpr.AddAnnotation(ctor?.DeclaringType);
			identifier.AddAnnotation(ctor);
			CopyAnnotations(constructorInitializer, instExpr);
			ConvertNodes(constructorInitializer.Arguments, result.Arguments);
			
			return EndNode(constructorInitializer, new ExpressionStatement(result));
		}
		
		public AstNode VisitDestructorDeclaration(CSharp.DestructorDeclaration destructorDeclaration, object data)
		{
			var finalizer = new MethodDeclaration() { Name = Identifier.Create(BoxedTextColor.InstanceMethod, "Finalize"), IsSub = true };
			
			ConvertNodes(destructorDeclaration.Attributes, finalizer.Attributes);
			ConvertNodes(destructorDeclaration.ModifierTokens, finalizer.ModifierTokens);
			finalizer.Modifiers |= Modifiers.Overrides;
			finalizer.Body = (BlockStatement)destructorDeclaration.Body.AcceptVisitor(this, data);
			
			return EndNode(destructorDeclaration, finalizer);
		}
		
		public AstNode VisitEnumMemberDeclaration(CSharp.EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			var result = new EnumMemberDeclaration();
			
			ConvertNodes(enumMemberDeclaration.Attributes, result.Attributes);
			result.Name = Identifier.Create(enumMemberDeclaration.NameToken.Annotations, enumMemberDeclaration.Name);
			result.Value = (Expression)enumMemberDeclaration.Initializer.AcceptVisitor(this, data);
			
			return EndNode(enumMemberDeclaration, result);
		}

		bool IsOwnerAModule(CSharp.AstNode node)
		{
			if (types.Count > 0 && types.Peek().ClassType == ClassType.Module)
				return true;
			var def = node.Annotation<dnlib.DotNet.IMemberDef>();
			if (def == null)
				return false;
			var td = def.DeclaringType;
			return td != null && td.DeclaringType == null && td.IsSealed && td.IsDefined(stringMicrosoftVisualBasicCompilerServices, stringStandardModuleAttribute);
		}
		static readonly UTF8String stringMicrosoftVisualBasicCompilerServices = new UTF8String("Microsoft.VisualBasic.CompilerServices");
		static readonly UTF8String stringStandardModuleAttribute = new UTF8String("StandardModuleAttribute");

		public AstNode VisitEventDeclaration(CSharp.EventDeclaration eventDeclaration, object data)
		{
			members.Push(new MemberInfo());
			
			EventDeclaration result = null;
			foreach (var evt in eventDeclaration.Variables) {
				result = new EventDeclaration();

				ConvertNodes(eventDeclaration.Attributes, result.Attributes);
				if (types.Any()) {
					if (IsOwnerAModule(eventDeclaration))
						eventDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
				}
				result.Modifiers = ConvertModifiers(eventDeclaration.Modifiers, eventDeclaration);
				result.Name = Identifier.Create(evt.NameToken.Annotations, evt.Name);
				result.ReturnType = (AstType)eventDeclaration.ReturnType.AcceptVisitor(this, data);
				
				CreateImplementsClausesForEvent(eventDeclaration, result);
			}
			
			members.Pop();
			
			return EndNode(eventDeclaration, result);
		}
		
		public AstNode VisitCustomEventDeclaration(CSharp.CustomEventDeclaration customEventDeclaration, object data)
		{
			var result = new EventDeclaration();
			
			members.Push(new MemberInfo());
			
			ConvertNodes(customEventDeclaration.Attributes, result.Attributes);
			if (IsOwnerAModule(customEventDeclaration))
				customEventDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			result.Modifiers = ConvertModifiers(customEventDeclaration.Modifiers, customEventDeclaration);
			result.IsCustom = true;
			result.Name = Identifier.Create(customEventDeclaration.NameToken.Annotations, customEventDeclaration.Name);
			result.ReturnType = (AstType)customEventDeclaration.ReturnType.AcceptVisitor(this, data);
			CreateImplementsClausesForEvent(customEventDeclaration, result);
			result.AddHandlerBlock = (Accessor)customEventDeclaration.AddAccessor.AcceptVisitor(this, data);
			result.RemoveHandlerBlock = (Accessor)customEventDeclaration.RemoveAccessor.AcceptVisitor(this, data);
			
			members.Pop();
			
			return EndNode(customEventDeclaration, result);
		}
		
		public AstNode VisitFieldDeclaration(CSharp.FieldDeclaration fieldDeclaration, object data)
		{
			var decl = new FieldDeclaration();
			
			members.Push(new MemberInfo());
			
			ConvertNodes(fieldDeclaration.Attributes, decl.Attributes);
			if (IsOwnerAModule(fieldDeclaration))
				fieldDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			var modifiers = fieldDeclaration.Modifiers;
			if (modifiers == CSharp.Modifiers.None)
				modifiers |= CSharp.Modifiers.Private;
			decl.Modifiers = ConvertModifiers(modifiers, fieldDeclaration);
			ConvertNodes(fieldDeclaration.Variables, decl.Variables);
			
			members.Pop();
			
			return EndNode(fieldDeclaration, decl);
		}
		
		public AstNode VisitIndexerDeclaration(CSharp.IndexerDeclaration indexerDeclaration, object data)
		{
			var decl = new PropertyDeclaration();
			
			members.Push(new MemberInfo());
			
			ConvertNodes(indexerDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), decl.Attributes);
			decl.Getter = (Accessor)indexerDeclaration.Getter.AcceptVisitor(this, data);
			if (IsOwnerAModule(indexerDeclaration))
				indexerDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			decl.Modifiers = ConvertModifiers(indexerDeclaration.Modifiers, indexerDeclaration);
			decl.Name = Identifier.Create(BoxedTextColor.InstanceProperty, indexerDeclaration.Name);
			ConvertNodes(indexerDeclaration.Parameters, decl.Parameters);
			ConvertNodes(indexerDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), decl.ReturnTypeAttributes);
			CreateImplementsClausesForProperty(indexerDeclaration, decl);
			decl.ReturnType = (AstType)indexerDeclaration.ReturnType.AcceptVisitor(this, data);
			decl.Setter = (Accessor)indexerDeclaration.Setter.AcceptVisitor(this, data);
			
			if (!decl.Setter.IsNull) {
				Identifier id;
				decl.Setter.Parameters.Add(new ParameterDeclaration() {
				                           	Name = id = new Identifier(BoxedTextColor.Parameter, "value", TextLocation.Empty),
				                           	Type = (AstType)indexerDeclaration.ReturnType.AcceptVisitor(this, data),
				                           });
				var method = decl.Setter.Annotation<MethodDef>();
				if (method != null) {
					var p = method.Parameters.FirstOrDefault(a => a.IsNormalMethodParameter);
					if (p != null)
						id.AddAnnotation(p);
				}
			}
			
			members.Pop();
			
			return EndNode(indexerDeclaration, decl);
		}
		
		public AstNode VisitMethodDeclaration(CSharp.MethodDeclaration methodDeclaration, object data)
		{
			CSharp.Attribute attr;
			
			if (IsOwnerAModule(methodDeclaration))
				methodDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			
			if ((methodDeclaration.Modifiers & CSharp.Modifiers.Extern) == CSharp.Modifiers.Extern && HasAttribute(methodDeclaration.Attributes, "System.Runtime.InteropServices.DllImportAttribute", out attr)) {
				var result = new ExternalMethodDeclaration();
				
				members.Push(new MemberInfo());
				
				// remove AttributeSection if only one attribute is present
				var attrSec = (CSharp.AttributeSection)attr.Parent;
				if (attrSec.Attributes.Count == 1)
					attrSec.Remove();
				else
					attr.Remove();
				
				result.Library = (attr.Arguments.First().AcceptVisitor(this, data) as PrimitiveExpression).Value.ToString();
				result.CharsetModifier = ConvertCharset(attr.Arguments);
				result.Alias = ConvertAlias(attr.Arguments);
				
				ConvertNodes(methodDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), result.Attributes);
				ConvertNodes(methodDeclaration.ModifierTokens, result.ModifierTokens);
				result.Name = Identifier.Create(methodDeclaration.NameToken.Annotations, methodDeclaration.Name);
				result.IsSub = IsSub(methodDeclaration.ReturnType);
				ConvertNodes(methodDeclaration.Parameters, result.Parameters);
				ConvertNodes(methodDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), result.ReturnTypeAttributes);
				if (!result.IsSub)
					result.ReturnType = (AstType)methodDeclaration.ReturnType.AcceptVisitor(this, data);
				
				if (members.Pop().inIterator) {
					result.Modifiers |= Modifiers.Iterator;
				}
				result.Modifiers &= ~Modifiers.Shared;
				
				return EndNode(methodDeclaration, result);
			} else {
				var result = new MethodDeclaration();
				
				members.Push(new MemberInfo());
				
				ConvertNodes(methodDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), result.Attributes);
				ConvertNodes(methodDeclaration.ModifierTokens, result.ModifierTokens);
				result.Name = Identifier.Create(methodDeclaration.NameToken.Annotations, methodDeclaration.Name);
				result.IsSub = IsSub(methodDeclaration.ReturnType);
				ConvertNodes(methodDeclaration.Parameters, result.Parameters);
				ConvertNodes(methodDeclaration.TypeParameters, result.TypeParameters);
				ConvertNodes(methodDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), result.ReturnTypeAttributes);
				CreateImplementsClausesForMethod(methodDeclaration, result);
				if (!result.IsSub)
					result.ReturnType = (AstType)methodDeclaration.ReturnType.AcceptVisitor(this, data);
				
				if (methodDeclaration.IsExtensionMethod) {
					AttributeBlock block = new AttributeBlock();
					var attrRef = module.UpdateRowId(module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "ExtensionAttribute"));
					block.Attributes.Add(new Ast.Attribute() { Type = CreateType(attrRef) });
					result.Attributes.Add(block);
				}

				var md = methodDeclaration.Annotation<MethodDef>();
				if (md != null)
					result.Modifiers |= GetExtraMethodModifiers(md);
				
				result.Body = (BlockStatement)methodDeclaration.Body.AcceptVisitor(this, data);
				
				if (members.Pop().inIterator) {
					result.Modifiers |= Modifiers.Iterator;
				}

				return EndNode(methodDeclaration, result);
			}
		}

		Dictionary<TypeDef, Dictionary<string, uint>> modifiersDict = new Dictionary<TypeDef, Dictionary<string, uint>>();
		Modifiers GetExtraMethodModifiers(MethodDef md) {
			Dictionary<string, uint> dict;
			uint count;
			if (!modifiersDict.TryGetValue(md.DeclaringType, out dict)) {
				modifiersDict.Add(md.DeclaringType, dict = new Dictionary<string, uint>());
				foreach (var m in md.DeclaringType.Methods) {
					dict.TryGetValue(m.Name, out count);
					if (!m.IsNewSlot && (m.IsAbstract || m.IsFinal || m.IsVirtual))
						count++;
					count += (1U << 16);
					dict[m.Name] = count;
				}
			}
			if (dict.TryGetValue(md.Name, out count)) {
				uint numOverrides = count & 0xFFFF;
				uint totalMethods = count >> 16;
				if (numOverrides >= 1 && totalMethods > 1)
					return Modifiers.Overloads;
			}
			return Modifiers.None;
		}

		static MethodBaseSig GetMethodBaseSig(ITypeDefOrRef type, MethodBaseSig msig, IList<TypeSig> methodGenArgs = null) {
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

		static ITypeDefOrRef GetReplacedType(ITypeDefOrRef iface, ITypeDefOrRef typeToFix) {
			IList<TypeSig> typeGenArgs = null;
			var ts = iface as TypeSpec;
			if (ts != null) {
				var genSig = ts.TypeSig.ToGenericInstSig();
				if (genSig != null)
					typeGenArgs = genSig.GenericArguments;
			}
			if (typeGenArgs == null)
				return typeToFix;
			return GenericArgumentResolver.Resolve(typeToFix.ToTypeSig(), typeGenArgs, null).ToTypeDefOrRef();
		}

		struct ImplementsResult {
			public ITypeDefOrRef Type { get; }
			public string OriginalName { get; }
			public object Reference { get; }
			public ImplementsResult(IMethodDefOrRef method, object reference)
				: this(method.DeclaringType, method.Name, reference) {
			}
			public ImplementsResult(ITypeDefOrRef type, string originalName, object reference) {
				Type = type;
				OriginalName = originalName;
				Reference = reference;
			}
		}

		static bool IsSameType(List<ImplementsResult> overrides, ITypeDefOrRef type, UTF8String memberName) {
			if (overrides == null)
				return false;
			foreach (var ovr in overrides) {
				if (ovr.OriginalName != memberName)
					continue;
				var comparer = new SigComparer(0);
				if (comparer.Equals(type, ovr.Type))
					return true;
			}
			return false;
		}

		IEnumerable<ImplementsResult> GetMethods(MethodDef method) {
			List<ImplementsResult> overrides = null;
			foreach (var o in method.Overrides) {
				var m = o.MethodDeclaration.ResolveMethodDef();
				if (m == null)
					continue;
				if (!m.DeclaringType.IsInterface)
					continue;
				var res = new ImplementsResult(o.MethodDeclaration, o.MethodDeclaration);
				if (overrides == null)
					overrides = new List<ImplementsResult>();
				overrides.Add(res);
				yield return res;
			}
			var comparer = new SigComparer(0, method.Module);
			foreach (var ii in method.DeclaringType.Interfaces) {
				var iface = ii.Interface.Resolve();
				if (iface == null)
					continue;
				foreach (var ifaceMethod in iface.Methods) {
					if (ifaceMethod.Name != method.Name)
						continue;
					var ifaceMethodSig = GetMethodBaseSig(ii.Interface, ifaceMethod.MethodSig);
					if (!comparer.Equals(ifaceMethodSig, method.MethodSig))
						continue;
					if (IsSameType(overrides, ii.Interface, ifaceMethod.Name))
						continue;
					yield return new ImplementsResult(ii.Interface, ifaceMethod.Name, ifaceMethod);
				}
			}
		}

		static IEnumerable<ImplementsResult> GetProperties(PropertyDef prop) {
			List<ImplementsResult> overrides = null;
			var pm = prop.GetMethod ?? prop.SetMethod;
			if (pm != null) {
				foreach (var o in pm.Overrides) {
					var m = o.MethodDeclaration.ResolveMethodDef();
					if (m == null)
						continue;
					if (!m.DeclaringType.IsInterface)
						continue;
					var p = m.DeclaringType.Properties.FirstOrDefault(a => a.GetMethod == m || a.SetMethod == m);
					if (p == null)
						continue;
					var res = new ImplementsResult(o.MethodDeclaration.DeclaringType, p.Name, (object)GetProperty(o.MethodDeclaration) ?? o.MethodDeclaration);
					if (overrides == null)
						overrides = new List<ImplementsResult>();
					overrides.Add(res);
					yield return res;
				}
			}
			var comparer = new SigComparer(0, prop.Module);
			foreach (var ii in prop.DeclaringType.Interfaces) {
				var iface = ii.Interface.Resolve();
				if (iface == null)
					continue;
				foreach (var ifaceProp in iface.Properties) {
					if (ifaceProp.Name != prop.Name)
						continue;
					var ifaceMethodSig = GetMethodBaseSig(ii.Interface, ifaceProp.PropertySig);
					// mcs doesn't set HasThis if it's an instance property
					if (ifaceMethodSig.HasThis != prop.PropertySig.HasThis) {
						ifaceMethodSig = new MethodSig(ifaceMethodSig.CallingConvention, ifaceMethodSig.GenParamCount, ifaceMethodSig.RetType, ifaceMethodSig.Params, ifaceMethodSig.ParamsAfterSentinel);
						ifaceMethodSig.HasThis = prop.PropertySig.HasThis;
					}
					if (!comparer.Equals(ifaceMethodSig, prop.PropertySig))
						continue;
					if (IsSameType(overrides, ii.Interface, ifaceProp.Name))
						continue;
					yield return new ImplementsResult(ii.Interface, ifaceProp.Name, ifaceProp);
				}
			}
		}

		static PropertyDef GetProperty(IMethodDefOrRef method) {
			var md = method.ResolveMethodDef();
			if (md == null)
				return null;
			foreach (var p in md.DeclaringType.Properties) {
				if (p.GetMethods.Contains(md))
					return p;
				if (p.SetMethods.Contains(md))
					return p;
				if (p.OtherMethods.Contains(md))
					return p;
			}
			return null;
		}

		static EventDef GetEvent(IMethodDefOrRef method) {
			var md = method.ResolveMethodDef();
			if (md == null)
				return null;
			foreach (var e in md.DeclaringType.Events) {
				if (e.AddMethod == md || e.RemoveMethod == md || e.InvokeMethod == md)
					return e;
				if (e.OtherMethods.Contains(md))
					return e;
			}
			return null;
		}

		static IEnumerable<ImplementsResult> GetEvents(EventDef evt) {
			List<ImplementsResult> overrides = null;
			var em = evt.AddMethod ?? evt.RemoveMethod ?? evt.InvokeMethod;
			if (em != null) {
				foreach (var o in em.Overrides) {
					var m = o.MethodDeclaration.ResolveMethodDef();
					if (m == null)
						continue;
					if (!m.DeclaringType.IsInterface)
						continue;
					var e = m.DeclaringType.Events.FirstOrDefault(a => a.AddMethod == m || a.RemoveMethod == m || a.InvokeMethod == m);
					if (e == null)
						continue;
					var res = new ImplementsResult(o.MethodDeclaration.DeclaringType, e.Name, (object)GetEvent(o.MethodDeclaration) ?? o.MethodDeclaration);
					if (overrides == null)
						overrides = new List<ImplementsResult>();
					overrides.Add(res);
					yield return res;
				}
			}
			var comparer = new SigComparer(0, evt.Module);
			foreach (var ii in evt.DeclaringType.Interfaces) {
				var iface = ii.Interface.Resolve();
				if (iface == null)
					continue;
				foreach (var ifaceEvent in iface.Events) {
					if (ifaceEvent.Name != evt.Name)
						continue;
					var eventType = GetReplacedType(ii.Interface, ifaceEvent.EventType);
					if (!comparer.Equals(eventType, evt.EventType))
						continue;
					if (IsSameType(overrides, ii.Interface, ifaceEvent.Name))
						continue;
					yield return new ImplementsResult(ii.Interface, ifaceEvent.Name, ifaceEvent);
				}
			}
		}

		StringBuilder createTypeStringBuilder = new StringBuilder();
		AstType CreateType(ITypeDefOrRef type) {
			//TODO: Use full namespace with Global if necessary, else add an Imports statement and don't use a namespace
			var options = Decompiler.Ast.ConvertTypeOptions.IncludeNamespace;
			var astType = Decompiler.Ast.AstBuilder.ConvertType(type, createTypeStringBuilder, null, options);
			return (AstType)astType.AcceptVisitor(this, null);
		}

		void CreateImplementsClausesForMethod(CSharp.MethodDeclaration orig, MethodDeclaration result) {
			var md = orig.Annotation<MethodDef>();
			if (md == null || !md.IsVirtual)
				return;
			var type = md.DeclaringType;
			if (type.IsInterface)
				return;

			foreach (var t in GetMethods(md))
				result.ImplementsClause.Add(InterfaceMemberSpecifier.CreateWithData(CreateType(t.Type), t.OriginalName, t.Reference));
		}

		void CreateImplementsClausesForProperty(CSharp.IndexerDeclaration orig, PropertyDeclaration result) {
			var pd = orig.Annotation<PropertyDef>();
			if (pd == null)
				return;
			if (pd.GetMethod?.IsVirtual == false && pd.SetMethod?.IsVirtual == false)
				return;
			var type = pd.DeclaringType;
			if (type.IsInterface)
				return;

			foreach (var t in GetProperties(pd))
				result.ImplementsClause.Add(InterfaceMemberSpecifier.CreateWithData(CreateType(t.Type), t.OriginalName, t.Reference));
		}

		void CreateImplementsClausesForProperty(CSharp.PropertyDeclaration orig, PropertyDeclaration result) {
			var pd = orig.Annotation<PropertyDef>();
			if (pd == null)
				return;
			if (pd.GetMethod?.IsVirtual == false && pd.SetMethod?.IsVirtual == false)
				return;
			var type = pd.DeclaringType;
			if (type.IsInterface)
				return;

			foreach (var t in GetProperties(pd))
				result.ImplementsClause.Add(InterfaceMemberSpecifier.CreateWithData(CreateType(t.Type), t.OriginalName, t.Reference));
		}

		void CreateImplementsClausesForEvent(CSharp.CustomEventDeclaration orig, EventDeclaration result) {
			var ed = orig.Annotation<EventDef>();
			if (ed == null)
				return;
			if (ed.AddMethod?.IsVirtual == false && ed.RemoveMethod?.IsVirtual == false && ed.InvokeMethod?.IsVirtual == false)
				return;
			var type = ed.DeclaringType;
			if (type.IsInterface)
				return;

			foreach (var t in GetEvents(ed))
				result.ImplementsClause.Add(InterfaceMemberSpecifier.CreateWithData(CreateType(t.Type), t.OriginalName, t.Reference));
		}

		void CreateImplementsClausesForEvent(CSharp.EventDeclaration orig, EventDeclaration result) {
			var ed = orig.Annotation<EventDef>();
			if (ed == null)
				return;
			if (ed.AddMethod?.IsVirtual == false && ed.RemoveMethod?.IsVirtual == false && ed.InvokeMethod?.IsVirtual == false)
				return;
			var type = ed.DeclaringType;
			if (type.IsInterface)
				return;

			foreach (var t in GetEvents(ed))
				result.ImplementsClause.Add(InterfaceMemberSpecifier.CreateWithData(CreateType(t.Type), t.OriginalName, t.Reference));
		}

		string ConvertAlias(CSharp.AstNodeCollection<CSharp.Expression> arguments)
		{
			var pattern = new CSharp.AssignmentExpression() {
				Left = new CSharp.IdentifierExpression("EntryPoint"),
				Operator = CSharp.AssignmentOperatorType.Assign,
				Right = new AnyNode("alias")
			};
			
			var result = arguments
				.Select(expr => pattern.Match(expr))
				.FirstOrDefault(r => r.Success);
			
			if (result.Success && result.Has("alias")) {
				return result.Get<CSharp.PrimitiveExpression>("alias")
					.First().Value.ToString();
			}
			
			return null;
		}
		
		CharsetModifier ConvertCharset(CSharp.AstNodeCollection<CSharp.Expression> arguments)
		{
			var pattern = new CSharp.AssignmentExpression() {
				Left = new CSharp.IdentifierExpression("CharSet"),
				Operator = CSharp.AssignmentOperatorType.Assign,
				Right = new NamedNode(
					"modifier",
					new CSharp.MemberReferenceExpression() {
						Target = new CSharp.TypeReferenceExpression { Type = new AnyNode() },
						MemberName = Pattern.AnyString
					})
			};
			
			var result = arguments
				.Select(expr => pattern.Match(expr))
				.FirstOrDefault(r => r.Success);
			
			if (result.Success && result.Has("modifier")) {
				switch (result.Get<CSharp.MemberReferenceExpression>("modifier").First().MemberName) {
					case "Auto":
						return CharsetModifier.Auto;
					case "Ansi":
						return CharsetModifier.Ansi;
					case "Unicode":
						return CharsetModifier.Unicode;
				}
			}
			
			return CharsetModifier.None;
		}
		
		bool IsSub(CSharp.AstType returnType)
		{
			var t = returnType as CSharp.PrimitiveType;
			return t != null && t.Keyword == "void";
		}
		
		public AstNode VisitOperatorDeclaration(CSharp.OperatorDeclaration operatorDeclaration, object data)
		{
			MemberDeclaration result;
			members.Push(new MemberInfo());
			
			if (IsOwnerAModule(operatorDeclaration))
				operatorDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			
			if (operatorDeclaration.OperatorType == CSharp.OperatorType.Increment || operatorDeclaration.OperatorType == CSharp.OperatorType.Decrement) {
				var m = new MethodDeclaration();
				result = m;
				
				ConvertNodes(operatorDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), m.Attributes);
				ConvertNodes(operatorDeclaration.ModifierTokens, m.ModifierTokens);
				m.Name = Identifier.Create(VisualBasicMetadataTextColorProvider.Instance.GetColor((object)operatorDeclaration.Annotation<dnlib.DotNet.IMethod>() ?? BoxedTextColor.InstanceMethod), operatorDeclaration.OperatorType == CSharp.OperatorType.Increment ? "op_Increment" : "op_Decrement");
				ConvertNodes(operatorDeclaration.Parameters, m.Parameters);
				ConvertNodes(operatorDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), m.ReturnTypeAttributes);
				m.ReturnType = (AstType)operatorDeclaration.ReturnType.AcceptVisitor(this, data);
				m.Body = (BlockStatement)operatorDeclaration.Body.AcceptVisitor(this, data);
			} else {
				var op = new OperatorDeclaration();
				result = op;
				
				ConvertNodes(operatorDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), op.Attributes);
				ConvertNodes(operatorDeclaration.ModifierTokens, op.ModifierTokens);
				switch (operatorDeclaration.OperatorType) {
					case CSharp.OperatorType.LogicalNot:
					case CSharp.OperatorType.OnesComplement:
						op.Operator = OverloadableOperatorType.Not;
						break;
					case CSharp.OperatorType.True:
						op.Operator = OverloadableOperatorType.IsTrue;
						break;
					case CSharp.OperatorType.False:
						op.Operator = OverloadableOperatorType.IsFalse;
						break;
					case CSharp.OperatorType.Implicit:
						op.Modifiers |= Modifiers.Widening;
						op.Operator = OverloadableOperatorType.CType;
						break;
					case CSharp.OperatorType.Explicit:
						op.Modifiers |= Modifiers.Narrowing;
						op.Operator = OverloadableOperatorType.CType;
						break;
					case CSharp.OperatorType.Addition:
						op.Operator = OverloadableOperatorType.Add;
						break;
					case CSharp.OperatorType.Subtraction:
						op.Operator = OverloadableOperatorType.Subtract;
						break;
					case CSharp.OperatorType.UnaryPlus:
						op.Operator = OverloadableOperatorType.UnaryPlus;
						break;
					case CSharp.OperatorType.UnaryNegation:
						op.Operator = OverloadableOperatorType.UnaryMinus;
						break;
					case CSharp.OperatorType.Multiply:
						op.Operator = OverloadableOperatorType.Multiply;
						break;
					case CSharp.OperatorType.Division:
						op.Operator = OverloadableOperatorType.Divide;
						break;
					case CSharp.OperatorType.Modulus:
						op.Operator = OverloadableOperatorType.Modulus;
						break;
					case CSharp.OperatorType.BitwiseAnd:
						op.Operator = OverloadableOperatorType.BitwiseAnd;
						break;
					case CSharp.OperatorType.BitwiseOr:
						op.Operator = OverloadableOperatorType.BitwiseOr;
						break;
					case CSharp.OperatorType.ExclusiveOr:
						op.Operator = OverloadableOperatorType.ExclusiveOr;
						break;
					case CSharp.OperatorType.LeftShift:
						op.Operator = OverloadableOperatorType.ShiftLeft;
						break;
					case CSharp.OperatorType.RightShift:
						op.Operator = OverloadableOperatorType.ShiftRight;
						break;
					case CSharp.OperatorType.Equality:
						op.Operator = OverloadableOperatorType.Equality;
						break;
					case CSharp.OperatorType.Inequality:
						op.Operator = OverloadableOperatorType.InEquality;
						break;
					case CSharp.OperatorType.GreaterThan:
						op.Operator = OverloadableOperatorType.GreaterThan;
						break;
					case CSharp.OperatorType.LessThan:
						op.Operator = OverloadableOperatorType.LessThan;
						break;
					case CSharp.OperatorType.GreaterThanOrEqual:
						op.Operator = OverloadableOperatorType.GreaterThanOrEqual;
						break;
					case CSharp.OperatorType.LessThanOrEqual:
						op.Operator = OverloadableOperatorType.LessThanOrEqual;
						break;
					default:
						throw new Exception("Invalid value for OperatorType");
				}
				ConvertNodes(operatorDeclaration.Parameters, op.Parameters);
				ConvertNodes(operatorDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), op.ReturnTypeAttributes);
				op.ReturnType = (AstType)operatorDeclaration.ReturnType.AcceptVisitor(this, data);
				op.Body = (BlockStatement)operatorDeclaration.Body.AcceptVisitor(this, data);
			}
			
			members.Pop();
			
			return EndNode(operatorDeclaration, result);
			
		}
		
		public AstNode VisitParameterDeclaration(CSharp.ParameterDeclaration parameterDeclaration, object data)
		{
			var param = new ParameterDeclaration();
			
			ConvertNodes(parameterDeclaration.Attributes, param.Attributes);
			param.Modifiers = ConvertParamModifiers(parameterDeclaration.ParameterModifier);
			if ((parameterDeclaration.ParameterModifier & CSharp.ParameterModifier.Out) == CSharp.ParameterModifier.Out) {
				AttributeBlock block = new AttributeBlock();
				var attrRef = module.UpdateRowId(module.CorLibTypes.GetTypeRef("System.Runtime.InteropServices", "OutAttribute"));
				block.Attributes.Add(new Ast.Attribute() { Type = CreateType(attrRef) });
				param.Attributes.Add(block);
			}
			param.Name = Identifier.Create(parameterDeclaration.NameToken.Annotations, parameterDeclaration.Name);
			param.Type = (AstType)parameterDeclaration.Type.AcceptVisitor(this, data);
			param.OptionalValue = (Expression)parameterDeclaration.DefaultExpression.AcceptVisitor(this, data);
			if (!param.OptionalValue.IsNull)
				param.Modifiers |= Modifiers.Optional;
			
			return EndNode(parameterDeclaration, param);
		}
		
		Modifiers ConvertParamModifiers(CSharp.ParameterModifier mods)
		{
			switch (mods) {
				case CSharp.ParameterModifier.None:
				case CSharp.ParameterModifier.This:
					return Modifiers.None;
				case CSharp.ParameterModifier.In:
				case CSharp.ParameterModifier.Ref:
				case CSharp.ParameterModifier.Out:
					return Modifiers.ByRef;
				case CSharp.ParameterModifier.Params:
					return Modifiers.ParamArray;
				default:
					throw new Exception("Invalid value for ParameterModifier");
			}
		}
		
		public AstNode VisitPropertyDeclaration(CSharp.PropertyDeclaration propertyDeclaration, object data)
		{
			var decl = new PropertyDeclaration();
			
			members.Push(new MemberInfo());
			
			if (IsOwnerAModule(propertyDeclaration))
				propertyDeclaration.Modifiers &= ~CSharp.Modifiers.Static;
			
			ConvertNodes(propertyDeclaration.Attributes.Where(section => section.AttributeTarget != "return"), decl.Attributes);
			decl.Getter = (Accessor)propertyDeclaration.Getter.AcceptVisitor(this, data);
			decl.Modifiers = ConvertModifiers(propertyDeclaration.Modifiers, propertyDeclaration);
			decl.Name = Identifier.Create(propertyDeclaration.NameToken.Annotations, propertyDeclaration.Name);
			ConvertNodes(propertyDeclaration.Attributes.Where(section => section.AttributeTarget == "return"), decl.ReturnTypeAttributes);
			CreateImplementsClausesForProperty(propertyDeclaration, decl);
			decl.ReturnType = (AstType)propertyDeclaration.ReturnType.AcceptVisitor(this, data);
			decl.Setter = (Accessor)propertyDeclaration.Setter.AcceptVisitor(this, data);
			
			if (!decl.Setter.IsNull) {
				Identifier id;
				decl.Setter.Parameters.Add(new ParameterDeclaration() {
				                           	Name = id = new Identifier(BoxedTextColor.Parameter, "value", TextLocation.Empty),
				                           	Type = (AstType)propertyDeclaration.ReturnType.AcceptVisitor(this, data),
				                           });
				var method = decl.Setter.Annotation<MethodDef>();
				if (method != null) {
					var p = method.Parameters.FirstOrDefault(a => a.IsNormalMethodParameter);
					if (p != null)
						id.AddAnnotation(p);
				}
			}
			
			if (members.Pop().inIterator) {
				decl.Modifiers |= Modifiers.Iterator;
			}

			ConvertNodes(propertyDeclaration.Variables, decl.Variables);
			ConvertNodes(provider.GetParametersForProperty(propertyDeclaration), decl.Parameters);
			
			return EndNode(propertyDeclaration, decl);
		}
		
		public AstNode VisitVariableInitializer(CSharp.VariableInitializer variableInitializer, object data)
		{
			var decl = new VariableDeclaratorWithTypeAndInitializer();
			
			// look for type in parent
			decl.Type = (AstType)variableInitializer.Parent
				.GetChildByRole(CSharp.Roles.Type)
				.AcceptVisitor(this, data);
			decl.Identifiers.Add(new VariableIdentifier() { Name = Identifier.Create(variableInitializer.NameToken.Annotations, variableInitializer.Name) });
			decl.Initializer = (Expression)variableInitializer.Initializer.AcceptVisitor(this, data);
			
			return EndNode(variableInitializer, decl);
		}
		
		public AstNode VisitFixedFieldDeclaration(CSharp.FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitFixedVariableInitializer(CSharp.FixedVariableInitializer fixedVariableInitializer, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitSyntaxTree(CSharp.SyntaxTree syntaxTree, object data)
		{
			var unit = new CompilationUnit();

			foreach (var node in syntaxTree.Children)
				unit.AddChild(node.AcceptVisitor(this, null), CompilationUnit.MemberRole);
			
			return EndNode(syntaxTree, unit);
		}
		
		public AstNode VisitSimpleType(CSharp.SimpleType simpleType, object data)
		{
			var type = new SimpleType(simpleType.IdentifierToken.Annotations, simpleType.Identifier);
			ConvertNodes(simpleType.TypeArguments, type.TypeArguments);
			
			return EndNode(simpleType, type);
		}
		
		public AstNode VisitMemberType(CSharp.MemberType memberType, object data)
		{
			AstType target = null;
			
			if (memberType.Target is CSharp.SimpleType && ((CSharp.SimpleType)(memberType.Target)).Identifier.Equals("global", StringComparison.Ordinal))
				target = new PrimitiveType("Global");
			else
				target = (AstType)memberType.Target.AcceptVisitor(this, data);
			
			var type = new QualifiedType(target, Identifier.Create(memberType.MemberNameToken.Annotations, memberType.MemberName));
			ConvertNodes(memberType.TypeArguments, type.TypeArguments);
			
			return EndNode(memberType, type);
		}
		
		public AstNode VisitComposedType(CSharp.ComposedType composedType, object data)
		{
			AstType type = new ComposedType();
			
			ConvertNodes(composedType.ArraySpecifiers, ((ComposedType)type).ArraySpecifiers);
			((ComposedType)type).BaseType = (AstType)composedType.BaseType.AcceptVisitor(this, data);
			((ComposedType)type).HasNullableSpecifier = composedType.HasNullableSpecifier;
			
			for (int i = 0; i < composedType.PointerRank; i++) {
				var tmp = new SimpleType(Identifier.Create(BoxedTextColor.Keyword, "__Pointer"));
				tmp.TypeArguments.Add(type);
				type = tmp;
			}
			
			return EndNode(composedType, type);
		}
		
		public AstNode VisitArraySpecifier(CSharp.ArraySpecifier arraySpecifier, object data)
		{
			return EndNode(arraySpecifier, new ArraySpecifier(arraySpecifier.Dimensions));
		}
		
		public AstNode VisitPrimitiveType(CSharp.PrimitiveType primitiveType, object data)
		{
			string typeName;
			
			switch (primitiveType.Keyword) {
				case "object":
					typeName = "Object";
					break;
				case "bool":
					typeName = "Boolean";
					break;
				case "char":
					typeName = "Char";
					break;
				case "sbyte":
					typeName = "SByte";
					break;
				case "byte":
					typeName = "Byte";
					break;
				case "short":
					typeName = "Short";
					break;
				case "ushort":
					typeName = "UShort";
					break;
				case "int":
					typeName = "Integer";
					break;
				case "uint":
					typeName = "UInteger";
					break;
				case "long":
					typeName = "Long";
					break;
				case "ulong":
					typeName = "ULong";
					break;
				case "float":
					typeName = "Single";
					break;
				case "double":
					typeName = "Double";
					break;
				case "decimal":
					typeName = "Decimal";
					break;
				case "string":
					typeName = "String";
					break;
					// generic constraints
				case "new":
					typeName = "New";
					break;
				case "struct":
					typeName = "Structure";
					break;
				case "class":
					typeName = "Class";
					break;
				case "void":
					typeName = "Void";
					break;
				case "__arglist":
					typeName = "__ArgList";
					break;
				default:
					typeName = "unknown";
					break;
			}
			
			return EndNode(primitiveType, new PrimitiveType(typeName));
		}
		
		public AstNode VisitComment (CSharp.Comment comment, object data)
		{
			if (!comment.IsDocumentation)
				return null;

			var c = new Comment (comment.Content, comment.CommentType == CSharp.CommentType.Documentation);
			
			if (comment.CommentType == CSharp.CommentType.MultiLine)
				throw new NotImplementedException ();
			
			return EndNode (comment, c);
		}
		
		public AstNode VisitPreProcessorDirective (CSharp.PreProcessorDirective preProcessorDirective, object data)
		{
			// TODO
			return null;
		}

		public AstNode VisitTypeParameterDeclaration(CSharp.TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			var param = new TypeParameterDeclaration() {
				Variance = typeParameterDeclaration.Variance,
				NameToken = Identifier.Create(typeParameterDeclaration.NameToken.Annotations, typeParameterDeclaration.Name)
			};
			param.NameToken.AddAnnotation(typeParameterDeclaration.Annotation<object>());
			
			var constraint = typeParameterDeclaration.Parent
				.GetChildrenByRole(CSharp.Roles.Constraint)
				.SingleOrDefault(c => c.TypeParameter.Identifier == typeParameterDeclaration.Name);
			
			if (constraint != null)
				ConvertNodes(constraint.BaseTypes, param.Constraints);
			
			// TODO : typeParameterDeclaration.Attributes get lost?
			//ConvertNodes(typeParameterDeclaration.Attributes
			
			return EndNode(typeParameterDeclaration, param);
		}
		
		public AstNode VisitConstraint(CSharp.Constraint constraint, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitCSharpTokenNode(CSharp.CSharpTokenNode cSharpTokenNode, object data)
		{
			var mod = cSharpTokenNode as CSharp.CSharpModifierToken;
			if (mod != null) {
				var convertedModifiers = ConvertModifiers(mod.Modifier, mod.Parent);
				VBModifierToken token = null;
				if (convertedModifiers != Modifiers.None) {
					token = new VBModifierToken(TextLocation.Empty, convertedModifiers);
					return EndNode(cSharpTokenNode, token);
				}
				return EndNode(cSharpTokenNode, token);
			} else {
				throw new NotSupportedException("Should never visit individual tokens");
			}
		}
		
		Modifiers ConvertModifiers(CSharp.Modifiers modifier, CSharp.AstNode container)
		{
			if ((modifier & CSharp.Modifiers.Any) == CSharp.Modifiers.Any)
				return Modifiers.Any;
			
			var mod = Modifiers.None;
			
			if ((modifier & CSharp.Modifiers.Const) == CSharp.Modifiers.Const)
				mod |= Modifiers.Const;
			if ((modifier & CSharp.Modifiers.Partial) == CSharp.Modifiers.Partial)
				mod |= Modifiers.Partial;
			if ((modifier & CSharp.Modifiers.Abstract) == CSharp.Modifiers.Abstract) {
				if (container is CSharp.TypeDeclaration)
					mod |= Modifiers.MustInherit;
				else
					mod |= Modifiers.MustOverride;
			}
			if ((modifier & CSharp.Modifiers.Static) == CSharp.Modifiers.Static)
				mod |= Modifiers.Shared;
			
			if ((modifier & CSharp.Modifiers.Public) == CSharp.Modifiers.Public)
				mod |= Modifiers.Public;
			if ((modifier & CSharp.Modifiers.Protected) == CSharp.Modifiers.Protected)
				mod |= Modifiers.Protected;
			if ((modifier & CSharp.Modifiers.Internal) == CSharp.Modifiers.Internal)
				mod |= Modifiers.Friend;
			if ((modifier & CSharp.Modifiers.Private) == CSharp.Modifiers.Private)
				mod |= Modifiers.Private;
			if (container is CSharp.IndexerDeclaration)
				mod |= Modifiers.Default;
			bool writeable = IsWriteableProperty(container);
			bool readable = IsReadableProperty(container);
			if (writeable && !readable)
				mod |= Modifiers.WriteOnly;
			if (readable && !writeable)
				mod |= Modifiers.ReadOnly;
			
			if ((modifier & CSharp.Modifiers.Override) == CSharp.Modifiers.Override)
				mod |= Modifiers.Overrides;
			if ((modifier & CSharp.Modifiers.Virtual) == CSharp.Modifiers.Virtual)
				mod |= Modifiers.Overridable;
			if ((modifier & CSharp.Modifiers.Async) == CSharp.Modifiers.Async)
				mod |= Modifiers.Async;
			if ((modifier & CSharp.Modifiers.Sealed) == CSharp.Modifiers.Sealed)
				mod |= Modifiers.NotInheritable;

			return mod;
		}
		
		bool IsReadableProperty(ICSharpCode.NRefactory.CSharp.AstNode container)
		{
			if (container is CSharp.IndexerDeclaration) {
				var i = container as CSharp.IndexerDeclaration;
				return !i.Getter.IsNull;
			}
			
			if (container is CSharp.PropertyDeclaration) {
				var p = container as CSharp.PropertyDeclaration;
				return !p.Getter.IsNull;
			}
			
			return false;
		}
		
		bool IsWriteableProperty(ICSharpCode.NRefactory.CSharp.AstNode container)
		{
			if (container is CSharp.IndexerDeclaration) {
				var i = container as CSharp.IndexerDeclaration;
				return !i.Setter.IsNull;
			}
			
			if (container is CSharp.PropertyDeclaration) {
				var p = container as CSharp.PropertyDeclaration;
				return !p.Setter.IsNull;
			}
			
			return false;
		}
		
		public AstNode VisitIdentifier(CSharp.Identifier identifier, object data)
		{
			var ident = Identifier.Create(identifier.Annotations, identifier.Name, identifier.StartLocation);
			
			return EndNode(identifier, ident);
		}
		
		public AstNode VisitPatternPlaceholder(CSharp.AstNode placeholder, ICSharpCode.NRefactory.PatternMatching.Pattern pattern, object data)
		{
			throw new NotImplementedException();
		}
		
		void ConvertNodes<T>(IEnumerable<CSharp.AstNode> nodes, VB.AstNodeCollection<T> result, Func<T, T> transform = null) where T: VB.AstNode
		{
			foreach (var node in nodes) {
				T n = (T)node.AcceptVisitor(this, null);
				if (transform != null)
					n = transform(n);
				if (n != null)
					result.Add(n);
			}
		}

		void ConvertMembers<T, S, M>(CSharp.AstNode parent, T result, Role<S> sourceRole, Role<M> targetRole) where T : VB.AstNode where S : CSharp.AstNode where M : VB.AstNode
		{
			foreach (var node in parent.Children) {
				if (node.Role == CSharp.Roles.Comment) {
					var n = (Comment)node.AcceptVisitor(this, null);
					if (n != null)
						result.AddChild(n, AstNode.Roles.Comment);
				}
				
				if (node.Role == sourceRole) {
					var n = (M)node.AcceptVisitor(this, null);
					if (n != null)
						result.AddChild(n, targetRole);
				}
			}
		}

		T EndNode<T>(CSharp.AstNode node, T result) where T : VB.AstNode
		{
			if (result != null) {
				CopyComments(node, result);
				CopyAnnotations(node, result);
			}
			
			return result;
		}

		void CopyAnnotations<T>(CSharp.AstNode node, T result) where T : VB.AstNode
		{
			foreach (var ann in node.Annotations)
				result.AddAnnotation(ann);
		}
		
		bool HasAttribute(CSharp.AstNodeCollection<CSharp.AttributeSection> attributes, string name, out CSharp.Attribute foundAttribute)
		{
			foreach (var attr in attributes.SelectMany(a => a.Attributes)) {
				if (provider.GetTypeNameForAttribute(attr) == name) {
					foundAttribute = attr;
					return true;
				}
			}
			foundAttribute = null;
			return false;
		}
		
		public AstNode VisitDocumentationReference(CSharp.DocumentationReference documentationReference, object data)
		{
			throw new NotImplementedException();
		}
		
		public AstNode VisitNewLine(CSharp.NewLineNode newLineNode, object data)
		{
			return null;
		}
		
		public AstNode VisitWhitespace(CSharp.WhitespaceNode whitespaceNode, object data)
		{
			return null;
		}
		
		public AstNode VisitText(CSharp.TextNode textNode, object data)
		{
			return null;
		}
		
		public AstNode VisitNullNode(ICSharpCode.NRefactory.CSharp.AstNode nullNode, object data)
		{
			return null;
		}
		
		public AstNode VisitErrorNode(ICSharpCode.NRefactory.CSharp.AstNode errorNode, object data)
		{
			return null;
		}

		void CopyComments(CSharp.AstNode node, AstNode result)
		{
			foreach (CSharp.Comment c in node.GetChildrenByRole(CSharp.Roles.Comment).Reverse()) {
				if (c.IsDocumentation)
					continue;
				result.InsertChildAfter(null, new Comment(c.Content) { References = c.References }, AstNode.Roles.Comment);
			}
		}
	}
}
