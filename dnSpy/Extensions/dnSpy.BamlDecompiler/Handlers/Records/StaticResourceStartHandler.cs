using System.Xml.Linq;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Handlers {
	class StaticResourceStartHandler : IHandler, IDeferHandler {
		public BamlRecordType Type => BamlRecordType.StaticResourceStart;

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (StaticResourceStartRecord)((BamlBlockNode)node).Record;
			var key = XamlResourceKey.FindKeyInSiblings(node);

			key.StaticResources.Add(node);
			return null;
		}

		public BamlElement TranslateDefer(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (StaticResourceStartRecord)((BamlBlockNode)node).Record;
			var doc = new BamlElement(node);
			var elemType = ctx.ResolveType(record.TypeId);
			doc.Xaml = new XElement(elemType.ToXName(ctx));
			doc.Xaml.Element.AddAnnotation(elemType);
			parent.Xaml.Element.Add(doc.Xaml.Element);
			HandlerMap.ProcessChildren(ctx, (BamlBlockNode)node, doc);
			return doc;
		}
	}
}
