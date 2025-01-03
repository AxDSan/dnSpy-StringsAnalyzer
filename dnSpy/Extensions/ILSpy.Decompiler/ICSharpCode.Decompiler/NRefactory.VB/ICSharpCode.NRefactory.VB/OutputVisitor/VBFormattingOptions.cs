// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


namespace ICSharpCode.NRefactory.VB {
	/// <summary>
	/// Description of VBFormattingOptions.
	/// </summary>
	public class VBFormattingOptions
	{
		public NumberFormatter NumberFormatter;

		public VBFormattingOptions()
		{
			NumberFormatter = NumberFormatter.GetVBInstance(hex: false, upper: true);
		}
	}
}
