using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	class FlattenSwitchBlocks : IAstTransformPoolObject
	{
		public void Reset(DecompilerContext context)
		{
		}

		public void Run(AstNode compilationUnit)
		{
			foreach (var switchSection in compilationUnit.Descendants.OfType<SwitchSection>())
			{
				if (switchSection.Statements.Count != 1)
					continue;

				var blockStatement = switchSection.Statements.First() as BlockStatement;
				if (blockStatement == null || blockStatement.Statements.Any(ContainsLocalDeclaration))
					continue;
				if (blockStatement.HiddenStart != null || blockStatement.HiddenEnd != null)
					continue;
				if (blockStatement.GetAllILSpans().Count > 0)
					continue;

				blockStatement.Remove();
				blockStatement.Statements.MoveTo(switchSection.Statements);
			}
		}

		bool ContainsLocalDeclaration(AstNode node)
		{
			if (node is VariableDeclarationStatement)
				return true;
			if (node is BlockStatement)
				return false;
			foreach (var child in node.Children) {
				if (ContainsLocalDeclaration(child))
					return true;
			}
			return false;
		}
	}
}
