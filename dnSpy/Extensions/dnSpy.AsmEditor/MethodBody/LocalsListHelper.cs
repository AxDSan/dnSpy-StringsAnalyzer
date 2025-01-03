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
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class LocalsListHelper : ListBoxHelperBase<LocalVM> {
		CilBodyVM? cilBodyVM;
		readonly TypeSigCreator typeSigCreator;

		protected override string AddNewBeforeSelectionMessage => dnSpy_AsmEditor_Resources.Local_Command1;
		protected override string AddNewAfterSelectionMessage => dnSpy_AsmEditor_Resources.Local_Command2;
		protected override string AppendNewMessage => dnSpy_AsmEditor_Resources.Local_Command3;
		protected override string RemoveSingularMessage => dnSpy_AsmEditor_Resources.Local_Command4;
		protected override string RemovePluralMessage => dnSpy_AsmEditor_Resources.Local_Command5;
		protected override string RemoveAllMessage => dnSpy_AsmEditor_Resources.Local_Command6;

		public LocalsListHelper(ListView listView, Window ownerWindow)
			: base(listView) => typeSigCreator = new TypeSigCreator(ownerWindow);

		protected override LocalVM[] GetSelectedItems() => listBox.SelectedItems.Cast<LocalVM>().ToArray();

		protected override void OnDataContextChangedInternal(object dataContext) {
			cilBodyVM = ((MethodBodyVM)dataContext).CilBodyVM;
			coll = ((MethodBodyVM)dataContext).CilBodyVM.LocalsListVM;
			coll.CollectionChanged += coll_CollectionChanged;
			InitializeLocals(coll);

			AddStandardMenuHandlers();
		}

		void coll_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems is not null)
				InitializeLocals(e.NewItems);
		}

		void InitializeLocals(System.Collections.IList list) {
			foreach (LocalVM? local in list)
				local!.TypeSigCreator = typeSigCreator;
		}

		protected override void CopyItemsAsText(LocalVM[] locals) {
			Array.Sort(locals, (a, b) => a.Index.CompareTo(b.Index));

			var output = new StringBuilderTextColorOutput();

			for (int i = 0; i < locals.Length; i++) {
				if (i > 0)
					output.WriteLine();

				var local = locals[i];
				output.Write(BoxedTextColor.Number, local.Index.ToString());
				output.Write(BoxedTextColor.Text, "\t");
				output.Write(BoxedTextColor.Text, local.IsPinned ? dnSpy_AsmEditor_Resources.Local_Pinned_Character : string.Empty);
				output.Write(BoxedTextColor.Text, "\t");
				output.Write(BoxedTextColor.Text, local.DebuggerHidden ? dnSpy_AsmEditor_Resources.Local_CompilerGenerated_Character : string.Empty);
				output.Write(BoxedTextColor.Text, "\t");
				output.Write(BoxedTextColor.Local, local.Name ?? string.Empty);
				output.Write(BoxedTextColor.Text, "\t");
				BodyUtils.WriteObject(output, local.Type);
			}
			if (locals.Length > 1)
				output.WriteLine();

			try {
				Clipboard.SetText(output.ToString());
			}
			catch (ExternalException) { }
		}

		protected override bool CanUseClipboardData(LocalVM[] data, bool fromThisInstance) => true;

		protected override LocalVM[] BeforeCopyingData(LocalVM[] data, bool fromThisInstance) {
			if (fromThisInstance)
				return data;
			var newData = new LocalVM[data.Length];
			for (int i = 0; i < data.Length; i++)
				newData[i] = data[i].Import(cilBodyVM!.TypeSigCreatorOptions, cilBodyVM.OwnerModule);
			return newData;
		}
	}
}
