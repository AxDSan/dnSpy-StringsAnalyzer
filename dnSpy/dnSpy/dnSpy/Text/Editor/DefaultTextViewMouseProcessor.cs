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
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor {
	sealed class DefaultTextViewMouseProcessor : DefaultMouseProcessor {
		readonly IWpfTextView wpfTextView;
		readonly IEditorOperations editorOperations;

		public DefaultTextViewMouseProcessor(IWpfTextView wpfTextView, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			editorOperations = editorOperationsFactoryService.GetEditorOperations(wpfTextView);
		}

		MouseLocation GetLocation(MouseEventArgs e) => MouseLocation.Create(wpfTextView, e, insertionPosition: true);

		bool IsInSelection(VirtualSnapshotPoint point) {
			if (wpfTextView.Selection.IsEmpty)
				return false;
			foreach (var span in wpfTextView.Selection.VirtualSelectedSpans) {
				if (span.Contains(point))
					return true;
			}
			return false;
		}

		public override void OnMouseRightButtonDown(object? sender, MouseButtonEventArgs e) {
			e.Handled = true;
			var mouseLoc = GetLocation(e);
			wpfTextView.Caret.MoveTo(mouseLoc.TextViewLine, mouseLoc.Point.X, true);
			if (!IsInSelection(mouseLoc.Position))
				wpfTextView.Selection.Clear();
			wpfTextView.Caret.EnsureVisible();
		}

		void UpdateSelectionMode() {
			bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
			bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
			if (isAlt && wpfTextView.Selection.IsEmpty)
				wpfTextView.Selection.Mode = TextSelectionMode.Box;
			else if (isAlt && isShift)
				wpfTextView.Selection.Mode = TextSelectionMode.Box;
		}

		void SelectToMousePosition(MouseEventArgs e, bool extendSelection) =>
			SelectToMousePosition(GetLocation(e), extendSelection, false);
		void SelectToMousePosition(MouseLocation mouseLoc, bool extendSelection, bool allowVirtualSpace) {
			UpdateSelectionMode();
			var snapshotLine = mouseLoc.TextViewLine.Start.GetContainingLine();
			editorOperations.MoveCaret(mouseLoc.TextViewLine, mouseLoc.Point.X, extendSelection);
			if (wpfTextView.Selection.Mode == TextSelectionMode.Box)
				allowVirtualSpace = true;
			// The caret can move to an auto-indented location if the line is empty. Move to
			// the first character if the user clicked somewhere before the auto-indented location.
			if (!allowVirtualSpace && snapshotLine.Length == 0 && mouseLoc.Point.X < wpfTextView.Caret.Left)
				editorOperations.MoveCaret(mouseLoc.TextViewLine, 0, extendSelection);
		}

		public override void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e) {
			e.Handled = true;
			var mouseLoc = GetLocation(e);
			int clickCount = e.ClickCount;
			if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Control)
				clickCount = 2;
			switch (clickCount) {
			default:
			case 1:
				bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
				bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
				if (!isShift && !isAlt)
					wpfTextView.Selection.Mode = TextSelectionMode.Stream;
				SelectToMousePosition(mouseLoc, isShift, isAlt);
				break;

			case 2:
				editorOperations.MoveCaret(mouseLoc.TextViewLine, mouseLoc.Point.X, false);
				editorOperations.SelectCurrentWord();
				break;

			case 3:
				editorOperations.SelectLine(mouseLoc.TextViewLine, false);
				// Seems to match VS behavior
				var end = mouseLoc.TextViewLine.TextRight;
				if (mouseLoc.TextViewLine.IsLastTextViewLineForSnapshotLine)
					end += wpfTextView.FormattedLineSource.ColumnWidth;
				if (mouseLoc.Point.X < end)
					wpfTextView.Caret.MoveTo(mouseLoc.TextViewLine.Start);
				break;
			}
			wpfTextView.Caret.EnsureVisible();
			mouseLeftDownInfo = new MouseLeftDownInfo(GetSelectionOrCaretIfNoSelection(), mouseLoc.Point, clickCount, wpfTextView.TextSnapshot.Version);
		}
		MouseLeftDownInfo? mouseLeftDownInfo;

		struct MouseLeftDownInfo {
			public VirtualSnapshotSpan Span { get; }
			public Point Point { get; }
			public int Clicks { get; }
			ITextVersion Version { get; set; }
			public MouseLeftDownInfo(VirtualSnapshotSpan span, Point point, int clicks, ITextVersion version) {
				Span = span;
				Point = point;
				Clicks = clicks;
				Version = version;
			}

			public bool TryAdvanceVersion(ITextVersion newVersion) {
				var v = Version;
				while (v != newVersion) {
					var changes = v.Changes;
					if (changes is null)
						break;
					if (changes.Count == 1 && changes[0].OldPosition == 0 && changes[0].OldLength == v.Length)
						return false;
					v = v.Next;
				}
				Version = newVersion;
				return true;
			}
		}

		public override void OnMouseLeftButtonUp(object? sender, MouseButtonEventArgs e) {
			bool oldMouseCaptured = mouseCaptured;
			CancelMouseLeftButtonSelection();
			if (oldMouseCaptured) {
				// We're always called, so don't mark it as handled
				// e.Handled = true;
				return;
			}
		}

		void CancelMouseLeftButtonSelection() {
			mouseLeftDownInfo = null;
			if (mouseCaptured) {
				StopScrolling();
				wpfTextView.VisualElement.ReleaseMouseCapture();
				mouseCaptured = false;
				return;
			}
		}

		VirtualSnapshotSpan GetSelectionOrCaretIfNoSelection() {
			GetSelectionOrCaretIfNoSelection(out var start, out var end);
			return new VirtualSnapshotSpan(start, end);
		}

		void GetSelectionOrCaretIfNoSelection(out VirtualSnapshotPoint start, out VirtualSnapshotPoint end) {
			if (!wpfTextView.Selection.IsEmpty) {
				start = wpfTextView.Selection.Start;
				end = wpfTextView.Selection.End;
			}
			else {
				start = wpfTextView.Caret.Position.VirtualBufferPosition;
				end = wpfTextView.Caret.Position.VirtualBufferPosition;
			}
		}

		public override void OnMouseMove(object? sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				if (mouseLeftDownInfo is not null && !mouseLeftDownInfo.Value.TryAdvanceVersion(wpfTextView.TextSnapshot.Version)) {
					CancelMouseLeftButtonSelection();
					return;
				}
				if (!mouseCaptured && mouseLeftDownInfo is not null) {
					var mouseLoc = GetLocation(e);
					var dist = mouseLeftDownInfo.Value.Point - mouseLoc.Point;
					bool movedEnough = Math.Abs(dist.X) >= SystemParameters.MinimumHorizontalDragDistance ||
									   Math.Abs(dist.Y) >= SystemParameters.MinimumVerticalDragDistance;
					if (movedEnough && wpfTextView.VisualElement.CaptureMouse()) {
						mouseCaptured = true;
						e.Handled = true;
						return;
					}
				}
				else if (mouseCaptured) {
					e.Handled = true;
					Debug2.Assert(mouseLeftDownInfo is not null);
					if (mouseLeftDownInfo is null)
						StopScrolling();
					else if (mouseLeftDownInfo.Value.Clicks == 2 || mouseLeftDownInfo.Value.Clicks == 3) {
						Debug2.Assert(dispatcherTimer is null);
						StopScrolling();

						var mouseLoc = GetLocation(e);
						// Same behavior as in VS: don't scroll if it's word or line selection
						if (!mouseLoc.TextViewLine.IsVisible())
							return;
						wpfTextView.Caret.MoveTo(mouseLoc.Position);

						wpfTextView.Selection.Mode = TextSelectionMode.Stream;
						if (mouseLeftDownInfo.Value.Clicks == 2)
							editorOperations.SelectCurrentWord();
						else
							editorOperations.SelectLine(wpfTextView.Caret.ContainingTextViewLine, false);
						GetSelectionOrCaretIfNoSelection(out var selStart, out var selEnd);

						VirtualSnapshotPoint anchorPoint, activePoint;
						var translatedSpan = mouseLeftDownInfo.Value.Span.TranslateTo(selStart.Position.Snapshot, SpanTrackingMode.EdgeExclusive);
						if (selStart < translatedSpan.Start) {
							activePoint = selStart;
							anchorPoint = translatedSpan.End;
						}
						else {
							activePoint = selEnd;
							anchorPoint = translatedSpan.Start;
						}
						wpfTextView.Selection.Select(anchorPoint, activePoint);
						wpfTextView.Caret.MoveTo(activePoint);
						wpfTextView.Caret.EnsureVisible();
					}
					else {
						SelectToMousePosition(e, true);
						UpdateScrolling(e);
					}
					return;
				}
			}
		}
		bool mouseCaptured;
		DispatcherTimer? dispatcherTimer;
		double dispatcherTimerXCoord;

		void StopScrolling() {
			dispatcherTimer?.Stop();
			dispatcherTimer = null;
		}

		void UpdateScrolling(MouseEventArgs e) {
			var mouseLoc = GetLocation(e);
			dispatcherTimerXCoord = mouseLoc.Point.X;
			var scrollDir = GetScrollDirection(mouseLoc, out var interval);
			if (scrollDir is null) {
				StopScrolling();
				wpfTextView.Caret.EnsureVisible();
				return;
			}

			if (dispatcherTimer is not null) {
				// It resets the timer if we write a new value, even if it's identical to the original value
				if (dispatcherTimer.Interval != interval)
					dispatcherTimer.Interval = interval;
			}
			else {
				dispatcherTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e2) => OnScroll(scrollDir.Value, dispatcherTimerXCoord), wpfTextView.VisualElement.Dispatcher);
				OnScroll(scrollDir.Value, dispatcherTimerXCoord);
			}
		}

		ScrollDirection? GetScrollDirection(MouseLocation mouseLoc, out TimeSpan interval) {
			// Give prio to scrolling up/down (more common than scrolling left/right)
			if (mouseLoc.Point.Y < wpfTextView.ViewportTop) {
				interval = GetVerticalInterval(mouseLoc.Point.Y - wpfTextView.ViewportTop);
				return ScrollDirection.Up;
			}
			if (mouseLoc.Point.Y >= wpfTextView.ViewportBottom) {
				interval = GetVerticalInterval(mouseLoc.Point.Y - wpfTextView.ViewportBottom);
				return ScrollDirection.Down;
			}
			if (mouseLoc.Point.X < wpfTextView.ViewportLeft) {
				interval = GetHorizontalInterval(mouseLoc.Point.X - wpfTextView.ViewportLeft);
				return ScrollDirection.Left;
			}
			if (mouseLoc.Point.X >= wpfTextView.ViewportRight) {
				interval = GetHorizontalInterval(mouseLoc.Point.X - wpfTextView.ViewportRight);
				return ScrollDirection.Right;
			}
			interval = TimeSpan.Zero;
			return null;
		}

		TimeSpan GetVerticalInterval(double dist) => GetInterval(dist, wpfTextView.LineHeight);
		TimeSpan GetHorizontalInterval(double dist) => GetInterval(dist, DefaultCharacterWidth);
		TimeSpan GetInterval(double dist, double length) {
			const double SCROLL_INTERVAL_MS = 250;
			if (Math.Abs(dist) < 2 * length)
				return TimeSpan.FromMilliseconds(SCROLL_INTERVAL_MS);
			return TimeSpan.FromMilliseconds(SCROLL_INTERVAL_MS / 16);
		}

		double DefaultCharacterWidth => wpfTextView.FormattedLineSource.ColumnWidth;

		enum ScrollDirection {
			Left,
			Right,
			Up,
			Down,
		}

		void OnScroll(ScrollDirection value, double xCoordinate) {
			ITextViewLine line;
			SnapshotPoint lineStart;
			switch (value) {
			case ScrollDirection.Left:
				line = wpfTextView.Caret.ContainingTextViewLine;
				if (line.TextLeft >= wpfTextView.ViewportLeft)
					StopScrolling();
				else if (wpfTextView.Caret.InVirtualSpace || wpfTextView.Caret.Position.BufferPosition != line.Start)
					editorOperations.MoveToPreviousCharacter(true);
				else {
					wpfTextView.ViewportLeft = line.TextLeft;
					StopScrolling();
				}
				break;

			case ScrollDirection.Right:
				line = wpfTextView.Caret.ContainingTextViewLine;
				if (line.TextRight <= wpfTextView.ViewportRight)
					StopScrolling();
				else if (wpfTextView.Caret.InVirtualSpace || wpfTextView.Caret.Position.BufferPosition < line.End)
					editorOperations.MoveToNextCharacter(true);
				else {
					wpfTextView.ViewportLeft = Math.Max(0, line.TextRight - wpfTextView.ViewportWidth);
					StopScrolling();
				}
				break;

			case ScrollDirection.Up:
				line = wpfTextView.TextViewLines.FirstVisibleLine;
				if (line.VisibilityState == VisibilityState.FullyVisible && !line.IsFirstDocumentLine())
					line = wpfTextView.GetTextViewLineContainingBufferPosition(line.Start - 1);
				lineStart = line.Start;
				if (line.VisibilityState != VisibilityState.FullyVisible)
					wpfTextView.DisplayTextLineContainingBufferPosition(line.Start, 0, ViewRelativePosition.Top);
				if (!line.IsValid)
					line = wpfTextView.GetTextViewLineContainingBufferPosition(lineStart);
				if (line.IsFirstDocumentLine())
					StopScrolling();
				editorOperations.MoveCaret(line, xCoordinate, true);
				break;

			case ScrollDirection.Down:
				line = wpfTextView.TextViewLines.LastVisibleLine;
				if (line.VisibilityState == VisibilityState.FullyVisible && !line.IsLastDocumentLine())
					line = wpfTextView.GetTextViewLineContainingBufferPosition(line.GetPointAfterLineBreak());
				lineStart = line.Start;
				if (line.VisibilityState != VisibilityState.FullyVisible)
					wpfTextView.DisplayTextLineContainingBufferPosition(line.Start, 0, ViewRelativePosition.Bottom);
				if (!line.IsValid)
					line = wpfTextView.GetTextViewLineContainingBufferPosition(lineStart);
				if (line.IsLastDocumentLine())
					StopScrolling();
				editorOperations.MoveCaret(line, xCoordinate, true);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			wpfTextView.Caret.EnsureVisible();
		}
	}
}
