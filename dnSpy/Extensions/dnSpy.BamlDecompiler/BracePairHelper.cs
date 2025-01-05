using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.BamlDecompiler {
	readonly struct BracePairHelper {
		readonly IDecompilerOutput output;
		readonly CodeBracesRangeFlags flags;
		readonly int leftStart, leftEnd;

		BracePairHelper(IDecompilerOutput output, int leftStart, int leftEnd, CodeBracesRangeFlags flags) {
			this.output = output;
			this.leftStart = leftStart;
			this.leftEnd = leftEnd;
			this.flags = flags;
		}

		public static BracePairHelper Create(IDecompilerOutput output, string s, CodeBracesRangeFlags flags) {
			int start = output.NextPosition;
			output.Write(s, BoxedTextColor.Punctuation);
			return new BracePairHelper(output, start, output.NextPosition, flags);
		}

		public void Write(string s) {
			int start = output.NextPosition;
			output.Write(s, BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(leftStart, leftEnd - leftStart), new TextSpan(start, output.NextPosition - start), flags);
		}
	}
}
