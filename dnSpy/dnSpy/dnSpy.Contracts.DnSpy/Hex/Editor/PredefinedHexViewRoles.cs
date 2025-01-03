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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Predefined hex view roles
	/// </summary>
	public static class PredefinedHexViewRoles {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		const string prefix = "hex-";
		public const string Analyzable = prefix + nameof(Analyzable);
		public const string Debuggable = prefix + nameof(Debuggable);
		public const string Document = prefix + nameof(Document);
		public const string Editable = prefix + nameof(Editable);
		public const string EmbeddedPeekHexView = prefix + nameof(EmbeddedPeekHexView);
		public const string Interactive = prefix + nameof(Interactive);
		public const string PreviewHexView = prefix + nameof(PreviewHexView);
		public const string PrimaryDocument = prefix + nameof(PrimaryDocument);
		public const string Printable = prefix + nameof(Printable);
		public const string Structured = prefix + nameof(Structured);
		public const string Zoomable = prefix + nameof(Zoomable);
		public const string CanHaveBackgroundImage = prefix + nameof(CanHaveBackgroundImage);
		public const string CanHaveCurrentLineHighlighter = prefix + nameof(CanHaveCurrentLineHighlighter);
		public const string CanHaveColumnLineSeparator = prefix + nameof(CanHaveColumnLineSeparator);
		public const string CanHaveIntellisenseControllers = prefix + nameof(CanHaveIntellisenseControllers);
		public const string CanHighlightActiveColumn = prefix + nameof(CanHighlightActiveColumn);
		public const string CanHaveGlyphMargin = prefix + nameof(CanHaveGlyphMargin);
		public const string HexEditorGroup = prefix + nameof(HexEditorGroup);
		public const string HexEditorGroupDefault = prefix + nameof(HexEditorGroupDefault);
		public const string HexEditorGroupDebuggerMemory = prefix + nameof(HexEditorGroupDebuggerMemory);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
