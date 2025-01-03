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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Tagging;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexViewClassifierAggregatorService))]
	sealed class HexViewClassifierAggregatorServiceImpl : HexViewClassifierAggregatorService {
		readonly HexViewTagAggregatorFactoryService hexViewTagAggregatorFactoryService;
		readonly VSTC.IClassificationTypeRegistryService classificationTypeRegistryService;

		[ImportingConstructor]
		HexViewClassifierAggregatorServiceImpl(HexViewTagAggregatorFactoryService hexViewTagAggregatorFactoryService, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.hexViewTagAggregatorFactoryService = hexViewTagAggregatorFactoryService;
			this.classificationTypeRegistryService = classificationTypeRegistryService;
		}

		public override HexClassifier GetClassifier(HexView hexView) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			return new HexViewClassifierAggregator(hexViewTagAggregatorFactoryService, classificationTypeRegistryService, hexView);
		}
	}
}
