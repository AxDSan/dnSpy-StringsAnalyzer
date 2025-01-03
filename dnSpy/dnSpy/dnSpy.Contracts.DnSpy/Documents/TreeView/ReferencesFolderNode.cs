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

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// References node
	/// </summary>
	public abstract class ReferencesFolderNode : DocumentTreeNodeData {
		/// <summary>
		/// Creates a <see cref="AssemblyReferenceNode"/>
		/// </summary>
		/// <param name="asmRef">Assembly reference</param>
		/// <returns></returns>
		public abstract AssemblyReferenceNode Create(AssemblyRef asmRef);

		/// <summary>
		/// Creates a <see cref="ModuleReferenceNode"/>
		/// </summary>
		/// <param name="modRef">Module reference</param>
		/// <returns></returns>
		public ModuleReferenceNode Create(ModuleRef modRef) => Context.DocumentTreeView.Create(modRef);
	}
}
