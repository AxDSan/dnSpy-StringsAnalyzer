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
using System.Windows;
using System.Windows.Media;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Editor {
	static class TextFormattingUtilities {
		public static void UpdateForceClearTypeIfNeeded(DependencyObject target, bool forceClearTypeIfNeeded, IClassificationFormatMap classificationFormatMap) {
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			if (classificationFormatMap is null)
				throw new ArgumentNullException(nameof(classificationFormatMap));

			// Remote Desktop seems to force disable-ClearType so to prevent Consolas from looking
			// really ugly and to prevent the colors (eg. keyword color) to look like different
			// colors, force ClearType if the font is Consolas. VS also does this.
			var fontName = classificationFormatMap.DefaultTextProperties.GetFontName();
			bool forceClearType = forceClearTypeIfNeeded && IsForceClearTypeFontName(fontName);
			if (forceClearType)
				target.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType);
			else
				target.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Auto);
		}

		static bool IsForceClearTypeFontName(string name) => StringComparer.OrdinalIgnoreCase.Equals("Consolas", name);
	}
}
