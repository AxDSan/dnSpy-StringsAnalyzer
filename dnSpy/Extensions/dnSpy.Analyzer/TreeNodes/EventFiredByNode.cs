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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventFiredByNode : SearchNode {
		readonly EventDef analyzedEvent;
		readonly FieldDef? eventBackingField;
		readonly MethodDef? eventFiringMethod;

		ConcurrentDictionary<MethodDef, int>? foundMethods;

		public EventFiredByNode(EventDef analyzedEvent) {
			this.analyzedEvent = analyzedEvent ?? throw new ArgumentNullException(nameof(analyzedEvent));

			eventBackingField = GetBackingField(analyzedEvent);
			var eventType = analyzedEvent.EventType.ResolveTypeDef();
			if (eventType is not null)
				eventFiringMethod = eventType.Methods.First(md => md.Name == "Invoke");
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.RaisedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedEvent, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}

			foundMethods = null;
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			// HACK: in lieu of proper flow analysis, I'm going to use a simple heuristic
			// If the method accesses the event's backing field, and calls invoke on a delegate
			// with the same signature, then it is (most likely) raise the given event.

			foreach (var method in type.Methods) {
				if (!method.HasBody)
					continue;
				Instruction? foundInstr = null;
				bool readBackingField = false;
				foreach (var instr in method.Body.Instructions) {
					var code = instr.OpCode.Code;
					if ((code == Code.Ldfld || code == Code.Ldflda || code == Code.Ldsfld || code == Code.Ldsflda || code == Code.Ldtoken) && instr.Operand is IField fr && fr.IsField) {
						if (CheckEquals(fr.ResolveFieldDef(), eventBackingField)) {
							readBackingField = true;
						}
					}
					else if (readBackingField && (code == Code.Callvirt || code == Code.Call)) {
						if (instr.Operand is IMethod mr && eventFiringMethod is not null && mr.Name == eventFiringMethod.Name && CheckEquals(mr.ResolveMethodDef(), eventFiringMethod)) {
							foundInstr = instr;
							break;
						}
					}
				}

				if (foundInstr is not null) {
					if (GetOriginalCodeLocation(method) is MethodDef codeLocation && !HasAlreadyBeenFound(codeLocation)) {
						var node = new MethodNode(codeLocation) { Context = Context };
						if (codeLocation == method)
							node.SourceRef = new SourceRef(method, foundInstr.Offset, foundInstr.Operand as IMDTokenProvider);
						yield return node;
					}
				}
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) => !foundMethods!.TryAdd(method, 0);

		// HACK: we should probably examine add/remove methods to determine this
		static FieldDef? GetBackingField(EventDef ev) {
			var fieldName = ev.Name;
			var vbStyleFieldName = fieldName + "Event";
			var fieldType = ev.EventType;
			if (fieldType is null)
				return null;

			foreach (var fd in ev.DeclaringType.Fields) {
				if (fd.Name == fieldName || fd.Name == vbStyleFieldName)
					if (new SigComparer().Equals(fd.FieldType, fieldType))
						return fd;
			}

			return null;
		}


		public static bool CanShow(EventDef ev) => GetBackingField(ev) is not null;
	}
}
