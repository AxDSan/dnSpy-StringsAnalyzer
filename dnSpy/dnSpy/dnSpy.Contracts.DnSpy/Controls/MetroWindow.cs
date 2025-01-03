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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// The window class used by all dnSpy windows
	/// </summary>
	public class MetroWindow : Window {
		/// <summary>
		/// Full screen command
		/// </summary>
		public static readonly RoutedCommand FullScreenCommand = new RoutedCommand("FullScreen", typeof(MetroWindow));

		/// <summary>
		/// Raised when a new <see cref="MetroWindow"/> instance has been created
		/// </summary>
		internal static event EventHandler<MetroWindowCreatedEventArgs>? MetroWindowCreated;

		/// <summary>
		/// Constructor
		/// </summary>
		public MetroWindow() {
			SetValue(WindowChrome.WindowChromeProperty, CreateWindowChromeObject());
			// Since the system menu had to be disabled, we must add this command
			var cmd = new RelayCommand(a => ShowSystemMenu(this), a => !IsFullScreen);
			InputBindings.Add(new KeyBinding(cmd, Key.Space, ModifierKeys.Alt));
			MetroWindowCreated?.Invoke(this, new MetroWindowCreatedEventArgs(this));
		}

		/// <inheritdoc/>
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);

			var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			Debug2.Assert(hwndSource is not null);
			if (hwndSource is not null) {
				hwndSource.AddHook(WndProc);
				wpfDpi = new Size(96.0 * hwndSource.CompositionTarget.TransformToDevice.M11, 96.0 * hwndSource.CompositionTarget.TransformToDevice.M22);

				var w = Width;
				var h = Height;
				WindowDpi = GetDpi(hwndSource.Handle) ?? wpfDpi;
			}

			WindowUtils.UpdateWin32Style(this);
		}

		static bool? canCall_GetDpi = null;
		static Size? GetDpi(IntPtr hWnd) {
			if (canCall_GetDpi == false)
				return null;
			try {
				var res = GetDpi_Win81(hWnd);
				canCall_GetDpi = true;
				return res;
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canCall_GetDpi = false;
			return null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static Size GetDpi_Win81(IntPtr hWnd) {
			const int MONITOR_DEFAULTTONEAREST = 0x00000002;
			var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
			const int MDT_EFFECTIVE_DPI = 0;
			int hr = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out int dpiX, out int dpiY);
			Debug.Assert(hr == 0);
			if (hr != 0)
				return new Size(96, 96);
			return new Size(dpiX, dpiY);
		}

		struct RECT {
			public int left, top, right, bottom;
			RECT(bool dummy) => left = top = right = bottom = 0;// disable compiler warning
		}

		[DllImport("user32", SetLastError = true)]
		static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
		[DllImport("shcore", SetLastError = true)]
		static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out int dpiX, out int dpiY);

		int WM_DPICHANGED_counter = 0;
		IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			const int WM_DPICHANGED = 0x02E0;
			const int WM_MOUSEHWHEEL = 0x020E;

			if (msg == WM_DPICHANGED) {
				if (WM_DPICHANGED_counter != 0)
					return IntPtr.Zero;
				WM_DPICHANGED_counter++;
				try {
					int newDpiY = (ushort)(wParam.ToInt64() >> 16);
					int newDpiX = (ushort)wParam.ToInt64();

					WindowDpi = new Size(newDpiX, newDpiY);

					return IntPtr.Zero;
				}
				finally {
					WM_DPICHANGED_counter--;
				}
			}

			if (msg == WM_MOUSEHWHEEL) {
				short delta = (short)unchecked((uint)(long)wParam >> 16);
				if (delta == 0) {
					return IntPtr.Zero;
				}

				var element = Mouse.DirectlyOver;
				if (element is null)
					return IntPtr.Zero;

				handled = HandleHoriztonalScroll(element, (short)(delta / 10));

				return IntPtr.Zero;
			}

			return IntPtr.Zero;
		}

		private protected virtual bool HandleHoriztonalScroll(IInputElement element, short delta) {
			ScrollViewer? scrollViewer = null;
			if (element is ScrollViewer scViewer) {
				scrollViewer = scViewer;
			}
			else if (element is DependencyObject dependencyObject) {
				var current = UIUtilities.GetParent(dependencyObject);

				while (current != null) {
					if (current is ScrollViewer newScrollViewer) {
						scrollViewer = newScrollViewer;
						break;
					}
					current = UIUtilities.GetParent(current);
				}
			}

			if (scrollViewer is not null) {
				scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + delta);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the DPI
		/// </summary>
		public Size WindowDpi {
			get => windowDpi;
			private set {
				if (windowDpi != value) {
					windowDpi = value;
					SetTextFormattingMode(this, 1.0);
					WindowDpiChanged?.Invoke(this, EventArgs.Empty);
					DsImage.SetDpi(this, windowDpi.Width);
				}
			}
		}
		Size windowDpi;
		Size wpfDpi;

		/// <summary>
		/// Raised when the DPI (<see cref="WindowDpi"/>) has changed
		/// </summary>
		public event EventHandler? WindowDpiChanged;

		/// <summary>
		/// Show system menu command
		/// </summary>
		public static ICommand ShowSystemMenuCommand => new RelayCommand(a => ShowSystemMenu(a), a => true);

		/// <summary>
		/// Raised when full screen state has changed
		/// </summary>
		public event EventHandler? IsFullScreenChanged;

		/// <summary>
		/// Is full screen property
		/// </summary>
		public static readonly DependencyProperty IsFullScreenProperty =
			DependencyProperty.Register(nameof(IsFullScreen), typeof(bool), typeof(MetroWindow),
			new FrameworkPropertyMetadata(false, OnIsFullScreenChanged));

		/// <summary>
		/// Gets/sets the full screen state
		/// </summary>
		public bool IsFullScreen {
			get => (bool)GetValue(IsFullScreenProperty);
			set => SetValue(IsFullScreenProperty, value);
		}

		static void OnIsFullScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var window = (MetroWindow)d;
			var wc = (WindowChrome)window.GetValue(WindowChrome.WindowChromeProperty);
			if (window.IsFullScreen)
				window.InitializeWindowCaptionAndResizeBorder(wc, false);
			else
				window.InitializeWindowCaptionAndResizeBorder(wc);

			window.IsFullScreenChanged?.Invoke(window, EventArgs.Empty);
		}

		/// <summary>
		/// System menu image property
		/// </summary>
		public static readonly DependencyProperty SystemMenuImageProperty =
			DependencyProperty.Register(nameof(SystemMenuImage), typeof(ImageReference), typeof(MetroWindow),
			new FrameworkPropertyMetadata(default(ImageReference)));

		/// <summary>
		/// Gets/sets the system menu image
		/// </summary>
		public ImageReference SystemMenuImage {
			get => (ImageReference)GetValue(SystemMenuImageProperty);
			set => SetValue(SystemMenuImageProperty, value);
		}

		/// <summary>
		/// Maximized element property
		/// </summary>
		public static readonly DependencyProperty MaximizedElementProperty = DependencyProperty.RegisterAttached(
			"MaximizedElement", typeof(bool), typeof(MetroWindow), new UIPropertyMetadata(false, OnMaximizedElementChanged));

		static void OnMaximizedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var border = d as Border;
			Debug2.Assert(border is not null);
			if (border is null)
				return;
			var win = Window.GetWindow(border) as MetroWindow;
			if (win is null)
				return;

			new MaximizedWindowFixer(win, border);
		}

		// When the window is maximized, the part of the window where the (in our case, hidden) resize
		// border is located, is hidden. Add a padding to a border element whose value exactly equals
		// the border width and reset it when it's not maximized.
		sealed class MaximizedWindowFixer {
			readonly Border border;
			readonly Thickness oldThickness;
			readonly MetroWindow metroWindow;

			public MaximizedWindowFixer(MetroWindow metroWindow, Border border) {
				this.border = border;
				oldThickness = border.BorderThickness;
				this.metroWindow = metroWindow;
				metroWindow.StateChanged += MetroWindow_StateChanged;
				border.Loaded += border_Loaded;
			}

			void border_Loaded(object? sender, RoutedEventArgs e) {
				border.Loaded -= border_Loaded;
				UpdatePadding(metroWindow);
			}

			void MetroWindow_StateChanged(object? sender, EventArgs e) => UpdatePadding((MetroWindow)sender!);

			void UpdatePadding(MetroWindow window) {
				Debug2.Assert(window is not null);

				var state = window.IsFullScreen ? WindowState.Maximized : window.WindowState;
				switch (state) {
				default:
				case WindowState.Normal:
					border.ClearValue(Border.PaddingProperty);
					border.BorderThickness = oldThickness;
					break;

				case WindowState.Minimized:
				case WindowState.Maximized:
					double magicx, magicy;

					magicx = magicy = 10;//TODO: Figure out how this value is calculated (it's not ResizeBorderThickness.Left/Top)

					double deltax = magicx - SystemParameters.ResizeFrameVerticalBorderWidth;
					double deltay = magicy - SystemParameters.ResizeFrameHorizontalBorderHeight;
					Debug.Assert(deltax >= 0 && deltay >= 0);
					if (deltax < 0)
						deltax = 0;
					if (deltay < 0)
						deltay = 0;

					border.Padding = new Thickness(
						SystemParameters.BorderWidth + border.BorderThickness.Left + deltax,
						SystemParameters.BorderWidth + border.BorderThickness.Top + deltay,
						SystemParameters.BorderWidth + border.BorderThickness.Right + deltax,
						SystemParameters.BorderWidth + border.BorderThickness.Bottom + deltay);
					border.BorderThickness = new Thickness(0);
					break;
				}
			}
		}

		/// <summary>
		/// Sets the maximized-element value
		/// </summary>
		/// <param name="element">Element</param>
		/// <param name="value">New value</param>
		public static void SetMaximizedElement(UIElement element, bool value) => element.SetValue(MaximizedElementProperty, value);

		/// <summary>
		/// Gets the maximized-element value
		/// </summary>
		/// <param name="element">Element</param>
		/// <returns></returns>
		public static bool GetMaximizedElement(UIElement element) => (bool)element.GetValue(MaximizedElementProperty);

		/// <summary>
		/// Use resize border property
		/// </summary>
		public static readonly DependencyProperty UseResizeBorderProperty =
			DependencyProperty.Register(nameof(UseResizeBorder), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true, OnUseResizeBorderChanged));

		/// <summary>
		/// Gets/sets whether a resize border should be used
		/// </summary>
		public bool UseResizeBorder {
			get => (bool)GetValue(UseResizeBorderProperty);
			set => SetValue(UseResizeBorderProperty, value);
		}

		static void OnUseResizeBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var win = (MetroWindow)d;
			var wc = (WindowChrome)win.GetValue(WindowChrome.WindowChromeProperty);
			if (wc is null)
				return;

			win.InitializeWindowCaptionAndResizeBorder(wc);
		}

		/// <summary>
		/// Sets the use-resize-border value
		/// </summary>
		/// <param name="element">Element</param>
		/// <param name="value">New value</param>
		public static void SetUseResizeBorder(UIElement element, bool value) => element.SetValue(UseResizeBorderProperty, value);

		/// <summary>
		/// Gets the use-resize-border value
		/// </summary>
		/// <param name="element">Element</param>
		/// <returns></returns>
		public static bool GetUseResizeBorder(UIElement element) => (bool)element.GetValue(UseResizeBorderProperty);

		/// <summary>
		/// Is debugging property
		/// </summary>
		public static readonly DependencyProperty IsDebuggingProperty =
			DependencyProperty.Register(nameof(IsDebugging), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Gets/sets whether debugging mode is enabled
		/// </summary>
		public bool IsDebugging {
			get => (bool)GetValue(IsDebuggingProperty);
			set => SetValue(IsDebuggingProperty, value);
		}

		/// <summary>
		/// Active caption property
		/// </summary>
		public static readonly DependencyProperty ActiveCaptionProperty =
			DependencyProperty.Register(nameof(ActiveCaption), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Active caption text property
		/// </summary>
		public static readonly DependencyProperty ActiveCaptionTextProperty =
			DependencyProperty.Register(nameof(ActiveCaptionText), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Active debugging border property
		/// </summary>
		public static readonly DependencyProperty ActiveDebuggingBorderProperty =
			DependencyProperty.Register(nameof(ActiveDebuggingBorder), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Active default border property
		/// </summary>
		public static readonly DependencyProperty ActiveDefaultBorderProperty =
			DependencyProperty.Register(nameof(ActiveDefaultBorder), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Inactive border property
		/// </summary>
		public static readonly DependencyProperty InactiveBorderProperty =
			DependencyProperty.Register(nameof(InactiveBorder), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Inactive caption property
		/// </summary>
		public static readonly DependencyProperty InactiveCaptionProperty =
			DependencyProperty.Register(nameof(InactiveCaption), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Inactive caption text property
		/// </summary>
		public static readonly DependencyProperty InactiveCaptionTextProperty =
			DependencyProperty.Register(nameof(InactiveCaptionText), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Button inactive border property
		/// </summary>
		public static readonly DependencyProperty ButtonInactiveBorderProperty =
			DependencyProperty.Register(nameof(ButtonInactiveBorder), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Button inactive glyph property
		/// </summary>
		public static readonly DependencyProperty ButtonInactiveGlyphProperty =
			DependencyProperty.Register(nameof(ButtonInactiveGlyph), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Button hover inactive property
		/// </summary>
		public static readonly DependencyProperty ButtonHoverInactiveProperty =
			DependencyProperty.Register(nameof(ButtonHoverInactive), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Button hover inactive border property
		/// </summary>
		public static readonly DependencyProperty ButtonHoverInactiveBorderProperty =
			DependencyProperty.Register(nameof(ButtonHoverInactiveBorder), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Button hover inactive glyph property
		/// </summary>
		public static readonly DependencyProperty ButtonHoverInactiveGlyphProperty =
			DependencyProperty.Register(nameof(ButtonHoverInactiveGlyph), typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		/// <summary>
		/// Gets/sets the active caption brush
		/// </summary>
		public Brush ActiveCaption {
			get => (Brush)GetValue(ActiveCaptionProperty);
			set => SetValue(ActiveCaptionProperty, value);
		}

		/// <summary>
		/// Gets/sets the active caption text brush
		/// </summary>
		public Brush ActiveCaptionText {
			get => (Brush)GetValue(ActiveCaptionTextProperty);
			set => SetValue(ActiveCaptionTextProperty, value);
		}

		/// <summary>
		/// Gets/sets the active debugging border brush
		/// </summary>
		public Brush ActiveDebuggingBorder {
			get => (Brush)GetValue(ActiveDebuggingBorderProperty);
			set => SetValue(ActiveDebuggingBorderProperty, value);
		}

		/// <summary>
		/// Gets/sets the active default border brush
		/// </summary>
		public Brush ActiveDefaultBorder {
			get => (Brush)GetValue(ActiveDefaultBorderProperty);
			set => SetValue(ActiveDefaultBorderProperty, value);
		}

		/// <summary>
		/// Gets/sets the inactive border brush
		/// </summary>
		public Brush InactiveBorder {
			get => (Brush)GetValue(InactiveBorderProperty);
			set => SetValue(InactiveBorderProperty, value);
		}

		/// <summary>
		/// Gets/sets the inactive caption brush
		/// </summary>
		public Brush InactiveCaption {
			get => (Brush)GetValue(InactiveCaptionProperty);
			set => SetValue(InactiveCaptionProperty, value);
		}

		/// <summary>
		/// Gets/sets the inactive caption text brush
		/// </summary>
		public Brush InactiveCaptionText {
			get => (Brush)GetValue(InactiveCaptionTextProperty);
			set => SetValue(InactiveCaptionTextProperty, value);
		}

		/// <summary>
		/// Gets/sets the button inactive border brush
		/// </summary>
		public Brush ButtonInactiveBorder {
			get => (Brush)GetValue(ButtonInactiveBorderProperty);
			set => SetValue(ButtonInactiveBorderProperty, value);
		}

		/// <summary>
		/// Gets/stes the button inactive glyph brush
		/// </summary>
		public Brush ButtonInactiveGlyph {
			get => (Brush)GetValue(ButtonInactiveGlyphProperty);
			set => SetValue(ButtonInactiveGlyphProperty, value);
		}

		/// <summary>
		/// Gets/sets the button hover inactive brush
		/// </summary>
		public Brush ButtonHoverInactive {
			get => (Brush)GetValue(ButtonHoverInactiveProperty);
			set => SetValue(ButtonHoverInactiveProperty, value);
		}

		/// <summary>
		/// Gets/sets the button hover inactive border brush
		/// </summary>
		public Brush ButtonHoverInactiveBorder {
			get => (Brush)GetValue(ButtonHoverInactiveBorderProperty);
			set => SetValue(ButtonHoverInactiveBorderProperty, value);
		}

		/// <summary>
		/// Gets/sets the button hover inactive glyph brush
		/// </summary>
		public Brush ButtonHoverInactiveGlyph {
			get => (Brush)GetValue(ButtonHoverInactiveGlyphProperty);
			set => SetValue(ButtonHoverInactiveGlyphProperty, value);
		}

		/// <summary>
		/// Show menu button property
		/// </summary>
		public static readonly DependencyProperty ShowMenuButtonProperty =
			DependencyProperty.Register(nameof(ShowMenuButton), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));

		/// <summary>
		/// Show minimize button property
		/// </summary>
		public static readonly DependencyProperty ShowMinimizeButtonProperty =
			DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));

		/// <summary>
		/// Show maximize button property
		/// </summary>
		public static readonly DependencyProperty ShowMaximizeButtonProperty =
			DependencyProperty.Register(nameof(ShowMaximizeButton), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));

		/// <summary>
		/// Show close button property
		/// </summary>
		public static readonly DependencyProperty ShowCloseButtonProperty =
			DependencyProperty.Register(nameof(ShowCloseButton), typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));

		/// <summary>
		/// Gets/sets whether to show the menu button
		/// </summary>
		public bool ShowMenuButton {
			get => (bool)GetValue(ShowMenuButtonProperty);
			set => SetValue(ShowMenuButtonProperty, value);
		}

		/// <summary>
		/// Gets/sets whether to show the minimize button
		/// </summary>
		public bool ShowMinimizeButton {
			get => (bool)GetValue(ShowMinimizeButtonProperty);
			set => SetValue(ShowMinimizeButtonProperty, value);
		}

		/// <summary>
		/// Gets/sets whether to show the maximize button
		/// </summary>
		public bool ShowMaximizeButton {
			get => (bool)GetValue(ShowMaximizeButtonProperty);
			set => SetValue(ShowMaximizeButtonProperty, value);
		}

		/// <summary>
		/// Gets/sets whether to show the close button
		/// </summary>
		public bool ShowCloseButton {
			get => (bool)GetValue(ShowCloseButtonProperty);
			set => SetValue(ShowCloseButtonProperty, value);
		}

		static MetroWindow() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroWindow), new FrameworkPropertyMetadata(typeof(MetroWindow)));

		// If these get updated, also update the templates if necessary
		static readonly CornerRadius CornerRadius = new CornerRadius(0, 0, 0, 0);
		static readonly Thickness GlassFrameThickness = new Thickness(0);
		// NOTE: Keep these in sync: CaptionHeight + ResizeBorderThickness.Top = GridCaptionHeight
		static readonly double CaptionHeight = 20;
		static readonly Thickness ResizeBorderThickness = new Thickness(10, 10, 5, 5);
		/// <summary>
		/// Gets the grid caption height
		/// </summary>
		public static readonly GridLength GridCaptionHeight = new GridLength(CaptionHeight + ResizeBorderThickness.Top, GridUnitType.Pixel);

		/// <inheritdoc/>
		protected override void OnStateChanged(EventArgs e) {
			base.OnStateChanged(e);
			if (WindowState == WindowState.Normal)
				ClearValue(Window.WindowStateProperty);

			var wc = (WindowChrome)GetValue(WindowChrome.WindowChromeProperty);
			switch (WindowState) {
			case WindowState.Normal:
				InitializeWindowCaptionAndResizeBorder(wc);
				ClearValue(Window.WindowStateProperty);
				break;

			case WindowState.Minimized:
			case WindowState.Maximized:
				InitializeWindowCaptionAndResizeBorder(wc, false);
				break;

			default:
				break;
			}
		}

		WindowChrome CreateWindowChromeObject() {
			var wc = new WindowChrome {
				CornerRadius = CornerRadius,
				GlassFrameThickness = GlassFrameThickness,
				NonClientFrameEdges = NonClientFrameEdges.None,
			};
			InitializeWindowCaptionAndResizeBorder(wc);
			return wc;
		}

		void InitializeWindowCaptionAndResizeBorder(WindowChrome wc) => InitializeWindowCaptionAndResizeBorder(wc, UseResizeBorder);

		void InitializeWindowCaptionAndResizeBorder(WindowChrome wc, bool useResizeBorder) {
			var scale = 1.0;
			if (useResizeBorder) {
				wc.CaptionHeight = CaptionHeight * scale;
				wc.ResizeBorderThickness = new Thickness(
					ResizeBorderThickness.Left * scale,
					ResizeBorderThickness.Top * scale,
					ResizeBorderThickness.Right * scale,
					ResizeBorderThickness.Bottom * scale);
			}
			else {
				if (IsFullScreen)
					wc.CaptionHeight = 0d;
				else
					wc.CaptionHeight = GridCaptionHeight.Value * scale;
				wc.ResizeBorderThickness = new Thickness(0);
			}
		}

		static void ShowSystemMenu(object? o) {
			var depo = o as DependencyObject;
			if (depo is null)
				return;
			var win = Window.GetWindow(depo);
			if (win is null)
				return;

			var scale = 1.0;
			var p = win.PointToScreen(new Point(0 * scale, GridCaptionHeight.Value * scale));
			WindowUtils.ShowSystemMenu(win, p);
		}

		/// <summary>
		/// Sets the scale transform
		/// </summary>
		/// <param name="target">Target that gets the scale transform</param>
		/// <param name="scale">Scale to use where 1.0 is 100%</param>
		public void SetScaleTransform(DependencyObject? target, double scale) => SetScaleTransform(target, target, scale);

		void SetScaleTransform(DependencyObject? textObj, DependencyObject? vc, double scale) {
			Debug.Assert(textObj != this);
			if (vc is null || textObj is null)
				return;

			if (scale == 1)
				vc.SetValue(LayoutTransformProperty, Transform.Identity);
			else {
				var st = new ScaleTransform(scale, scale);
				st.Freeze();
				vc.SetValue(LayoutTransformProperty, st);
			}

			SetTextFormattingMode(textObj, scale);
		}

		void SetTextFormattingMode(DependencyObject textObj, double scale) {
			if (scale == 1) {
				if (textObj is Window)
					TextOptions.SetTextFormattingMode(textObj, TextFormattingMode.Display);
				else {
					// Inherit the property. We assume that the root element has TextFormattingMode.Display.
					// We shouldn't set it to Display since this UI element could be inside a parent that
					// has been zoomed. We must inherit its Ideal mode, and not force Display mode.
					textObj.ClearValue(TextOptions.TextFormattingModeProperty);
				}
			}
			else {
				// We must set it to Ideal or the text will be blurry
				TextOptions.SetTextFormattingMode(textObj, TextFormattingMode.Ideal);
			}
		}
	}

	static class WindowUtils {
		[DllImport("user32")]
		static extern bool IsWindow(IntPtr hWnd);
		[DllImport("user32")]
		static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32")]
		static extern uint TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
		[DllImport("user32")]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32")]
		extern static int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32")]
		extern static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		internal static void UpdateWin32Style(MetroWindow window) {
			const int GWL_STYLE = -16;
			const int WS_SYSMENU = 0x00080000;

			IntPtr hWnd = new WindowInteropHelper(window).Handle;

			// The whole title bar is restyled. We must hide the system menu or Windows
			// will sometimes paint the title bar for us.
			SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		internal static void ShowSystemMenu(Window window, Point p) {
			var hWnd = new WindowInteropHelper(window).Handle;
			if (hWnd == IntPtr.Zero)
				return;
			if (!IsWindow(hWnd))
				return;

			var hMenu = GetSystemMenu(hWnd, false);
			uint res = TrackPopupMenuEx(hMenu, 0x100, (int)p.X, (int)p.Y, hWnd, IntPtr.Zero);
			if (res != 0)
				PostMessage(hWnd, 0x112, IntPtr.Size == 4 ? new IntPtr((int)res) : new IntPtr(res), IntPtr.Zero);
		}

		internal static void SetState(Window window, WindowState state) {
			switch (state) {
			case WindowState.Normal:
				Restore(window);
				break;

			case WindowState.Minimized:
				Minimize(window);
				break;

			case WindowState.Maximized:
				Maximize(window);
				break;
			}
		}

		internal static void Minimize(Window window) => window.WindowState = WindowState.Minimized;
		internal static void Maximize(Window window) => window.WindowState = WindowState.Maximized;
		internal static void Restore(Window window) => window.ClearValue(Window.WindowStateProperty);
	}
}
