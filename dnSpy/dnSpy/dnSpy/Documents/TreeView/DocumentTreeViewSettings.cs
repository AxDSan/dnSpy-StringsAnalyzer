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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.TreeView {
	class DocumentTreeViewSettings : ViewModelBase, IDocumentTreeViewSettings {
		public bool SyntaxHighlight {
			get => syntaxHighlightDocumentTreeView;
			set {
				if (syntaxHighlightDocumentTreeView != value) {
					syntaxHighlightDocumentTreeView = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
				}
			}
		}
		bool syntaxHighlightDocumentTreeView = true;

		public bool SingleClickExpandsTreeViewChildren {
			get => singleClickExpandsTreeViewChildren;
			set {
				if (singleClickExpandsTreeViewChildren != value) {
					singleClickExpandsTreeViewChildren = value;
					OnPropertyChanged(nameof(SingleClickExpandsTreeViewChildren));
				}
			}
		}
		bool singleClickExpandsTreeViewChildren = true;

		public bool ShowAssemblyVersion {
			get => showAssemblyVersion;
			set {
				if (showAssemblyVersion != value) {
					showAssemblyVersion = value;
					OnPropertyChanged(nameof(ShowAssemblyVersion));
				}
			}
		}
		bool showAssemblyVersion = true;

		public bool ShowAssemblyPublicKeyToken {
			get => showAssemblyPublicKeyToken;
			set {
				if (showAssemblyPublicKeyToken != value) {
					showAssemblyPublicKeyToken = value;
					OnPropertyChanged(nameof(ShowAssemblyPublicKeyToken));
				}
			}
		}
		bool showAssemblyPublicKeyToken = false;

		public bool ShowToken {
			get => showToken;
			set {
				if (showToken != value) {
					showToken = value;
					OnPropertyChanged(nameof(ShowToken));
				}
			}
		}
		bool showToken = true;

		public bool DeserializeResources {
			get => false;
			set { }
		}

		public DocumentFilterType FilterDraggedItems {
			get => filterDraggedItems;
			set {
				if (filterDraggedItems != value) {
					filterDraggedItems = value;
					OnPropertyChanged(nameof(FilterDraggedItems));
				}
			}
		}
		DocumentFilterType filterDraggedItems = DocumentFilterType.AllSupported;

		MemberKind[] memberKinds = new MemberKind[5] {
			MemberKind.Methods,
			MemberKind.Properties,
			MemberKind.Events,
			MemberKind.Fields,
			MemberKind.NestedTypes,
		};

		public MemberKind MemberKind0 {
			get => memberKinds[0];
			set => SetMemberKind(0, value);
		}

		public MemberKind MemberKind1 {
			get => memberKinds[1];
			set => SetMemberKind(1, value);
		}

		public MemberKind MemberKind2 {
			get => memberKinds[2];
			set => SetMemberKind(2, value);
		}

		public MemberKind MemberKind3 {
			get => memberKinds[3];
			set => SetMemberKind(3, value);
		}

		public MemberKind MemberKind4 {
			get => memberKinds[4];
			set => SetMemberKind(4, value);
		}

		void SetMemberKind(int index, MemberKind newValue) {
			if (memberKinds[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(memberKinds, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				memberKinds[otherIndex] = memberKinds[index];
				memberKinds[index] = newValue;

				OnPropertyChanged(string.Format(MemberKind_format, otherIndex));
			}
			OnPropertyChanged(string.Format(MemberKind_format, index));
		}
		static string MemberKind_format = nameof(MemberKind0).Substring(0, nameof(MemberKind0).Length - 1) + "{0}";

		public DocumentTreeViewSettings Clone() => CopyTo(new DocumentTreeViewSettings());

		public DocumentTreeViewSettings CopyTo(DocumentTreeViewSettings other) {
			other.SyntaxHighlight = SyntaxHighlight;
			other.SingleClickExpandsTreeViewChildren = SingleClickExpandsTreeViewChildren;
			other.ShowAssemblyVersion = ShowAssemblyVersion;
			other.ShowAssemblyPublicKeyToken = ShowAssemblyPublicKeyToken;
			other.ShowToken = ShowToken;
			other.DeserializeResources = DeserializeResources;
			other.FilterDraggedItems = FilterDraggedItems;
			other.MemberKind0 = MemberKind0;
			other.MemberKind1 = MemberKind1;
			other.MemberKind2 = MemberKind2;
			other.MemberKind3 = MemberKind3;
			other.MemberKind4 = MemberKind4;
			return other;
		}
	}

	[Export, Export(typeof(IDocumentTreeViewSettings))]
	sealed class DocumentTreeViewSettingsImpl : DocumentTreeViewSettings {
		static readonly Guid SETTINGS_GUID = new Guid("3E04ABE0-FD5E-4938-B40C-F86AA0FA377D");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DocumentTreeViewSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			SingleClickExpandsTreeViewChildren = sect.Attribute<bool?>(nameof(SingleClickExpandsTreeViewChildren)) ?? SingleClickExpandsTreeViewChildren;
			ShowAssemblyVersion = sect.Attribute<bool?>(nameof(ShowAssemblyVersion)) ?? ShowAssemblyVersion;
			ShowAssemblyPublicKeyToken = sect.Attribute<bool?>(nameof(ShowAssemblyPublicKeyToken)) ?? ShowAssemblyPublicKeyToken;
			ShowToken = sect.Attribute<bool?>(nameof(ShowToken)) ?? ShowToken;
			DeserializeResources = sect.Attribute<bool?>(nameof(DeserializeResources)) ?? DeserializeResources;
			FilterDraggedItems = sect.Attribute<DocumentFilterType?>(nameof(FilterDraggedItems)) ?? FilterDraggedItems;
			MemberKind0 = sect.Attribute<MemberKind?>(nameof(MemberKind0)) ?? MemberKind0;
			MemberKind1 = sect.Attribute<MemberKind?>(nameof(MemberKind1)) ?? MemberKind1;
			MemberKind2 = sect.Attribute<MemberKind?>(nameof(MemberKind2)) ?? MemberKind2;
			MemberKind3 = sect.Attribute<MemberKind?>(nameof(MemberKind3)) ?? MemberKind3;
			MemberKind4 = sect.Attribute<MemberKind?>(nameof(MemberKind4)) ?? MemberKind4;
			PropertyChanged += DocumentTreeViewSettingsImpl_PropertyChanged;
		}

		void DocumentTreeViewSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(SingleClickExpandsTreeViewChildren), SingleClickExpandsTreeViewChildren);
			sect.Attribute(nameof(ShowAssemblyVersion), ShowAssemblyVersion);
			sect.Attribute(nameof(ShowAssemblyPublicKeyToken), ShowAssemblyPublicKeyToken);
			sect.Attribute(nameof(ShowToken), ShowToken);
			sect.Attribute(nameof(DeserializeResources), DeserializeResources);
			sect.Attribute(nameof(FilterDraggedItems), FilterDraggedItems);
			sect.Attribute(nameof(MemberKind0), MemberKind0);
			sect.Attribute(nameof(MemberKind1), MemberKind1);
			sect.Attribute(nameof(MemberKind2), MemberKind2);
			sect.Attribute(nameof(MemberKind3), MemberKind3);
			sect.Attribute(nameof(MemberKind4), MemberKind4);
		}
	}
}
