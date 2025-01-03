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

using System.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	static class TextSelectionExtensions {
		public static string GetText(this ITextSelection textSelection) {
			if (textSelection.Mode == TextSelectionMode.Stream)
				return textSelection.StreamSelectionSpan.GetText();
			var sb = new StringBuilder();
			var snapshot = textSelection.TextView.TextSnapshot;
			int i = 0;
			foreach (var s in textSelection.SelectedSpans) {
				if (i++ > 0)
					sb.AppendLine();
				sb.Append(snapshot.GetText(s));
			}
			if (i > 1)
				sb.AppendLine();
			return sb.ToString();
		}
	}
}
