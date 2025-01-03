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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IIntellisenseSessionStackMapService))]
	sealed class IntellisenseSessionStackMapService : IIntellisenseSessionStackMapService {
		public IIntellisenseSessionStack GetStackForTextView(ITextView textView) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			var wpfTextView = textView as IWpfTextView;
			if (wpfTextView is null)
				throw new InvalidOperationException();
			return textView.Properties.GetOrCreateSingletonProperty(typeof(IntellisenseSessionStack), () => new IntellisenseSessionStack(wpfTextView));
		}
	}
}
