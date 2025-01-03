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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Settings;

namespace dnSpy.MainApp {
	sealed class SavedWindowState {
		public Rect Bounds;
		public bool IsFullScreen;
		public WindowState WindowState;

		public SavedWindowState() {
		}

		public SavedWindowState(MetroWindow window) {
			Bounds = window.RestoreBounds;
			var source = PresentationSource.FromVisual(window);
			Debug2.Assert(source is not null);
			if (source is not null) {
				var t = Bounds;
				t = Rect.Transform(Bounds, source.CompositionTarget.TransformToDevice);
				Bounds = new Rect(t.TopLeft, Bounds.Size);
			}
			IsFullScreen = window.IsFullScreen;
			WindowState = window.WindowState;
		}

		public SavedWindowState Write(ISettingsSection section) {
			section.Attribute(nameof(Bounds), Bounds);
			section.Attribute(nameof(IsFullScreen), IsFullScreen);
			section.Attribute(nameof(WindowState), WindowState);
			return this;
		}

		public SavedWindowState Read(ISettingsSection section) {
			Bounds = section.Attribute<Rect?>(nameof(Bounds)) ?? Rect.Empty;
			IsFullScreen = section.Attribute<bool?>(nameof(IsFullScreen)) ?? false;
			WindowState = section.Attribute<WindowState?>(nameof(WindowState)) ?? WindowState.Normal;
			return this;
		}
	}

	sealed class SavedWindowStateRestorer {
		readonly MetroWindow window;
		readonly SavedWindowState settings;
		readonly Rect defaultWindowLocation;

		public SavedWindowStateRestorer(MetroWindow window, SavedWindowState settings, Rect defaultWindowLocation) {
			this.window = window;
			this.settings = settings;
			this.defaultWindowLocation = defaultWindowLocation;
			window.SourceInitialized += Window_SourceInitialized;
			window.ContentRendered += Window_ContentRendered;
		}

		void Window_ContentRendered(object? sender, EventArgs e) {
			window.ContentRendered -= Window_ContentRendered;
			if (!settings.IsFullScreen)
				WindowUtils.SetState(window, settings.WindowState);
			window.IsFullScreen = settings.IsFullScreen;
		}

		void Window_SourceInitialized(object? sender, EventArgs e) {
			window.SourceInitialized -= Window_SourceInitialized;
			if (settings.Bounds == Rect.Empty) {
				window.Height = defaultWindowLocation.Height;
				window.Width = defaultWindowLocation.Width;
			}
			else {
				var savedSettings = GetSavedWindowSettings();
				var rect = savedSettings ?? defaultWindowLocation;
				window.Top = rect.Top;
				window.Left = rect.Left;
				window.Height = rect.Height;
				window.Width = rect.Width;
			}
		}

		Rect? GetSavedWindowSettings() {
			var savedBounds = settings.Bounds;
			if (savedBounds == Rect.Empty)
				return null;

			const int MIN_WIDTH = 50, MIN_HEIGHT = 50;
			foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
				var rect = new System.Drawing.Rectangle((int)savedBounds.X, (int)savedBounds.Y, (int)savedBounds.Width, (int)savedBounds.Height);
				rect.Intersect(screen.Bounds);
				if (rect.Height >= MIN_HEIGHT && rect.Width >= MIN_WIDTH) {
					var t = savedBounds;
					t = Rect.Transform(savedBounds, PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice);
					return new Rect(t.TopLeft, savedBounds.Size);
				}
			}

			return null;
		}
	}
}
