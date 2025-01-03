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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Field attributes
	/// </summary>
	[Flags]
	public enum DmdFieldAttributes : ushort {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		FieldAccessMask		= 0x0007,
		PrivateScope		= 0x0000,
		Private				= 0x0001,
		FamANDAssem			= 0x0002,
		Assembly			= 0x0003,
		Family				= 0x0004,
		FamORAssem			= 0x0005,
		Public				= 0x0006,
		Static				= 0x0010,
		InitOnly			= 0x0020,
		Literal				= 0x0040,
		NotSerialized		= 0x0080,
		SpecialName			= 0x0200,
		PinvokeImpl			= 0x2000,
		RTSpecialName		= 0x0400,
		HasFieldMarshal		= 0x1000,
		HasDefault			= 0x8000,
		HasFieldRVA			= 0x0100,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
