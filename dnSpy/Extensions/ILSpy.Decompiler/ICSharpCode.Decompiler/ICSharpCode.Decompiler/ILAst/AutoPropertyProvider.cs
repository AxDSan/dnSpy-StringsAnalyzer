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

using System.Collections.Generic;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	sealed class AutoPropertyProvider {
		// No need to use a dictionary since there'll be only a few types, usually just one,
		// but could be more if there are nested types.
		readonly List<AutoPropertyInfo> typeInfos;
		int typeInfosCount;

		public AutoPropertyProvider() {
			typeInfos = new List<AutoPropertyInfo>();
		}

		AutoPropertyInfo AllocAutoPropertyInfo() {
			AutoPropertyInfo info;
			if (typeInfosCount < typeInfos.Count) {
				info = typeInfos[typeInfosCount++];
				info.Reset();
			}
			else {
				typeInfos.Add(info = new AutoPropertyInfo());
				typeInfosCount++;
			}
			return info;
		}

		AutoPropertyInfo Find(TypeDef type) {
			for (int i = 0; i < typeInfosCount; i++) {
				var info = typeInfos[i];
				if (info.Type == type)
					return info;
			}
			return default(AutoPropertyInfo);
		}

		public AutoPropertyInfo GetOrCreate(TypeDef type) {
			var info = Find(type);
			if (info == null) {
				info = AllocAutoPropertyInfo();
				info.Initialize(type);
			}
			return info;
		}

		public void Reset() {
			typeInfosCount = 0;
		}
	}
}
