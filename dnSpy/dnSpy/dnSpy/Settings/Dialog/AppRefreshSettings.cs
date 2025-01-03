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
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Settings.Dialog {
	sealed class AppRefreshSettings : IAppRefreshSettings {
		readonly Dictionary<Guid, object?> dict = new Dictionary<Guid, object?>();

		public void Add(Guid guid, object? value = null) => dict[guid] = value;

		public bool Has(Guid guid) => dict.ContainsKey(guid);

		public object? GetValue(Guid guid) {
			dict.TryGetValue(guid, out var value);
			return value;
		}
	}
}
