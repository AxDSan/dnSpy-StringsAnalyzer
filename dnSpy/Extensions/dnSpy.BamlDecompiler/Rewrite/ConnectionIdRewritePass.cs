/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Properties;
using dnSpy.BamlDecompiler.Xaml;
using dnSpy.Contracts.Decompiler;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;

namespace dnSpy.BamlDecompiler.Rewrite {
	internal class ConnectionIdRewritePass : IRewritePass {
		static bool Impl(MethodDef method, MethodDef ifaceMethod) {
			if (method.HasOverrides) {
				var comparer = new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable);
				if (method.Overrides.Any(m => comparer.Equals(m.MethodDeclaration, ifaceMethod)))
					return true;
			}

			if (method.Name != ifaceMethod.Name)
				return false;
			return TypesHierarchyHelpers.MatchInterfaceMethod(method, ifaceMethod, ifaceMethod.DeclaringType);
		}

		public void Run(XamlContext ctx, XDocument document) {
			var xClass = document.Root.Elements().First().Attribute(ctx.GetKnownNamespace("Class", XamlContext.KnownNamespace_Xaml));
			if (xClass is null)
				return;

			var type = ctx.Module.Find(xClass.Value, true);
			if (type is null)
				return;

			var componentConnectorConnect = ctx.Baml.KnownThings.Types(KnownTypes.IComponentConnector).FindMethod("Connect");
			var styleConnectorConnect = ctx.Baml.KnownThings.Types(KnownTypes.IStyleConnector).FindMethod("Connect");

			var connIds = new Dictionary<int, Action<XamlContext, XElement>>();

			if (!CollectConnectionIds(ctx, componentConnectorConnect, type, connIds)) {
				var msg = dnSpy_BamlDecompiler_Resources.Error_IComponentConnectorConnectCannotBeParsed;
				document.Root.AddBeforeSelf(new XComment(string.Format(msg, type.ReflectionFullName)));
			}

			if (!CollectConnectionIds(ctx, styleConnectorConnect, type, connIds)) {
				var msg = dnSpy_BamlDecompiler_Resources.Error_IStyleConnectorConnectCannotBeParsed;
				document.Root.AddBeforeSelf(new XComment(string.Format(msg, type.ReflectionFullName)));
			}

			foreach (var elem in document.Elements()) {
				ProcessElement(ctx, elem, connIds);
			}
		}

		bool CollectConnectionIds(XamlContext ctx, MethodDef connectInterfaceMethod, TypeDef currentType, Dictionary<int, Action<XamlContext, XElement>> allConnIds) {
			MethodDef connect = null;
			foreach (var method in currentType.Methods) {
				if (Impl(method, connectInterfaceMethod)) {
					connect = method;
					break;
				}
			}

			if (connect is not null) {
				Dictionary<int, Action<XamlContext, XElement>> connIds = null;
				try {
					connIds = ExtractConnectionId(ctx, connect);
				}
				catch {
				}

				if (connIds is null)
					return false;

				foreach (var keyValuePair in connIds)
					allConnIds.Add(keyValuePair.Key, keyValuePair.Value);
			}

			return true;
		}

		static void ProcessElement(XamlContext ctx, XElement elem, Dictionary<int, Action<XamlContext, XElement>> connIds) {
			CheckConnectionId(ctx, elem, connIds);
			foreach (var child in elem.Elements()) {
				ProcessElement(ctx, child, connIds);
			}
		}

		static void CheckConnectionId(XamlContext ctx, XElement elem, Dictionary<int, Action<XamlContext, XElement>> connIds) {
			var connId = elem.Annotation<BamlConnectionId>();
			if (connId is null)
				return;

			if (!connIds.TryGetValue((int)connId.Id, out var cb)) {
				elem.AddBeforeSelf(new XComment(string.Format(dnSpy_BamlDecompiler_Resources.Error_UnknownConnectionId, connId.Id)));
				return;
			}

			cb(ctx, elem);
		}

		struct FieldAssignment {
			public string FieldName;

			public void Callback(XamlContext ctx, XElement elem) {
				var xName = ctx.GetKnownNamespace("Name", XamlContext.KnownNamespace_Xaml);
				if (elem.Attribute("Name") is null && elem.Attribute(xName) is null)
					elem.Add(new XAttribute(xName, FieldName));
			}
		}

		struct EventAttachment {
			public TypeDef AttachedType;
			public string EventName;
			public string MethodName;

