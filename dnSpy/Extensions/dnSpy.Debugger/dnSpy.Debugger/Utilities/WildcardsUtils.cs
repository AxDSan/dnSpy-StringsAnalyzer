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
using System.Text.RegularExpressions;

namespace dnSpy.Debugger.Utilities {
	static class WildcardsUtils {
		public static Regex CreateRegex(string wildcardString) {
			if (wildcardString is null)
				throw new ArgumentNullException(nameof(wildcardString));
			const RegexOptions flags = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
			return new Regex("^" + Regex.Escape(wildcardString).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", flags);
		}
	}
}
