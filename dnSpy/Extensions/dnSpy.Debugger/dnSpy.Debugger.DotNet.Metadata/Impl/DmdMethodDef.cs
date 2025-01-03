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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdMethodDef : DmdMethodInfoBase {
		public sealed override DmdType? DeclaringType { get; }
		public sealed override DmdType? ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x06000000 + rid);
		public sealed override bool IsMetadataReference => false;

		public sealed override bool IsGenericMethodDefinition => GetMethodSignature().GenericParameterCount != 0;
		public sealed override bool IsGenericMethod => GetMethodSignature().GenericParameterCount != 0;

		public sealed override DmdParameterInfo ReturnParameter {
			get {
				var f = ExtraFields;
				if (f.__returnParameter_DONT_USE is null)
					InitializeParameters();
				return f.__returnParameter_DONT_USE!;
			}
		}

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdMethodDef(uint rid, DmdType declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdMethodInfo? Resolve(bool throwOnError) => this;

		protected abstract DmdType[]? CreateGenericParameters();
		public sealed override ReadOnlyCollection<DmdType> GetGenericArguments() {
			var f = ExtraFields;
			if (f.__genericParameters_DONT_USE is not null)
				return f.__genericParameters_DONT_USE;
			var res = CreateGenericParameters();
			Interlocked.CompareExchange(ref f.__genericParameters_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
			return f.__genericParameters_DONT_USE!;
		}

		public sealed override ReadOnlyCollection<DmdParameterInfo> GetParameters() {
			var f = ExtraFields;
			if (f.__parameters_DONT_USE is null)
				InitializeParameters();
			return f.__parameters_DONT_USE!;
		}
		void InitializeParameters() {
			var f = ExtraFields;
			if (f.__parameters_DONT_USE is not null)
				return;
			var info = CreateParameters();
			Debug.Assert(info.parameters.Length == GetMethodSignature().GetParameterTypes().Count);
			lock (LockObject) {
				if (f.__parameters_DONT_USE is null) {
					f.__returnParameter_DONT_USE = info.returnParameter;
					f.__parameters_DONT_USE = ReadOnlyCollectionHelpers.Create(info.parameters);
				}
			}
		}
		protected abstract (DmdParameterInfo? returnParameter, DmdParameterInfo[] parameters) CreateParameters();

		internal override DmdMethodInfo? GetParentDefinition() {
			((DmdTypeBase)ReflectedType!).InitializeParentDefinitions();
			return parentDefinition;
		}

		internal DmdMethodInfo SetParentDefinition(DmdMethodInfo method) => parentDefinition = method;
		// We can't put this in ExtraFields since it's init'd whenever the code finds all methods in the class,
		// so most methods would alloc their ExtraFields.
		DmdMethodInfo? parentDefinition;

		public sealed override DmdMethodInfo GetGenericMethodDefinition() {
			if (!IsGenericMethodDefinition)
				throw new InvalidOperationException();
			if ((object)ReflectedType! == DeclaringType)
				return this;
			return DeclaringType!.GetMethod(Module, MetadataToken) as DmdMethodInfo ?? throw new InvalidOperationException();
		}

		public sealed override DmdMethodInfo MakeGenericMethod(IList<DmdType> typeArguments) => AppDomain.MakeGenericMethod(this, typeArguments);

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is null)
				InitializeCustomAttributes();
			return f.__customAttributes_DONT_USE!;
		}

		void InitializeCustomAttributes() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is not null)
				return;
			var info = CreateCustomAttributes();
			var newSAs = ReadOnlyCollectionHelpers.Create(info.sas);
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info.cas, newSAs, info.implMap);
			lock (LockObject) {
				if (f.__customAttributes_DONT_USE is null) {
					f.__securityAttributes_DONT_USE = newSAs;
					f.__customAttributes_DONT_USE = newCAs;
				}
			}
		}

		ExtraFieldsImpl ExtraFields {
			get {
				if (__extraFields_DONT_USE is ExtraFieldsImpl f)
					return f;
				Interlocked.CompareExchange(ref __extraFields_DONT_USE, new ExtraFieldsImpl(), null);
				return __extraFields_DONT_USE!;
			}
		}
		volatile ExtraFieldsImpl? __extraFields_DONT_USE;

		// Most of the fields aren't used so we alloc them when needed
		sealed class ExtraFieldsImpl {
			public volatile ReadOnlyCollection<DmdType>? __genericParameters_DONT_USE;
			public volatile ReadOnlyCollection<DmdParameterInfo>? __parameters_DONT_USE;
			public volatile DmdParameterInfo? __returnParameter_DONT_USE;
			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __customAttributes_DONT_USE;
			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __securityAttributes_DONT_USE;
			public volatile uint __rva_DONT_USE;
			public volatile bool __rva_initd_DONT_USE;
		}

		protected abstract (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas, DmdImplMap? implMap) CreateCustomAttributes();

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is null)
				InitializeCustomAttributes();
			return f.__securityAttributes_DONT_USE!;
		}

		protected abstract uint GetRVA();
		public sealed override uint RVA {
			get {
				var f = ExtraFields;
				if (!f.__rva_initd_DONT_USE) {
					f.__rva_DONT_USE = GetRVA();
					f.__rva_initd_DONT_USE = true;
				}
				return f.__rva_DONT_USE;
			}
		}
	}
}
