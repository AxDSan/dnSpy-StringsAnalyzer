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
using System.Diagnostics;
using dnSpy.Contracts.Menus;

namespace dnSpy.Menus {
	sealed class MenuItemContext : IMenuItemContext {
		public bool IsDisposed { get; private set; }
		public bool OpenedFromKeyboard { get; }
		public Guid MenuGuid { get; }
		public GuidObject CreatorObject => guidObjects[0];
		public IEnumerable<GuidObject> GuidObjects => guidObjects;
		readonly List<GuidObject> guidObjects;

		public MenuItemContext(Guid menuGuid, bool openedFromKeyboard, GuidObject creatorObject, IEnumerable<GuidObject>? guidObjects) {
			MenuGuid = menuGuid;
			OpenedFromKeyboard = openedFromKeyboard;
			this.guidObjects = new List<GuidObject>();
			this.guidObjects.Add(creatorObject);
			if (guidObjects is not null)
				this.guidObjects.AddRange(guidObjects);
			state = new Dictionary<object, object>();
		}

		public T? GetOrCreateState<T>(object key, Func<T> createState) where T : class {
			Debug2.Assert(key is not null);
			T? value;
			if (state.TryGetValue(key, out var o)) {
				value = o as T;
				Debug2.Assert(o is null || value is not null);
				return value;
			}
			value = createState();
			state[key] = value;
			return value;
		}
		readonly Dictionary<object, object> state;

		public T Find<T>() {
			foreach (var o in GuidObjects) {
				if (o.Object is T)
					return (T)o.Object;
			}
			return default!;
		}

		public event EventHandler? OnDisposed;

		public void Dispose() {
			if (IsDisposed)
				return;
			IsDisposed = true;
			OnDisposed?.Invoke(this, EventArgs.Empty);
			OnDisposed = null;

			// Clear everything. We don't want to hold on to objects that could've gotten disposed,
			// eg. IDocumentViewer. Those instances could throw ObjectDisposedException
			guidObjects.Clear();
			guidObjects.Add(new GuidObject(Guid.Empty, disposedObject));
			state.Clear();
		}
		static readonly object disposedObject = new object();
	}
}
