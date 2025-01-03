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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Compiler {
	partial class EditCodeDlg : WindowBase {
		public EditCodeDlg() {
			InitializeComponent();

			decompilingControl.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));

			DataContextChanged += (s, e) => {
				if (DataContext is EditCodeVM vm) {
					vm.PropertyChanged += EditCodeVM_PropertyChanged;
					vm.OwnerWindow = this;
					vm.CodeCompiled += EditCodeVM_CodeCompiled;
					if (vm.HasDecompiled)
						RemoveProgressBar();
					else {
						vm.PropertyChanged += (s2, e2) => {
							if (e2.PropertyName == nameof(vm.HasDecompiled) && vm.HasDecompiled)
								RemoveProgressBar();
						};
					}
					InputBindings.Add(new KeyBinding(vm.AddGacReferenceCommand, Key.O, ModifierKeys.Control | ModifierKeys.Shift));
					InputBindings.Add(new KeyBinding(vm.AddAssemblyReferenceCommand, Key.O, ModifierKeys.Control));
					InputBindings.Add(new KeyBinding(vm.AddDocumentsCommand, Key.A, ModifierKeys.Control | ModifierKeys.Shift));
					InputBindings.Add(new KeyBinding(vm.GoToNextDiagnosticCommand, Key.F4, ModifierKeys.None));
					InputBindings.Add(new KeyBinding(vm.GoToNextDiagnosticCommand, Key.F8, ModifierKeys.None));
					InputBindings.Add(new KeyBinding(vm.GoToPreviousDiagnosticCommand, Key.F4, ModifierKeys.Shift));
					InputBindings.Add(new KeyBinding(vm.GoToPreviousDiagnosticCommand, Key.F8, ModifierKeys.Shift));
				}
			};
			diagnosticsListView.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
				(s, e) => CopyToClipboard(diagnosticsListView.SelectedItems.OfType<CompilerDiagnosticVM>().ToArray()),
				(s, e) => e.CanExecute = diagnosticsListView.SelectedItems.Count != 0));
			diagnosticsListView.SelectionChanged += DiagnosticsListView_SelectionChanged;
		}

		void EditCodeVM_CodeCompiled(object? sender, EventArgs e) {
			((EditCodeVM)sender!).CodeCompiled -= EditCodeVM_CodeCompiled;
			ClickOK();
		}

		protected override void OnClosed(EventArgs e) {
			progressBar.IsIndeterminate = false;
			base.OnClosed(e);
			if (DataContext is EditCodeVM vm)
				vm.CodeCompiled -= EditCodeVM_CodeCompiled;
		}

		void RemoveProgressBar() {
			// An indeterminate progress bar that is collapsed still animates so make sure
			// it's not in the tree at all.
			decompilingControl.Child = null;
			decompilingControl.Visibility = Visibility.Collapsed;
		}

		void diagnosticsListView_MouseDoubleClick(object? sender, MouseButtonEventArgs e) {
			if (!UIUtilities.IsLeftDoubleClick<ListViewItem>(diagnosticsListView, e))
				return;

			var vm = DataContext as EditCodeVM;
			var diag = diagnosticsListView.SelectedItem as CompilerDiagnosticVM;
			Debug2.Assert(vm is not null && diag is not null);
			if (vm is null || diag is null)
				return;

			vm.MoveTo(diag);
		}

		void EditCodeVM_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var vm = (EditCodeVM)sender!;
			if (e.PropertyName == nameof(vm.SelectedDocument))
				UIUtilities.Focus(vm.SelectedDocument?.TextView.VisualElement);
		}

		void DiagnosticsListView_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
			var item = diagnosticsListView.SelectedItem;
			if (item is null)
				return;
			diagnosticsListView.ScrollIntoView(item);
		}

		void CopyToClipboard(CompilerDiagnosticVM[] diags) {
			if (diags.Length == 0)
				return;

			var sb = new StringBuilder();

			foreach (var header in viewHeaders) {
				if (sb.Length > 0)
					sb.Append('\t');
				sb.Append(header);
			}
			sb.AppendLine();

			foreach (var d in diags) {
				d.WriteTo(sb);
				sb.AppendLine();
			}

			if (sb.Length > 0) {
				try {
					Clipboard.SetText(sb.ToString());
				}
				catch (ExternalException) { }
			}
		}
		static readonly string[] viewHeaders = new string[] {
			dnSpy_AsmEditor_Resources.CompileDiagnostics_Header_Severity,
			dnSpy_AsmEditor_Resources.CompileDiagnostics_Header_Code,
			dnSpy_AsmEditor_Resources.CompileDiagnostics_Header_Description,
			dnSpy_AsmEditor_Resources.CompileDiagnostics_Header_File,
			dnSpy_AsmEditor_Resources.CompileDiagnostics_Header_Line,
		};
	}
}
