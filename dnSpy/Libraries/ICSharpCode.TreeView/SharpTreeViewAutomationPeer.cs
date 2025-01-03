using System.Windows.Automation.Peers;

namespace ICSharpCode.TreeView {
	class SharpTreeViewAutomationPeer : FrameworkElementAutomationPeer {
		internal SharpTreeViewAutomationPeer(SharpTreeView owner) : base(owner) { }

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tree;
	}
}
