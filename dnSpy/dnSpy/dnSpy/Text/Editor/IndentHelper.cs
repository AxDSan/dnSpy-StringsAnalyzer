/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Editor {
	static class IndentHelper {
		/// <summary>
		/// Gets the desired indentation
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="smartIndentationService">Smart indentation service</param>
		/// <param name="line">Line</param>
		/// <returns></returns>
		public static int? GetDesiredIndentation(ITextView textView, ISmartIndentationService smartIndentationService, ITextSnapshotLine line) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			if (smartIndentationService is null)
				throw new ArgumentNullException(nameof(smartIndentationService));
			if (line is null)
				throw new ArgumentNullException(nameof(line));

			var indentStyle = textView.Options.GetIndentStyle();
			switch (indentStyle) {
			case IndentStyle.None:
				return 0;

			case IndentStyle.Block:
				return GetDesiredBlockIndentation(textView, line);

			case IndentStyle.Smart:
				var indentSize = smartIndentationService.GetDesiredIndentation(textView, line);
				Debug2.Assert(indentSize is null || indentSize.Value >= 0);
				return indentSize;

			default:
				Debug.Fail($"Invalid {nameof(IndentStyle)}: {indentStyle}");
				return null;
			}
		}

		/// <summary>
		/// Gets the <see cref="IndentStyle.Block"/> indentation
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="line">Line</param>
		/// <returns></returns>
		public static int? GetDesiredBlockIndentation(ITextView textView, ITextSnapshotLine line) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			if (line is null)
				throw new ArgumentNullException(nameof(line));

			while (line.LineNumber != 0) {
				line = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
				if (line.Length != 0)
					return GetDesiredBlockIndentation(line, textView.Options.GetTabSize());
			}
			return null;
		}

		static int? GetDesiredBlockIndentation(ITextSnapshotLine line, int tabSize) {
			var snapshot = line.Snapshot;
			int len = line.Length;
			int offset = line.Start.Position;
			int indentation = 0;
			for (int i = 0; i < len; i++, offset++) {
				char c = snapshot[offset];
				if (c == '\t')
					indentation = indentation / tabSize * tabSize + tabSize;
				else if (char.IsWhiteSpace(c))
					indentation++;
				else
					break;
			}
			return indentation;
		}
	}
}
