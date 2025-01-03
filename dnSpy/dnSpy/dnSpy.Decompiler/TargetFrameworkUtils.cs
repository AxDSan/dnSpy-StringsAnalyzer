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

using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler {
	static class TargetFrameworkUtils {
		/// <summary>
		/// Gets the arch as a string
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static string GetArchString(ModuleDef module) {
			if (module is null)
				return "???";

			if (module.Machine.IsI386()) {
				// See https://github.com/dotnet/coreclr/blob/master/src/inc/corhdr.h
				int c = (module.Is32BitRequired ? 2 : 0) + (module.Is32BitPreferred ? 1 : 0);
				switch (c) {
				case 0: // no special meaning, MachineType and ILONLY flag determine image requirements
					if (!module.IsILOnly)
						return "x86";
					return dnSpy_Decompiler_Resources.Decompile_AnyCPU64BitPreferred;
				case 1: // illegal, reserved for future use
					return "???";
				case 2: // image is x86-specific
					return "x86";
				case 3: // image is platform neutral and prefers to be loaded 32-bit when possible
					return dnSpy_Decompiler_Resources.Decompile_AnyCPU32BitPreferred;
				}
			}

			return GetArchString(module.Machine);
		}

		/// <summary>
		/// Gets the arch as a string
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static string GetArchString(Machine machine) {
			if (machine.IsI386())
				return "x86";
			else if (machine.IsAMD64())
				return "x64";
			else if (machine == Machine.IA64)
				return "IA-64";
			else if (machine.IsARMNT())
				return "ARM";
			else if (machine.IsARM64())
				return "ARM64";
			else {
				Debug.Fail("Unknown machine");
				return machine.ToString();
			}
		}
	}
}
