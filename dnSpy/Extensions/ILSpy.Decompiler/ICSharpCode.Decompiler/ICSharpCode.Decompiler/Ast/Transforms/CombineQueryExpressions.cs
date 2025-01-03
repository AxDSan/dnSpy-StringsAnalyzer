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
using System.Linq;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

//TODO: Verify that no ILSpans have been removed from this file

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Combines query expressions and removes transparent identifiers.
	/// </summary>
	public class CombineQueryExpressions : IAstTransformPoolObject
	{
		DecompilerContext context;

		public CombineQueryExpressions(DecompilerContext context)
		{
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
		}

		public void Run(AstNode compilationUnit)
		{
			if (!context.Settings.QueryExpressions)
				return;
			CombineQueries(compilationUnit, new Dictionary<string, object>());
		}

		static readonly InvocationExpression castPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new AnyNode("inExpr"),
				MemberName = "Cast",
				TypeArguments = { new AnyNode("targetType") }
			}};

		void CombineQueries(AstNode node, Dictionary<string, object> fromOrLetIdentifiers)
		{
			AstNode next;
			for (AstNode child = node.FirstChild; child != null; child = next) {
				// store referece to next child before transformation
				next = child.NextSibling;
				CombineQueries(child, fromOrLetIdentifiers);
			}
			QueryExpression query = node as QueryExpression;
			if (query != null) {
				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				QueryExpression innerQuery = fromClause.Expression as QueryExpression;
				if (innerQuery != null) {
					if (TryRemoveTransparentIdentifier(query, fromClause, innerQuery, fromOrLetIdentifiers)) {
						RemoveTransparentIdentifierReferences(query, fromOrLetIdentifiers);
					} else {
						QueryContinuationClause continuation = new QueryContinuationClause();
						continuation.PrecedingQuery = innerQuery.Detach();
						continuation.IdentifierToken = (Identifier)fromClause.IdentifierToken.Clone();
						continuation.CopyAnnotationsFrom(fromClause);
						fromClause.ReplaceWith(continuation);
					}
				} else {
					Match m = castPattern.Match(fromClause.Expression);
					if (m.Success) {
						fromClause.Type = m.Get<AstType>("targetType").Single().Detach();
						fromClause.Expression = m.Get<Expression>("inExpr").Single().Detach();
					}
				}
			}
		}

		static readonly QuerySelectClause selectTransparentIdentifierPattern = new QuerySelectClause {
			Expression = new AnonymousTypeCreateExpression {
				Initializers = {
					new Repeat(
						new Choice {
							new IdentifierExpression(Pattern.AnyString).WithName("expr"), // name is equivalent to name = name
							new MemberReferenceExpression(new AnyNode(), Pattern.AnyString).WithName("expr"), // expr.name is equivalent to name = expr.name
							new NamedExpression {
								Name = Pattern.AnyString,
								Expression = new AnyNode()
							}.WithName("expr")
						}
					) { MinCount = 1 }
				}
			}
		};

		bool IsTransparentIdentifier(string identifier)
		{
			return identifier.StartsWith("<>", StringComparison.Ordinal) && (identifier.Contains("TransparentIdentifier") || identifier.Contains("TranspIdent"));
		}

		bool TryRemoveTransparentIdentifier(QueryExpression query, QueryFromClause fromClause, QueryExpression innerQuery, Dictionary<string, object> letClauses)
		{
			if (!IsTransparentIdentifier(fromClause.Identifier))
				return false;
			QuerySelectClause selectClause = innerQuery.Clauses.Last() as QuerySelectClause;
			Match match = selectTransparentIdentifierPattern.Match(selectClause);
			if (!match.Success)
				return false;

			// from * in (from x in ... select new { members of anonymous type }) ...
			// =>
			// from x in ... { let x = ... } ...
			fromClause.Remove();
			selectClause.Remove();
			// Move clauses from innerQuery to query
			QueryClause insertionPos = null;
			foreach (var clause in innerQuery.Clauses) {
				query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
			}

			foreach (var expr in match.Get<Expression>("expr")) {
				switch (expr) {
				case IdentifierExpression identifier:
					letClauses[identifier.Identifier] = identifier.Annotation<ILVariable>();
					continue;
				case MemberReferenceExpression member:
					query.Clauses.InsertAfter(insertionPos, new QueryLetClause { IdentifierToken = (Identifier)member.MemberNameToken.Clone(), Expression = member.Detach() });
					break;
				case NamedExpression namedExpression:
					if (namedExpression.Expression is IdentifierExpression identifierExpression && namedExpression.Name == identifierExpression.Identifier) {
						letClauses[namedExpression.Name] = identifierExpression.Annotation<ILVariable>();
						continue;
					}
					query.Clauses.InsertAfter(insertionPos, new QueryLetClause { IdentifierToken = (Identifier)namedExpression.NameToken.Clone(), Expression = namedExpression.Expression.Detach() });
					break;
				}
			}
			return true;
		}

		/// <summary>
		/// Removes all occurrences of transparent identifiers
		/// </summary>
		void RemoveTransparentIdentifierReferences(AstNode node, Dictionary<string, object> fromOrLetIdentifiers)
		{
			foreach (AstNode child in node.Children) {
				RemoveTransparentIdentifierReferences(child, fromOrLetIdentifiers);
			}
			MemberReferenceExpression mre = node as MemberReferenceExpression;
			if (mre != null) {
				IdentifierExpression ident = mre.Target as IdentifierExpression;
				if (ident != null && IsTransparentIdentifier(ident.Identifier)) {
					IdentifierExpression newIdent = IdentifierExpression.Create(mre.MemberName, mre.MemberNameToken.Annotation<object>());
					mre.TypeArguments.MoveTo(newIdent.TypeArguments);
					newIdent.CopyAnnotationsFrom(mre);
					newIdent.RemoveAnnotations<PropertyDeclaration>(); // remove the reference to the property of the anonymous type
					if (fromOrLetIdentifiers.TryGetValue(mre.MemberName, out var annotation))
						newIdent.AddAnnotation(annotation);
					mre.ReplaceWith(newIdent);
					return;
				}
			}
		}
	}
}
