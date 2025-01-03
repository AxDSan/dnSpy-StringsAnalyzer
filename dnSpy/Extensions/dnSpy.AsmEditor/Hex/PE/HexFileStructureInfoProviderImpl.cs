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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexFileStructureInfoProviderFactory))]
	[VSUTIL.Name("AsmEditor")]
	[VSUTIL.Order(Before = PredefinedHexFileStructureInfoProviderFactoryNames.Default)]
	sealed class HexFileStructureInfoProviderFactoryImpl : HexFileStructureInfoProviderFactory {
		readonly PEStructureProviderFactory peStructureProviderFactory;

		[ImportingConstructor]
		HexFileStructureInfoProviderFactoryImpl(PEStructureProviderFactory peStructureProviderFactory) => this.peStructureProviderFactory = peStructureProviderFactory;

		public override HexFileStructureInfoProvider? Create(HexView hexView) =>
			new HexFileStructureInfoProviderImpl(peStructureProviderFactory);
	}

	sealed class HexFileStructureInfoProviderImpl : HexFileStructureInfoProvider {
		readonly PEStructureProviderFactory peStructureProviderFactory;

		public HexFileStructureInfoProviderImpl(PEStructureProviderFactory peStructureProviderFactory) => this.peStructureProviderFactory = peStructureProviderFactory ?? throw new ArgumentNullException(nameof(peStructureProviderFactory));

		sealed class PEStructure {
			readonly PEStructureProvider peStructureProvider;
			readonly HexVM[] hexStructures;
			readonly HexSpan metadataTablesSpan;

			public static PEStructure TryCreate(PEStructureProviderFactory peStructureProviderFactory, HexBufferFile file) {
				if (file.Properties.TryGetProperty(typeof(PEStructure), out PEStructure peStructure))
					return peStructure;

				var provider = peStructureProviderFactory.TryGetProvider(file);
				if (provider is not null)
					peStructure = new PEStructure(provider);

				file.Properties.AddProperty(typeof(PEStructure), peStructure);
				return peStructure;
			}

			PEStructure(PEStructureProvider peStructureProvider) {
				this.peStructureProvider = peStructureProvider;

				var list = new List<HexVM> {
					peStructureProvider.ImageDosHeader,
					peStructureProvider.ImageFileHeader,
					peStructureProvider.ImageOptionalHeader,
				};
				if (peStructureProvider.ImageCor20Header is not null)
					list.Add(peStructureProvider.ImageCor20Header);
				if (peStructureProvider.StorageSignature is not null)
					list.Add(peStructureProvider.StorageSignature);
				if (peStructureProvider.StorageHeader is not null)
					list.Add(peStructureProvider.StorageHeader);
				if (peStructureProvider.TablesStream is not null)
					list.Add(peStructureProvider.TablesStream);
				list.AddRange(peStructureProvider.Sections);
				list.AddRange(peStructureProvider.StorageStreams);
				hexStructures = list.ToArray();

				var tblsStream = peStructureProvider.TablesStream;
				if (tblsStream is not null) {
					var first = tblsStream.MetadataTables.FirstOrDefault(a => a is not null);
					var last = tblsStream.MetadataTables.LastOrDefault(a => a is not null);
					Debug2.Assert(first is not null);
					if (first is not null) {
						Debug2.Assert(last is not null);
						metadataTablesSpan = HexSpan.FromBounds(first.Span.Start, last.Span.End);
					}
				}
			}

			public FieldAndStructure? GetField(HexPosition position) {
				foreach (var structure in hexStructures) {
					if (structure.Span.Contains(position)) {
						foreach (var field in structure.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(structure, field);
						}
					}
				}
				if (metadataTablesSpan.Contains(position)) {
					foreach (var mdTbl in peStructureProvider.TablesStream!.MetadataTables) {
						if (mdTbl is null || !mdTbl.Span.Contains(position))
							continue;
						var offset = position - mdTbl.Span.Start;
						if (offset >= uint.MaxValue)
							break;
						uint index = (uint)(offset.ToUInt64() / (uint)mdTbl.TableInfo.RowSize);
						Debug.Assert(index < mdTbl.Rows);
						if (index >= mdTbl.Rows)
							break;
						var record = mdTbl.Get((int)index);
						foreach (var field in record.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(record, field);
						}
						break;
					}
				}
				return null;
			}
		}

		readonly struct FieldAndStructure {
			public HexVM Structure { get; }
			public HexField Field { get; }
			public FieldAndStructure(HexVM structure, HexField field) {
				Structure = structure ?? throw new ArgumentNullException(nameof(structure));
				Field = field ?? throw new ArgumentNullException(nameof(field));
			}
		}

		public override object? GetReference(HexBufferFile file, ComplexData structure, HexPosition position) {
			var peStructure = PEStructure.TryCreate(peStructureProviderFactory, file);
			if (peStructure is null)
				return null;

			var info = peStructure.GetField(position);
			if (info is not null)
				return new HexFieldReference(file, info.Value.Structure, info.Value.Field);

			return null;
		}

		public override HexIndexes[]? GetSubStructureIndexes(HexBufferFile file, ComplexData structure, HexPosition position) {
			if (structure is PeSectionsData sections)
				return Array.Empty<HexIndexes>();

			return null;
		}
	}
}