			public void Callback(XamlContext ctx, XElement elem) {
				var type = elem.Annotation<XamlType>();
				if (type is not null && type.TypeNamespace == "System.Windows" && type.TypeName == "Style") {
					elem.Add(new XElement(type.Namespace + "EventSetter", new XAttribute("Event", EventName), new XAttribute("Handler", MethodName)));
					return;
				}

				XName name;
				if (AttachedType is not null) {
					var clrNs = AttachedType.ReflectionNamespace;
					var xmlNs = ctx.XmlNs.LookupXmlns(AttachedType.DefinitionAssembly, clrNs);
					name = ctx.GetXmlNamespace(xmlNs)?.GetName(EventName) ?? AttachedType.Name + "." + EventName;
				}
				else
					name = EventName;

				elem.Add(new XAttribute(name, MethodName));
			}
		}

		struct Error {
			public string Msg;

			public void Callback(XamlContext ctx, XElement elem) => elem.AddBeforeSelf(new XComment(Msg));
		}

		Dictionary<int, Action<XamlContext, XElement>> ExtractConnectionId(XamlContext ctx, MethodDef method) {
			var context = new DecompilerContext(0, method.Module) {
				CurrentType = method.DeclaringType,
				CurrentMethod = method,
				CancellationToken = ctx.CancellationToken
			};
			var body = new ILBlock(new ILAstBuilder().Build(method, true, context));
			new ILAstOptimizer().Optimize(context, body, out _, out _, out _);

			var infos = GetCaseBlocks(body);
			if (infos is null)
				return null;
			var connIds = new Dictionary<int, Action<XamlContext, XElement>>();
			foreach (var info in infos) {
				Action<XamlContext, XElement> cb = null;

				if (MatchEventSetterCreation(info.nodes, out var evAttach))
					cb += evAttach.Callback;
				else {
					foreach (var node in info.nodes) {
						if (node is not ILExpression expr)
							continue;

						switch (expr.Code) {
						case ILCode.Stfld:
							cb += new FieldAssignment { FieldName = IdentifierEscaper.Escape(((IField)expr.Operand).Name) }.Callback;
							break;

						case ILCode.Call:
						case ILCode.Callvirt:
							var operand = (IMethod)expr.Operand;
							if (operand.Name == "AddHandler" && operand.DeclaringType.FullName == "System.Windows.UIElement") {
								// Attached event
								var re = expr.Arguments[1];
								var ctor = expr.Arguments[2];
								var reField = re.Operand as IField;

								if (re.Code != ILCode.Ldsfld || ctor.Code != ILCode.Newobj ||
									ctor.Arguments.Count != 2 || ctor.Arguments[1].Code != ILCode.Ldftn) {
									cb += new Error { Msg = string.Format(dnSpy_BamlDecompiler_Resources.Error_AttachedEvent, reField.Name) }.Callback;
									break;
								}
								var handler = (IMethod)ctor.Arguments[1].Operand;
								string evName = reField.Name;
								if (evName.EndsWith("Event", StringComparison.Ordinal))
									evName = evName.Substring(0, evName.Length - 5);

								cb += new EventAttachment {
									AttachedType = reField.DeclaringType.ResolveTypeDefThrow(),
									EventName = evName,
									MethodName = IdentifierEscaper.Escape(handler.Name)
								}.Callback;
							}
							else {
								// CLR event
								var add = operand.ResolveMethodDefThrow();
								var ev = add.DeclaringType.Events.FirstOrDefault(e => e.AddMethod == add);

								var ctor = expr.Arguments[1];
								if (ev is null || ctor.Code != ILCode.Newobj ||
									ctor.Arguments.Count != 2 || ctor.Arguments[1].Code != ILCode.Ldftn) {
									cb += new Error { Msg = string.Format(dnSpy_BamlDecompiler_Resources.Error_AttachedEvent, add.Name) }.Callback;
									break;
								}
								var handler = (IMethod)ctor.Arguments[1].Operand;

								cb += new EventAttachment {
									EventName = ev.Name,
									MethodName = IdentifierEscaper.Escape(handler.Name)
								}.Callback;
							}
							break;
						}
					}
				}

				if (cb is not null) {
					foreach (var id in info.connIds)
						connIds[id] = cb;
				}
			}

			return connIds;
		}

