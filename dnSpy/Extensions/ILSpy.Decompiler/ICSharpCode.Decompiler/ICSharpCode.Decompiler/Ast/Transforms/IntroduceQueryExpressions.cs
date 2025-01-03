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
using System.Linq;
using dnSpy.Contracts.Text;
using ICSharpCode.NRefactory.CSharp;

//TODO: Verify that no ILSpans have been removed from this file

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Decompiles query expressions.
	/// Based on C# 4.0 spec, §7.16.2 Query expression translation
	/// </summary>
	public class IntroduceQueryExpressions : IAstTransformPoolObject
	{
		DecompilerContext context;

		public IntroduceQueryExpressions(DecompilerContext context)
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
			DecompileQueries(compilationUnit);
			// After all queries were decompiled, detect degenerate queries (queries not property terminated with 'select' or 'group')
			// and fix them, either by adding a degenerate select, or by combining them with another query.
			foreach (QueryExpression query in compilationUnit.Descendants.OfType<QueryExpression>()) {
				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				if (IsDegenerateQuery(query)) {
					// introduce select for degenerate query
					query.Clauses.Add(new QuerySelectClause {
						Expression = IdentifierExpression
									 .Create(fromClause.Identifier, fromClause.IdentifierToken.Annotation<object>())
									 .CopyAnnotationsFrom(fromClause)
					});
				}
				// See if the data source of this query is a degenerate query,
				// and combine the queries if possible.
				QueryExpression innerQuery = fromClause.Expression as QueryExpression;
				while (IsDegenerateQuery(innerQuery)) {
					QueryFromClause innerFromClause = (QueryFromClause)innerQuery.Clauses.First();
					if (fromClause.Identifier != innerFromClause.Identifier)
						break;
					// Replace the fromClause with all clauses from the inner query
					fromClause.Remove();
					QueryClause insertionPos = null;
					foreach (var clause in innerQuery.Clauses) {
						query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
					}
					fromClause = innerFromClause;
					innerQuery = fromClause.Expression as QueryExpression;
				}
			}
		}

		bool IsDegenerateQuery(QueryExpression query)
		{
			if (query == null)
				return false;
			var lastClause = query.Clauses.LastOrDefault();
			return !(lastClause is QuerySelectClause || lastClause is QueryGroupClause);
		}

		void DecompileQueries(AstNode node)
		{
			QueryExpression query = DecompileQuery(node as InvocationExpression);
			if (query != null)
				node.ReplaceWith(query);
			AstNode next;
			for (AstNode child = (query ?? node).FirstChild; child != null; child = next) {
				// store referece to next child before transformation
				next = child.NextSibling;
				DecompileQueries(child);
			}
		}

		QueryExpression DecompileQuery(InvocationExpression invocation)
		{
			if (invocation == null)
				return null;
			MemberReferenceExpression mre = invocation.Target as MemberReferenceExpression;
			if (mre == null)
				return null;
			switch (mre.MemberName) {
				case "Select":
					{
						if (invocation.Arguments.Count != 1)
							return null;
						if (!IsComplexQuery(mre))
							return null;
						ParameterDeclaration parameter;
						Expression body;
						Expression expr = invocation.Arguments.Single();
						if (MatchSimpleLambda(expr, out parameter, out body)) {
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
							query.Clauses.Add(new QuerySelectClause { Expression = WrapExpressionInParenthesesIfNecessary(body.Detach(), parameter.Name) }.CopyAnnotationsFrom(expr));
							return query;
						}
						return null;
					}
				case "GroupBy":
					{
						if (invocation.Arguments.Count == 2) {
							ParameterDeclaration parameter1, parameter2;
							Expression keySelector, elementSelector;
							if (MatchSimpleLambda(invocation.Arguments.ElementAt(0), out parameter1, out keySelector)
							    && MatchSimpleLambda(invocation.Arguments.ElementAt(1), out parameter2, out elementSelector)
							    && parameter1.Name == parameter2.Name)
							{
								QueryExpression query = new QueryExpression();
								query.Clauses.Add(MakeFromClause(parameter1, mre.Target.Detach()));
								var queryGroupClause = new QueryGroupClause {
									Projection = elementSelector.Detach(),
									Key = keySelector.Detach()
								};
								query.Clauses.Add(queryGroupClause);
								return query;
							}
						} else if (invocation.Arguments.Count == 1) {
							ParameterDeclaration parameter;
							Expression keySelector;
							if (MatchSimpleLambda(invocation.Arguments.Single(), out parameter, out keySelector)) {
								QueryExpression query = new QueryExpression();
								query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
								query.Clauses.Add(new QueryGroupClause { Projection = IdentifierExpression.Create(parameter.Name, BoxedTextColor.Parameter).CopyAnnotationsFrom(parameter), Key = keySelector.Detach() });
								return query;
							}
						}
						return null;
					}
				case "SelectMany":
					{
						if (invocation.Arguments.Count != 2)
							return null;
						ParameterDeclaration parameter;
						Expression collectionSelector;
						var fromExpressionLambda = invocation.Arguments.ElementAt(0);
						if (!MatchSimpleLambda(fromExpressionLambda, out parameter, out collectionSelector))
							return null;
						LambdaExpression lambda = invocation.Arguments.ElementAt(1) as LambdaExpression;
						if (lambda != null && lambda.Parameters.Count == 2 && lambda.Body is Expression) {
							ParameterDeclaration p1 = lambda.Parameters.ElementAt(0);
							ParameterDeclaration p2 = lambda.Parameters.ElementAt(1);
							if (p1.Name == parameter.Name) {
								QueryExpression query = new QueryExpression();
								query.Clauses.Add(MakeFromClause(p1, mre.Target.Detach()));
								query.Clauses.Add(MakeFromClause(p2, collectionSelector.Detach()).CopyAnnotationsFrom(fromExpressionLambda));
								query.Clauses.Add(new QuerySelectClause { Expression = WrapExpressionInParenthesesIfNecessary(((Expression)lambda.Body).Detach(), parameter.Name) });
								return query;
							}
						}
						return null;
					}
				case "Where":
					{
						if (invocation.Arguments.Count != 1)
							return null;
						if (!IsComplexQuery(mre))
							return null;
						ParameterDeclaration parameter;
						Expression body;
						Expression expr = invocation.Arguments.Single();
						if (MatchSimpleLambda(expr, out parameter, out body)) {
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
							query.Clauses.Add(new QueryWhereClause { Condition = body.Detach() }.CopyAnnotationsFrom(expr));
							return query;
						}
						return null;
					}
				case "OrderBy":
				case "OrderByDescending":
				case "ThenBy":
				case "ThenByDescending":
					{
						if (invocation.Arguments.Count != 1)
							return null;
						if (!IsComplexQuery(mre))
							return null;
						var lambda = invocation.Arguments.Single();
						ParameterDeclaration parameter;
						Expression orderExpression;
						if (MatchSimpleLambda(lambda, out parameter, out orderExpression)) {
							if (ValidateThenByChain(invocation, parameter.Name)) {
								QueryOrderClause orderClause = new QueryOrderClause();
								InvocationExpression tmp = invocation;
								while (mre.MemberName == "ThenBy" || mre.MemberName == "ThenByDescending") {
									// insert new ordering at beginning
									orderClause.Orderings.InsertAfter(
										null, new QueryOrdering {
											Expression = orderExpression.Detach(),
											Direction = (mre.MemberName == "ThenBy" ? QueryOrderingDirection.None : QueryOrderingDirection.Descending)
										}.CopyAnnotationsFrom(lambda));

									tmp = (InvocationExpression)mre.Target;
									mre = (MemberReferenceExpression)tmp.Target;
									MatchSimpleLambda(tmp.Arguments.Single(), out parameter, out orderExpression);
								}
								// insert new ordering at beginning
								orderClause.Orderings.InsertAfter(
									null, new QueryOrdering {
										Expression = orderExpression.Detach(),
										Direction = (mre.MemberName == "OrderBy" ? QueryOrderingDirection.None : QueryOrderingDirection.Descending)
									}.CopyAnnotationsFrom(lambda));

								QueryExpression query = new QueryExpression();
								query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
								query.Clauses.Add(orderClause);
								return query;
							}
						}
						return null;
					}
				case "Join":
				case "GroupJoin":
					{
						if (invocation.Arguments.Count != 4)
							return null;
						Expression source1 = mre.Target;
						Expression source2 = invocation.Arguments.ElementAt(0);
						ParameterDeclaration  element1, element2;
						Expression key1, key2;
						if (!MatchSimpleLambda(invocation.Arguments.ElementAt(1), out element1, out key1))
							return null;
						if (!MatchSimpleLambda(invocation.Arguments.ElementAt(2), out element2, out key2))
							return null;
						LambdaExpression lambda = invocation.Arguments.ElementAt(3) as LambdaExpression;
						if (lambda != null && lambda.Parameters.Count == 2 && lambda.Body is Expression) {
							ParameterDeclaration p1 = lambda.Parameters.ElementAt(0);
							ParameterDeclaration p2 = lambda.Parameters.ElementAt(1);
							if (p1.Name == element1.Name && (p2.Name == element2.Name || mre.MemberName == "GroupJoin")) {
								QueryExpression query = new QueryExpression();
								query.Clauses.Add(MakeFromClause(element1, source1.Detach()));
								QueryJoinClause joinClause = new QueryJoinClause();
								joinClause.JoinIdentifierToken = Identifier.Create(element2.Name).WithAnnotation(BoxedTextColor.Parameter);    // join elementName2
								joinClause.JoinIdentifierToken.CopyAnnotationsFrom(element2);
								joinClause.InExpression = source2.Detach();  // in source2
								joinClause.OnExpression = key1.Detach();     // on key1
								joinClause.EqualsExpression = key2.Detach(); // equals key2
								if (mre.MemberName == "GroupJoin") {
									joinClause.IntoIdentifierToken = Identifier.Create(p2.Name).WithAnnotation(BoxedTextColor.Parameter); // into p2.Name
									joinClause.IntoIdentifierToken.CopyAnnotationsFrom(p2);
								}
								query.Clauses.Add(joinClause);
								query.Clauses.Add(new QuerySelectClause { Expression = ((Expression)lambda.Body).Detach() }.CopyAnnotationsFrom(lambda));
								return query;
							}
						}
						return null;
					}
				default:
					return null;
			}
		}

		static bool IsComplexQuery(MemberReferenceExpression mre)
		{
			return mre.Target is InvocationExpression && mre.Parent is InvocationExpression || mre.Parent?.Parent is QueryClause;
		}

		QueryFromClause MakeFromClause(ParameterDeclaration parameter, Expression body)
		{
			QueryFromClause fromClause = new QueryFromClause {
				IdentifierToken = Identifier.Create(parameter.Name).WithAnnotation(BoxedTextColor.Parameter),
				Expression = body
			};
			fromClause.CopyAnnotationsFrom(parameter);
			return fromClause;
		}

		/// <summary>
		/// This fixes #437: Decompilation of query expression loses material parentheses
		/// We wrap the expression in parentheses if:
		/// - the Select-call is explicit (see caller(s))
		/// - the expression is a plain identifier matching the parameter name
		/// </summary>
		Expression WrapExpressionInParenthesesIfNecessary(Expression expression, string parameterName)
		{
			if (expression is IdentifierExpression ident && parameterName.Equals(ident.Identifier, StringComparison.Ordinal))
				return new ParenthesizedExpression(expression);
			return expression;
		}

		/// <summary>
		/// Ensure that all ThenBy's are correct, and that the list of ThenBy's is terminated by an 'OrderBy' invocation.
		/// </summary>
		bool ValidateThenByChain(InvocationExpression invocation, string expectedParameterName)
		{
			if (invocation == null || invocation.Arguments.Count != 1)
				return false;
			MemberReferenceExpression mre = invocation.Target as MemberReferenceExpression;
			if (mre == null)
				return false;
			ParameterDeclaration parameter;
			Expression body;
			if (!MatchSimpleLambda(invocation.Arguments.Single(), out parameter, out body))
				return false;
			if (parameter.Name != expectedParameterName)
				return false;

			if (mre.MemberName == "OrderBy" || mre.MemberName == "OrderByDescending")
				return true;
			else if (mre.MemberName == "ThenBy" || mre.MemberName == "ThenByDescending")
				return ValidateThenByChain(mre.Target as InvocationExpression, expectedParameterName);
			else
				return false;
		}

		/// <summary>Matches simple lambdas of the form "a => b"</summary>
		bool MatchSimpleLambda(Expression expr, out ParameterDeclaration parameter, out Expression body)
		{
			LambdaExpression lambda = expr as LambdaExpression;
			if (lambda != null && lambda.Parameters.Count == 1 && lambda.Body is Expression) {
				ParameterDeclaration p = lambda.Parameters.Single();
				if (p.ParameterModifier == ParameterModifier.None) {
					parameter = p;
					body = (Expression)lambda.Body;
					return true;
				}
			}
			parameter = null;
			body = null;
			return false;
		}
	}
}
