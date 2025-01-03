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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Document base class
	/// </summary>
	public abstract class DsDocument : IDsDocument2 {
		/// <inheritdoc/>
		public abstract DsDocumentInfo? SerializedDocument { get; }
		/// <inheritdoc/>
		public abstract IDsDocumentNameKey Key { get; }
		/// <inheritdoc/>
		public AssemblyDef? AssemblyDef => ModuleDef?.Assembly;
		/// <inheritdoc/>
		public virtual ModuleDef? ModuleDef => null;
		/// <inheritdoc/>
		public virtual IPEImage? PEImage => (ModuleDef as ModuleDefMD)?.Metadata?.PEImage;

		/// <inheritdoc/>
		public string Filename {
			get => filename;
			set {
				if (filename != value) {
					filename = value;
					OnPropertyChanged(nameof(Filename));
				}
			}
		}
		string filename = string.Empty;

		/// <summary>
		/// Gets called when a property has changed
		/// </summary>
		/// <param name="propName">Name of property</param>
		protected virtual void OnPropertyChanged(string propName) {
		}

		/// <inheritdoc/>
		public bool IsAutoLoaded { get; set; }

		/// <inheritdoc/>
		public TList<IDsDocument> Children {
			get {
				if (children is null) {
					lock (lockObj) {
						if (children is null) {
							children = CreateChildren();
							Debug2.Assert(children is not null);
							if (children is null)
								children = new TList<IDsDocument>();
						}
					}
				}
				return children;
			}
		}
		readonly object lockObj;
		TList<IDsDocument>? children;

		/// <inheritdoc/>
		public bool ChildrenLoaded => children is not null;

		/// <summary>
		/// Creates the children
		/// </summary>
		/// <returns></returns>
		protected virtual TList<IDsDocument> CreateChildren() => new TList<IDsDocument>();

		/// <summary>
		/// Constructor
		/// </summary>
		protected DsDocument() => lockObj = new object();

		/// <inheritdoc/>
		public T? AddAnnotation<T>(T? annotation) where T : class => annotations.AddAnnotation(annotation);
		/// <inheritdoc/>
		public T? Annotation<T>() where T : class => annotations.Annotation<T>();
		/// <inheritdoc/>
		public IEnumerable<T> Annotations<T>() where T : class => annotations.Annotations<T>();
		/// <inheritdoc/>
		public void RemoveAnnotations<T>() where T : class => annotations.RemoveAnnotations<T>();
		readonly AnnotationsImpl annotations = new AnnotationsImpl();
		/// <inheritdoc/>
		public virtual void OnAdded() { }
	}

	/// <summary>
	/// Unknown type of file
	/// </summary>
	public sealed class DsUnknownDocument : DsDocument {
		/// <inheritdoc/>
		public override DsDocumentInfo? SerializedDocument => DsDocumentInfo.CreateDocument(Filename);

		/// <inheritdoc/>
		public override IDsDocumentNameKey Key => new FilenameKey(Filename);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Filename</param>
		public DsUnknownDocument(string filename) => Filename = filename ?? string.Empty;
	}

	/// <summary>
	/// PE file
	/// </summary>
	public sealed class DsPEDocument : DsDocument, IDsPEDocument, IDisposable {
		/// <inheritdoc/>
		public override DsDocumentInfo? SerializedDocument => DsDocumentInfo.CreateDocument(Filename);
		/// <inheritdoc/>
		public override IDsDocumentNameKey Key => FilenameKey.CreateFullPath(Filename);
		/// <inheritdoc/>
		public override IPEImage? PEImage { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">PE image</param>
		public DsPEDocument(IPEImage peImage) {
			PEImage = peImage;
			Filename = peImage.Filename ?? string.Empty;
		}

		/// <inheritdoc/>
		public void Dispose() => PEImage!.Dispose();
	}

	/// <summary>
	/// .NET file base class
	/// </summary>
	public abstract class DsDotNetDocumentBase : DsDocument, IDsDotNetDocument, IInMemoryDocument {
		/// <inheritdoc/>
		public override ModuleDef? ModuleDef { get; }
		/// <inheritdoc/>
		public virtual bool IsActive => true;

		/// <summary>true if the symbols have been loaded</summary>
		protected bool loadedSymbols;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true if symbols should be loaded</param>
		protected DsDotNetDocumentBase(ModuleDef module, bool loadSyms) {
			ModuleDef = module;
			loadedSymbols = loadSyms;
			Filename = module.Location ?? string.Empty;
			module.EnableTypeDefFindCache = true;
		}

		/// <inheritdoc/>
		public override void OnAdded() {
			if (loadedSymbols)
				LoadSymbols();
			base.OnAdded();
		}

		/// <summary>
		/// Creates a module context
		/// </summary>
		/// <param name="asmResolver">Assembly resolver</param>
		/// <returns></returns>
		public static ModuleContext CreateModuleContext(IAssemblyResolver asmResolver) {
			var moduleCtx = new ModuleContext();
			moduleCtx.AssemblyResolver = asmResolver;
			// Disable WinMD projection since the user probably expects that clicking on a type
			// will take you to that type, and not to the projected CLR type.
			// The decompiler shouldn't have a problem with this since it uses SigComparer() which
			// defaults to projecting WinMD types.
			moduleCtx.Resolver = new Resolver(moduleCtx.AssemblyResolver) { ProjectWinMDRefs = false };
			return moduleCtx;
		}

		void LoadSymbols() {
			Debug2.Assert(ModuleDef is not null);
			// Happens if a module has been removed but then the exact same instance
			// was re-added.
			if (ModuleDef.PdbState is not null)
				return;

			var m = ModuleDef as ModuleDefMD;
			if (m is null)
				return;
			try {
				m.LoadPdb();
			}
			catch {
			}
		}
	}

	/// <summary>
	/// .NET file
	/// </summary>
	public class DsDotNetDocument : DsDotNetDocumentBase, IDisposable {
		readonly bool isAsmNode;

		/// <inheritdoc/>
		public override IDsDocumentNameKey Key => FilenameKey.CreateFullPath(Filename);
		/// <inheritdoc/>
		public override DsDocumentInfo? SerializedDocument => documentInfo;
		DsDocumentInfo documentInfo;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentInfo">Document info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <param name="isAsmNode">true if it's an assembly node, false if it's a module node</param>
		protected DsDotNetDocument(DsDocumentInfo documentInfo, ModuleDef module, bool loadSyms, bool isAsmNode)
			: base(module, loadSyms) {
			this.documentInfo = documentInfo;
			this.isAsmNode = isAsmNode;
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string propName) {
			base.OnPropertyChanged(propName);
			if (propName == nameof(Filename))
				documentInfo = DsDocumentInfo.CreateDocument(Filename);
		}

		/// <summary>
		/// Creates an assembly
		/// </summary>
		/// <param name="documentInfo">Document info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <returns></returns>
		public static DsDotNetDocument CreateAssembly(DsDocumentInfo documentInfo, ModuleDef module, bool loadSyms) => new DsDotNetDocument(documentInfo, module, loadSyms, true);

		/// <summary>
		/// Creates a module
		/// </summary>
		/// <param name="documentInfo">Document info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <returns></returns>
		public static DsDotNetDocument CreateModule(DsDocumentInfo documentInfo, ModuleDef module, bool loadSyms) => new DsDotNetDocument(documentInfo, module, loadSyms, false);

		/// <summary>
		/// Creates an assembly
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static DsDotNetDocument CreateAssembly(IDsDotNetDocument module) => new DsDotNetDocumentAsmWithMod(module);

		/// <inheritdoc/>
		protected override TList<IDsDocument> CreateChildren() {
			var asm = AssemblyDef;
			var list = new TList<IDsDocument>(asm is null ? 1 : asm.Modules.Count);
			if (isAsmNode && asm is not null) {
				bool foundThis = false;
				foreach (var module in asm.Modules) {
					if (ModuleDef == module) {
						Debug.Assert(!foundThis);
						foundThis = true;
					}
					list.Add(new DsDotNetDocument(DsDocumentInfo.CreateDocument(module.Location), module, loadedSymbols, false));
				}
				Debug.Assert(foundThis);
			}
			return list;
		}

		/// <inheritdoc/>
		public void Dispose() => ModuleDef!.Dispose();
	}

	sealed class DsDotNetDocumentAsmWithMod : DsDotNetDocument {
		IDsDotNetDocument? module;

		public DsDotNetDocumentAsmWithMod(IDsDotNetDocument modmodule)
			: base(modmodule.SerializedDocument ?? new DsDocumentInfo(), modmodule.ModuleDef!, false, true) => module = modmodule;

		protected override TList<IDsDocument> CreateChildren() {
			Debug2.Assert(module is not null);
			var list = new TList<IDsDocument>();
			if (module is not null)
				list.Add(module);
			module = null;
			return list;
		}
	}

	/// <summary>
	/// mmap'd I/O helper methods
	/// </summary>
	static class MemoryMappedIOHelper {
		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		/// <param name="document">Document</param>
		public static void DisableMemoryMappedIO(IDsDocument document) {
			if (document is null)
				return;
			DisableMemoryMappedIO(document.PEImage);
		}

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		/// <param name="peImage">PE image</param>
		public static void DisableMemoryMappedIO(IPEImage? peImage) {
			if (peImage is null)
				return;
			// Files in the GAC are read-only so there's no need to disable memory mapped I/O to
			// allow other programs to write to the file.
			if (GacInfo.IsGacPath(peImage.Filename))
				return;
			(peImage as IInternalPEImage)?.UnsafeDisableMemoryMappedIO();
		}
	}
}
