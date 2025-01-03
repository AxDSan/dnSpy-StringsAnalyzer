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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	sealed class RegisteredCommandElement : IRegisteredCommandElement {
		public ICommandTargetCollection CommandTarget { get; }
		readonly CommandService commandService;
		readonly KeyShortcutCollection keyShortcutCollection;
		readonly List<CommandTargetFilterInfo> commandTargetInfos;
		WeakReference weakSourceElement;
		WeakReference weakTarget;

		readonly struct CommandTargetFilterInfo {
			public ICommandTargetFilter Filter { get; }
			public double Order { get; }

			public CommandTargetFilterInfo(ICommandTargetFilter filter, double order) {
				Filter = filter;
				Order = order;
			}
		}

		sealed class CommandTargetCollection : ICommandTargetCollection {
			RegisteredCommandElement? registeredCommandElement;

			public CommandTargetCollection(RegisteredCommandElement registeredCommandElement) => this.registeredCommandElement = registeredCommandElement;

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				if (registeredCommandElement?.TryGetTargetOrUnregister() is null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.CanExecute(group, cmdId);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
				object? result = null;
				return Execute(group, cmdId, args, ref result);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
				if (registeredCommandElement?.TryGetTargetOrUnregister() is null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.Execute(group, cmdId, args, ref result);
			}

			public void AddFilter(ICommandTargetFilter filter, double order) =>
				registeredCommandElement?.AddFilter(filter, order);
			public void RemoveFilter(ICommandTargetFilter filter) =>
				registeredCommandElement?.RemoveFilter(filter);
		}

		sealed class NextCommandTarget : ICommandTarget {
			readonly WeakReference filterWeakRef;
			readonly WeakReference ownerWeakRef;

			public NextCommandTarget(RegisteredCommandElement owner, ICommandTargetFilter filter) {
				ownerWeakRef = new WeakReference(owner);
				filterWeakRef = new WeakReference(filter);
			}

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				if (filterWeakRef.Target is ICommandTargetFilter filter && ownerWeakRef.Target is RegisteredCommandElement owner)
					return owner.CanExecuteNext(filter, group, cmdId);
				return CommandTargetStatus.NotHandled;
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
				object? result = null;
				return Execute(group, cmdId, args, ref result);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
				if (filterWeakRef.Target is ICommandTargetFilter filter && ownerWeakRef.Target is RegisteredCommandElement owner)
					owner.ExecuteNext(filter, group, cmdId, args, ref result);
				return CommandTargetStatus.NotHandled;
			}
		}

		public RegisteredCommandElement(CommandService commandService, UIElement sourceElement, KeyShortcutCollection keyShortcutCollection, object target) {
			if (sourceElement is null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
			weakSourceElement = new WeakReference(sourceElement);
			weakTarget = new WeakReference(target);
			this.keyShortcutCollection = keyShortcutCollection ?? throw new ArgumentNullException(nameof(keyShortcutCollection));
			commandTargetInfos = new List<CommandTargetFilterInfo>();
			CommandTarget = new CommandTargetCollection(this);
			sourceElement.PreviewKeyDown += SourceElement_PreviewKeyDown;
			sourceElement.PreviewTextInput += SourceElement_PreviewTextInput;
		}

		UIElement? TryGetSourceElement() => weakSourceElement.Target as UIElement;
		object? TryGetTarget() => weakTarget.Target as object;

		object? TryGetTargetOrUnregister() {
			if (TryGetTarget() is object target)
				return target;

			Unregister();
			return null;
		}

		CommandInfo GetCommand(KeyEventArgs e, object target, out bool waitForSecondKey) {
			var keyInput = new KeyInput(e);
			ProviderAndCommand result;
			if (prevKey is not null) {
				waitForSecondKey = false;
				var keyShortcut = new KeyShortcut(prevKey.Value, keyInput);
				result = keyShortcutCollection.GetTwoKeyShortcuts(keyShortcut).FirstOrDefault();
				prevKey = null;
			}
			else {
				if (keyShortcutCollection.IsTwoKeyCombo(keyInput)) {
					waitForSecondKey = true;
					prevKey = keyInput;
					result = default;
				}
				else {
					waitForSecondKey = false;
					result = keyShortcutCollection.GetOneKeyShortcuts(keyInput).FirstOrDefault();
				}
			}
			if (result.IsDefault)
				return StandardIds.Unknown.ToCommandInfo();
			return result.Command;
		}
		KeyInput? prevKey;

		void SourceElement_PreviewKeyDown(object? sender, KeyEventArgs e) {
			var target = TryGetTargetOrUnregister();
			if (target is null)
				return;
			var cmd = GetCommand(e, target, out bool waitForSecondKey);
			if (waitForSecondKey) {
				e.Handled = true;
				return;
			}
			ExecuteCommand(cmd, e);
		}

		void ExecuteCommand(CommandInfo cmd, RoutedEventArgs e) {
			if (CommandTarget.CanExecute(cmd.Group, cmd.ID) != CommandTargetStatus.Handled)
				return;

			object? result = null;
			var res = CommandTarget.Execute(cmd.Group, cmd.ID, cmd.Arguments, ref result);
			if (res == CommandTargetStatus.Handled)
				e.Handled = true;
		}

		void SourceElement_PreviewTextInput(object? sender, TextCompositionEventArgs e) {
			Debug2.Assert(prevKey is null);
			prevKey = null;
			var target = TryGetTargetOrUnregister();
			if (target is null)
				return;

			var cmd = commandService.CreateCommandInfo(target, e.Text);
			if (cmd is null)
				return;

			ExecuteCommand(cmd.Value, e);
		}

		CommandTargetStatus CanExecute(Guid group, int cmdId) => CanExecute(0, group, cmdId);
		CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) =>
			Execute(0, group, cmdId, args, ref result);

		CommandTargetStatus CanExecuteNext(ICommandTargetFilter filter, Guid group, int cmdId) {
			int index = IndexOf(filter);
			if (index < 0)
				return CommandTargetStatus.NotHandled;
			return CanExecute(index + 1, group, cmdId);
		}

		CommandTargetStatus ExecuteNext(ICommandTargetFilter filter, Guid group, int cmdId, object? args, ref object? result) {
			int index = IndexOf(filter);
			if (index < 0)
				return CommandTargetStatus.NotHandled;
			return Execute(index + 1, group, cmdId, args, ref result);
		}

		CommandTargetStatus CanExecute(int currentIndex, Guid group, int cmdId) {
			if (currentIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(currentIndex));
			var infos = commandTargetInfos.ToArray();
			for (int i = currentIndex; i < infos.Length; i++) {
				var res = infos[i].Filter.CanExecute(group, cmdId);
				if (res == CommandTargetStatus.Handled)
					return res;
				if (res == CommandTargetStatus.LetWpfHandleCommand)
					return CommandTargetStatus.NotHandled;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			return CommandTargetStatus.NotHandled;
		}

		CommandTargetStatus Execute(int currentIndex, Guid group, int cmdId, object? args, ref object? result) {
			if (currentIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(currentIndex));
			var infos = commandTargetInfos.ToArray();
			for (int i = currentIndex; i < infos.Length; i++) {
				result = null;
				var res = infos[i].Filter.Execute(group, cmdId, args, ref result);
				if (res == CommandTargetStatus.Handled)
					return res;
				if (res == CommandTargetStatus.LetWpfHandleCommand)
					return CommandTargetStatus.NotHandled;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			result = null;
			return CommandTargetStatus.NotHandled;
		}

		int GetNewFilterIndex(double order) {
			for (int i = 0; i < commandTargetInfos.Count; i++) {
				if (order <= commandTargetInfos[i].Order)
					return i;
			}
			return commandTargetInfos.Count;
		}

		int IndexOf(ICommandTargetFilter filter) {
			for (int i = 0; i < commandTargetInfos.Count; i++) {
				if (commandTargetInfos[i].Filter == filter)
					return i;
			}
			return -1;
		}

		public void AddFilter(ICommandTargetFilter filter, double order) {
			if (filter is null)
				throw new ArgumentNullException(nameof(filter));
			if (IndexOf(filter) >= 0)
				throw new ArgumentException("Filter has already been added to the list");
			int index = GetNewFilterIndex(order);
			commandTargetInfos.Insert(index, new CommandTargetFilterInfo(filter, order));
			filter.SetNextCommandTarget(new NextCommandTarget(this, filter));
		}

		public void RemoveFilter(ICommandTargetFilter filter) {
			if (filter is null)
				throw new ArgumentNullException(nameof(filter));
			int index = IndexOf(filter);
			if (index < 0)
				return;
			commandTargetInfos.RemoveAt(index);
		}

		public void Unregister() {
			if (TryGetSourceElement() is UIElement sourceElement) {
				sourceElement.PreviewKeyDown -= SourceElement_PreviewKeyDown;
				sourceElement.PreviewTextInput -= SourceElement_PreviewTextInput;
			}
			weakSourceElement = new WeakReference(null);
			weakTarget = new WeakReference(null);
			foreach (var c in commandTargetInfos.ToArray())
				c.Filter.Dispose();
			commandTargetInfos.Clear();
		}
	}
}
