// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using dnSpy.Contracts.Decompiler;

namespace ICSharpCode.NRefactory.VB {
	/// <summary>
	/// Output formatter for the Output visitor.
	/// </summary>
	public interface IOutputFormatter
	{
		void StartNode(AstNode node);
		void EndNode(AstNode node);
		
		/// <summary>
		/// Writes an identifier.
		/// If the identifier conflicts with a keyword, the output visitor will
		/// call <c>WriteToken("[")</c> before and <c>WriteToken("]")</c> after calling WriteIdentifier().
		/// </summary>
		void WriteIdentifier(string identifier, object data, object extraData = null);
		
		/// <summary>
		/// Writes a keyword to the output.
		/// </summary>
		void WriteKeyword(string keyword);
		
		/// <summary>
		/// Writes a token to the output.
		/// </summary>
		void WriteToken(string token, object data, object reference = null);
		void Space();
		
		void Indent();
		void Unindent();
		
		void NewLine();
		
		void WriteComment(bool isDocumentation, string content, CSharp.CommentReference[] refs);

		void DebugStart(AstNode node);
		void DebugHidden(object hiddenILSpans);
		void DebugExpression(AstNode node);
		void DebugEnd(AstNode node);
		int NextPosition { get; }
		void AddHighlightedKeywordReference(object reference, int start, int end);
		void AddBracePair(int leftStart, int leftEnd, int rightStart, int rightEnd, CodeBracesRangeFlags flags);
		void AddBlock(int start, int end, CodeBracesRangeFlags flags);
		void AddLineSeparator(int position);
	}
}
