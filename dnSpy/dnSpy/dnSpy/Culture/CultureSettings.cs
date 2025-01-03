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

namespace dnSpy.Culture {
	interface ICultureSettings : INotifyPropertyChanged {
		string UIName { get; }
	}

	class CultureSettings : ViewModelBase, ICultureSettings {
		public string UIName {
			get => uiName;
			set {
				if (uiName != value) {
					uiName = value;
					OnPropertyChanged(nameof(UIName));
				}
			}
		}
		string uiName = string.Empty;
	}

	[Export, Export(typeof(ICultureSettings))]
	sealed class CultureSettingsImpl : CultureSettings {
		static readonly Guid SETTINGS_GUID = new Guid("4D05C47D-3F6A-429E-9CB3-232E10D45468");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		CultureSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			UIName = sect.Attribute<string>(nameof(UIName)) ?? UIName;
			PropertyChanged += CultureSettingsImpl_PropertyChanged;
		}

		void CultureSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UIName), UIName);
		}
	}
}
