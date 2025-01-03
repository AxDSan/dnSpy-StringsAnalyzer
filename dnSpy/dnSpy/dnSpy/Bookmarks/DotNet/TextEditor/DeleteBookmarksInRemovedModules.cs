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
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;
using dnSpy.UI;

namespace dnSpy.Bookmarks.DotNet.TextEditor {
	[Export(typeof(IBookmarksServiceListener))]
	sealed class DeleteBookmarksInRemovedModules : IBookmarksServiceListener {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<IModuleIdProvider> moduleIdProvider;
		BookmarksService? bookmarksService;

		[ImportingConstructor]
		DeleteBookmarksInRemovedModules(UIDispatcher uiDispatcher, Lazy<IDocumentTabService> documentTabService, Lazy<IModuleIdProvider> moduleIdProvider) {
			this.uiDispatcher = uiDispatcher;
			this.documentTabService = documentTabService;
			this.moduleIdProvider = moduleIdProvider;
		}

		void IBookmarksServiceListener.Initialize(BookmarksService bookmarksService) {
			this.bookmarksService = bookmarksService;
			uiDispatcher.UI(() => Initialize_UI());
		}

		void Initialize_UI() {
			uiDispatcher.VerifyAccess();
			documentTabService.Value.DocumentCollectionChanged += DocumentTabService_FileCollectionChanged;
		}

		void DocumentTabService_FileCollectionChanged(object? sender, NotifyDocumentCollectionChangedEventArgs e) {
			Debug2.Assert(bookmarksService is not null);
			if (bookmarksService is null)
				return;
			switch (e.Type) {
			case NotifyDocumentCollectionType.Clear:
			case NotifyDocumentCollectionType.Remove:
				var existing = new HashSet<ModuleId>(documentTabService.Value.DocumentTreeView.GetAllModuleNodes().Select(a => moduleIdProvider.Value.Create(a.Document.ModuleDef)));
				var removed = new HashSet<ModuleId>(e.Documents.Select(a => moduleIdProvider.Value.Create(a.ModuleDef)));
				existing.Remove(new ModuleId());
				removed.Remove(new ModuleId());
				List<Bookmark>? bookmarksToRemove = null;
				foreach (var bm in bookmarksService.Bookmarks) {
					if (!(bm.Location is IDotNetBookmarkLocation loc))
						continue;

					// Don't auto-remove bookmarks in dynamic modules since they have no disk file. The
					// user must delete these him/herself.
					if (loc.Module.IsDynamic)
						continue;

					// If the file is still in the TV, don't delete anything. This can happen if
					// we've loaded an in-memory module and the node just got removed.
					if (existing.Contains(loc.Module))
						continue;

					if (removed.Contains(loc.Module)) {
						if (bookmarksToRemove is null)
							bookmarksToRemove = new List<Bookmark>();
						bookmarksToRemove.Add(bm);
					}
				}
				if (bookmarksToRemove is not null)
					bookmarksService.Remove(bookmarksToRemove.ToArray());
				break;

			case NotifyDocumentCollectionType.Add:
				break;
			}
		}
	}
}
