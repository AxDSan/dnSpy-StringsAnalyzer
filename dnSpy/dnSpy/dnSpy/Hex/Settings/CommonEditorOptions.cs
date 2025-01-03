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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings.HexGroups;

namespace dnSpy.Hex.Settings {
	abstract class CommonEditorOptions {
		protected HexViewOptionsGroup Group { get; }
		public string SubGroup { get; }

		protected CommonEditorOptions(HexViewOptionsGroup group, string subGroup) {
			Group = group ?? throw new ArgumentNullException(nameof(group));
			SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
		}

		public HexOffsetFormat HexOffsetFormat {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.HexOffsetFormatId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.HexOffsetFormatId, value);
		}

		public bool ValuesLowerCaseHex {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ValuesLowerCaseHexId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ValuesLowerCaseHexId, value);
		}

		public bool OffsetLowerCaseHex {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.OffsetLowerCaseHexId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.OffsetLowerCaseHexId, value);
		}

		public int GroupSizeInBytes {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.GroupSizeInBytesId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.GroupSizeInBytesId, value);
		}

		public bool EnableColorization {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.EnableColorizationId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.EnableColorizationId, value);
		}

		public bool RemoveExtraTextLineVerticalPixels {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId, value);
		}

		public bool ShowColumnLines {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ShowColumnLinesId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ShowColumnLinesId, value);
		}

		public HexColumnLineKind ColumnLine0 {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ColumnLine0Id);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ColumnLine0Id, value);
		}

		public HexColumnLineKind ColumnLine1 {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ColumnLine1Id);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ColumnLine1Id, value);
		}

		public HexColumnLineKind ColumnGroupLine0 {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ColumnGroupLine0Id);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ColumnGroupLine0Id, value);
		}

		public HexColumnLineKind ColumnGroupLine1 {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.ColumnGroupLine1Id);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.ColumnGroupLine1Id, value);
		}

		public bool HighlightActiveColumn {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.HighlightActiveColumnId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.HighlightActiveColumnId, value);
		}

		public bool HighlightCurrentValue {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.HighlightCurrentValueId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.HighlightCurrentValueId, value);
		}

		public int HighlightCurrentValueDelayMilliSeconds {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.HighlightCurrentValueDelayMilliSecondsId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.HighlightCurrentValueDelayMilliSecondsId, value);
		}

		public int EncodingCodePage {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.EncodingCodePageId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.EncodingCodePageId, value);
		}

		public bool HighlightStructureUnderMouseCursor {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewOptions.HighlightStructureUnderMouseCursorId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewOptions.HighlightStructureUnderMouseCursorId, value);
		}

		public bool EnableHighlightCurrentLine {
			get => Group.GetOptionValue(SubGroup, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId);
			set => Group.SetOptionValue(SubGroup, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId, value);
		}

		public bool EnableMouseWheelZoom {
			get => Group.GetOptionValue(SubGroup, DefaultWpfHexViewOptions.EnableMouseWheelZoomId);
			set => Group.SetOptionValue(SubGroup, DefaultWpfHexViewOptions.EnableMouseWheelZoomId, value);
		}

		public double ZoomLevel {
			get => Group.GetOptionValue(SubGroup, DefaultWpfHexViewOptions.ZoomLevelId);
			set => Group.SetOptionValue(SubGroup, DefaultWpfHexViewOptions.ZoomLevelId, value);
		}

		public bool HorizontalScrollBar {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewHostOptions.HorizontalScrollBarId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewHostOptions.HorizontalScrollBarId, value);
		}

		public bool VerticalScrollBar {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewHostOptions.VerticalScrollBarId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewHostOptions.VerticalScrollBarId, value);
		}

		public bool SelectionMargin {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewHostOptions.SelectionMarginId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewHostOptions.SelectionMarginId, value);
		}

		public bool ZoomControl {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewHostOptions.ZoomControlId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewHostOptions.ZoomControlId, value);
		}

		public bool GlyphMargin {
			get => Group.GetOptionValue(SubGroup, DefaultHexViewHostOptions.GlyphMarginId);
			set => Group.SetOptionValue(SubGroup, DefaultHexViewHostOptions.GlyphMarginId, value);
		}

		public bool ForceClearTypeIfNeeded {
			get => Group.GetOptionValue(SubGroup, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId);
			set => Group.SetOptionValue(SubGroup, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId, value);
		}
	}
}
