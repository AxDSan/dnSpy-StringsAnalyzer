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
using System.Globalization;
using System.Windows.Data;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class TabColumnConverter : IValueConverter {
		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) => writer.Clear();
		}

		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as TabVM;
			var tag = parameter as string;
			if (vm is null || tag is null)
				return null;

			var writer = Cache.GetWriter();
			try {
				var printer = new TabPrinter(writer);
				if (tag == PredefinedTextClassifierTags.TabsDialogName)
					printer.WriteName(vm);
				else if (tag == PredefinedTextClassifierTags.TabsDialogModule)
					printer.WriteModule(vm);
				else if (tag == PredefinedTextClassifierTags.TabsDialogPath)
					printer.WritePath(vm);
				else
					return null;

				var context = new TextClassifierContext(writer.Text, tag, vm.Owner.Settings.SyntaxHighlight, writer.Colors);
				return vm.Owner.TextElementProvider.CreateTextElement(vm.Owner.ClassificationFormatMap, context, ContentTypes.TabsDialog, TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis);
			}
			finally {
				Cache.FreeWriter(writer);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
