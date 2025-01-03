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
using System.Globalization;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageFileHeaderVM : HexVM {
		public override string Name { get; }
		public UInt16FlagsHexField MachineVM { get; }
		public UInt16HexField NumberOfSectionsVM { get; }
		public UInt32HexField TimeDateStampVM { get; }

		public string TimeDateStampString {
			get {
				if (TimeDateStampVM.DataFieldVM.HasError)
					return string.Empty;

				var date = EpochToDate((uint)TimeDateStampVM.DataFieldVM.ObjectValue!).ToLocalTime();
				return date.ToString(CultureInfo.CurrentCulture.DateTimeFormat);
			}
		}

		DateTime EpochToDate(uint val) => Epoch.AddSeconds(val);
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public UInt32HexField PointerToSymbolTableVM { get; }
		public UInt32HexField NumberOfSymbolsVM { get; }
		public UInt16HexField SizeOfOptionalHeaderVM { get; }
		public UInt16FlagsHexField CharacteristicsVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		static readonly IntegerHexBitFieldEnumInfo[] MachineInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0x014C, "I386"),
			new IntegerHexBitFieldEnumInfo(0x8664, "AMD64"),
			new IntegerHexBitFieldEnumInfo(0x0200, "IA64"),
			new IntegerHexBitFieldEnumInfo(0x01C4, "ARMNT"),
			new IntegerHexBitFieldEnumInfo(0xAA64, "ARM64"),
			new IntegerHexBitFieldEnumInfo(0, dnSpy_AsmEditor_Resources.Unknown),
			new IntegerHexBitFieldEnumInfo(0x0162, "R3000"),
			new IntegerHexBitFieldEnumInfo(0x0166, "R4000"),
			new IntegerHexBitFieldEnumInfo(0x0168, "R10000"),
			new IntegerHexBitFieldEnumInfo(0x0169, "WCEMIPSV2"),
			new IntegerHexBitFieldEnumInfo(0x0184, "ALPHA"),
			new IntegerHexBitFieldEnumInfo(0x01A2, "SH3"),
			new IntegerHexBitFieldEnumInfo(0x01A3, "SH3DSP"),
			new IntegerHexBitFieldEnumInfo(0x01A4, "SH3E"),
			new IntegerHexBitFieldEnumInfo(0x01A6, "SH4"),
			new IntegerHexBitFieldEnumInfo(0x01A8, "SH5"),
			new IntegerHexBitFieldEnumInfo(0x01C0, "ARM"),
			new IntegerHexBitFieldEnumInfo(0x01C2, "THUMB"),
			new IntegerHexBitFieldEnumInfo(0x01D3, "AM33"),
			new IntegerHexBitFieldEnumInfo(0x01F0, "POWERPC"),
			new IntegerHexBitFieldEnumInfo(0x01F1, "POWERPCFP"),
			new IntegerHexBitFieldEnumInfo(0x0266, "MIPS16"),
			new IntegerHexBitFieldEnumInfo(0x0284, "ALPHA64"),
			new IntegerHexBitFieldEnumInfo(0x0366, "MIPSFPU"),
			new IntegerHexBitFieldEnumInfo(0x0466, "MIPSFPU16"),
			new IntegerHexBitFieldEnumInfo(0x0520, "TRICORE"),
			new IntegerHexBitFieldEnumInfo(0x0CEF, "CEF"),
			new IntegerHexBitFieldEnumInfo(0x0EBC, "EBC"),
			new IntegerHexBitFieldEnumInfo(0x9041, "M32R"),
			new IntegerHexBitFieldEnumInfo(0xC0EE, "CEE"),
		};

		public ImageFileHeaderVM(HexBuffer buffer, PeFileHeaderData fileHeader)
			: base(fileHeader.Span) {
			Name = fileHeader.Name;
			MachineVM = new UInt16FlagsHexField(fileHeader.Machine);
			MachineVM.Add(new IntegerHexBitField(fileHeader.Machine.Name, 0, 16, MachineInfos));
			NumberOfSectionsVM = new UInt16HexField(fileHeader.NumberOfSections);
			TimeDateStampVM = new UInt32HexField(fileHeader.TimeDateStamp.Data, fileHeader.TimeDateStamp.Name);
			TimeDateStampVM.DataFieldVM.PropertyChanged += (s, e) => OnPropertyChanged(nameof(TimeDateStampString));
			PointerToSymbolTableVM = new UInt32HexField(fileHeader.PointerToSymbolTable);
			NumberOfSymbolsVM = new UInt32HexField(fileHeader.NumberOfSymbols);
			SizeOfOptionalHeaderVM = new UInt16HexField(fileHeader.SizeOfOptionalHeader);
			CharacteristicsVM = new UInt16FlagsHexField(fileHeader.Characteristics);
			CharacteristicsVM.Add(new BooleanHexBitField("Relocs Stripped", 0));
			CharacteristicsVM.Add(new BooleanHexBitField("Executable Image", 1));
			CharacteristicsVM.Add(new BooleanHexBitField("Line Nums Stripped", 2));
			CharacteristicsVM.Add(new BooleanHexBitField("Local Syms Stripped", 3));
			CharacteristicsVM.Add(new BooleanHexBitField("Aggressive WS Trim", 4));
			CharacteristicsVM.Add(new BooleanHexBitField("Large Address Aware", 5));
			CharacteristicsVM.Add(new BooleanHexBitField("Reserved 0040h", 6));
			CharacteristicsVM.Add(new BooleanHexBitField("Bytes Reversed Lo", 7));
			CharacteristicsVM.Add(new BooleanHexBitField("32-Bit Machine", 8));
			CharacteristicsVM.Add(new BooleanHexBitField("Debug Stripped", 9));
			CharacteristicsVM.Add(new BooleanHexBitField("Removable Run From Swap", 10));
			CharacteristicsVM.Add(new BooleanHexBitField("Net Run From Swap", 11));
			CharacteristicsVM.Add(new BooleanHexBitField("System", 12));
			CharacteristicsVM.Add(new BooleanHexBitField("Dll", 13));
			CharacteristicsVM.Add(new BooleanHexBitField("Up System Only", 14));
			CharacteristicsVM.Add(new BooleanHexBitField("Bytes Reversed Hi", 15));

			hexFields = new HexField[] {
				MachineVM,
				NumberOfSectionsVM,
				TimeDateStampVM,
				PointerToSymbolTableVM,
				NumberOfSymbolsVM,
				SizeOfOptionalHeaderVM,
				CharacteristicsVM,
			};
		}
	}
}
