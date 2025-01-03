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

using dnSpy.Contracts.Command;

namespace dnSpy.Scripting.Roslyn.Commands {
	/// <summary>
	/// Extension methods
	/// </summary>
	static class RoslynReplIdsExtensions {
		/// <summary>
		/// Converts <paramref name="id"/> to a <see cref="CommandInfo"/>
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns></returns>
		public static CommandInfo ToCommandInfo(this RoslynReplIds id) => new CommandInfo(RoslynReplCommandConstants.RoslynReplGroup, (int)id);

		/// <summary>
		/// Converts <paramref name="id"/> to a <see cref="CommandInfo"/>
		/// </summary>
		/// <param name="id">ID</param>
		/// <param name="arguments">Arguments or null</param>
		/// <returns></returns>
		public static CommandInfo ToCommandInfo(this RoslynReplIds id, object? arguments) => new CommandInfo(RoslynReplCommandConstants.RoslynReplGroup, (int)id, arguments);
	}
}
