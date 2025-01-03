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
using System.Diagnostics;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Resources {
	sealed class ResourceOptions {
		public ResourceType ResourceType;
		public UTF8String? Name;
		public ManifestResourceAttributes Attributes;
		public AssemblyRef? Assembly;
		public FileDef? File;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public ResourceOptions() {
		}

		public ResourceOptions(Resource resource) {
			ResourceType = resource.ResourceType;
			Name = resource.Name ?? UTF8String.Empty;
			Attributes = resource.Attributes;
			CustomAttributes.AddRange(resource.CustomAttributes);
			switch (resource.ResourceType) {
			case ResourceType.Embedded:
				break;

			case ResourceType.AssemblyLinked:
				Assembly = ((AssemblyLinkedResource)resource).Assembly;
				break;

			case ResourceType.Linked:
				File = ((LinkedResource)resource).File;
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		public void CopyTo(Resource resource) {
			switch (ResourceType) {
			case dnlib.DotNet.ResourceType.Embedded:
				// Always cast it to catch errors
				var er = (EmbeddedResource)resource;
				break;

			case dnlib.DotNet.ResourceType.AssemblyLinked:
				var al = (AssemblyLinkedResource)resource;
				Debug2.Assert(Assembly is not null);
				al.Assembly = Assembly;
				break;

			case dnlib.DotNet.ResourceType.Linked:
				var lr = (LinkedResource)resource;
				Debug2.Assert(File is not null);
				lr.File = File;
				break;

			default:
				throw new InvalidOperationException();
			}

			resource.Name = Name;
			resource.Attributes = Attributes;
			resource.CustomAttributes.Clear();
			resource.CustomAttributes.AddRange(CustomAttributes);
		}
	}
}
