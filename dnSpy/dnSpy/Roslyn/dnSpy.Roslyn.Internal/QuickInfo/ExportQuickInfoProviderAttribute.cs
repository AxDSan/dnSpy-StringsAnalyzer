// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace dnSpy.Roslyn.Internal.QuickInfo {
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class)]
	internal class ExportQuickInfoProviderAttribute : ExportAttribute {
		public string Name { get; }
		public string Language { get; }

		public ExportQuickInfoProviderAttribute(string name, string language)
			: base(typeof(IQuickInfoProvider)) {
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Language = language ?? throw new ArgumentNullException(nameof(language));
		}
	}
}
