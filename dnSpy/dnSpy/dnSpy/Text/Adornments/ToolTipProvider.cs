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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Adornments {
	sealed class ToolTipProvider : IToolTipProvider {
		readonly IWpfTextView wpfTextView;
		readonly ISpaceReservationManager spaceReservationManager;
		ISpaceReservationAgent? toolTipAgent;

#pragma warning disable CS0169
		[Export(typeof(SpaceReservationManagerDefinition))]
		[Name(PredefinedSpaceReservationManagerNames.ToolTip)]
		static readonly SpaceReservationManagerDefinition? toolTipSpaceReservationManagerDefinition;
#pragma warning restore CS0169

		public ToolTipProvider(IWpfTextView wpfTextView) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			spaceReservationManager = wpfTextView.GetSpaceReservationManager(PredefinedSpaceReservationManagerNames.ToolTip);
		}

		public void ClearToolTip() {
			if (toolTipAgent is not null)
				spaceReservationManager.RemoveAgent(toolTipAgent);
		}

		public void ShowToolTip(ITrackingSpan span, object toolTipContent) {
			if (span is null)
				throw new ArgumentNullException(nameof(span));
			if (toolTipContent is null)
				throw new ArgumentNullException(nameof(toolTipContent));
			ShowToolTip(span, toolTipContent, PopupStyles.None);
		}

		public void ShowToolTip(ITrackingSpan span, object toolTipContent, PopupStyles style) {
			if (span is null)
				throw new ArgumentNullException(nameof(span));
			if (toolTipContent is null)
				throw new ArgumentNullException(nameof(toolTipContent));
			if ((style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) == (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));

			ClearToolTip();

			var uiElement = GetUIElement(toolTipContent);
			if (uiElement is null)
				throw new ArgumentException();

			spaceReservationManager.AgentChanged += SpaceReservationManager_AgentChanged;
			toolTipAgent = spaceReservationManager.CreatePopupAgent(span, style, uiElement);
			spaceReservationManager.AddAgent(toolTipAgent);
		}

		void SpaceReservationManager_AgentChanged(object? sender, SpaceReservationAgentChangedEventArgs e) {
			if (e.OldAgent == toolTipAgent) {
				spaceReservationManager.AgentChanged -= SpaceReservationManager_AgentChanged;
				toolTipAgent = null;
			}
		}

		UIElement? GetUIElement(object toolTipContent) {
			if (toolTipContent is UIElement elem)
				return elem;
			if (toolTipContent is string s)
				return CreateUIElement(s);
			return null;
		}

		UIElement CreateUIElement(string s) {
			var border = new Border {
				Child = new TextBlock {
					Text = s,
					Padding = new Thickness(1),
				},
				BorderThickness = new Thickness(1),
			};
			border.SetResourceReference(Control.BackgroundProperty, "ToolTipBackground");
			border.SetResourceReference(Control.ForegroundProperty, "ToolTipForeground");
			border.SetResourceReference(Border.BorderBrushProperty, "ToolTipBorderBrush");
			return border;
		}
	}
}
