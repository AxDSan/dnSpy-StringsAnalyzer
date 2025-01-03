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
using System.IO;
using System.Xml.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	readonly struct XmlSettingsReader {
		readonly ISettingsService mgr;
		readonly string filename;

		public XmlSettingsReader(ISettingsService mgr, string? filename = null) {
			this.mgr = mgr;
			this.filename = filename ?? AppDirectories.SettingsFilename;
		}

		public void Read() {
			if (!File.Exists(filename))
				return;
			var doc = XDocument.Load(filename, LoadOptions.None);
			var root = doc.Root;
			if (root?.Name == XmlSettingsConstants.XML_ROOT_NAME)
				Read(root);
		}

		void Read(XElement root) {
			foreach (var xmlSect in root.Elements(XmlSettingsConstants.SECTION_NAME)) {
				var name = XmlUtils.UnescapeAttributeValue((string?)xmlSect.Attribute(XmlSettingsConstants.SECTION_ATTRIBUTE_NAME));
				if (name is null)
					continue;
				if (!Guid.TryParse(name, out var guid))
					continue;
				var section = mgr.GetOrCreateSection(guid);
				ReadSection(xmlSect, section, 0);
			}
		}

		void ReadSection(XElement xml, ISettingsSection section, int recursionCounter) {
			if (recursionCounter >= XmlSettingsConstants.MAX_CHILD_DEPTH)
				return;

			foreach (var xmlAttr in xml.Attributes()) {
				var attrName = xmlAttr.Name.LocalName;
				if (attrName == XmlSettingsConstants.SECTION_ATTRIBUTE_NAME)
					continue;
				section.Attribute(attrName, XmlUtils.UnescapeAttributeValue(xmlAttr.Value));
			}

			foreach (var xmlSect in xml.Elements(XmlSettingsConstants.SECTION_NAME)) {
				var name = XmlUtils.UnescapeAttributeValue((string?)xmlSect.Attribute(XmlSettingsConstants.SECTION_ATTRIBUTE_NAME));
				if (name is null)
					continue;
				var childSection = section.CreateSection(name);
				ReadSection(xmlSect, childSection, recursionCounter + 1);
			}
		}
	}
}
