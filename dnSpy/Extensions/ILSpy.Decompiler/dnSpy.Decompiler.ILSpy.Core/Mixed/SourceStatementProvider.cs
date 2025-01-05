using System;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.ILSpy.Core.Mixed {
	struct SourceStatementProvider {
		readonly string text;
		readonly MethodDebugInfo debugInfo;
		SourceStatement? lastStatement;

		public SourceStatementProvider(string text, MethodDebugInfo debugInfo) {
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			this.debugInfo = debugInfo ?? throw new ArgumentNullException(nameof(debugInfo));
			lastStatement = null;
		}

		public (string? line, TextSpan span) GetStatement(uint ilOffset) {
			if (text is null || debugInfo is null)
				return default;

			var lastStmt = lastStatement;
			var stmt = debugInfo.GetSourceStatementByCodeOffset(ilOffset);
			lastStatement = stmt;
			if (stmt is null)
				return default;
			if (lastStmt == stmt)
				return default;

			int startPos = stmt.Value.TextSpan.Start;
			while (startPos > 0 && text[startPos - 1] != '\n')
				startPos--;
			var endPos = stmt.Value.TextSpan.End;
			while (endPos < text.Length) {
				var c = text[endPos];
				if (c == '\r' || c == '\n')
					break;
				endPos++;
			}

			return (text.Substring(startPos, endPos - startPos), new TextSpan(stmt.Value.TextSpan.Start - startPos, stmt.Value.TextSpan.Length));
		}
	}
}
