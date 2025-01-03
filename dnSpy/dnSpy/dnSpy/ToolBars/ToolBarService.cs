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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.ToolBars;
using dnSpy.Images;

namespace dnSpy.ToolBars {
	abstract class ToolBarItemMD {
		public abstract IToolBarItem ToolBarItem { get; }
		public abstract IToolBarItemMetadata Metadata { get; }
	}

	sealed class ToolBarButtonMD : ToolBarItemMD {
		readonly Lazy<IToolBarButton, IToolBarButtonMetadata> md;

		public override IToolBarItem ToolBarItem => md.Value;
		public override IToolBarItemMetadata Metadata => md.Metadata;

		public ToolBarButtonMD(Lazy<IToolBarButton, IToolBarButtonMetadata> md) => this.md = md;
	}

	sealed class ToolBarObjectMD : ToolBarItemMD {
		readonly Lazy<IToolBarObject, IToolBarObjectMetadata> md;

		public override IToolBarItem ToolBarItem => md.Value;
		public override IToolBarItemMetadata Metadata => md.Metadata;

		public ToolBarObjectMD(Lazy<IToolBarObject, IToolBarObjectMetadata> md) => this.md = md;
	}

	sealed class ToolBarItemGroupMD {
		public readonly double Order;
		public readonly List<ToolBarItemMD> Items;

		public ToolBarItemGroupMD(double order) {
			Order = order;
			Items = new List<ToolBarItemMD>();
		}
	}

	[Export(typeof(IToolBarService))]
	sealed class ToolBarService : IToolBarService {
		[ImportingConstructor]
		ToolBarService([ImportMany] IEnumerable<Lazy<IToolBarButton, IToolBarButtonMetadata>> tbButtonMef, [ImportMany] IEnumerable<Lazy<IToolBarObject, IToolBarObjectMetadata>> tbObjectMef) {
			this.tbButtonMef = tbButtonMef;
			this.tbObjectMef = tbObjectMef;
		}

		void InitializeToolBarItems() {
			if (guidToGroups is not null)
				return;

			var dict = new Dictionary<Guid, Dictionary<string, ToolBarItemGroupMD>>();
			foreach (var md in GetToolBarItemMDs()) {
				string ownerGuidString;

				ownerGuidString = md.Metadata.OwnerGuid ?? ToolBarConstants.APP_TB_GUID;
				bool b = Guid.TryParse(ownerGuidString, out var ownerGuid);
				Debug.Assert(b, $"ToolBarItem: Couldn't parse OwnerGuid property: '{ownerGuidString}'");
				if (!b)
					continue;

				b = !string.IsNullOrEmpty(md.Metadata.Group);
				Debug.Assert(b, "ToolBarItem: Group property is empty or null");
				if (!b)
					continue;
				b = Menus.MenuService.ParseGroup(md.Metadata.Group!, out double groupOrder, out string groupName);
				Debug.Assert(b, "ToolBarItem: Group property must be of the format \"<order>,<name>\" where <order> is a System.Double");
				if (!b)
					continue;

				if (!dict.TryGetValue(ownerGuid, out var groupDict))
					dict.Add(ownerGuid, groupDict = new Dictionary<string, ToolBarItemGroupMD>());
				if (!groupDict.TryGetValue(groupName, out var mdGroup))
					groupDict.Add(groupName, mdGroup = new ToolBarItemGroupMD(groupOrder));
				Debug.Assert(mdGroup.Order == groupOrder, $"ToolBarItem: Group order is different: {mdGroup.Order} vs {groupOrder}");
				mdGroup.Items.Add(md);
			}

			guidToGroups = new Dictionary<Guid, List<ToolBarItemGroupMD>>();
			foreach (var kv in dict) {
				var groups = new List<ToolBarItemGroupMD>(kv.Value.Select(a => a.Value).OrderBy(a => a.Order));
				foreach (var g in groups)
					g.Items.Sort((a, b) => a.Metadata.Order.CompareTo(b.Metadata.Order));
				guidToGroups.Add(kv.Key, groups);
			}
		}
		Dictionary<Guid, List<ToolBarItemGroupMD>>? guidToGroups;

