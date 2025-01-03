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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using dnSpy.Contracts.Debugger.StartDebugging;

namespace dnSpy.Debugger.DbgUI {
	[ExportDbgProcessStarter(PredefinedDbgProcessStarterOrders.DefaultExe)]
	sealed class DefaultDbgProcessStarter : DbgProcessStarter {
		public override bool IsSupported(string filename, out ProcessStarterResult result) {
			result = ProcessStarterResult.None;

			if (!PortableExecutableFileHelpers.IsExecutable(filename))
				return false;

			if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(filename), ".exe"))
				result |= ProcessStarterResult.WrongExtension;
			return true;
		}

		public override bool TryStart(string filename, [NotNullWhen(false)] out string? error) {
			var startInfo = new ProcessStartInfo(filename);
			startInfo.WorkingDirectory = Path.GetDirectoryName(filename)!;
			startInfo.UseShellExecute = false;
			Process.Start(startInfo);
			error = null;
			return true;
		}
	}
}
