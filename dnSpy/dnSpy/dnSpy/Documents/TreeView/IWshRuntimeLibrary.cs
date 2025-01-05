/*
    Copyright (C) 2023 ElektroKill

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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IWshRuntimeLibrary {
	[ComImport]
	[Guid("F935DC21-1CF0-11D0-ADB9-00C04FD58A0B")]
	[TypeIdentifier]
	public interface IWshShell { }

	[ComImport]
	[Guid("24BE5A30-EDFE-11D2-B933-00104B365C9F")]
	[TypeIdentifier]
	public interface IWshShell2 : IWshShell { }

	[ComImport]
	[Guid("41904400-BE18-11D3-A28B-00104BD35090")]
	[TypeIdentifier]
	public interface IWshShell3 : IWshShell2 {
		[DispId(100)]
		object SpecialFolders {
			[DispId(100)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		[DispId(200)]
		object Environment {
			[DispId(200)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		[DispId(1000)]
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int Run([MarshalAs(UnmanagedType.BStr)] [In] string Command, [MarshalAs(UnmanagedType.Struct)] [In] ref object WindowStyle, [MarshalAs(UnmanagedType.Struct)] [In] ref object WaitOnReturn);

		[DispId(1001)]
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int Popup([MarshalAs(UnmanagedType.BStr)] [In] string Text, [MarshalAs(UnmanagedType.Struct)] [In] ref object SecondsToWait, [MarshalAs(UnmanagedType.Struct)] [In] ref object Title, [MarshalAs(UnmanagedType.Struct)] [In] ref object Type);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1002)]
		[return: MarshalAs(UnmanagedType.IDispatch)]
		object CreateShortcut([In][MarshalAs(UnmanagedType.BStr)] string PathLink);
	}

	[ComImport]
	[Guid("41904400-BE18-11D3-A28B-00104BD35090")]
	[CoClass(typeof(object))]
	[TypeIdentifier]
	public interface WshShell : IWshShell3 { }

	[ComImport]
	[DefaultMember("FullName")]
	[Guid("F935DC23-1CF0-11D0-ADB9-00C04FD58A0B")]
	[TypeIdentifier]
	public interface IWshShortcut {
		[DispId(0)]
		string FullName {
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[DispId(0)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(1000)]
		string Arguments {
			[DispId(1000)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(1000)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[param: MarshalAs(UnmanagedType.BStr)]
			[param: In]
			set;
		}

		[DispId(1001)]
		string Description {
			[DispId(1001)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(1001)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[param: MarshalAs(UnmanagedType.BStr)]
			[param: In]
			set;
		}

		[DispId(1002)]
		string Hotkey {
			[DispId(1002)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(1002)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[param: MarshalAs(UnmanagedType.BStr)]
			[param: In]
			set;
		}

		[DispId(1003)]
		string IconLocation {
			[DispId(1003)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(1003)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[param: MarshalAs(UnmanagedType.BStr)]
			[param: In]
			set;
		}

		[DispId(1004)]
		string RelativePath {
			[DispId(1004)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[param: MarshalAs(UnmanagedType.BStr)]
			[param: In]
			set;
		}

		[DispId(1005)]
		string TargetPath {
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[DispId(1005)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[DispId(1005)]
			[param: In]
			[param: MarshalAs(UnmanagedType.BStr)]
			set;
		}
	}
}
