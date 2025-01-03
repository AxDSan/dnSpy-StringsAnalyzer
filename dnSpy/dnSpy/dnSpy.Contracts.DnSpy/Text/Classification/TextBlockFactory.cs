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
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using dnSpy.Contracts.DnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using DOC = System.Windows.Documents;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Creates a <see cref="TextBlock"/>
	/// </summary>
	static class TextBlockFactory {
		/// <summary>
		/// Flags
		/// </summary>
		[Flags]
		public enum Flags {
			/// <summary>
			/// Nothing is set
			/// </summary>
			None,

			/// <summary>
			/// Don't set the <see cref="TextBlock"/>'s font
			/// </summary>
			DisableSetTextBlockFontFamily			= 1,

			/// <summary>
			/// If set, the text won't word wrap
			/// </summary>
			DisableWordWrap							= 2,

			/// <summary>
			/// If set, don't set font size
			/// </summary>
			DisableFontSize							= 4,

			/// <summary>
			/// Filter out newlines by replacing them with spaces
			/// </summary>
			FilterOutNewlines						= 8,
		}

		static string ToString(string s, bool filterOutNewLines) {
			if (!filterOutNewLines)
				return s;
			if (s.IndexOfAny(LineConstants.newLineChars) < 0)
				return s;
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (Array.IndexOf(LineConstants.newLineChars, c) >= 0)
					sb.Append(' ');
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Creates a <see cref="TextBlock"/>
		/// </summary>
		/// <param name="text">Full text</param>
		/// <param name="defaultProperties">Default text run properties</param>
		/// <param name="orderedPropsAndSpans">Ordered enumerable of spans and text run properties</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public static TextBlock Create(string text, TextFormattingRunProperties defaultProperties, IEnumerable<TextRunPropertiesAndSpan> orderedPropsAndSpans, Flags flags = Flags.None) {
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (defaultProperties is null)
				throw new ArgumentNullException(nameof(defaultProperties));
			if (orderedPropsAndSpans is null)
				throw new ArgumentNullException(nameof(orderedPropsAndSpans));
			text = WpfUnicodeUtils.ReplaceBadChars(text);

			var textBlock = new TextBlock();
			bool filterOutNewlines = (flags & Flags.FilterOutNewlines) != 0;

			if (!defaultProperties.BackgroundBrushEmpty)
				textBlock.Background = defaultProperties.BackgroundBrush;
			if (!defaultProperties.ForegroundBrushEmpty)
				textBlock.Foreground = defaultProperties.ForegroundBrush;
			if (!defaultProperties.BoldEmpty)
				textBlock.FontWeight = defaultProperties.Bold ? FontWeights.Bold : FontWeights.Normal;
			if (!defaultProperties.ItalicEmpty)
				textBlock.FontStyle = defaultProperties.Italic ? FontStyles.Italic : FontStyles.Normal;
			if (!defaultProperties.FontRenderingEmSizeEmpty && (flags & Flags.DisableFontSize) == 0)
				textBlock.FontSize = defaultProperties.FontRenderingEmSize;
			if (!defaultProperties.TextDecorationsEmpty)
				textBlock.TextDecorations = defaultProperties.TextDecorations;
			if (!defaultProperties.TextEffectsEmpty)
				textBlock.TextEffects = defaultProperties.TextEffects;
			if ((flags & Flags.DisableSetTextBlockFontFamily) == 0 && !defaultProperties.TypefaceEmpty)
				textBlock.FontFamily = defaultProperties.Typeface.FontFamily;

			if ((flags & Flags.DisableWordWrap) == 0)
				textBlock.TextWrapping = TextWrapping.Wrap;

			propsAndSpansList.Clear();
			propsAndSpansList.AddRange(orderedPropsAndSpans);
			if (propsAndSpansList.Count == 0)
				textBlock.Text = text;
			else if (CanUseOnlyTextBlock(propsAndSpansList, text)) {
				var properties = propsAndSpansList[0].Properties;
				if (!properties.BackgroundBrushEmpty)
					textBlock.Background = properties.BackgroundBrush;
				if (!properties.ForegroundBrushEmpty)
					textBlock.Foreground = properties.ForegroundBrush;
				if (!properties.BoldEmpty)
					textBlock.FontWeight = properties.Bold ? FontWeights.Bold : FontWeights.Normal;
				if (!properties.ItalicEmpty)
					textBlock.FontStyle = properties.Italic ? FontStyles.Italic : FontStyles.Normal;
				if (!properties.FontRenderingEmSizeEmpty && (flags & Flags.DisableFontSize) == 0)
					textBlock.FontSize = properties.FontRenderingEmSize;
				if (!properties.TextDecorationsEmpty)
					textBlock.TextDecorations = properties.TextDecorations;
				if (!properties.TextEffectsEmpty)
					textBlock.TextEffects = properties.TextEffects;
				if (!properties.TypefaceEmpty && !IsSameTypeFace(defaultProperties, properties))
					textBlock.FontFamily = properties.Typeface.FontFamily;
				textBlock.Text = ToString(text, filterOutNewlines);
			}
			else {
				int textOffset = 0;
				foreach (var tag in propsAndSpansList) {
					if (textOffset < tag.Span.Start)
						textBlock.Inlines.Add(CreateRun(ToString(text.Substring(textOffset, tag.Span.Start - textOffset), filterOutNewlines), defaultProperties, null, flags));
					textBlock.Inlines.Add(CreateRun(ToString(text.Substring(tag.Span.Start, tag.Span.Length), filterOutNewlines), defaultProperties, tag.Properties, flags));
					textOffset = tag.Span.End;
				}
				if (textOffset < text.Length)
					textBlock.Inlines.Add(CreateRun(ToString(text.Substring(textOffset), filterOutNewlines), defaultProperties, null, flags));
			}

			propsAndSpansList.Clear();
			return textBlock;
		}
		static readonly List<TextRunPropertiesAndSpan> propsAndSpansList = new List<TextRunPropertiesAndSpan>();

		static bool CanUseOnlyTextBlock(List<TextRunPropertiesAndSpan> list, string text) {
			if (list.Count != 1)
				return false;
			var ps = list[0];
			if (ps.Span != new Span(0, text.Length))
				return false;
			var props = ps.Properties;
			// Don't use just a TextBlock if bg is used since the bg applies to the whole TextBlock,
			// not the text shown in the TextBlock.
			return props.BackgroundBrushEmpty;
		}

		static DOC.Run CreateRun(string text, TextFormattingRunProperties defaultProperties, TextFormattingRunProperties? properties, Flags flags) {
			var run = new DOC.Run(text);

			if (properties is null)
				return run;

			if (!properties.BackgroundBrushEmpty)
				run.Background = properties.BackgroundBrush;
			if (!properties.ForegroundBrushEmpty)
				run.Foreground = properties.ForegroundBrush;
			if (!properties.BoldEmpty)
				run.FontWeight = properties.Bold ? FontWeights.Bold : FontWeights.Normal;
			if (!properties.ItalicEmpty)
				run.FontStyle = properties.Italic ? FontStyles.Italic : FontStyles.Normal;
			if (!properties.FontRenderingEmSizeEmpty && (flags & Flags.DisableFontSize) == 0)
				run.FontSize = properties.FontRenderingEmSize;
			if (!properties.TextDecorationsEmpty)
				run.TextDecorations = properties.TextDecorations;
			if (!properties.TextEffectsEmpty)
				run.TextEffects = properties.TextEffects;
			if (!properties.TypefaceEmpty && !IsSameTypeFace(defaultProperties, properties))
				run.FontFamily = properties.Typeface.FontFamily;

			return run;
		}

		static bool IsSameTypeFace(TextFormattingRunProperties a, TextFormattingRunProperties b) {
			if (a.TypefaceEmpty != b.TypefaceEmpty)
				return false;
			if (a.Typeface == b.Typeface)
				return true;
			return GetFontName(a) == GetFontName(b);
		}

		static string? GetFontName(TextFormattingRunProperties props) {
			if (props.TypefaceEmpty)
				return string.Empty;
			if (!props.Typeface.FontFamily.FamilyNames.TryGetValue(language, out string? name))
				name = null;
			return name ?? string.Empty;
		}
		static readonly XmlLanguage language = XmlLanguage.GetLanguage("en-US");
	}

	/// <summary>
	/// Text properties and span
	/// </summary>
	internal readonly struct TextRunPropertiesAndSpan {
		/// <summary>
		/// Span
		/// </summary>
		public Span Span { get; }

		/// <summary>
		/// Text properties
		/// </summary>
		public TextFormattingRunProperties Properties { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="properties">Text properties</param>
		public TextRunPropertiesAndSpan(Span span, TextFormattingRunProperties properties) {
			Span = span;
			Properties = properties ?? throw new System.ArgumentNullException(nameof(properties));
		}
	}
}
