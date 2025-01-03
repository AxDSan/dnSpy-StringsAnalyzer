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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Text.Editor;

namespace dnSpy.Text.Settings {
	abstract class TabsAppSettingsPageBase : AppSettingsPage {
		public sealed override string Title => dnSpy_Resources.TabsSettings;
		public sealed override object? UIObject => this;

		public Int32VM TabSizeVM { get; }
		public Int32VM IndentSizeVM { get; }

		public bool ConvertTabsToSpaces {
			get => convertTabsToSpaces;
			set {
				if (convertTabsToSpaces != value) {
					convertTabsToSpaces = value;
					OnPropertyChanged(nameof(ConvertTabsToSpaces));
				}
			}
		}
		bool convertTabsToSpaces;

		readonly ICommonEditorOptions options;

		protected TabsAppSettingsPageBase(ICommonEditorOptions options) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			TabSizeVM = new Int32VM(options.TabSize, a => { }, true) { Min = OptionsHelpers.MinimumTabSize, Max = OptionsHelpers.MaximumTabSize };
			IndentSizeVM = new Int32VM(options.IndentSize, a => { }, true) { Min = OptionsHelpers.MinimumIndentSize, Max = OptionsHelpers.MaximumIndentSize };
			ConvertTabsToSpaces = options.ConvertTabsToSpaces;
		}

		public override void OnApply() {
			if (!TabSizeVM.HasError)
				options.TabSize = TabSizeVM.Value;
			if (!IndentSizeVM.HasError)
				options.IndentSize = IndentSizeVM.Value;
			options.ConvertTabsToSpaces = ConvertTabsToSpaces;
		}
	}
}
