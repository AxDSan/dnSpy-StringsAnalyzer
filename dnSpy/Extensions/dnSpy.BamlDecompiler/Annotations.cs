using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler {
	sealed class BamlConnectionId {
		public uint Id { get; }

		public BamlConnectionId(uint id) => Id = id;
	}

	sealed class TargetTypeAnnotation {
		public XamlType Type { get; }

		public TargetTypeAnnotation(XamlType type) => Type = type;
	}
}
