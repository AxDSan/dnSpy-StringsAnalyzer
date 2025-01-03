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
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.BackgroundImage;

namespace dnSpy.BackgroundImage {
	interface IBackgroundImageSettings {
		event EventHandler? SettingsChanged;
		string[] Images { get; }
		Stretch Stretch { get; }
		StretchDirection StretchDirection { get; }
		double Opacity { get; }
		double HorizontalOffset { get; }
		double VerticalOffset { get; }
		double LeftMarginWidthPercent { get; }
		double RightMarginWidthPercent { get; }
		double TopMarginHeightPercent { get; }
		double BottomMarginHeightPercent { get; }
		double MaxHeight { get; }
		double MaxWidth { get; }
		double Zoom { get; }
		ImagePlacement ImagePlacement { get; }
		bool IsRandom { get; }
		bool IsEnabled { get; }
		TimeSpan Interval { get; }
	}

	sealed class BackgroundImageSettings : IBackgroundImageSettings {
		readonly RawSettings rawSettings;

		public event EventHandler? SettingsChanged;

		public string[] Images => rawSettings.Images.ToArray();
		public Stretch Stretch => rawSettings.Stretch;
		public StretchDirection StretchDirection => rawSettings.StretchDirection;
		public double Opacity => rawSettings.Opacity;
		public double HorizontalOffset => rawSettings.HorizontalOffset;
		public double VerticalOffset => rawSettings.VerticalOffset;
		public double LeftMarginWidthPercent => rawSettings.LeftMarginWidthPercent;
		public double RightMarginWidthPercent => rawSettings.RightMarginWidthPercent;
		public double TopMarginHeightPercent => rawSettings.TopMarginHeightPercent;
		public double BottomMarginHeightPercent => rawSettings.BottomMarginHeightPercent;
		public double MaxHeight => rawSettings.MaxHeight;
		public double MaxWidth => rawSettings.MaxWidth;
		public double Zoom => rawSettings.Zoom;
		public ImagePlacement ImagePlacement => rawSettings.ImagePlacement;
		public bool IsRandom => rawSettings.IsRandom;
		public bool IsEnabled => rawSettings.IsEnabled;
		public TimeSpan Interval => rawSettings.Interval;

		public BackgroundImageSettings(RawSettings rawSettings) => this.rawSettings = rawSettings ?? throw new ArgumentNullException(nameof(rawSettings));

		public void RaiseSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
	}
}
