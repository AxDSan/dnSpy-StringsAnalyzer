using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler {
	internal class BamlConnectionId {
		public uint Id { get; }

		public BamlConnectionId(uint id) => Id = id;
	}

	internal class TargetTypeAnnotation
	{
		public XamlType Type { get; }

		public TargetTypeAnnotation(XamlType type) => Type = type;
	}
}
