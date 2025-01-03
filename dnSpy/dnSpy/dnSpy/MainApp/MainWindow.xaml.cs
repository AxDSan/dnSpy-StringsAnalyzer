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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.MainApp {
	sealed partial class MainWindow : MetroWindow {
		public MainWindow(object? content) {
			InitializeComponent();
			contentPresenter.Content = content;
			CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close(), (s, e) => e.CanExecute = true));
		}

		private protected override bool HandleHoriztonalScroll(IInputElement element, short delta) {
			if (element is WpfTextView wpfTextView) {
				if ((wpfTextView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) == 0) {
					var deltaDouble = (double)delta;
					var currentViewport = wpfTextView.ViewportLeft;

					bool isReverseScroll = deltaDouble < 0;

					if (isReverseScroll) {
						if (currentViewport > 0) {
							if (currentViewport + deltaDouble < 0) {
								deltaDouble = 0 - currentViewport;
							}

							wpfTextView.ViewScroller.ScrollViewportHorizontallyByPixels(deltaDouble);
							return true;
						}
					}
					else {
						var maxScroll = Math.Max(currentViewport, wpfTextView.MaxTextRightCoordinate - wpfTextView.ViewportWidth + WpfTextViewConstants.EXTRA_HORIZONTAL_SCROLLBAR_WIDTH);
						if (currentViewport < maxScroll) {
							if (currentViewport + deltaDouble > maxScroll) {
								deltaDouble = maxScroll - currentViewport;
							}

							wpfTextView.ViewScroller.ScrollViewportHorizontallyByPixels(deltaDouble);
							return true;
						}
					}
				}

				return false;
			}
			return base.HandleHoriztonalScroll(element, delta);
		}
	}
}
