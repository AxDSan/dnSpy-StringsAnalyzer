// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace ICSharpCode.NRefactory.VB.Ast {
	/// <summary>
	/// A type reference in the VB AST.
	/// </summary>
	public abstract class AstType : AstNode
	{
		#region Null
		public new static readonly AstType Null = new NullAstType();
		
		sealed class NullAstType : AstType
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator AstType(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : AstType, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder(this, child, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch(other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection(Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection(role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		public virtual AstType MakeArrayType(int rank = 1)
		{
			return new ComposedType { BaseType = this }.MakeArrayType(rank);
		}
		
		public static AstType FromName(string fullName, object data)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException("fullName");
			fullName = fullName.Trim();
			if (!fullName.Contains("."))
				return SimpleType.CreateWithColor(data, fullName);
			string[] parts = fullName.Split('.');
			
			AstType type = SimpleType.CreateWithColor(BoxedTextColor.Namespace, parts.First());
			
			for (int i = 1; i < parts.Length; i++) {
				var part = parts[i];
				var tt = i + 1 == parts.Length ? data : BoxedTextColor.Namespace;
				type = new QualifiedType(type, Identifier.Create(tt, part));
			}
			
			return type;
		}
		
		/// <summary>
		/// Builds an expression that can be used to access a static member on this type.
		/// </summary>
		public MemberAccessExpression Member(object annotation, string memberName)
		{
			return new TypeReferenceExpression { Type = this }.Member(annotation, memberName);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<Expression> arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke2(null, methodName, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, params Expression[] arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke2(null, methodName, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke(null, methodName, typeArguments, arguments);
		}
	}
}
