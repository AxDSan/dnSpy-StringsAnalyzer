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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	sealed class TextViewModel : ITextViewModel {
		public PropertyCollection Properties { get; }
		public ITextDataModel DataModel { get; }
		public ITextBuffer DataBuffer => DataModel.DataBuffer;
		public ITextBuffer EditBuffer { get; }
		public ITextBuffer VisualBuffer => EditBuffer;

		public TextViewModel(ITextDataModel textDataModel)
			: this(textDataModel, textDataModel.DataBuffer) {
		}

		public TextViewModel(ITextDataModel textDataModel, ITextBuffer editBuffer) {
			Properties = new PropertyCollection();
			DataModel = textDataModel ?? throw new ArgumentNullException(nameof(textDataModel));
			EditBuffer = editBuffer ?? throw new ArgumentNullException(nameof(editBuffer));
		}

		public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint) => editBufferPoint;
		public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode) =>
			editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);
		public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity) => true;
		public void Dispose() { }
	}
}
