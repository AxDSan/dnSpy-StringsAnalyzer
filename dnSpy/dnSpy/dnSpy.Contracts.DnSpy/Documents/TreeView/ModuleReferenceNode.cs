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
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A module reference node
	/// </summary>
	public abstract class ModuleReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the module reference
		/// </summary>
		public ModuleRef ModuleRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => ModuleRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="moduleRef">Module reference</param>
		protected ModuleReferenceNode(ModuleRef moduleRef) => ModuleRef = moduleRef ?? throw new ArgumentNullException(nameof(moduleRef));
	}
}
