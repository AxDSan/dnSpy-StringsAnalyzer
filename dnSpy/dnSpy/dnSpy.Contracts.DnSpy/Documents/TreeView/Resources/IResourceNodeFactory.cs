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
using dnlib.DotNet.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Creates resource nodes
	/// </summary>
	public interface IResourceNodeFactory {
		/// <summary>
		/// Creates a <see cref="DocumentTreeNodeData"/> instance. Pass it to <see cref="ITreeView.Create(TreeNodeData)"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="resource">Resource</param>
		/// <param name="treeNodeGroup">Group</param>
		/// <returns></returns>
		DocumentTreeNodeData Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup);

		/// <summary>
		/// Creates a <see cref="DocumentTreeNodeData"/> instance. Pass it to <see cref="ITreeView.Create(TreeNodeData)"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="resourceElement">Resource</param>
		/// <param name="treeNodeGroup">Group</param>
		/// <returns></returns>
		DocumentTreeNodeData Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup);
	}
}
