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
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Theme marker definition
	/// </summary>
	abstract class ThemeMarkerFormatDefinition : MarkerFormatDefinition, IThemeFormatDefinition {
		readonly TextColor textColor;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textColor">Color</param>
		protected ThemeMarkerFormatDefinition(TextColor textColor) => this.textColor = textColor;

		/// <summary>
		/// Creates a new <see cref="ResourceDictionary"/>
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public ResourceDictionary CreateResourceDictionary(ITheme theme) {
			if (theme is null)
				throw new ArgumentNullException(nameof(theme));

			var res = CreateResourceDictionary();

			var fg = GetForeground(theme);
			if (fg is not null)
				res[ForegroundBrushId] = fg;

			var bg = GetBackground(theme);
			if (bg is not null)
				res[BackgroundBrushId] = bg;

			return res;
		}

		Brush? GetForeground(ITheme theme) => theme.GetExplicitColor(textColor.ToColorType()).Foreground;
		Brush? GetBackground(ITheme theme) => theme.GetExplicitColor(textColor.ToColorType()).Background;
	}
}
