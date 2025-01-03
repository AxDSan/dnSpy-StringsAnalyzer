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
using System.Collections;
using System.Collections.Generic;
using dnSpy.Contracts.TreeView;

namespace dnSpy.TreeView {
	sealed class SharpTreeNodeChildrenList : IList<ITreeNode> {
		public DsSharpTreeNode Node => node;
		readonly DsSharpTreeNode node;

		public SharpTreeNodeChildrenList(TreeNodeImpl owner) => node = new DsSharpTreeNode(owner);

		public ITreeNode this[int index] {
			get => ((DsSharpTreeNode)node.Children[index]).TreeNodeImpl;
			set => node.Children[index] = GetAndVerifyTreeNodeImpl(value).Node;
		}

		public int Count => node.Children.Count;
		public bool IsReadOnly => false;

		public void Add(ITreeNode item) => node.Children.Add(GetAndVerifyTreeNodeImpl(item).Node);

		public void Clear() {
			if (node.Children.Count != 0)
				node.Children.Clear();
		}

		public bool Contains(ITreeNode item) {
			if (item is null)
				return false;
			return node.Children.Contains(GetAndVerifyTreeNodeImpl(item).Node);
		}

		public void CopyTo(ITreeNode[] array, int arrayIndex) {
			if (arrayIndex < 0 || arrayIndex + node.Children.Count > array.Length)
				throw new InvalidOperationException();
			for (int i = 0; i < node.Children.Count; i++)
				array[i + arrayIndex] = ((DsSharpTreeNode)node.Children[i]).TreeNodeImpl;
		}

		public IEnumerator<ITreeNode> GetEnumerator() {
			foreach (DsSharpTreeNode n in node.Children)
				yield return n.TreeNodeImpl;
		}

		public int IndexOf(ITreeNode item) {
			for (int i = 0; i < node.Children.Count; i++) {
				if (((DsSharpTreeNode)node.Children[i]).TreeNodeImpl == item)
					return i;
			}
			return -1;
		}

		TreeNodeImpl GetAndVerifyTreeNodeImpl(ITreeNode treeNode) {
			if (treeNode is null)
				throw new ArgumentNullException(nameof(treeNode));
			var impl = treeNode as TreeNodeImpl;
			if (impl is null)
				throw new InvalidOperationException("ITreeNode is not our impl class. Only insert nodes in the correct owner tree");
			if (impl.TreeView != node.TreeNodeImpl.TreeView)
				throw new InvalidOperationException($"Tried add a tree node from TreeView({impl.TreeView.Guid}) to TreeView({node.TreeNodeImpl.TreeView.Guid}). Only insert nodes in the correct tree view");
			return impl;
		}

		public void Insert(int index, ITreeNode item) => node.Children.Insert(index, GetAndVerifyTreeNodeImpl(item).Node);

		public bool Remove(ITreeNode item) {
			if (item is null)
				return false;
			var removedNode = GetAndVerifyTreeNodeImpl(item).Node;
			bool b = node.Children.Remove(removedNode);
			if (b)
				node.TreeNodeImpl.TreeView.OnRemoved(removedNode.TreeNodeImpl.Data);
			return b;
		}

		public void RemoveAt(int index) {
			var removedNode = (DsSharpTreeNode)node.Children[index];
			node.Children.RemoveAt(index);
			node.TreeNodeImpl.TreeView.OnRemoved(removedNode.TreeNodeImpl.Data);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
