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
using System.Runtime.InteropServices;

namespace dnSpy.Hex {
	static class NativeMethods {
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesWritten);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
		public const uint PAGE_NOACCESS = 0x01;
		public const uint PAGE_READONLY = 0x02;
		public const uint PAGE_READWRITE = 0x04;
		public const uint PAGE_WRITECOPY = 0x08;
		public const uint PAGE_EXECUTE = 0x10;
		public const uint PAGE_EXECUTE_READ = 0x20;
		public const uint PAGE_EXECUTE_READWRITE = 0x40;
		public const uint PAGE_EXECUTE_WRITECOPY = 0x80;

		public const uint PAGE_GUARD = 0x100;
		public const uint PAGE_NOCACHE = 0x200;
		public const uint PAGE_WRITECOMBINE = 0x400;

		[DllImport("kernel32", SetLastError = true, EntryPoint = "VirtualQueryEx")]
		public static extern int VirtualQueryEx32(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);
		[DllImport("kernel32", SetLastError = true, EntryPoint = "VirtualQueryEx")]
		public static extern int VirtualQueryEx64(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, ulong dwLength);

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION32 {
			public uint BaseAddress;
			public uint AllocationBase;
			public uint AllocationProtect;
			public uint RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION64 {
			public ulong BaseAddress;
			public ulong AllocationBase;
			public uint AllocationProtect;
			public uint __alignment1;
			public ulong RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
			public uint __alignment2;
		}
		public const int MEMORY_BASIC_INFORMATION32_SIZE = 7 * 4;
		public const int MEMORY_BASIC_INFORMATION64_SIZE = 3 * 8 + 6 * 4;
		public const uint MEM_COMMIT = 0x1000;
		public const uint MEM_FREE = 0x10000;
		public const uint MEM_RESERVE = 0x2000;

		[DllImport("kernel32", SetLastError = true)]
		public static extern uint GetProcessId(IntPtr hProcess);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

		[DllImport("kernel32", SetLastError = false)]
		public static extern void GetSystemInfo(out SYSTEM_INFO Info);

		[DllImport("kernel32", SetLastError = false)]
		public static extern void GetNativeSystemInfo(out SYSTEM_INFO Info);

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO {
			public uint dwOemId;
			public uint dwPageSize;
			public IntPtr lpMinimumApplicationAddress;
			public IntPtr lpMaximumApplicationAddress;
			public IntPtr dwActiveProcessorMask;
			public uint dwNumberOfProcessors;
			public uint dwProcessorType;
			public uint dwAllocationGranularity;
			public ushort wProcessorLevel;
			public ushort wProcessorRevision;
		}
	}
}
