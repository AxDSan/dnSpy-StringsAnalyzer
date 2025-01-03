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
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Commands {
	abstract class EditMenuHandler : MenuItemBase<AsmEditorContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		readonly IDocumentTreeView documentTreeView;

		protected EditMenuHandler(IDocumentTreeView documentTreeView) => this.documentTreeView = documentTreeView;

		protected sealed override AsmEditorContext? CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_EDIT_GUID))
				return null;
			return CreateContext();
		}

		public AsmEditorContext CreateContext() => new AsmEditorContext(documentTreeView.TreeView.TopLevelSelection.OfType<DocumentTreeNodeData>().ToArray());
	}
}
