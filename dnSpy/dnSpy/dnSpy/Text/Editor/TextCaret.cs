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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextCaret : ITextCaret {
		public double Left => textCaretLayer.Left;
		public double Right => textCaretLayer.Right;
		public double Top => textCaretLayer.Top;
		public double Bottom => textCaretLayer.Bottom;
		public double Width => textCaretLayer.Width;
		public double Height => textCaretLayer.Height;
		public bool InVirtualSpace => currentPosition.VirtualSpaces > 0;
		public bool OverwriteMode => textCaretLayer.OverwriteMode;
		public ITextViewLine ContainingTextViewLine => GetLine(currentPosition.Position, Affinity)!;
		internal VirtualSnapshotPoint CurrentPosition => currentPosition;
		PositionAffinity Affinity { get; set; }

		public bool IsHidden {
			get => textCaretLayer.IsHidden;
			set => textCaretLayer.IsHidden = value;
		}

		public event EventHandler<CaretPositionChangedEventArgs>? PositionChanged;
		public CaretPosition Position => new CaretPosition(currentPosition, textView.BufferGraph.CreateMappingPoint(currentPosition.Position, PointTrackingMode.Positive), Affinity);
		VirtualSnapshotPoint currentPosition;

		readonly IWpfTextView textView;
		readonly ISmartIndentationService smartIndentationService;
		readonly TextCaretLayer textCaretLayer;
		readonly ImeState imeState;
		double preferredXCoordinate;

		public TextCaret(IWpfTextView textView, IAdornmentLayer caretLayer, ISmartIndentationService smartIndentationService, IClassificationFormatMap classificationFormatMap) {
			if (caretLayer is null)
				throw new ArgumentNullException(nameof(caretLayer));
			if (classificationFormatMap is null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			imeState = new ImeState();
			this.smartIndentationService = smartIndentationService ?? throw new ArgumentNullException(nameof(smartIndentationService));
			preferredXCoordinate = 0;
			__preferredYCoordinate = 0;
			Affinity = PositionAffinity.Successor;
			currentPosition = new VirtualSnapshotPoint(textView.TextSnapshot, 0);
			textView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			textView.Options.OptionChanged += Options_OptionChanged;
			textView.VisualElement.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_GotKeyboardFocus), true);
			textView.VisualElement.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(VisualElement_LostKeyboardFocus), true);
			textView.LayoutChanged += TextView_LayoutChanged;
			textCaretLayer = new TextCaretLayer(this, caretLayer, classificationFormatMap);
			InputMethod.SetIsInputMethodSuspended(textView.VisualElement, true);
		}

		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldSnapshot != e.NewSnapshot)
				OnImplicitCaretPositionChanged();
			if (imeState.CompositionStarted)
				MoveImeCompositionWindow();
		}

		void VisualElement_GotKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) => InitializeIME();
		void VisualElement_LostKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) => StopIME(true);

		void CancelCompositionString() {
			if (imeState.Context == IntPtr.Zero)
				return;
			const int NI_COMPOSITIONSTR = 0x0015;
			const int CPS_CANCEL = 0x0004;
			ImeState.ImmNotifyIME(imeState.Context, NI_COMPOSITIONSTR, CPS_CANCEL, 0);
		}

		void InitializeIME() {
			if (imeState.HwndSource is not null)
				return;
			imeState.HwndSource = PresentationSource.FromVisual(textView.VisualElement) as HwndSource;
			if (imeState.HwndSource is null)
				return;

			Debug.Assert(imeState.Context == IntPtr.Zero);
			Debug.Assert(imeState.HWND == IntPtr.Zero);
			Debug.Assert(imeState.OldContext == IntPtr.Zero);
			if (textView.Options.DoesViewProhibitUserInput()) {
				imeState.Context = IntPtr.Zero;
				imeState.HWND = IntPtr.Zero;
			}
			else {
				imeState.HWND = ImeState.ImmGetDefaultIMEWnd(IntPtr.Zero);
				imeState.Context = ImeState.ImmGetContext(imeState.HWND);
			}
			imeState.OldContext = ImeState.ImmAssociateContext(imeState.HwndSource.Handle, imeState.Context);
			imeState.HwndSource.AddHook(WndProc);
			TfThreadMgrHelper.SetFocus();
		}

		void StopIME(bool cancelCompositionString) {
			if (imeState.HwndSource is null)
				return;
			if (cancelCompositionString)
				CancelCompositionString();
			ImeState.ImmAssociateContext(imeState.HwndSource.Handle, imeState.OldContext);
			ImeState.ImmReleaseContext(imeState.HWND, imeState.Context);
			imeState.HwndSource.RemoveHook(WndProc);
			imeState.Clear();
			textCaretLayer.SetImeStarted(false);
		}

		static class TfThreadMgrHelper {
			static bool initd;
			static ITfThreadMgr? tfThreadMgr;

			[DllImport("msctf")]
			static extern int TF_CreateThreadMgr(out ITfThreadMgr pptim);

			[ComImport, Guid("AA80E801-2021-11D2-93E0-0060B067B86E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			interface ITfThreadMgr {
				void _VtblGap0_5();
				[PreserveSig]
				int SetFocus(IntPtr pdimFocus);
			}

			public static void SetFocus() {
				if (!initd) {
					initd = true;
					TF_CreateThreadMgr(out tfThreadMgr);
				}
				tfThreadMgr?.SetFocus(IntPtr.Zero);
			}
		}

		sealed class ImeState {
			[DllImport("imm32")]
			public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
			[DllImport("imm32")]
			public static extern IntPtr ImmGetContext(IntPtr hWnd);
			[DllImport("imm32")]
			public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
			[DllImport("imm32")]
			public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
			[DllImport("imm32")]
			public static extern bool ImmNotifyIME(IntPtr hIMC, uint dwAction, uint dwIndex, uint dwValue);
			[DllImport("imm32")]
			public static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpCompForm);

			[StructLayout(LayoutKind.Sequential)]
			public struct COMPOSITIONFORM {
				public int dwStyle;
				public POINT ptCurrentPos;
				public RECT rcArea;
			}

			[StructLayout(LayoutKind.Sequential)]
			public class POINT {
				public int x;
				public int y;

				public POINT(int x, int y) {
					this.x = x;
					this.y = y;
				}
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT {
				public int left;
				public int top;
				public int right;
				public int bottom;

				public RECT(int left, int top, int right, int bottom) {
					this.left = left;
					this.top = top;
					this.right = right;
					this.bottom = bottom;
				}
			}

			public HwndSource? HwndSource;
			public IntPtr Context;
			public IntPtr HWND;
			public IntPtr OldContext;
			public bool CompositionStarted;

			public void Clear() {
				HwndSource = null;
				Context = IntPtr.Zero;
				HWND = IntPtr.Zero;
				OldContext = IntPtr.Zero;
				CompositionStarted = false;
			}
		}

		IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (textView.IsClosed)
				return IntPtr.Zero;
			const int WM_IME_STARTCOMPOSITION = 0x010D;
			const int WM_IME_ENDCOMPOSITION = 0x010E;
			const int WM_IME_COMPOSITION = 0x010F;
			if (msg == WM_IME_COMPOSITION) {
				MoveImeCompositionWindow();
				return IntPtr.Zero;
			}
			else if (msg == WM_IME_STARTCOMPOSITION) {
				Debug.Assert(!imeState.CompositionStarted);
				imeState.CompositionStarted = true;
				EnsureVisible();
				textCaretLayer.SetImeStarted(true);
				MoveImeCompositionWindow();
				return IntPtr.Zero;
			}
			else if (msg == WM_IME_ENDCOMPOSITION) {
				Debug.Assert(imeState.CompositionStarted);
				imeState.CompositionStarted = false;
				textCaretLayer.SetImeStarted(false);
				return IntPtr.Zero;
			}
			return IntPtr.Zero;
		}

		void MoveImeCompositionWindow() {
			if (imeState.Context == IntPtr.Zero)
				return;
			var line = ContainingTextViewLine;
			if (line.VisibilityState == VisibilityState.Unattached)
				return;
			var charBounds = line.GetExtendedCharacterBounds(currentPosition);

			const int CFS_DEFAULT = 0x0000;
			const int CFS_FORCE_POSITION = 0x0020;

			var compForm = new ImeState.COMPOSITIONFORM();
			compForm.dwStyle = CFS_DEFAULT;

			var rootVisual = imeState.HwndSource!.RootVisual;
			GeneralTransform? generalTransform = null;
			if (rootVisual is not null && rootVisual.IsAncestorOf(textView.VisualElement))
				generalTransform = textView.VisualElement.TransformToAncestor(rootVisual);

			var compTarget = imeState.HwndSource.CompositionTarget;
			if (generalTransform is not null && compTarget is not null) {
				var transform = compTarget.TransformToDevice;
				compForm.dwStyle = CFS_FORCE_POSITION;

				var caretPoint = transform.Transform(generalTransform.Transform(new Point(charBounds.Left - textView.ViewportLeft, charBounds.TextTop - textView.ViewportTop)));
				var viewPointTop = transform.Transform(generalTransform.Transform(new Point(0, 0)));
				var viewPointBottom = transform.Transform(generalTransform.Transform(new Point(textView.ViewportWidth, textView.ViewportHeight)));

				compForm.ptCurrentPos = new ImeState.POINT(Math.Max(0, (int)caretPoint.X), Math.Max(0, (int)caretPoint.Y));
				compForm.rcArea = new ImeState.RECT(
					Math.Max(0, (int)viewPointTop.X), Math.Max(0, (int)viewPointTop.Y),
					Math.Max(0, (int)viewPointBottom.X), Math.Max(0, (int)viewPointBottom.Y));
			}

			ImeState.ImmSetCompositionWindow(imeState.Context, ref compForm);
		}

		void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceName) {
				if (currentPosition.VirtualSpaces > 0 && textView.Selection.Mode != TextSelectionMode.Box && !textView.Options.IsVirtualSpaceEnabled())
					MoveTo(currentPosition.Position);
			}
			else if (e.OptionId == DefaultTextViewOptions.OverwriteModeName)
				textCaretLayer.OverwriteMode = textView.Options.IsOverwriteModeEnabled();
			else if (e.OptionId == DefaultTextViewOptions.ViewProhibitUserInputName) {
				StopIME(false);
				InitializeIME();
			}
		}

		void TextBuffer_ContentTypeChanged(object? sender, ContentTypeChangedEventArgs e) =>
			// The value is cached, make sure it uses the latest snapshot
			OnImplicitCaretPositionChanged();

		void TextBuffer_ChangedHighPriority(object? sender, TextContentChangedEventArgs e) {
			// The value is cached, make sure it uses the latest snapshot
			OnImplicitCaretPositionChanged();
			if (textView.Options.IsAutoScrollEnabled()) {
				// Delay this so we don't cause extra events to be raised inside the Changed event
				textView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(AutoScrollCaret));
			}
		}

		void AutoScrollCaret() {
			if (textView.IsClosed)
				return;
			if (!textView.Options.IsAutoScrollEnabled())
				return;
			var line = ContainingTextViewLine;
			if (line.IsLastDocumentLine()) {
				MoveTo(line.End);
				if (textView.ViewportHeight != 0)
					EnsureVisible();
			}
		}

		void OnImplicitCaretPositionChanged() => SetPositionCore(currentPosition.TranslateTo(textView.TextSnapshot));

		void SetPositionCore(VirtualSnapshotPoint bufferPosition) {
			if (currentPosition != bufferPosition) {
				currentPosition = bufferPosition;
				textCaretLayer.CaretPositionChanged();
				if (imeState.CompositionStarted)
					MoveImeCompositionWindow();
			}
		}

		void SetExplicitPosition(VirtualSnapshotPoint bufferPosition) {
			var oldPos = Position;
			SetPositionCore(bufferPosition);
			var newPos = Position;
			if (oldPos != newPos)
				PositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(textView, oldPos, newPos));
		}

		public void EnsureVisible() {
			var line = ContainingTextViewLine;
			if (line.VisibilityState != VisibilityState.FullyVisible) {
				ViewRelativePosition relativeTo;
				var firstVisibleLine = textView.TextViewLines?.FirstVisibleLine;
				if (firstVisibleLine is null || !firstVisibleLine.IsVisible())
					relativeTo = ViewRelativePosition.Top;
				else if (line.Start.Position <= firstVisibleLine.Start.Position)
					relativeTo = ViewRelativePosition.Top;
				else
					relativeTo = ViewRelativePosition.Bottom;
				textView.DisplayTextLineContainingBufferPosition(line.Start, 0, relativeTo);
			}

			double left = textCaretLayer.Left;
			double right = textCaretLayer.Right;

			double availWidth = Math.Max(0, textView.ViewportWidth - textCaretLayer.Width);
			double extraScroll;
			if (availWidth >= WpfTextViewConstants.EXTRA_HORIZONTAL_WIDTH)
				extraScroll = WpfTextViewConstants.EXTRA_HORIZONTAL_WIDTH;
			else
				extraScroll = availWidth / 2;
			if (textView.ViewportWidth == 0) {
				// Don't do anything if there's zero width. This can happen during
				// startup when code accesses the caret before the window is shown.
			}
			else if (left < textView.ViewportLeft)
				textView.ViewportLeft = left - extraScroll;
			else if (right > textView.ViewportRight)
				textView.ViewportLeft = right + extraScroll - textView.ViewportWidth;
		}

		bool CanAutoIndent(ITextViewLine line) {
			if (line.Start != line.End)
				return false;
			if (textView.Options.IsVirtualSpaceEnabled())
				return false;
			if (textView.Selection.Mode != TextSelectionMode.Stream)
				return false;

			return true;
		}

		VirtualSnapshotPoint FilterColumn(VirtualSnapshotPoint pos) {
			if (!pos.IsInVirtualSpace)
				return pos;
			if (textView.Options.IsVirtualSpaceEnabled())
				return pos;
			if (textView.Selection.Mode != TextSelectionMode.Stream)
				return pos;
			return new VirtualSnapshotPoint(pos.Position);
		}

		public CaretPosition MoveTo(ITextViewLine textLine) =>
			MoveTo(textLine, preferredXCoordinate, false, true, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate) =>
			MoveTo(textLine, xCoordinate, true, true, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition) =>
			MoveTo(textLine, xCoordinate, captureHorizontalPosition, true, true);
		CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition, bool captureVerticalPosition, bool canAutoIndent) {
			if (textLine is null)
				throw new ArgumentNullException(nameof(textLine));

			bool filterPos = true;
			// Don't auto indent if it's at column 0
			if (canAutoIndent && CanAutoIndent(textLine) && xCoordinate > textLine.TextRight) {
				if (textView is IWpfTextView wpfView) {
					int indentation = IndentHelper.GetDesiredIndentation(textView, smartIndentationService, textLine.Start.GetContainingLine()) ?? 0;
					var textBounds = textLine.GetExtendedCharacterBounds(new VirtualSnapshotPoint(textLine.Start, indentation));
					xCoordinate = textBounds.Leading;
					filterPos = false;
				}
			}

			var bufferPosition = textLine.GetInsertionBufferPositionFromXCoordinate(xCoordinate);
			Affinity = textLine.IsLastTextViewLineForSnapshotLine || bufferPosition.Position != textLine.End ? PositionAffinity.Successor : PositionAffinity.Predecessor;
			if (filterPos)
				bufferPosition = FilterColumn(bufferPosition);
			SetExplicitPosition(bufferPosition);
			if (captureHorizontalPosition)
				preferredXCoordinate = Left;
			if (captureVerticalPosition)
				SavePreferredYCoordinate();
			return Position;
		}

		public CaretPosition MoveTo(SnapshotPoint bufferPosition) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition));
		public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity);
		public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition);

		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition) =>
			MoveTo(bufferPosition, PositionAffinity.Successor);
		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity) =>
			MoveTo(bufferPosition, caretAffinity, true);
		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition) {
			if (bufferPosition.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();

			Affinity = caretAffinity;
			// Don't call FilterColumn() or pressing END on an empty line won't indent it to a virtual column
			//bufferPosition = FilterColumn(bufferPosition);
			SetExplicitPosition(bufferPosition);
			if (captureHorizontalPosition)
				preferredXCoordinate = Left;
			SavePreferredYCoordinate();
			return Position;
		}

		public CaretPosition MoveToNextCaretPosition() {
			if (textView.Options.IsVirtualSpaceEnabled() || textView.Selection.Mode == TextSelectionMode.Box) {
				bool useVirtSpaces;
				if (currentPosition.VirtualSpaces > 0)
					useVirtSpaces = true;
				else {
					var snapshotLine = currentPosition.Position.GetContainingLine();
					useVirtSpaces = currentPosition.Position >= snapshotLine.End;
				}
				if (useVirtSpaces) {
					if (currentPosition.VirtualSpaces != int.MaxValue)
						return MoveTo(new VirtualSnapshotPoint(currentPosition.Position, currentPosition.VirtualSpaces + 1));
					return Position;
				}
			}
			if (currentPosition.Position.Position == currentPosition.Position.Snapshot.Length)
				return Position;

			var line = textView.GetTextViewLineContainingBufferPosition(currentPosition.Position);
			var span = line.GetTextElementSpan(currentPosition.Position);
			return MoveTo(span.End);
		}

		public CaretPosition MoveToPreviousCaretPosition() {
			if (currentPosition.VirtualSpaces > 0 && (textView.Options.IsVirtualSpaceEnabled() || textView.Selection.Mode == TextSelectionMode.Box))
				return MoveTo(new VirtualSnapshotPoint(currentPosition.Position, currentPosition.VirtualSpaces - 1));
			if (currentPosition.Position.Position == 0)
				return Position;

			var currentLine = textView.GetTextViewLineContainingBufferPosition(currentPosition.Position);
			var span = currentLine.GetTextElementSpan(currentPosition.Position);
			var newPos = span.Start;
			if (currentPosition.VirtualSpaces == 0 && newPos.Position != 0) {
				newPos -= 1;
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line.IsLastTextViewLineForSnapshotLine && newPos > line.End)
					newPos = line.End;
				newPos = line.GetTextElementSpan(newPos).Start;
			}
			if (textView.Options.IsVirtualSpaceEnabled() || textView.Selection.Mode == TextSelectionMode.Box) {
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line.ExtentIncludingLineBreak != currentLine.ExtentIncludingLineBreak)
					newPos = currentLine.Start;
			}
			return MoveTo(newPos);
		}

		double PreferredYCoordinate => Math.Min(__preferredYCoordinate, textView.ViewportHeight) + textView.ViewportTop;
		double __preferredYCoordinate;

		ITextViewLine? GetVisibleCaretLine() {
			if (textView.TextViewLines is null)
				return null;
			var line = ContainingTextViewLine;
			if (line.IsVisible())
				return line;
			var firstVisLine = textView.TextViewLines.FirstVisibleLine;
			// Don't return FirstVisibleLine/LastVisibleLine since they will return a hidden line if it fails to find a visible line
			if (line.Start.TranslateTo(textView.TextSnapshot, PointTrackingMode.Negative) <= firstVisLine.Start.TranslateTo(textView.TextSnapshot, PointTrackingMode.Negative))
				return textView.TextViewLines.FirstOrDefault(a => a.IsVisible());
			return textView.TextViewLines.LastOrDefault(a => a.IsVisible());
		}

		void SavePreferredYCoordinate() {
			var line = GetVisibleCaretLine();
			if (line is not null)
				__preferredYCoordinate = (line.Top + line.Bottom) / 2 - textView.ViewportTop;
			else
				__preferredYCoordinate = 0;
		}

		public CaretPosition MoveToPreferredCoordinates() {
			var textLine = textView.TextViewLines.GetTextViewLineContainingYCoordinate(PreferredYCoordinate);
			if (textLine is null || !textLine.IsVisible())
				textLine = PreferredYCoordinate <= textView.ViewportTop ? textView.TextViewLines.FirstVisibleLine : textView.TextViewLines.LastVisibleLine;
			return MoveTo(textLine, preferredXCoordinate, false, false, true);
		}

		ITextViewLine? GetLine(SnapshotPoint bufferPosition, PositionAffinity affinity) {
			bufferPosition = bufferPosition.TranslateTo(textView.TextSnapshot, GetPointTrackingMode(affinity));
			var line = textView.GetTextViewLineContainingBufferPosition(bufferPosition);
			if (line is null)
				return null;
			if (affinity == PositionAffinity.Successor)
				return line;
			if (line.Start.Position == 0 || line.Start != bufferPosition)
				return line;
			if (bufferPosition.GetContainingLine().Start == bufferPosition)
				return line;
			return textView.GetTextViewLineContainingBufferPosition(bufferPosition - 1);
		}

		static PointTrackingMode GetPointTrackingMode(PositionAffinity affinity) => affinity == PositionAffinity.Predecessor ? PointTrackingMode.Negative : PointTrackingMode.Positive;

		public void Dispose() {
			StopIME(true);
			textView.TextBuffer.ChangedHighPriority -= TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			textView.Options.OptionChanged -= Options_OptionChanged;
			textView.VisualElement.GotKeyboardFocus -= VisualElement_GotKeyboardFocus;
			textView.VisualElement.LostKeyboardFocus -= VisualElement_LostKeyboardFocus;
			textView.LayoutChanged -= TextView_LayoutChanged;
			textCaretLayer.Dispose();
		}
	}
}
