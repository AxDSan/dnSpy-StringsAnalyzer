using System;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.ILSpy.Core.Mixed {
	sealed class DecompilerOutputImpl : IDecompilerOutput {
		readonly StringBuilder sb;
		int indentLevel;
		bool addIndent;
		MethodDef? method;
		MethodDebugInfo? methodDebugInfo;
		MethodDebugInfo? kickoffMethodDebugInfo;

		const string TAB_SPACES = "    ";

		public int Length => sb.Length;
		public int NextPosition => sb.Length + (addIndent ? indentLevel * TAB_SPACES.Length : 0);
		public bool UsesCustomData => true;

		public DecompilerOutputImpl(StringBuilder sb) => this.sb = sb;

		public void Initialize(MethodDef method) {
			sb.Clear();
			indentLevel = 0;
			addIndent = true;
			methodDebugInfo = null;
			kickoffMethodDebugInfo = null;
			this.method = method;
		}

		public (MethodDebugInfo debugInfo, MethodDebugInfo? stateMachineDebugInfo) TryGetMethodDebugInfo() {
			if (methodDebugInfo is not null) {
				if (kickoffMethodDebugInfo is not null)
					return (kickoffMethodDebugInfo, methodDebugInfo);
				return (methodDebugInfo, null);
			}
			return default;
		}

		public void AddCustomData<TData>(string id, TData data) {
			if (id == PredefinedCustomDataIds.DebugInfo && data is MethodDebugInfo debugInfo) {
				if (debugInfo.Method == method)
					methodDebugInfo = debugInfo;
				else if (debugInfo.KickoffMethod is { } m && m == method) {
					var body = m.Body;
					int bodySize = body?.GetCodeSize() ?? 0;
					var scope = new MethodDebugScope(new ILSpan(0, (uint)bodySize), Array.Empty<MethodDebugScope>(), Array.Empty<SourceLocal>(), Array.Empty<ImportInfo>(), Array.Empty<MethodDebugConstant>());
					kickoffMethodDebugInfo = new MethodDebugInfo(debugInfo.CompilerName, debugInfo.DecompilerSettingsVersion, StateMachineKind.None, m, null, null, Array.Empty<SourceStatement>(), scope, null, null);
					methodDebugInfo = debugInfo;
				}
			}
		}

		public void DecreaseIndent() => indentLevel--;
		public void IncreaseIndent() => indentLevel++;

		public void WriteLine() {
			addIndent = true;
			sb.Append(Environment.NewLine);
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			for (int i = 0; i < indentLevel; i++)
				sb.Append(TAB_SPACES);
		}

		void AddText(string text) {
			if (addIndent)
				AddIndent();
			sb.Append(text);
		}

		void AddText(string text, int index, int length) {
			if (addIndent)
				AddIndent();
			sb.Append(text, index, length);
		}

		public void Write(string text, object color) => AddText(text);
		public void Write(string text, int index, int length, object color) => AddText(text, index, length);
		public void Write(string text, object? reference, DecompilerReferenceFlags flags, object color) => AddText(text);
		public void Write(string text, int index, int length, object? reference, DecompilerReferenceFlags flags, object color) => AddText(text, index, length);

		public override string ToString() => sb.ToString();
	}
}
