using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace ICSharpCode.TreeView {
	class SharpTreeViewItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider {
		internal SharpTreeViewItemAutomationPeer(SharpTreeViewItem owner) : base(owner) {
			SharpTreeViewItem.DataContextChanged += OnDataContextChanged;
			if (SharpTreeViewItem.DataContext is not SharpTreeNode node) return;
			node.PropertyChanged += OnPropertyChanged;
		}

		SharpTreeViewItem SharpTreeViewItem => (SharpTreeViewItem)Owner;

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TreeItem;

		public override object GetPattern(PatternInterface patternInterface) =>
			patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

		public void Collapse() { }

		public void Expand() { }

		public ExpandCollapseState ExpandCollapseState {
			get {
				if (SharpTreeViewItem.DataContext is not SharpTreeNode node || !node.ShowExpander)
					return ExpandCollapseState.LeafNode;
				return node.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
			}
		}

		void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName != "IsExpanded") return;
			if (sender is not SharpTreeNode node || node.Children.Count == 0) return;
			bool newValue = node.IsExpanded;
			bool oldValue = !newValue;
			RaisePropertyChangedEvent(
				ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
				newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
		}

		void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) {
			if (e.OldValue is SharpTreeNode oldNode)
				oldNode.PropertyChanged -= OnPropertyChanged;
			if (e.NewValue is SharpTreeNode newNode)
				newNode.PropertyChanged += OnPropertyChanged;
		}
	}
}