		IEnumerable<ToolBarItemMD> GetToolBarItemMDs() {
			foreach (var i in tbButtonMef)
				yield return new ToolBarButtonMD(i);
			foreach (var i in tbObjectMef)
				yield return new ToolBarObjectMD(i);
		}
		readonly IEnumerable<Lazy<IToolBarButton, IToolBarButtonMetadata>> tbButtonMef;
		readonly IEnumerable<Lazy<IToolBarObject, IToolBarObjectMetadata>> tbObjectMef;

		public ToolBar InitializeToolBar(ToolBar? toolBar, Guid toolBarGuid, IInputElement? commandTarget) {
			InitializeToolBarItems();
			Debug2.Assert(guidToGroups is not null);
			if (toolBar is null) {
				toolBar = new ToolBar();
				toolBar.FocusVisualStyle = null;
			}

			toolBar.Items.Clear();

			bool b = guidToGroups.TryGetValue(toolBarGuid, out var groups);
			Debug.Assert(b);
			if (b) {
				var ctx = new ToolBarItemContext(toolBarGuid);

				var items = new List<ToolBarItemMD>();
				bool needSeparator = false;
				foreach (var group in groups!) {
					items.Clear();
					foreach (var item in group.Items) {
						if (item.ToolBarItem.IsVisible(ctx))
							items.Add(item);
					}
					if (items.Count == 0)
						continue;
					if (needSeparator)
						toolBar.Items.Add(new Separator());
					needSeparator = true;

					foreach (var item in items) {
						var obj = Create(item, ctx, commandTarget);
						if (obj is not null)
							toolBar.Items.Add(obj);
					}
				}
			}

			CommandManager.InvalidateRequerySuggested();
			return toolBar;
		}

		object? Create(ToolBarItemMD md, IToolBarItemContext ctx, IInputElement? commandTarget) {
			if (md is ToolBarButtonMD mdButton)
				return Create(mdButton, ctx, commandTarget);

			if (md is ToolBarObjectMD mdObj)
				return Create(mdObj, ctx, commandTarget);

			Debug.Fail("Unknown TB MD");
			return null;
		}

		object Create(ToolBarButtonMD md, IToolBarItemContext ctx, IInputElement? commandTarget) {
			var item = (IToolBarButton)md.ToolBarItem;
			var md2 = (IToolBarButtonMetadata)md.Metadata;

			var cmdHolder = item as ICommandHolder;
			var cmd = cmdHolder is not null ? cmdHolder.Command : new RelayCommand(a => item.Execute(ctx), a => item.IsEnabled(ctx));

			var header = ResourceHelper.GetStringOrNull(item, md2.Header);
			var icon = md2.Icon;
			var toolTip = ResourceHelper.GetStringOrNull(item, md2.ToolTip);
			header = item.GetHeader(ctx) ?? header;
			toolTip = item.GetToolTip(ctx) ?? toolTip;

			var imgRef = item.GetIcon(ctx) ?? ImageReferenceHelper.GetImageReference(item, icon) ?? default;
			var toggleButtonCmd = item as IToolBarToggleButton;
			Debug2.Assert(md2.IsToggleButton == (toggleButtonCmd is not null), "Implement IToolBarToggleButton if IsToggleButton is true");
			if (toggleButtonCmd is not null)
				return CreateToggleButton(toggleButtonCmd.GetBinding(ctx), cmd, commandTarget, header, toolTip, imgRef);
			return new ToolBarButtonVM(cmd, commandTarget, header, toolTip, imgRef);
		}

		object CreateToggleButton(Binding binding, ICommand command, IInputElement? commandTarget, string? header, string? toolTip, ImageReference imgRef) {
			var sp = new StackPanel();
			sp.Orientation = Orientation.Horizontal;
			if (!imgRef.IsDefault)
				sp.Children.Add(new DsImage { ImageReference = imgRef });
			if (!string.IsNullOrEmpty(header)) {
				sp.Children.Add(new TextBlock {
					Text = header,
					Margin = new Thickness(5, 0, 5, 0),
				});
			}
			var checkBox = new CheckBox { Content = sp };
			Debug2.Assert(binding is not null);
			if (binding is not null)
				checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
			if (!string.IsNullOrEmpty(toolTip))
				checkBox.ToolTip = toolTip;
			checkBox.FocusVisualStyle = null;
			return checkBox;
		}

		object Create(ToolBarObjectMD md, IToolBarItemContext ctx, IInputElement? commandTarget) {
			var item = (IToolBarObject)md.ToolBarItem;
			return item.GetUIObject(ctx, commandTarget);
		}
	}
}