		static bool MatchEventSetterCreation(List<ILNode> nodes, out EventAttachment @event) {
			@event = default;
			if (nodes.Count != 5 && nodes.Count != 4)
				return false;
			if (!nodes[0].Match(ILCode.Stloc, out ILVariable v, out ILExpression initializer))
				return false;
			if (!initializer.Match(ILCode.Newobj, out IMethod ctor) || ctor.DeclaringType.FullName != "System.Windows.EventSetter")
				return false;

			if (!nodes[1].Match(ILCode.CallvirtSetter, out IMethod setEventMethod, out List<ILExpression> args) || args.Count != 2)
				return false;
			if (!args[0].MatchLdloc(v))
				return false;
			if (setEventMethod.Name != "set_Event")
				return false;
			if (!args[1].Match(ILCode.Ldsfld, out IField eventField))
				return false;

			if (!nodes[2].Match(ILCode.CallvirtSetter, out IMethod setHandlerMethod, out args) || args.Count != 2)
				return false;
			if (!args[0].MatchLdloc(v))
				return false;
			if (setHandlerMethod.Name != "set_Handler")
				return false;
			if (!args[1].Match(ILCode.Newobj, out IMethod _, out args) || args.Count != 2)
				return false;
			if (!args[1].Match(ILCode.Ldftn, out IMethod handlerMethod))
				return false;

			if (!nodes[3].Match(ILCode.Callvirt, out IMethod addMethod, out args) || args.Count != 2)
				return false;
			if (!args[1].MatchLdloc(v))
				return false;
			if (addMethod.Name != "Add")
				return false;
			if (!args[0].Match(ILCode.CallvirtGetter, out IMethod getSettersMethod, out ILExpression arg))
				return false;
			if (getSettersMethod.Name != "get_Setters")
				return false;
			if (!arg.Match(ILCode.Castclass, out ITypeDefOrRef castType, out arg))
				return false;
			if (castType.FullName != "System.Windows.Style")
				return false;
			if (!arg.Match(ILCode.Ldloc, out ILVariable v2) || !v2.IsParameter || v2.OriginalParameter.MethodSigIndex != 1)
				return false;

			if (nodes.Count == 5 && !nodes[4].IsUnconditionalControlFlow())
				return false;

			string evName = eventField.Name;
			if (evName.EndsWith("Event", StringComparison.Ordinal))
				evName = evName.Substring(0, evName.Length - 5);

			@event = new EventAttachment { EventName = evName, MethodName = IdentifierEscaper.Escape(handlerMethod.Name) };
			return true;
		}

		static List<(IList<int> connIds, List<ILNode> nodes)> GetCaseBlocks(ILBlock method) {
			var list = new List<(IList<int>, List<ILNode>)>();
			var body = method.Body;
			if (body.Count == 0)
				return list;

			var sw = method.GetSelfAndChildrenRecursive<ILSwitch>().FirstOrDefault();
			if (sw is not null) {
				foreach (var lbl in sw.CaseBlocks) {
					if (lbl.Values is null)
						continue;
					list.Add((lbl.Values, lbl.Body));
				}
				return list;
			}

			int pos = 0;
			for (;;) {
				if (pos >= body.Count)
					return null;
				if (body[pos] is not ILCondition cond) {
					if (!body[pos].Match(ILCode.Stfld, out IField _, out var ldthis, out var ldci4) || !ldthis.MatchThis() || !ldci4.MatchLdcI4(1))
						return null;
					return list;
				}
				pos++;
				if (cond.TrueBlock is null || cond.FalseBlock is null)
					return null;

				bool isEq = true;
				var condExpr = cond.Condition;
				for (;;) {
					if (!condExpr.Match(ILCode.LogicNot, out ILExpression expr))
						break;
					isEq = !isEq;
					condExpr = expr;
				}
				if (condExpr.Code != ILCode.Ceq && condExpr.Code != ILCode.Cne)
					return null;
				if (condExpr.Arguments.Count != 2)
					return null;
				if (!condExpr.Arguments[0].Match(ILCode.Ldloc, out ILVariable v) || v.OriginalParameter?.Index != 1)
					return null;
				if (!condExpr.Arguments[1].Match(ILCode.Ldc_I4, out int val))
					return null;
				if (condExpr.Code == ILCode.Cne)
					isEq ^= true;

				if (isEq) {
					list.Add((new[] { val }, cond.TrueBlock.Body));
					if (cond.FalseBlock.Body.Count != 0) {
						body = cond.FalseBlock.Body;
						pos = 0;
					}
				}
				else {
					if (cond.FalseBlock.Body.Count != 0) {
						list.Add((new[] { val }, cond.FalseBlock.Body));
						if (cond.TrueBlock.Body.Count != 0) {
							body = cond.TrueBlock.Body;
							pos = 0;
						}
					}
					else {
						list.Add((new[] { val }, body.Skip(pos).ToList()));
						return list;
					}
				}
			}
		}
	}
}
