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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;

namespace dnSpy.Controls {
	sealed class WpfCommands : IWpfCommands {
		readonly List<WeakReference> uiElements;
		readonly List<CommandBinding> commandBindings;
		readonly List<InputBinding> inputBindings;
		readonly HashSet<CMKKey> addedKeyModifiers;

		public Guid Guid => guid;
		readonly Guid guid;

		readonly struct CMKKey : IEquatable<CMKKey> {
			readonly ICommand command;
			readonly ModifierKeys modifiers;
			readonly Key key;

			public CMKKey(ICommand command, ModifierKeys modifiers, Key key) {
				this.command = command;
				this.modifiers = modifiers;
				this.key = key;
			}

			public bool Equals(CMKKey other) =>
				command == other.command && modifiers == other.modifiers && key == other.key;

			public override bool Equals(object? obj) {
				if (obj is CMKKey)
					return Equals((CMKKey)obj);
				return false;
			}

			public override int GetHashCode() =>
				(command?.GetHashCode() ?? 0) ^ modifiers.GetHashCode() ^ key.GetHashCode();
		}

		public WpfCommands(Guid guid) {
			this.guid = guid;
			uiElements = new List<WeakReference>();
			commandBindings = new List<CommandBinding>();
			inputBindings = new List<InputBinding>();
			addedKeyModifiers = new HashSet<CMKKey>();
		}

		IEnumerable<UIElement> UIElements {
			get {
				foreach (var r in uiElements) {
					if (r.Target is UIElement u)
						yield return u;
				}
			}
		}

		static InputBinding Clone(InputBinding inputBinding) {
			// We must clone it since it contains a reference to the UIElement
			return (InputBinding)inputBinding.Clone();
		}

		public void Add(UIElement elem) {
			if (elem is null)
				throw new ArgumentNullException(nameof(elem));
			uiElements.Add(new WeakReference(elem));
			foreach (var c in commandBindings)
				elem.CommandBindings.Add(c);
			foreach (var i in inputBindings)
				elem.InputBindings.Add(Clone(i));
		}

		public void Remove(UIElement elem) {
			if (elem is null)
				throw new ArgumentNullException(nameof(elem));
			for (int i = uiElements.Count - 1; i >= 0; i--) {
				var t = uiElements[i].Target;
				if (t == elem || t is null)
					uiElements.RemoveAt(i);
			}
		}

		void Add(CommandBinding commandBinding) {
			commandBindings.Add(commandBinding);
			foreach (var u in UIElements)
				u.CommandBindings.Add(commandBinding);
		}

		void Add(InputBinding inputBinding) {
			inputBindings.Add(inputBinding);
			foreach (var u in UIElements)
				u.InputBindings.Add(Clone(inputBinding));
		}

		void AddIfNotAdded(KeyBinding kb) {
			var key = new CMKKey(kb.Command, kb.Modifiers, kb.Key);
			if (addedKeyModifiers.Contains(key))
				return;
			addedKeyModifiers.Add(key);
			Add(kb);
		}

		public void Add(RoutedCommand command, ICommand realCommand, ModifierKeys modifiers = ModifierKeys.None, Key key = Key.None) =>
			Add(command, (s, e) => realCommand.Execute(e.Parameter), (s, e) => e.CanExecute = realCommand.CanExecute(e.Parameter), modifiers, key);

		public void Add(RoutedCommand command, ExecutedRoutedEventHandler exec, CanExecuteRoutedEventHandler canExec, ModifierKeys modifiers1 = ModifierKeys.None, Key key1 = Key.None, ModifierKeys modifiers2 = ModifierKeys.None, Key key2 = Key.None, ModifierKeys modifiers3 = ModifierKeys.None, Key key3 = Key.None) {
			Add(new CommandBinding(command, exec, canExec));
			if (key1 != Key.None)
				AddIfNotAdded(new KeyBinding(command, key1, modifiers1));
			if (key2 != Key.None)
				AddIfNotAdded(new KeyBinding(command, key2, modifiers2));
			if (key3 != Key.None)
				AddIfNotAdded(new KeyBinding(command, key3, modifiers3));
		}

		public void Add(ICommand command, ModifierKeys modifiers, Key key) {
			if (key != Key.None)
				AddIfNotAdded(new KeyBinding(command, key, modifiers));
		}
	}
}
