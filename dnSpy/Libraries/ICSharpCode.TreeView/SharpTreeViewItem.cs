// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

namespace ICSharpCode.TreeView
{
	public class SharpTreeViewItem : ListViewItem
	{
		static readonly ClickHandler<Tuple<MouseButtonEventArgs, SharpTreeNode>> doubleClickHandler
			= new ClickHandler<Tuple<MouseButtonEventArgs, SharpTreeNode>>();

		static SharpTreeViewItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeViewItem),
			                                         new FrameworkPropertyMetadata(typeof(SharpTreeViewItem)));
		}

		public SharpTreeNode Node
		{
			get { return DataContext as SharpTreeNode; }
		}

		void UpdateAdaptor(SharpTreeNode node)
		{
			if (nodeView == null)
				return;
			if (node == null)
				return;

			var doAdaptor = nodeView.DataContext as SharpTreeNodeProxy;
			if (doAdaptor == null)
				nodeView.DataContext = (doAdaptor = new SharpTreeNodeProxy(node));
			else
				doAdaptor.UpdateObject(node);

			nodeView.UpdateTemplate();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == DataContextProperty)
			{
				UpdateAdaptor(e.NewValue as SharpTreeNode);
			}
		}


		SharpTreeNodeView nodeView;
		public SharpTreeNodeView NodeView
		{
			get { return nodeView; }
			internal set
			{
				if (nodeView != value)
				{
					nodeView = value;
					UpdateAdaptor(Node);
				}
			}
		}
		public SharpTreeView ParentTreeView { get; internal set; }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.F2:
					if (Node != null) {
						if (Node.IsEditable && ParentTreeView != null && ParentTreeView.SelectedItems.Count == 1 && ParentTreeView.SelectedItems[0] == Node) {
							Node.IsEditing = true;
							e.Handled = true;
						}
					}
					break;
				case Key.Escape:
					if (Node != null) {
						if (Node.IsEditing) {
							Node.IsEditing = false;
							e.Handled = true;
						}
					}
					break;
			}
		}

		protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new SharpTreeViewItemAutomationPeer(this);

		#region Mouse

		Point startPoint;
		bool wasSelected;
		bool wasDoubleClick;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			wasSelected = IsSelected;
			if (!IsSelected) {
				base.OnMouseLeftButtonDown(e);
			}

			if (ParentTreeView.CanDragAndDrop) {
				if (Mouse.LeftButton == MouseButtonState.Pressed) {
					startPoint = e.GetPosition(null);
					CaptureMouse();

					if (e.ClickCount == 2)
						wasDoubleClick = true;
				}
			}

			doubleClickHandler.MouseDown(new Tuple<MouseButtonEventArgs, SharpTreeNode>(e, Node));
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (IsMouseCaptured) {
				var currentPoint = e.GetPosition(null);
				if (Math.Abs(currentPoint.X - startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
				    Math.Abs(currentPoint.Y - startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance) {

					var selection = ParentTreeView.GetTopLevelSelection().ToArray();
					if (Node != null && Node.CanDrag(selection)) {
						Node.StartDrag(this, selection);
					}
				}
			}
		}

		private void SingleClickAction(Tuple<MouseButtonEventArgs, SharpTreeNode> context)
		{
			var node = context.Item2;
			if (!node.IsExpanded && node.SingleClickExpandsChildren)
				if (!node.IsRoot || ParentTreeView.ShowRootExpander)
					node.IsExpanded = !node.IsExpanded;
		}

		private void DoubleClickAction(Tuple<MouseButtonEventArgs, SharpTreeNode> context)
		{
			var e = context.Item1;
			var node = context.Item2;
			if (node != null)
				Node.ActivateItem(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (Node != null)
			{
				if (ParentTreeView.CanDragAndDrop)
				{
					if (wasDoubleClick)
					{
						wasDoubleClick = false;
						OnDoubleClick(e);
					}
					else
						SingleClickAction(new Tuple<MouseButtonEventArgs, SharpTreeNode>(e, Node));
				}
				else
					doubleClickHandler.MouseUp(SingleClickAction, DoubleClickAction);
			}

			ReleaseMouseCapture();
			if (wasSelected) {
				wasSelected = false;
				// Make sure the TV doesn't steal focus when double clicking something that will
				// trigger setting focus to eg. the text editor.
				if (!ignoreOnMouseLeftButtonDown)
					base.OnMouseLeftButtonDown(e);
			}
			ignoreOnMouseLeftButtonDown = false;
		}

		bool ignoreOnMouseLeftButtonDown = false;
		void OnDoubleClick(RoutedEventArgs e)
		{
			ignoreOnMouseLeftButtonDown = true;
			if (Node == null)
				return;
			Node.ActivateItem(e);
			if (!e.Handled) {
				if (!Node.IsRoot || ParentTreeView.ShowRootExpander) {
					Node.IsExpanded = !Node.IsExpanded;
				}
			}
		}

		#endregion

		#region Drag and Drop

		protected override void OnDragEnter(DragEventArgs e)
		{
			ParentTreeView.HandleDragEnter(this, e);
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			ParentTreeView.HandleDragOver(this, e);
		}

		protected override void OnDrop(DragEventArgs e)
		{
			ParentTreeView.HandleDrop(this, e);
		}

		protected override void OnDragLeave(DragEventArgs e)
		{
			ParentTreeView.HandleDragLeave(this, e);
		}

		#endregion
	}
}
