// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.ILAst;

namespace ICSharpCode.Decompiler {
	public class DecompilerContext
	{
		public MetadataTextColorProvider MetadataTextColorProvider;
		public ModuleDef CurrentModule;
		public CancellationToken CancellationToken;
		public TypeDef CurrentType;
		public MethodDef CurrentMethod;
		public DecompilerSettings Settings = new DecompilerSettings();
		public readonly int SettingsVersion;
		public bool CurrentMethodIsAsync;
		public bool CurrentMethodIsYieldReturn;
		public readonly DecompilerCache Cache;
		public bool CalculateILSpans;
		public bool AsyncMethodBodyDecompilation;
		public readonly List<string> UsingNamespaces = new List<string>();

		internal FieldToVariableMap VariableMap {
			get {
				if (variableMap == null)
					variableMap = new FieldToVariableMap();
				return variableMap;
			}
		}
		internal FieldToVariableMap variableMap;

		/// <summary>
		/// Used to pass variable names from a method to its anonymous methods.
		/// </summary>
		internal List<string> ReservedVariableNames = new List<string>();

		public DecompilerContext(int settingsVersion, ModuleDef currentModule, MetadataTextColorProvider metadataTextColorProvider = null)
			: this(settingsVersion, currentModule, metadataTextColorProvider, false) {
		}

		public DecompilerContext(int settingsVersion, ModuleDef currentModule, MetadataTextColorProvider metadataTextColorProvider, bool calculateILSpans)
		{
			this.SettingsVersion = settingsVersion;
			this.CurrentModule = currentModule;
			this.CalculateILSpans = calculateILSpans;
			this.Cache = new DecompilerCache(this);
			this.MetadataTextColorProvider = metadataTextColorProvider ?? CSharpMetadataTextColorProvider.Instance;
		}

		DecompilerContext(DecompilerContext other)
		{
			MetadataTextColorProvider = other.MetadataTextColorProvider;
			CurrentModule = other.CurrentModule;
			CancellationToken = other.CancellationToken;
			CurrentType = other.CurrentType;
			CurrentMethod = other.CurrentMethod;
			Settings = other.Settings.Clone();
			SettingsVersion = other.SettingsVersion;
			CurrentMethodIsAsync = other.CurrentMethodIsAsync;
			CurrentMethodIsYieldReturn = other.CurrentMethodIsYieldReturn;
			Cache = new DecompilerCache(this);
			CalculateILSpans = other.CalculateILSpans;
			AsyncMethodBodyDecompilation = other.AsyncMethodBodyDecompilation;
			UsingNamespaces.AddRange(other.UsingNamespaces);
			ReservedVariableNames.AddRange(other.ReservedVariableNames);
			// It's not cloned. It must be unique per base-enclosing method. I.e., it's shared
			// by the method and all inlined method bodies, but not by other non-inlined methods
			variableMap = null;
		}

		internal DecompilerContext CloneDontUse()
		{
			DecompilerContext ctx = (DecompilerContext)MemberwiseClone();
			ctx.ReservedVariableNames = new List<string>(ctx.ReservedVariableNames);
			return ctx;
		}

		internal DecompilerContext Clone() => new DecompilerContext(this);

		public void Reset()
		{
			this.CurrentModule = null;
			this.CancellationToken = CancellationToken.None;
			this.CurrentType = null;
			this.CurrentMethod = null;
			this.Settings = new DecompilerSettings();
			this.CurrentMethodIsAsync = false;
			this.CurrentMethodIsYieldReturn = false;
			this.UsingNamespaces.Clear();
			this.Cache.Reset();
			this.variableMap = null;
		}
	}
}
