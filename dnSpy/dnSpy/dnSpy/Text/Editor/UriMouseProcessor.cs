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
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IMouseProcessorProvider))]
	[Name(PredefinedDsMouseProcessorProviders.Uri)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class UriMouseProcessorProvider : IMouseProcessorProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		UriMouseProcessorProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IMessageBoxService messageBoxService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.messageBoxService = messageBoxService;
		}

		public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView) =>
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(UriMouseProcessor), () => new UriMouseProcessor(wpfTextView, viewTagAggregatorFactoryService, messageBoxService));
	}

	sealed class UriMouseProcessor : MouseProcessorBase {
		readonly IWpfTextView wpfTextView;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IMessageBoxService messageBoxService;
		readonly Cursor origCursor;
		bool hasWrittenCursor;

		public UriMouseProcessor(IWpfTextView wpfTextView, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IMessageBoxService messageBoxService) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService ?? throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			origCursor = wpfTextView.VisualElement.Cursor;
			wpfTextView.VisualElement.PreviewKeyDown += VisualElement_PreviewKeyDown;
			wpfTextView.VisualElement.PreviewKeyUp += VisualElement_PreviewKeyUp;
			wpfTextView.Closed += WpfTextView_Closed;
		}

		bool IsControlDown {
			get => isControlDown;
			set {
				if (isControlDown != value) {
					isControlDown = value;
					UpdateCursor(defaultMouseEventArgs);
				}
			}
		}
		bool isControlDown;
		static readonly MouseEventArgs defaultMouseEventArgs = new MouseEventArgs(Mouse.PrimaryDevice, 0);

		void UpdateIsControlDown(KeyEventArgs e) => IsControlDown = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0;
		void VisualElement_PreviewKeyUp(object? sender, KeyEventArgs e) => UpdateIsControlDown(e);
		void VisualElement_PreviewKeyDown(object? sender, KeyEventArgs e) => UpdateIsControlDown(e);

		IMappingTagSpan<IUrlTag>? GetUriSpan(MouseEventArgs e) {
			if (!IsControlDown)
				return null;
			var loc = MouseLocation.Create(wpfTextView, e, insertionPosition: false);
			if (loc.Position.VirtualSpaces > 0)
				return null;
			return UriHelper.GetUri(viewTagAggregatorFactoryService, wpfTextView, loc.Position.Position);
		}

		void UpdateCursor(MouseEventArgs e) {
			var tagSpan = GetUriSpan(e);
			if (tagSpan is null) {
				if (hasWrittenCursor) {
					wpfTextView.VisualElement.Cursor = origCursor;
					hasWrittenCursor = false;
				}
			}
			else {
				if (!hasWrittenCursor) {
					wpfTextView.VisualElement.Cursor = Cursors.Hand;
					hasWrittenCursor = true;
				}
			}
		}

		public override void PostprocessMouseMove(MouseEventArgs e) => UpdateCursor(e);

		public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e) {
			if (e.Handled)
				return;
			var tagSpan = GetUriSpan(e);
			if (tagSpan is null)
				return;
			e.Handled = true;
			StartBrowser(tagSpan.Tag.Url);
		}

		void StartBrowser(Uri uri) {
			try {
				if (uri.IsAbsoluteUri)
					Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
			}
			catch {
				messageBoxService.Show(dnSpy_Resources.CouldNotStartBrowser);
			}
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			wpfTextView.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
			wpfTextView.VisualElement.PreviewKeyUp -= VisualElement_PreviewKeyUp;
			wpfTextView.Closed -= WpfTextView_Closed;
		}
	}
}
