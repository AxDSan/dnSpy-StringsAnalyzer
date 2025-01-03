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
using dnlib.DotNet;
using dnSpy.AsmEditor.Event;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class EditedEventUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly EventNode ownerNode;
		readonly EventDef @event;
		readonly EventDefOptions originalEventDefOptions;
		readonly EventDefOptions newEventDefOptions;

		public EditedEventUpdater(ModuleDocumentNode modNode, EventDef originalEvent, EventDefOptions eventDefOptions) {
			var node = modNode.Context.DocumentTreeView.FindNode(originalEvent);
			if (node is null)
				throw new InvalidOperationException();
			ownerNode = node;
			@event = originalEvent;
			originalEventDefOptions = new EventDefOptions(originalEvent);
			newEventDefOptions = eventDefOptions;
		}

		public void Add() => newEventDefOptions.CopyTo(@event);
		public void Remove() => originalEventDefOptions.CopyTo(@event);
	}
}
