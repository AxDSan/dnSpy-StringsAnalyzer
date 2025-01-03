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
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Settings {
	abstract class CommonEditorOptions : ICommonEditorOptions {
		public IContentType ContentType { get; }

		public bool UseVirtualSpace {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.UseVirtualSpaceId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.UseVirtualSpaceId, value);
		}

		public WordWrapStyles WordWrapStyle {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.WordWrapStyleId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.WordWrapStyleId, value);
		}

		public bool LineNumberMargin {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.LineNumberMarginId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.LineNumberMarginId, value);
		}

		public bool EnableHighlightCurrentLine {
			get => group.GetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.EnableHighlightCurrentLineId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.EnableHighlightCurrentLineId, value);
		}

		public bool CutOrCopyBlankLineIfNoSelection {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId, value);
		}

		public bool DisplayUrlsAsHyperlinks {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.DisplayUrlsAsHyperlinksId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.DisplayUrlsAsHyperlinksId, value);
		}

		public bool ForceClearTypeIfNeeded {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsWpfViewOptions.ForceClearTypeIfNeededId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsWpfViewOptions.ForceClearTypeIfNeededId, value);
		}

		public bool HorizontalScrollBar {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.HorizontalScrollBarId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.HorizontalScrollBarId, value);
		}

		public bool VerticalScrollBar {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.VerticalScrollBarId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.VerticalScrollBarId, value);
		}

		public int TabSize {
			get => group.GetOptionValue(ContentType.TypeName, DefaultOptions.TabSizeOptionId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultOptions.TabSizeOptionId, OptionsHelpers.FilterTabSize(value));
		}

		public int IndentSize {
			get => group.GetOptionValue(ContentType.TypeName, DefaultOptions.IndentSizeOptionId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultOptions.IndentSizeOptionId, OptionsHelpers.FilterIndentSize(value));
		}

		public bool ConvertTabsToSpaces {
			get => group.GetOptionValue(ContentType.TypeName, DefaultOptions.ConvertTabsToSpacesOptionId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultOptions.ConvertTabsToSpacesOptionId, value);
		}

		public bool ReferenceHighlighting {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.ReferenceHighlightingId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.ReferenceHighlightingId, value);
		}

		public bool HighlightRelatedKeywords {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.HighlightRelatedKeywordsId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.HighlightRelatedKeywordsId, value);
		}

		public bool BraceMatching {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.BraceMatchingId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.BraceMatchingId, value);
		}

		public bool LineSeparators {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.LineSeparatorsId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.LineSeparatorsId, value);
		}

		public bool ShowBlockStructure {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.ShowBlockStructureId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.ShowBlockStructureId, value);
		}

		public BlockStructureLineKind BlockStructureLineKind {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.BlockStructureLineKindId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.BlockStructureLineKindId, value);
		}

		public bool CompressEmptyOrWhitespaceLines {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId, value);
		}

		public bool CompressNonLetterLines {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.CompressNonLetterLinesId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.CompressNonLetterLinesId, value);
		}

		public bool RemoveExtraTextLineVerticalPixels {
			get => group.GetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId, value);
		}

		public bool SelectionMargin {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.SelectionMarginId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.SelectionMarginId, value);
		}

		public bool GlyphMargin {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.GlyphMarginId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.GlyphMarginId, value);
		}

		public bool EnableMouseWheelZoom {
			get => group.GetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.EnableMouseWheelZoomId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.EnableMouseWheelZoomId, value);
		}

		public bool ZoomControl {
			get => group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.ZoomControlId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.ZoomControlId, value);
		}

		public double ZoomLevel {
			get => group.GetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.ZoomLevelId);
			set => group.SetOptionValue(ContentType.TypeName, DefaultWpfViewOptions.ZoomLevelId, value);
		}

		protected readonly ITextViewOptionsGroup group;

		protected CommonEditorOptions(ITextViewOptionsGroup group, IContentType contentType) {
			this.group = group ?? throw new ArgumentNullException(nameof(group));
			ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
		}
	}
}
