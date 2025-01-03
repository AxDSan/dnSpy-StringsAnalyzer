﻿// 
// FullTypeName.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.VB.Ast {
	public class SimpleType : AstType
	{
		public static SimpleType CreateWithColor(object color, string identifier)
		{
			return new SimpleType(identifier, color);
		}

		public static SimpleType CreateWithColor(object color, string identifier, TextLocation location)
		{
			return new SimpleType(identifier, color, location);
		}

		public SimpleType(Identifier identifier)
		{
			this.IdentifierToken = identifier;
		}

		public SimpleType(IEnumerable<object> annotations, string identifier)
		{
			this.IdentifierToken = Ast.Identifier.Create(annotations, identifier);
		}
		
		SimpleType(string identifier, object data)
		{
			this.IdentifierToken = Ast.Identifier.Create(data, identifier);
		}
		
		SimpleType(string identifier, object data, TextLocation location)
		{
			SetChildByRole (Roles.Identifier, new Identifier (data, identifier, location));
		}
		
		public string Identifier {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
// 			set {
// 				SetChildByRole (Roles.Identifier, new Identifier (TextToken.Default, value, TextLocation.Empty));
// 			}
		}

		public Identifier IdentifierToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole (Roles.Identifier, value);
			}
		}
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole (Roles.TypeArgument); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSimpleType(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			SimpleType o = other as SimpleType;
			return o != null && MatchString(this.Identifier, o.Identifier) && this.TypeArguments.DoMatch(o.TypeArguments, match);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder(this.Identifier);
			if (this.TypeArguments.Any()) {
				b.Append('(');
				b.Append("Of ");
				b.Append(string.Join(", ", this.TypeArguments));
				b.Append(')');
			}
			return b.ToString();
		}
	}
}

