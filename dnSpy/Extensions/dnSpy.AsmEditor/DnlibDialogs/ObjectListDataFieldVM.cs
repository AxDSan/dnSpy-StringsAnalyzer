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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ObjectListDataFieldVM : DataFieldVM<IList<object?>> {
		readonly TypeSigCreatorOptions? options;
		List<object?> objects = new List<object?>();

		public ICreateConstantType CreateConstantType {
			set => createConstantType = value;
		}
		ICreateConstantType? createConstantType;

		public ICommand AddObjectCommand => new RelayCommand(a => AddObject());
		public ICommand RemoveObjectCommand => new RelayCommand(a => RemoveObject(), a => RemoveObjectCanExecute());
		public ICommand ClearObjectsCommand => new RelayCommand(a => ClearObjects(), a => ClearObjectsCanExecute());

		readonly ModuleDef ownerModule;

		public ObjectListDataFieldVM(ModuleDef ownerModule, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: this(ownerModule, Array.Empty<object>(), onUpdated, options) {
		}

		public ObjectListDataFieldVM(ModuleDef ownerModule, IList<object?> value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: base(onUpdated) {
			this.ownerModule = ownerModule;
			if (options is not null) {
				this.options = options.Clone(dnSpy_AsmEditor_Resources.CreateType);
				this.options.NullTypeSigAllowed = true;
			}
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<object?> value) {
			objects.Clear();
			if (value is not null)
				objects.AddRange(value);
			return CalculateStringValue();
		}

		string CalculateStringValue() => string.Join(", ", objects.Select(a => DlgUtils.ValueToString(a, true)));

		void InitializeStringValue() => StringValue = CalculateStringValue();

		protected override string? ConvertToValue(out IList<object?> value) {
			value = objects.ToArray();
			return null;
		}

		static readonly ConstantType[] Constants = new ConstantType[] {
			ConstantType.Null,
			ConstantType.Boolean,
			ConstantType.Char,
			ConstantType.SByte,
			ConstantType.Byte,
			ConstantType.Int16,
			ConstantType.UInt16,
			ConstantType.Int32,
			ConstantType.UInt32,
			ConstantType.Int64,
			ConstantType.UInt64,
			ConstantType.Single,
			ConstantType.Double,
			ConstantType.String,
			ConstantType.Enum,
			ConstantType.Type,
			ConstantType.ObjectArray,
		};

		void AddObject() {
			if (createConstantType is null)
				throw new InvalidOperationException();
			var newObject = createConstantType.Create(ownerModule, null, Constants, true, true, options, out _, out bool canceled);
			if (canceled)
				return;

			objects.Add(newObject);
			InitializeStringValue();
		}

		void RemoveObject() {
			if (!RemoveObjectCanExecute())
				return;

			objects.RemoveAt(objects.Count - 1);
			InitializeStringValue();
		}

		bool RemoveObjectCanExecute() => objects.Count > 0;

		void ClearObjects() {
			if (!ClearObjectsCanExecute())
				return;

			objects.Clear();
			InitializeStringValue();
		}

		bool ClearObjectsCanExecute() => objects.Count > 0;
	}
}
