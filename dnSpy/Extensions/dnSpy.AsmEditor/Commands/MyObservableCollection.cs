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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Commands {
	sealed class MyObservableCollection<T> : ObservableCollection<T> {
		void OnPropertyChanged(string propName) => OnPropertyChanged(new PropertyChangedEventArgs(propName));

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}
		bool isEnabled = true;

		public int SelectedIndex {
			get => selectedIndex;
			set {
				if (selectedIndex != value) {
					selectedIndex = value;
					OnPropertyChanged(nameof(SelectedIndex));
				}
			}
		}
		int selectedIndex;

		public ICommand RemoveSelectedCommand => new RelayCommand(a => RemoveSelected(), a => RemoveSelectedCanExecute());
		public ICommand MoveSelectedUpCommand => new RelayCommand(a => MoveSelectedUp(), a => MoveSelectedUpCanExecute());
		public ICommand MoveSelectedDownCommand => new RelayCommand(a => MoveSelectedDown(), a => MoveSelectedDownCanExecute());

		void RemoveSelected() {
			if (!RemoveSelectedCanExecute())
				return;
			int index = SelectedIndex;
			RemoveAt(index);
			if (index < Count)
				SelectedIndex = index;
			else if (Count > 0)
				SelectedIndex = Count - 1;
			else
				SelectedIndex = -1;
		}

		bool RemoveSelectedCanExecute() => IsEnabled && SelectedIndex >= 0 && SelectedIndex < Count;

		void MoveSelectedUp() {
			if (!MoveSelectedUpCanExecute())
				return;
			int index = SelectedIndex;
			var item = this[index];
			RemoveAt(index);
			Insert(index - 1, item);
			SelectedIndex = index - 1;
		}

		bool MoveSelectedUpCanExecute() => IsEnabled && SelectedIndex > 0 && SelectedIndex < Count;

		void MoveSelectedDown() {
			if (!MoveSelectedDownCanExecute())
				return;
			int index = SelectedIndex;
			var item = this[index];
			RemoveAt(index);
			Insert(index + 1, item);
			SelectedIndex = index + 1;
		}

		bool MoveSelectedDownCanExecute() => IsEnabled && SelectedIndex >= 0 && SelectedIndex < Count - 1;
	}
}
