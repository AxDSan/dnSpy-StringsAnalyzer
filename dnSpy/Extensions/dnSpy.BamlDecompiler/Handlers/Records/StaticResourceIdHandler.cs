using System;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Handlers {
	class StaticResourceIdHandler : IHandler {
		public BamlRecordType Type => BamlRecordType.StaticResourceId;

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (StaticResourceIdRecord)((BamlRecordNode)node).Record;

			var found = node;
			XamlResourceKey key;
			do {
				key = XamlResourceKey.FindKeyInAncestors(found.Parent, out found);
			} while (key is not null && record.StaticResourceId >= key.StaticResources.Count);

			if (key is null)
				throw new Exception("Cannot find StaticResource @" + node.Record.Position);

			var resNode = key.StaticResources[record.StaticResourceId];

			var handler = (IDeferHandler)HandlerMap.LookupHandler(resNode.Type);
			var resElem = handler.TranslateDefer(ctx, resNode, parent);

			parent.Children.Add(resElem);
			resElem.Parent = parent;

			return resElem;
		}
	}
}
