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
using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventOverridesNode : SearchNode {
		readonly EventDef analyzedEvent;

		public EventOverridesNode(EventDef analyzedEvent) => this.analyzedEvent = analyzedEvent ?? throw new ArgumentNullException(nameof(analyzedEvent));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverriddenByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedEvent, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (!TypesHierarchyHelpers.IsBaseType(analyzedEvent.DeclaringType, type, resolveTypeArguments: false))
				yield break;

			foreach (EventDef eventDef in type.Events) {
				if (TypesHierarchyHelpers.IsBaseEvent(analyzedEvent, eventDef)) {
					MethodDef anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod ?? eventDef.InvokeMethod;
					if (anyAccessor is null)
						continue;
					bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
					yield return new EventNode(eventDef, hidesParent) { Context = Context };
				}
			}
		}

		public static bool CanShow(EventDef @event) {
			var accessor = @event.AddMethod ?? @event.RemoveMethod ?? @event.InvokeMethod;
			return accessor is not null && accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
