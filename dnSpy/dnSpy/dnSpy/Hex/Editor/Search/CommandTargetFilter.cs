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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor.Search {
	sealed class CommandTargetFilter : ICommandTargetFilter {
		readonly HexViewSearchService hexViewSearchService;

		public CommandTargetFilter(HexViewSearchServiceProvider hexViewSearchServiceProvider, WpfHexView wpfHexView) => hexViewSearchService = hexViewSearchServiceProvider.Get(wpfHexView);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Find:
				case StandardIds.Replace:
				case StandardIds.IncrementalSearchForward:
				case StandardIds.IncrementalSearchBackward:
				case StandardIds.FindNext:
				case StandardIds.FindPrevious:
				case StandardIds.FindNextSelected:
				case StandardIds.FindPreviousSelected:
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Find:
					hexViewSearchService.ShowFind();
					return CommandTargetStatus.Handled;

				case StandardIds.Replace:
					hexViewSearchService.ShowReplace();
					return CommandTargetStatus.Handled;

				case StandardIds.IncrementalSearchForward:
					hexViewSearchService.ShowIncrementalSearch(true);
					return CommandTargetStatus.Handled;

				case StandardIds.IncrementalSearchBackward:
					hexViewSearchService.ShowIncrementalSearch(false);
					return CommandTargetStatus.Handled;

				case StandardIds.FindNext:
					hexViewSearchService.FindNext(true);
					return CommandTargetStatus.Handled;

				case StandardIds.FindPrevious:
					hexViewSearchService.FindNext(false);
					return CommandTargetStatus.Handled;

				case StandardIds.FindNextSelected:
					hexViewSearchService.FindNextSelected(true);
					return CommandTargetStatus.Handled;

				case StandardIds.FindPreviousSelected:
					hexViewSearchService.FindNextSelected(false);
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}

	sealed class CommandTargetFilterFocus : ICommandTargetFilter {
		readonly HexViewSearchService hexViewSearchService;

		public CommandTargetFilterFocus(HexViewSearchServiceProvider hexViewSearchServiceProvider, WpfHexView wpfHexView) => hexViewSearchService = hexViewSearchServiceProvider.Get(wpfHexView);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) => hexViewSearchService.CanExecuteSearchControl(group, cmdId);

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) =>
			hexViewSearchService.ExecuteSearchControl(group, cmdId, args, ref result);

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
