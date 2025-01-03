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
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class MouseLocation {
		public ITextViewLine TextViewLine { get; }
		public VirtualSnapshotPoint Position { get; }
		public PositionAffinity Affinity { get; }
		public Point Point { get; }

		MouseLocation(ITextViewLine textViewLine, VirtualSnapshotPoint position, Point point) {
			TextViewLine = textViewLine ?? throw new ArgumentNullException(nameof(textViewLine));
			Position = position;
			Affinity = textViewLine.IsLastTextViewLineForSnapshotLine || position.Position != textViewLine.End ? PositionAffinity.Successor : PositionAffinity.Predecessor;
			Debug.Assert(position.VirtualSpaces == 0 || Affinity == PositionAffinity.Successor);
			Point = point;
		}

		static Point GetTextPoint(IWpfTextView wpfTextView, MouseEventArgs e) {
			var pos = e.GetPosition(wpfTextView.VisualElement);
			return new Point(wpfTextView.ViewportLeft + pos.X, wpfTextView.ViewportTop + pos.Y);
		}

		public static MouseLocation Create(IWpfTextView wpfTextView, MouseEventArgs e, bool insertionPosition) {
			ITextViewLine textViewLine;
			VirtualSnapshotPoint position;

			var point = GetTextPoint(wpfTextView, e);
			var line = wpfTextView.TextViewLines.GetTextViewLineContainingYCoordinate(point.Y);
			if (line is not null)
				textViewLine = line;
			else if (point.Y <= wpfTextView.ViewportTop)
				textViewLine = wpfTextView.TextViewLines.FirstVisibleLine;
			else
				textViewLine = wpfTextView.TextViewLines.LastVisibleLine;
			if (insertionPosition)
				position = textViewLine.GetInsertionBufferPositionFromXCoordinate(point.X);
			else
				position = textViewLine.GetVirtualBufferPositionFromXCoordinate(point.X);

			return new MouseLocation(textViewLine, position, point);
		}

		public static MouseLocation? TryCreateTextOnly(IWpfTextView wpfTextView, MouseEventArgs e, bool fullLineHeight) {
			var point = GetTextPoint(wpfTextView, e);
			var line = wpfTextView.TextViewLines.GetTextViewLineContainingYCoordinate(point.Y);
			if (line is null)
				return null;
			if (fullLineHeight) {
				if (!(line.Top <= point.Y && point.Y < line.Bottom))
					return null;
				if (!(line.Left <= point.X && point.X < line.Right))
					return null;
			}
			else {
				if (!(line.TextTop <= point.Y && point.Y < line.TextBottom))
					return null;
				if (!(line.TextLeft <= point.X && point.X < line.TextRight))
					return null;
			}
			var position = line.GetBufferPositionFromXCoordinate(point.X, true);
			if (position is null)
				return null;

			return new MouseLocation(line, new VirtualSnapshotPoint(position.Value), point);
		}

		public override string ToString() {
			var line = Position.Position.GetContainingLine();
			int col = Position.Position - line.Start + Position.VirtualSpaces;
			if (Affinity == PositionAffinity.Predecessor)
				return $"|({line.LineNumber + 1},{col + 1}) {Position} {TextViewLine.Extent}";
			return $"({line.LineNumber + 1},{col + 1})| {Position} {TextViewLine.Extent}";
		}
	}
}
