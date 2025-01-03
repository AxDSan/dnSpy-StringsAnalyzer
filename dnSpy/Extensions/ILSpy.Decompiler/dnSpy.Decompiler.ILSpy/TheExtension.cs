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

using System.Collections.Generic;
using dnSpy.Contracts.Extension;
using dnSpy.Decompiler.ILSpy.Properties;

namespace dnSpy.Decompiler.ILSpy {
	[ExportExtension]
	sealed class TheExtension : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get { yield return "Themes/wpf.styles.templates.xaml"; }
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription = dnSpy_Decompiler_ILSpy_Resources.Plugin_ShortDescription,
			Copyright = "Copyright 2011-2014 AlphaSierraPapa for the SharpDevelop Team",
		};

		public void OnEvent(ExtensionEvent @event, object? obj) {
		}
	}
}
