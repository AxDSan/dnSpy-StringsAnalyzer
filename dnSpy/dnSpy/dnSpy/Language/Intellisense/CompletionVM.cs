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
using System.Collections.Generic;
using System.Windows.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.MVVM;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionVM : ViewModelBase {
		public object? ImageUIObject { get; }
		public object DisplayTextObject => this;
		public object SuffixObject => this;
		public Completion Completion { get; }

		public IEnumerable<CompletionIconVM> AttributeIcons => attributeIcons ??= CreateAttributeIcons();
		IEnumerable<CompletionIconVM>? attributeIcons;

		public CompletionVM(Completion completion) {
			Completion = completion ?? throw new ArgumentNullException(nameof(completion));
			Completion.Properties.AddProperty(typeof(CompletionVM), this);
			ImageUIObject = CreateImageUIObject(completion);
		}

		static object? CreateImageUIObject(Completion completion) {
			var dsCompletion = completion as DsCompletion;
			if (dsCompletion is null) {
				var iconSource = completion.IconSource;
				if (iconSource is null)
					return null;
				return new Image {
					Width = 16,
					Height = 16,
					Source = iconSource,
				};
			}

			var imageReference = dsCompletion.ImageReference;
			if (imageReference.IsDefault)
				return null;
			return new DsImage { ImageReference = imageReference };
		}

		static object? CreateImageUIObject(CompletionIcon icon) {
			var dsIcon = icon as DsCompletionIcon;
			if (dsIcon is null) {
				var iconSource = icon.IconSource;
				if (iconSource is null)
					return null;
				return new Image {
					Width = 16,
					Height = 16,
					Source = iconSource,
				};
			}

			var imageReference = dsIcon.ImageReference;
			if (imageReference.IsDefault)
				return null;
			var image = new DsImage { ImageReference = imageReference };
			if (!dsIcon.ThemeImage) {
				DsImage.SetBackgroundColor(image, null);
				DsImage.SetBackgroundBrush(image, null);
			}
			return image;
		}

		public static CompletionVM? TryGet(Completion completion) {
			if (completion is null)
				return null;
			if (completion.Properties.TryGetProperty(typeof(CompletionVM), out CompletionVM vm))
				return vm;
			return null;
		}

		IEnumerable<CompletionIconVM> CreateAttributeIcons() {
			var icons = (Completion as Completion2)?.AttributeIcons;
			if (icons is null)
				return Array.Empty<CompletionIconVM>();
			var list = new List<CompletionIconVM>();
			foreach (var icon in icons) {
				var imageUIObject = CreateImageUIObject(icon);
				if (imageUIObject is not null)
					list.Add(new CompletionIconVM(imageUIObject));
			}
			return list;
		}
	}
}
