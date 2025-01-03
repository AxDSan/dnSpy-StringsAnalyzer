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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs.Dialogs {
	interface ITabsVMSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
	}

	class TabsVMSettings : ViewModelBase, ITabsVMSettings {
		public bool SyntaxHighlight {
			get => syntaxHighlight;
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
				}
			}
		}
		bool syntaxHighlight = true;
	}

	[Export(typeof(ITabsVMSettings))]
	sealed class TabsVMSettingsImpl : TabsVMSettings {
		static readonly Guid SETTINGS_GUID = new Guid("EB2D9511-93B9-4985-BB99-1758BF2A5ADE");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		TabsVMSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			PropertyChanged += TabsVMSettingsImpl_PropertyChanged;
		}

		void TabsVMSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
		}
	}
}
