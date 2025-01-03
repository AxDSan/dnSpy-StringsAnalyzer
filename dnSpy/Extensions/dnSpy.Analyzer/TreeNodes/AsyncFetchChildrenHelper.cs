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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class AsyncFetchChildrenHelper : AsyncNodeProvider {
		readonly SearchNode node;
		readonly Action completed;

		public AsyncFetchChildrenHelper(SearchNode node, Action completed)
			: base(node) {
			this.node = node;
			this.completed = completed;
			Start();
		}

		sealed class MessageNodeTreeNodeGroup : ITreeNodeGroup {
			public double Order { get; }

			public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
				if (x == y)
					return 0;
				var a = x as MessageNode;
				var b = y as MessageNode;
				if (a is null) return -1;
				if (b is null) return 1;
				return 0;
			}
		}

		sealed class MessageNode : TreeNodeData {
			public override Guid Guid => Guid.Empty;
			public override ImageReference Icon => DsImages.Search;
			public override object? ToolTip => null;
			public override void OnRefreshUI() { }
			public override ITreeNodeGroup? TreeNodeGroup => treeNodeGroup;
			readonly ITreeNodeGroup treeNodeGroup = new MessageNodeTreeNodeGroup();

			readonly IAnalyzerTreeNodeDataContext context;

			public MessageNode(IAnalyzerTreeNodeDataContext context) => this.context = context;

			static class Cache {
				static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
				public static TextClassifierTextColorWriter GetWriter() => writer;
				public static void FreeWriter(TextClassifierTextColorWriter writer) => writer.Clear();
			}

			public override object? Text {
				get {
					var writer = Cache.GetWriter();
					try {
						writer.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.Searching);
						var classifierContext = new TreeViewNodeClassifierContext(writer.Text, context.TreeView, this, isToolTip: false, colorize: context.SyntaxHighlight, colors: writer.Colors);
						var elem = context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAnalyzer, TextElementFlags.FilterOutNewLines);
						return elem;
					}
					finally {
						Cache.FreeWriter(writer);
					}
				}
			}
		}

		protected override void ThreadMethod() {
			AddMessageNode(() => new MessageNode(node.Context));
			foreach (var child in node.FetchChildrenInternal(cancellationToken))
				AddNode(child);
		}

		protected override void OnCompleted() => completed();
	}
}
