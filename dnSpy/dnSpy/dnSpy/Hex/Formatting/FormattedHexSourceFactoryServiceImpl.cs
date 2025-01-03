/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Formatting;
using TF = dnSpy.Text.Formatting;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Formatting {
	[Export(typeof(FormattedHexSourceFactoryService))]
	sealed class FormattedHexSourceFactoryServiceImpl : FormattedHexSourceFactoryService {
		readonly TF.ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		FormattedHexSourceFactoryServiceImpl(TF.ITextFormatterProvider textFormatterProvider) => this.textFormatterProvider = textFormatterProvider;

		public override HexFormattedLineSource Create(double baseIndent, bool useDisplayMode, HexClassifier aggregateClassifier, HexAndAdornmentSequencer sequencer, VSTC.IClassificationFormatMap classificationFormatMap) {
			if (aggregateClassifier is null)
				throw new ArgumentNullException(nameof(aggregateClassifier));
			if (sequencer is null)
				throw new ArgumentNullException(nameof(sequencer));
			if (classificationFormatMap is null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			return new HexFormattedLineSourceImpl(textFormatterProvider, baseIndent, useDisplayMode, aggregateClassifier, sequencer, classificationFormatMap);
		}
	}
}
