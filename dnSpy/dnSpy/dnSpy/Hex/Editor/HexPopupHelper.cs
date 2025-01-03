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

using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Controls;

namespace dnSpy.Hex.Editor {
	static class HexPopupHelper {
		const double maxHeightMultiplier = 0.8;
		const double maxWidthMultiplier = 0.8;

		public static Size GetMaxSize(WpfHexView wpfHexView) {
			var screen = new Screen(wpfHexView.VisualElement);
			var screenRect = screen.IsValid ? screen.DisplayRect : SystemParameters.WorkArea;
			var size = TransformFromDevice(wpfHexView, screenRect.Size);
			return new Size(size.Width * maxWidthMultiplier, size.Height * maxHeightMultiplier);
		}

		public static void SetScaleTransform(WpfHexView wpfHexView, FrameworkElement popupElement) {
			if (wpfHexView is null)
				return;
			var metroWindow = Window.GetWindow(wpfHexView.VisualElement) as MetroWindow;
			if (metroWindow is null)
				return;
			metroWindow.SetScaleTransform(popupElement, wpfHexView.ZoomLevel / 100);

			var maxSize = GetMaxSize(wpfHexView);
			popupElement.MaxWidth = maxSize.Width;
			popupElement.MaxHeight = maxSize.Height;
		}

		public static Size TransformFromDevice(WpfHexView wpfHexView, Size size) {
			var zoomMultiplier = wpfHexView.ZoomLevel == 0 ? 1 : 100 / wpfHexView.ZoomLevel;
			var source = PresentationSource.FromVisual(wpfHexView.VisualElement);
			var transformFromDevice = source?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
			var wpfRect = transformFromDevice.Transform(new Point(size.Width, size.Height));
			var width = wpfRect.X * zoomMultiplier;
			var height = wpfRect.Y * zoomMultiplier;
			return new Size(width, height);
		}

		public static Size TransformToDevice(WpfHexView wpfHexView, Size size) {
			var zoomMultiplier = wpfHexView.ZoomLevel == 0 ? 1 : wpfHexView.ZoomLevel / 100;
			var source = PresentationSource.FromVisual(wpfHexView.VisualElement);
			var transformToDevice = source?.CompositionTarget.TransformToDevice ?? Matrix.Identity;
			var wpfRect = transformToDevice.Transform(new Point(size.Width, size.Height));
			var width = wpfRect.X * zoomMultiplier;
			var height = wpfRect.Y * zoomMultiplier;
			return new Size(width, height);
		}

		public static Rect TransformFromDevice(WpfHexView wpfHexView, Rect rect) {
			var zoomMultiplier = wpfHexView.ZoomLevel == 0 ? 1 : 100 / wpfHexView.ZoomLevel;
			var source = PresentationSource.FromVisual(wpfHexView.VisualElement);
			var transformFromDevice = source?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
			var viewPoint = wpfHexView.VisualElement.PointToScreen(new Point(0, 0));
			var fixedRect = new Rect((rect.Left - viewPoint.X) * zoomMultiplier, (rect.Top - viewPoint.Y) * zoomMultiplier, rect.Width * zoomMultiplier, rect.Height * zoomMultiplier);
			var topLeft = transformFromDevice.Transform(fixedRect.TopLeft);
			var bottomRight = transformFromDevice.Transform(fixedRect.BottomRight);
			return new Rect(topLeft, bottomRight);
		}
	}
}
