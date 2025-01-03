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
using System.Linq;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Themes;
using dnSpy.Text.Formatting;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(ITextEditorFactoryService))]
	[Export(typeof(IDsTextEditorFactoryService))]
	sealed class TextEditorFactoryService : IDsTextEditorFactoryService {
		public event EventHandler<TextViewCreatedEventArgs>? TextViewCreated;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IEditorOptionsFactoryService editorOptionsFactoryService;
		readonly ICommandService commandService;
		readonly ISmartIndentationService smartIndentationService;
		readonly Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>[] wpfTextViewCreationListeners;
		readonly Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>[] textViewCreationListeners;
		readonly IFormattedTextSourceFactoryService formattedTextSourceFactoryService;
		readonly IViewClassifierAggregatorService viewClassifierAggregatorService;
		readonly ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly IAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly ILineTransformProviderService lineTransformProviderService;
		readonly IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider;
		readonly IMenuService menuService;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;
		readonly ISpaceReservationStackProvider spaceReservationStackProvider;
		readonly IWpfTextViewConnectionListenerServiceProvider wpfTextViewConnectionListenerServiceProvider;
		readonly IBufferGraphFactoryService bufferGraphFactoryService;
		readonly Lazy<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>[] textViewModelProviders;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly IThemeService themeService;
		readonly Lazy<ITextViewUndoManagerProvider> textViewUndoManagerProvider;
		ProviderSelector<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>? providerSelector;

		public ITextViewRoleSet AllPredefinedRoles => new TextViewRoleSet(allPredefinedRolesList);
		public ITextViewRoleSet DefaultRoles => new TextViewRoleSet(defaultRolesList);
		public ITextViewRoleSet NoRoles => new TextViewRoleSet(Array.Empty<string>());
		static readonly string[] allPredefinedRolesList = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.PrimaryDocument,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
		};
		static readonly string[] defaultRolesList = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
		};

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly IWpfTextView wpfTextView;
			readonly Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>>? createGuidObjects;

			public GuidObjectsProvider(IWpfTextView wpfTextView, Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>>? createGuidObjects) {
				this.wpfTextView = wpfTextView;
				this.createGuidObjects = createGuidObjects;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID, wpfTextView);
				var loc = wpfTextView.GetTextEditorPosition(args.OpenedFromKeyboard);
				if (loc is not null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORPOSITION_GUID, loc);

				if (createGuidObjects is not null) {
					foreach (var guidObject in createGuidObjects(args))
						yield return guidObject;
				}
			}
		}

		[ImportingConstructor]
		TextEditorFactoryService(ITextBufferFactoryService textBufferFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandService commandService, ISmartIndentationService smartIndentationService, [ImportMany] IEnumerable<Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> wpfTextViewCreationListeners, [ImportMany] IEnumerable<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> textViewCreationListeners, IFormattedTextSourceFactoryService formattedTextSourceFactoryService, IViewClassifierAggregatorService viewClassifierAggregatorService, ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService, IClassificationFormatMapService classificationFormatMapService, IEditorFormatMapService editorFormatMapService, IAdornmentLayerDefinitionService adornmentLayerDefinitionService, ILineTransformProviderService lineTransformProviderService, IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IMenuService menuService, IEditorOperationsFactoryService editorOperationsFactoryService, ISpaceReservationStackProvider spaceReservationStackProvider, IWpfTextViewConnectionListenerServiceProvider wpfTextViewConnectionListenerServiceProvider, IBufferGraphFactoryService bufferGraphFactoryService, [ImportMany] IEnumerable<Lazy<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>> textViewModelProviders, IContentTypeRegistryService contentTypeRegistryService, IThemeService themeService, Lazy<ITextViewUndoManagerProvider> textViewUndoManagerProvider) {
			this.textBufferFactoryService = textBufferFactoryService;
			this.editorOptionsFactoryService = editorOptionsFactoryService;
			this.commandService = commandService;
			this.smartIndentationService = smartIndentationService;
			this.wpfTextViewCreationListeners = wpfTextViewCreationListeners.ToArray();
			this.textViewCreationListeners = textViewCreationListeners.ToArray();
			this.formattedTextSourceFactoryService = formattedTextSourceFactoryService;
			this.viewClassifierAggregatorService = viewClassifierAggregatorService;
			this.textAndAdornmentSequencerFactoryService = textAndAdornmentSequencerFactoryService;
			this.classificationFormatMapService = classificationFormatMapService;
			this.editorFormatMapService = editorFormatMapService;
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.lineTransformProviderService = lineTransformProviderService;
			this.wpfTextViewMarginProviderCollectionProvider = wpfTextViewMarginProviderCollectionProvider;
			this.menuService = menuService;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			this.spaceReservationStackProvider = spaceReservationStackProvider;
			this.wpfTextViewConnectionListenerServiceProvider = wpfTextViewConnectionListenerServiceProvider;
			this.bufferGraphFactoryService = bufferGraphFactoryService;
			this.textViewModelProviders = textViewModelProviders.ToArray();
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.themeService = themeService;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
		}

		public IWpfTextView CreateTextView() => CreateTextView((TextViewCreatorOptions?)null);
		public IDsWpfTextView CreateTextView(TextViewCreatorOptions? options) => CreateTextView(textBufferFactoryService.CreateTextBuffer(), DefaultRoles, options);

		public IWpfTextView CreateTextView(ITextBuffer textBuffer) =>
			CreateTextView(textBuffer, (TextViewCreatorOptions?)null);
		public IDsWpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions? options) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			return CreateTextView(new TextDataModel(textBuffer), DefaultRoles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles) =>
			CreateTextView(textBuffer, roles, (TextViewCreatorOptions?)null);
		public IDsWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, TextViewCreatorOptions? options) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles is null)
				throw new ArgumentNullException(nameof(roles));
			return CreateTextView(new TextDataModel(textBuffer), roles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(textBuffer, roles, parentOptions, null);
		public IDsWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions? options) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles is null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions is null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(new TextDataModel(textBuffer), roles, parentOptions, options);
		}

		public IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(dataModel, roles, parentOptions, null);
		public IDsWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions? options) {
			if (dataModel is null)
				throw new ArgumentNullException(nameof(dataModel));
			if (roles is null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions is null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(CreateTextViewModel(dataModel, roles), roles, parentOptions, options);
		}

		ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles) {
			if (providerSelector is null)
				providerSelector = new ProviderSelector<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>(contentTypeRegistryService, textViewModelProviders);
			var contentType = dataModel.ContentType;
			foreach (var p in providerSelector.GetProviders(contentType)) {
				var model = p.Value.CreateTextViewModel(dataModel, roles);
				if (model is not null)
					return model;
			}
			return new TextViewModel(dataModel);
		}

		public IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(viewModel, roles, parentOptions, null);
		public IDsWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions? options) {
			if (viewModel is null)
				throw new ArgumentNullException(nameof(viewModel));
			if (roles is null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions is null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextViewImpl(viewModel, roles, parentOptions, options);
		}

		IDsWpfTextView CreateTextViewImpl(ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions? options) {
			var wpfTextView = new WpfTextView(textViewModel, roles, parentOptions, editorOptionsFactoryService, commandService, smartIndentationService, formattedTextSourceFactoryService, viewClassifierAggregatorService, textAndAdornmentSequencerFactoryService, classificationFormatMapService, editorFormatMapService, adornmentLayerDefinitionService, lineTransformProviderService, spaceReservationStackProvider, wpfTextViewConnectionListenerServiceProvider, bufferGraphFactoryService, wpfTextViewCreationListeners, textViewCreationListeners);

			if (options?.MenuGuid is not null) {
				var guidObjectsProvider = new GuidObjectsProvider(wpfTextView, options.CreateGuidObjects);
				menuService.InitializeContextMenu(wpfTextView.VisualElement, options.MenuGuid.Value, guidObjectsProvider, new ContextMenuInitializer(wpfTextView));
			}

			if (options?.EnableUndoHistory != false)
				textViewUndoManagerProvider.Value.GetTextViewUndoManager(wpfTextView);

			TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(wpfTextView));

			return wpfTextView;
		}

		public IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView is null)
				throw new ArgumentNullException(nameof(wpfTextView));
			var dsWpfTextView = wpfTextView as IDsWpfTextView;
			if (dsWpfTextView is null)
				throw new ArgumentException($"Only {nameof(IDsWpfTextView)}s are allowed.");
			return CreateTextViewHost(dsWpfTextView, setFocus);
		}

		public IDsWpfTextViewHost CreateTextViewHost(IDsWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView is null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return new WpfTextViewHost(wpfTextViewMarginProviderCollectionProvider, wpfTextView, editorOperationsFactoryService, themeService, setFocus);
		}

		public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles) => new TextViewRoleSet(roles);
		public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles) => new TextViewRoleSet(roles);
	}
}
