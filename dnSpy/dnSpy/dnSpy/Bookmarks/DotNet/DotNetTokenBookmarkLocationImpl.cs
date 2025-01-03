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

using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Bookmarks.DotNet {
	sealed class DotNetTokenBookmarkLocationImpl : DotNetTokenBookmarkLocation, IDotNetBookmarkLocation {
		public override string Type => PredefinedBookmarkLocationTypes.DotNetToken;
		public override ModuleId Module { get; }
		public override uint Token { get; }

		public DotNetBookmarkLocationFormatter? Formatter { get; set; }

		public DotNetTokenBookmarkLocationImpl(ModuleId module, uint token) {
			Module = module;
			Token = token;
		}

		protected override void CloseCore() { }

		public override bool Equals(object? obj) =>
			obj is DotNetTokenBookmarkLocationImpl other &&
			Module == other.Module &&
			Token == other.Token;

		public override int GetHashCode() => Module.GetHashCode() ^ (int)Token;
	}
}
