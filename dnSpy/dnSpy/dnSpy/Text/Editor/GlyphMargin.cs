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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.Left)]
	[Name(PredefinedMarginNames.Glyph)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveGlyphTextMarkerService)]
	[Order(Before = PredefinedMarginNames.LeftSelection)]
	sealed class GlyphMarginProvider : IWpfTextViewMarginProvider {
		readonly IMenuService menuService;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders;
		readonly Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders;
		readonly IMarginContextMenuService marginContextMenuHandlerProviderService;

		[ImportingConstructor]
		GlyphMarginProvider(IMenuService menuService, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, [ImportMany] IEnumerable<Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>> glyphMouseProcessorProviders, [ImportMany] IEnumerable<Lazy<IGlyphFactoryProvider, IGlyphMetadata>> glyphFactoryProviders, IMarginContextMenuService marginContextMenuHandlerProviderService) {
			this.menuService = menuService;
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			this.glyphMouseProcessorProviders = Orderer.Order(glyphMouseProcessorProviders).ToArray();
			this.glyphFactoryProviders = Orderer.Order(glyphFactoryProviders).ToArray();
			this.marginContextMenuHandlerProviderService = marginContextMenuHandlerProviderService;
		}

		public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new GlyphMargin(menuService, wpfTextViewHost, viewTagAggregatorFactoryService, editorFormatMapService, glyphMouseProcessorProviders, glyphFactoryProviders, marginContextMenuHandlerProviderService);
	}

	sealed class GlyphMargin : Canvas, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsGlyphMarginEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] lazyGlyphMouseProcessorProviders;
		readonly Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] lazyGlyphFactoryProviders;
		MouseProcessorCollection? mouseProcessorCollection;
		readonly Dictionary<Type, GlyphFactoryInfo> glyphFactories;
		ITagAggregator<IGlyphTag>? tagAggregator;
		IEditorFormatMap? editorFormatMap;
		Dictionary<object, LineInfo>? lineInfos;
		Canvas? iconCanvas;
		Canvas[] childCanvases;

		readonly struct GlyphFactoryInfo {
			public int Order { get; }
			public IGlyphFactory Factory { get; }
			public IGlyphFactoryProvider FactoryProvider { get; }
			public Canvas Canvas { get; }
			public GlyphFactoryInfo(int order, IGlyphFactory factory, IGlyphFactoryProvider glyphFactoryProvider) {
				Order = order;
				Factory = factory ?? throw new ArgumentNullException(nameof(factory));
				FactoryProvider = glyphFactoryProvider ?? throw new ArgumentNullException(nameof(glyphFactoryProvider));
				Canvas = new Canvas { Background = Brushes.Transparent };
			}
		}

		readonly struct LineInfo {
			public ITextViewLine Line { get; }
			public List<IconInfo> Icons { get; }

			public LineInfo(ITextViewLine textViewLine, List<IconInfo> icons) {
				Line = textViewLine ?? throw new ArgumentNullException(nameof(textViewLine));
				Icons = icons ?? throw new ArgumentNullException(nameof(icons));
			}
		}

		readonly struct IconInfo {
			public UIElement Element { get; }
			public double BaseTopValue { get; }
			public int Order { get; }
			public IconInfo(int order, UIElement element) {
				Element = element ?? throw new ArgumentNullException(nameof(element));
				BaseTopValue = GetBaseTopValue(element);
				Order = order;
			}

			static double GetBaseTopValue(UIElement element) {
				double top = GetTop(element);
				return double.IsNaN(top) ? 0 : top;
			}
		}

		// Need to make it a constant since ActualWidth isn't always valid when we need it
		const double MARGIN_WIDTH = 17;

		public GlyphMargin(IMenuService menuService, IWpfTextViewHost wpfTextViewHost, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders, Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders, IMarginContextMenuService marginContextMenuHandlerProviderService) {
			if (menuService is null)
				throw new ArgumentNullException(nameof(menuService));
			glyphFactories = new Dictionary<Type, GlyphFactoryInfo>();
			childCanvases = Array.Empty<Canvas>();
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService ?? throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			this.editorFormatMapService = editorFormatMapService ?? throw new ArgumentNullException(nameof(editorFormatMapService));
			lazyGlyphMouseProcessorProviders = glyphMouseProcessorProviders ?? throw new ArgumentNullException(nameof(glyphMouseProcessorProviders));
			lazyGlyphFactoryProviders = glyphFactoryProviders ?? throw new ArgumentNullException(nameof(glyphFactoryProviders));

			var binding = new Binding {
				Path = new PropertyPath(BackgroundProperty),
				Source = this,
			};
			SetBinding(DsImage.BackgroundBrushProperty, binding);

			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			wpfTextViewHost.TextView.ZoomLevelChanged += TextView_ZoomLevelChanged;
			IsVisibleChanged += GlyphMargin_IsVisibleChanged;
			UpdateVisibility();
			Width = MARGIN_WIDTH;
			ClipToBounds = true;
			menuService.InitializeContextMenu(VisualElement, new Guid(MenuConstants.GUIDOBJ_GLYPHMARGIN_GUID), marginContextMenuHandlerProviderService.Create(wpfTextViewHost, this, PredefinedMarginNames.Glyph), null, new Guid(MenuConstants.GLYPHMARGIN_GUID));
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
		void TextView_ZoomLevelChanged(object? sender, ZoomLevelChangedEventArgs e) {
			LayoutTransform = e.ZoomTransform;
			DsImage.SetZoom(this, e.NewZoomLevel / 100);
		}

		public ITextViewMargin? GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedMarginNames.Glyph) ? this : null;

		void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.GlyphMarginName)
				UpdateVisibility();
		}

		IMouseProcessor[] CreateMouseProcessors() {
			var list = new List<IMouseProcessor>();
			var contentType = wpfTextViewHost.TextView.TextDataModel.ContentType;
			foreach (var lazy in lazyGlyphMouseProcessorProviders) {
				if (!contentType.IsOfAnyType(lazy.Metadata.ContentTypes))
					continue;
				if (lazy.Metadata.GlyphMargins is null || !lazy.Metadata.GlyphMargins.Any()) {
					// Nothing
				}
				else if (!lazy.Metadata.GlyphMargins.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, ThemeClassificationTypeNameKeys.GlyphMargin)))
					continue;
				var mouseProcessor = lazy.Value.GetAssociatedMouseProcessor(wpfTextViewHost, this);
				if (mouseProcessor is null)
					continue;
				list.Add(mouseProcessor);
			}
			return list.ToArray();
		}

		void InitializeGlyphFactories(IContentType? beforeContentType, IContentType afterContentType) {
			Debug2.Assert(iconCanvas is not null);
			var oldFactories = new Dictionary<IGlyphFactoryProvider, IGlyphFactory>();
			foreach (var info in glyphFactories.Values)
				oldFactories[info.FactoryProvider] = info.Factory;
			glyphFactories.Clear();

			bool newFactory = false;
			int order = 0;
			foreach (var lazy in lazyGlyphFactoryProviders) {
				if (!afterContentType.IsOfAnyType(lazy.Metadata.ContentTypes))
					continue;
				IGlyphFactory? glyphFactory = null;
				foreach (var type in lazy.Metadata.TagTypes) {
					Debug2.Assert(type is not null);
					if (type is null)
						break;
					Debug.Assert(!glyphFactories.ContainsKey(type));
					if (glyphFactories.ContainsKey(type))
						continue;
					Debug.Assert(typeof(IGlyphTag).IsAssignableFrom(type));
					if (!typeof(IGlyphTag).IsAssignableFrom(type))
						continue;

					if (glyphFactory is null) {
						if (oldFactories.TryGetValue(lazy.Value, out glyphFactory))
							oldFactories.Remove(lazy.Value);
						else {
							glyphFactory = lazy.Value.GetGlyphFactory(wpfTextViewHost.TextView, this);
							if (glyphFactory is null)
								break;
							newFactory = true;
						}
					}

					glyphFactories.Add(type, new GlyphFactoryInfo(order++, glyphFactory, lazy.Value));
				}
			}

			foreach (var factory in oldFactories.Values)
				(factory as IDisposable)?.Dispose();
			if (newFactory || oldFactories.Count != 0) {
				childCanvases = glyphFactories.Values.OrderBy(a => a.Order).Select(a => a.Canvas).ToArray();
				iconCanvas.Children.Clear();
				foreach (var c in childCanvases)
					iconCanvas.Children.Add(c);

				if (beforeContentType is not null)
					RefreshEverything();
			}
		}

		void Initialize() {
			if (mouseProcessorCollection is not null)
				return;
			iconCanvas = new Canvas { Background = Brushes.Transparent };
			Children.Add(iconCanvas);
			mouseProcessorCollection = new MouseProcessorCollection(VisualElement, null, new DefaultMouseProcessor(), CreateMouseProcessors(), null);
			wpfTextViewHost.TextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			lineInfos = new Dictionary<object, LineInfo>();
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<IGlyphTag>(wpfTextViewHost.TextView);
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfTextViewHost.TextView);
			InitializeGlyphFactories(null, wpfTextViewHost.TextView.TextDataModel.ContentType);
		}

		void TextDataModel_ContentTypeChanged(object? sender, TextDataModelContentTypeChangedEventArgs e) =>
			InitializeGlyphFactories(e.BeforeContentType, e.AfterContentType);

		void GlyphMargin_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible && !wpfTextViewHost.IsClosed) {
				Initialize();
				RegisterEvents();
				UpdateBackground();
				SetTop(iconCanvas, -wpfTextViewHost.TextView.ViewportTop);
				RefreshEverything();
			}
			else {
				UnregisterEvents();
				lineInfos?.Clear();
				foreach (var c in childCanvases)
					c.Children.Clear();
			}
		}

		void RefreshEverything() {
			lineInfos!.Clear();
			foreach (var c in childCanvases)
				c.Children.Clear();
			OnNewLayout(wpfTextViewHost.TextView.TextViewLines, Array.Empty<ITextViewLine>());
		}

		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportTop != e.NewViewState.ViewportTop)
				SetTop(iconCanvas, -wpfTextViewHost.TextView.ViewportTop);
			OnNewLayout(e.NewOrReformattedLines, e.TranslatedLines);
		}

		void OnNewLayout(IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines) {
			Debug2.Assert(lineInfos is not null);
			var newInfos = new Dictionary<object, LineInfo>();

			foreach (var line in newOrReformattedLines)
				AddLine(newInfos, line);

			foreach (var line in translatedLines) {
				bool b = lineInfos.TryGetValue(line.IdentityTag, out var info);
				Debug.Assert(b);
				if (!b)
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
				foreach (var iconInfo in info.Icons)
					SetTop(iconInfo.Element, iconInfo.BaseTopValue + line.TextTop);
			}

			foreach (var line in wpfTextViewHost.TextView.TextViewLines) {
				if (newInfos.ContainsKey(line.IdentityTag))
					continue;
				if (!lineInfos.TryGetValue(line.IdentityTag, out var info))
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
			}

			foreach (var info in lineInfos.Values) {
				foreach (var iconInfo in info.Icons)
					childCanvases[iconInfo.Order].Children.Remove(iconInfo.Element);
			}
			lineInfos = newInfos;
		}

		void AddLine(Dictionary<object, LineInfo> newInfos, ITextViewLine line) {
			var wpfLine = line as IWpfTextViewLine;
			Debug2.Assert(wpfLine is not null);
			if (wpfLine is null)
				return;
			var info = new LineInfo(line, CreateIconInfos(wpfLine));
			newInfos.Add(line.IdentityTag, info);
			foreach (var iconInfo in info.Icons)
				childCanvases[iconInfo.Order].Children.Add(iconInfo.Element);
		}

		List<IconInfo> CreateIconInfos(IWpfTextViewLine line) {
			Debug2.Assert(tagAggregator is not null);
			var icons = new List<IconInfo>();
			foreach (var mappingSpan in tagAggregator.GetTags(line.ExtentAsMappingSpan)) {
				var tag = mappingSpan.Tag;
				Debug2.Assert(tag is not null);
				if (tag is null)
					continue;
				// Fails if someone forgot to Export(typeof(IGlyphFactoryProvider)) with the correct tag types
				bool b = glyphFactories.TryGetValue(tag.GetType(), out var factoryInfo);
				Debug.Assert(b);
				if (!b)
					continue;
				foreach (var span in mappingSpan.Span.GetSpans(wpfTextViewHost.TextView.TextSnapshot)) {
					if (!line.IntersectsBufferSpan(span))
						continue;
					var elem = factoryInfo.Factory.GenerateGlyph(line, tag);
					if (elem is null)
						continue;
					elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					var iconInfo = new IconInfo(factoryInfo.Order, elem);
					icons.Add(iconInfo);
					// ActualWidth isn't always valid when we're here so use the constant
					SetLeft(elem, (MARGIN_WIDTH - elem.DesiredSize.Width) / 2);
					SetTop(elem, iconInfo.BaseTopValue + line.TextTop);
				}
			}
			return icons;
		}

		void TagAggregator_BatchedTagsChanged(object? sender, BatchedTagsChangedEventArgs e) {
			Dispatcher.VerifyAccess();
			HashSet<ITextViewLine>? checkedLines = null;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(wpfTextViewHost.TextView.TextSnapshot))
					Update(span, ref checkedLines);
			}
		}

		void Update(SnapshotSpan span, ref HashSet<ITextViewLine>? checkedLines) {
			Debug.Assert(span.Snapshot == wpfTextViewHost.TextView.TextSnapshot);
			var intersection = span.Intersection(wpfTextViewHost.TextView.TextViewLines.FormattedSpan);
			if (intersection is null)
				return;
			var point = intersection.Value.Start;
			while (point <= intersection.Value.End) {
				var line = wpfTextViewHost.TextView.TextViewLines.GetTextViewLineContainingBufferPosition(point);
				if (line is null)
					break;
				if (checkedLines is null)
					checkedLines = new HashSet<ITextViewLine>();
				if (!checkedLines.Contains(line)) {
					checkedLines.Add(line);
					Update(line);
				}
				if (line.IsLastDocumentLine())
					break;
				point = line.GetPointAfterLineBreak();
			}
		}

		void Update(IWpfTextViewLine line) {
			Debug2.Assert(lineInfos is not null);
			Debug.Assert(line.VisibilityState != VisibilityState.Unattached);
			if (!lineInfos.TryGetValue(line.IdentityTag, out var info))
				return;
			lineInfos.Remove(line.IdentityTag);
			foreach (var iconInfo in info.Icons)
				childCanvases[iconInfo.Order].Children.Remove(iconInfo.Element);
			AddLine(lineInfos, line);
		}

		void EditorFormatMap_FormatMappingChanged(object? sender, FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.GlyphMargin))
				UpdateBackground();
		}

		void UpdateBackground() {
			if (editorFormatMap is null)
				return;
			var props = editorFormatMap.GetProperties(ThemeClassificationTypeNameKeys.GlyphMargin);
			var newBackground = ResourceDictionaryUtilities.GetBackgroundBrush(props, Brushes.Transparent);
			if (!BrushComparer.Equals(Background, newBackground)) {
				Background = newBackground;
				// The images could depend on the background color, so recreate every icon
				if (childCanvases.Any(a => a.Children.Count > 0))
					Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshEverything));
			}
		}

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfTextViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			editorFormatMap!.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			tagAggregator!.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			if (editorFormatMap is not null)
				editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			if (tagAggregator is not null)
				tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}

		public void Dispose() {
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			wpfTextViewHost.TextView.ZoomLevelChanged -= TextView_ZoomLevelChanged;
			wpfTextViewHost.TextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
			IsVisibleChanged -= GlyphMargin_IsVisibleChanged;
			UnregisterEvents();
			lineInfos?.Clear();
			iconCanvas?.Children.Clear();
			mouseProcessorCollection?.Dispose();
			tagAggregator?.Dispose();
		}
	}
}
