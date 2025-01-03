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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Analyzer.TreeNodes {
	abstract class AnalyzerTreeNodeData : TreeNodeData {
		public override Guid Guid => Guid.Empty;
		public sealed override bool SingleClickExpandsChildren => Context.SingleClickExpandsChildren;
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public IAnalyzerTreeNodeDataContext Context { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
		protected abstract ImageReference GetIcon(IDotNetImageService dnImgMgr);
		protected virtual ImageReference? GetExpandedIcon(IDotNetImageService dnImgMgr) => null;
		public sealed override ImageReference Icon => GetIcon(Context.DotNetImageService);
		public sealed override ImageReference? ExpandedIcon => GetExpandedIcon(Context.DotNetImageService);

		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) => writer.Clear();
		}

		public sealed override object? Text {
			get {
				if (cachedText?.Target is object cached)
					return cached;

				var writer = Cache.GetWriter();
				try {
					Write(writer, Context.Decompiler);
					var classifierContext = new TreeViewNodeClassifierContext(writer.Text, Context.TreeView, this, isToolTip: false, colorize: Context.SyntaxHighlight, colors: writer.Colors);
					var elem = Context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAnalyzer, TextElementFlags.FilterOutNewLines);
					cachedText = new WeakReference(elem);
					return elem;
				}
				finally {
					Cache.FreeWriter(writer);
				}
			}
		}
		WeakReference? cachedText;

		protected abstract void Write(ITextColorWriter output, IDecompiler decompiler);
		public sealed override object? ToolTip => null;
		public sealed override string ToString() => ToString(Context.Decompiler);

		public string ToString(IDecompiler decompiler) {
			var output = new StringBuilderTextColorOutput();
			Write(output, decompiler);
			return output.ToString();
		}

		public sealed override void OnRefreshUI() => cachedText = null;
		public abstract bool HandleAssemblyListChanged(IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies);
		public abstract bool HandleModelUpdated(IDsDocument[] documents);

		public static void CancelSelfAndChildren(TreeNodeData node) {
			foreach (var c in node.DescendantsAndSelf()) {
				if (c is IAsyncCancellable id)
					id.Cancel();
			}
		}

		public static void HandleAssemblyListChanged(ITreeNode node, IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies) {
			var children = node.DataChildren.ToArray();
			for (int i = children.Length - 1; i >= 0; i--) {
				var c = children[i];
				var n = c as AnalyzerTreeNodeData;
				if (n is null || !n.HandleAssemblyListChanged(removedAssemblies, addedAssemblies)) {
					AnalyzerTreeNodeData.CancelSelfAndChildren(c);
					node.Children.RemoveAt(i);
				}
			}
		}

		public static void HandleModelUpdated(ITreeNode node, IDsDocument[] documents) {
			var children = node.DataChildren.ToArray();
			for (int i = children.Length - 1; i >= 0; i--) {
				var c = children[i];
				var n = c as AnalyzerTreeNodeData;
				if (n is null || !n.HandleModelUpdated(documents)) {
					AnalyzerTreeNodeData.CancelSelfAndChildren(c);
					node.Children.RemoveAt(i);
				}
			}
		}

		protected IMemberRef GetOriginalCodeLocation(IMemberRef member) {
			// Emulate the original code. Only the C# override returned something other than the input
			if (Context.Decompiler.UniqueGuid != DecompilerConstants.LANGUAGE_CSHARP_ILSPY)
				return member;
			if (!Context.Decompiler.Settings.GetBoolean(DecompilerOptionConstants.AnonymousMethods_GUID))
				return member;
			return Helpers.GetOriginalCodeLocation(member);
		}

		sealed class TheTreeNodeGroup : ITreeNodeGroup {
			public static ITreeNodeGroup Instance = new TheTreeNodeGroup();

			TheTreeNodeGroup() {
			}

			public double Order => 100;

			public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
				if (x == y)
					return 0;
				var a = x as AnalyzerTreeNodeData;
				var b = y as AnalyzerTreeNodeData;
				if (a is null) return -1;
				if (b is null) return 1;
				return StringComparer.OrdinalIgnoreCase.Compare(a.ToString(), b.ToString());
			}
		}

		public override ITreeNodeGroup? TreeNodeGroup => TheTreeNodeGroup.Instance;
	}
}
