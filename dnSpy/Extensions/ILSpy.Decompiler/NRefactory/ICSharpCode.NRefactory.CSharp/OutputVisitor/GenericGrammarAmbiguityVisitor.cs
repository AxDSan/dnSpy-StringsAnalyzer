using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp {
	public class GenericGrammarAmbiguityVisitor : DepthFirstAstVisitor<bool> {
		/// <summary>
		/// Resolves ambiguities in the specified syntax tree.
		/// This method must be called after the InsertParenthesesVisitor, because the ambiguity depends on whether the
		/// final `>` in the possible-type-argument is followed by an opening parenthesis.
		/// </summary>
		public static void ResolveAmbiguities(AstNode rootNode)
		{
			foreach (var node in rootNode.Descendants.OfType<BinaryOperatorExpression>()) {
				if (CausesAmbiguityWithGenerics(node)) {
					node.ReplaceWith(n => new ParenthesizedExpression(n));
				}
			}
		}

		public static bool CausesAmbiguityWithGenerics(BinaryOperatorExpression binaryOperatorExpression)
		{
			if (binaryOperatorExpression.Operator != BinaryOperatorType.LessThan)
				return false;

			var v = new GenericGrammarAmbiguityVisitor();
			v.genericNestingLevel = 1;

			for (AstNode node = binaryOperatorExpression.Right; node != null; node = node.GetNextNode()) {
				if (node.AcceptVisitor(v))
					return v.ambiguityFound;
			}
			return false;
		}

		int genericNestingLevel;
		bool ambiguityFound;

		protected override bool VisitChildren(AstNode node)
		{
			// unhandled node: probably not syntactically valid in a typename

			// These are preconditions for all recursive Visit() calls.
			Debug.Assert(genericNestingLevel > 0);
			Debug.Assert(!ambiguityFound);

			// The return value merely indicates whether to stop visiting.
			return true; // stop visiting, no ambiguity found
		}

		public override bool VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			if (binaryOperatorExpression.Left.AcceptVisitor(this))
				return true;
			Debug.Assert(genericNestingLevel > 0);
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.LessThan:
					genericNestingLevel += 1;
					break;
				case BinaryOperatorType.GreaterThan:
					genericNestingLevel--;
					break;
				case BinaryOperatorType.ShiftRight when genericNestingLevel >= 2:
					genericNestingLevel -= 2;
					break;
				default:
					return true; // stop visiting, no ambiguity found
			}
			if (genericNestingLevel == 0) {
				// Of the all tokens that might follow `>` and trigger the ambiguity to be resolved in favor of generics,
				// `(` is the only one that might start an expression.
				ambiguityFound = binaryOperatorExpression.Right is ParenthesizedExpression;
				return true; // stop visiting
			}
			return binaryOperatorExpression.Right.AcceptVisitor(this);
		}

		public override bool VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			// identifier could also be valid in a type argument
			return false; // keep visiting
		}

		public override bool VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			return false; // keep visiting
		}

		public override bool VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			// MRE could also be valid in a type argument
			return memberReferenceExpression.Target.AcceptVisitor(this);
		}
	}
}
