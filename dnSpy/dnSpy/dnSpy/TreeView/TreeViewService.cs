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
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.TreeView {
	interface ITreeViewServiceImpl : ITreeViewService {
		IEnumerable<ITreeNodeDataProvider> GetProviders(Guid guid);
	}

	[Export(typeof(ITreeViewService))]
	sealed class TreeViewService : ITreeViewServiceImpl {
		readonly IThemeService themeService;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly Dictionary<Guid, List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>> guidToProvider;

		[ImportingConstructor]
		TreeViewService(IThemeService themeService, IClassificationFormatMapService classificationFormatMapService, [ImportMany] IEnumerable<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> treeNodeDataProviders) {
			this.themeService = themeService;
			this.classificationFormatMapService = classificationFormatMapService;
			guidToProvider = new Dictionary<Guid, List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>>();
			InitializeGuidToProvider(treeNodeDataProviders);
		}

		void InitializeGuidToProvider(IEnumerable<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> treeNodeDataProviders) {
			foreach (var provider in treeNodeDataProviders.OrderBy(a => a.Metadata.Order)) {
				bool b = Guid.TryParse(provider.Metadata.Guid, out var guid);
				Debug.Assert(b, $"Couldn't parse guid: '{provider.Metadata.Guid}'");
				if (!b)
					continue;

				if (!guidToProvider.TryGetValue(guid, out var list))
					guidToProvider.Add(guid, list = new List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>());
				list.Add(provider);
			}
		}

		public ITreeView Create(Guid guid, TreeViewOptions options) => new TreeViewImpl(this, themeService, classificationFormatMapService, guid, options);

		public IEnumerable<ITreeNodeDataProvider> GetProviders(Guid guid) {
			if (!guidToProvider.TryGetValue(guid, out var list))
				return Array.Empty<ITreeNodeDataProvider>();
			return list.Select(a => a.Value);
		}
	}
}
