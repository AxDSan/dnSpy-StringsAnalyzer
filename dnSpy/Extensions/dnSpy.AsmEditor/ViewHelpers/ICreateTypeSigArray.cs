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

using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;

namespace dnSpy.AsmEditor.ViewHelpers {
	interface ICreateTypeSigArray {
		/// <summary>
		/// Returns a created TypeSig array or null if user canceled.
		/// </summary>
		/// <param name="options">Type sig creator options</param>
		/// <param name="count">Number of types to create or null if any number of types can be
		/// created</param>
		/// <param name="typeSigs">Existing type sigs or null</param>
		/// <returns></returns>
		TypeSig[]? Create(TypeSigCreatorOptions options, int? count, TypeSig[]? typeSigs);
	}
}
