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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	sealed class HexBufferFileImpl : HexBufferFile {
		public override HexBufferFile? ParentFile { get; }
		public override event EventHandler<BufferFilesAddedEventArgs>? BufferFilesAdded;
		public override bool IsRemoved => isRemoved;
		public override event EventHandler? Removed;
		public override bool IsStructuresInitialized => isStructuresInitialized;
		public override event EventHandler? StructuresInitialized;

		public override IEnumerable<HexBufferFile> Files {
			get {
				if (files.Count == 0)
					yield break;
				// This property isn't defined to return an ordered enumerable of files so make
				// sure no-one depends on sorted files.
				int index = (GetHashCode() & int.MaxValue) % files.Count;
				for (int i = files.Count - 1; i >= 0; i--)
					yield return files[(index + i) % files.Count].Data;
			}
		}

		readonly Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories;
		readonly Lazy<BufferFileHeadersProviderFactory>[] bufferFileHeadersProviderFactories;
		readonly SpanDataCollection<HexBufferFileImpl> files;
		StructureProvider[]? structureProviders;
		BufferFileHeadersProvider[]? bufferFileHeadersProviders;
		bool isInitializing;
		bool isStructuresInitialized;
		bool isRemoved;

		public HexBufferFileImpl(HexBufferFile? parentFile, Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories, Lazy<BufferFileHeadersProviderFactory>[] bufferFileHeadersProviderFactories, HexBuffer buffer, HexSpan span, string name, string filename, string[] tags)
			: base(buffer, span, name, filename, tags) {
			if (parentFile?.Span.Contains(span) == false)
				throw new ArgumentOutOfRangeException(nameof(span));
			ParentFile = parentFile;
			this.structureProviderFactories = structureProviderFactories ?? throw new ArgumentNullException(nameof(structureProviderFactories));
			this.bufferFileHeadersProviderFactories = bufferFileHeadersProviderFactories ?? throw new ArgumentNullException(nameof(bufferFileHeadersProviderFactories));
			files = new SpanDataCollection<HexBufferFileImpl>();
		}

		public override HexBufferFile[] CreateFiles(params BufferFileOptions[] options) {
			if (options is null)
				throw new ArgumentNullException(nameof(options));
			var newFiles = new HexBufferFileImpl[options.Length];
			for (int i = 0; i < newFiles.Length; i++) {
				var opts = options[i];
				if (opts.IsDefault)
					throw new ArgumentException();
				if (!Span.Contains(opts.Span))
					throw new ArgumentOutOfRangeException();
				newFiles[i] = new HexBufferFileImpl(this, structureProviderFactories, bufferFileHeadersProviderFactories, Buffer, opts.Span, opts.Name, opts.Filename, opts.Tags);
			}
			files.Add(newFiles.Select(a => new SpanData<HexBufferFileImpl>(a.Span, a)));
			BufferFilesAdded?.Invoke(this, new BufferFilesAddedEventArgs(newFiles));
			return newFiles;
		}

		public override HexBufferFile? GetFile(HexPosition position, bool checkNestedFiles) {
			var file = files.FindData(position);
			if (file is null || !checkNestedFiles)
				return file;
			return file.GetFile(position, checkNestedFiles) ?? file;
		}

		void CreateStructureProviders(bool initialize) {
			if (structureProviders is null) {
				var list = new List<StructureProvider>(structureProviderFactories.Length);
				foreach (var lz in structureProviderFactories) {
					var provider = lz.Value.Create(this);
					if (provider is not null)
						list.Add(provider);
				}
				structureProviders = list.ToArray();
			}
			if (initialize && !isStructuresInitialized && !isInitializing) {
				isInitializing = true;
				var list = new List<StructureProvider>(structureProviders.Length);
				foreach (var provider in structureProviders) {
					if (provider.Initialize())
						list.Add(provider);
				}
				structureProviders = list.ToArray();
				isStructuresInitialized = true;
				StructuresInitialized?.Invoke(this, EventArgs.Empty);
				isInitializing = false;
			}
		}

		public override FileAndStructure? GetFileAndStructure(HexPosition position, bool checkNestedFiles) {
			Debug.Assert(Span.Contains(position));

			// Always initialize this first to make sure nested files get created
			CreateStructureProviders(true);
			Debug2.Assert(structureProviders is not null);

			if (checkNestedFiles && files.Count != 0) {
				var file = files.FindData(position);
				var info = file?.GetFileAndStructure(position, checkNestedFiles);
				if (info is not null)
					return info;
			}

			foreach (var provider in structureProviders) {
				var structure = provider.GetStructure(position);
				if (structure is not null)
					return new FileAndStructure(this, structure);
			}
			return null;
		}

		public override ComplexData? GetStructure(string id) {
			CreateStructureProviders(true);
			Debug2.Assert(structureProviders is not null);
			foreach (var provider in structureProviders) {
				var structure = provider.GetStructure(id);
				if (structure is not null)
					return structure;
			}
			return null;
		}

		public override THeaders? GetHeaders<THeaders>() where THeaders : class {
			if (bufferFileHeadersProviders is null) {
				CreateStructureProviders(true);
				var list = new List<BufferFileHeadersProvider>(bufferFileHeadersProviderFactories.Length);
				foreach (var lz in bufferFileHeadersProviderFactories) {
					var provider = lz.Value.Create(this);
					if (provider is not null)
						list.Add(provider);
				}
				bufferFileHeadersProviders = list.ToArray();
			}
			Debug2.Assert(structureProviders is not null);

			foreach (var provider in structureProviders) {
				var headers = provider.GetHeaders<THeaders>();
				if (headers is not null)
					return headers;
			}

			foreach (var provider in bufferFileHeadersProviders) {
				var headers = provider.GetHeaders<THeaders>();
				if (headers is not null)
					return headers;
			}

			return null;
		}

		internal void RaiseRemoved() {
			if (isRemoved)
				throw new InvalidOperationException();
			isRemoved = true;
			Removed?.Invoke(this, EventArgs.Empty);
			foreach (var file in files.Remove(new[] { Span }))
				file.Data.RaiseRemoved();
		}
	}
}
