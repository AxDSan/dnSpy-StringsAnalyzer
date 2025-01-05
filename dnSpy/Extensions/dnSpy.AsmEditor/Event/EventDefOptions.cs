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

using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Event {
	sealed class EventDefOptions {
		public EventAttributes Attributes;
		public UTF8String? Name;
		public ITypeDefOrRef? EventType;
		public MethodDef? AddMethod;
		public MethodDef? InvokeMethod;
		public MethodDef? RemoveMethod;
		public List<MethodDef> OtherMethods = new List<MethodDef>();
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public EventDefOptions() {
		}

		public EventDefOptions(EventDef evt) {
			Attributes = evt.Attributes;
			Name = evt.Name;
			EventType = evt.EventType;
			AddMethod = evt.AddMethod;
			InvokeMethod = evt.InvokeMethod;
			RemoveMethod = evt.RemoveMethod;
			OtherMethods.AddRange(evt.OtherMethods);
			CustomAttributes.AddRange(evt.CustomAttributes);
		}

		public EventDef CopyTo(EventDef evt) {
			evt.Attributes = Attributes;
			evt.Name = Name ?? UTF8String.Empty;
			evt.EventType = EventType;

			if (evt.AddMethod is not null)
				evt.AddMethod.IsAddOn = false;
			if (AddMethod is not null)
				AddMethod.IsAddOn = true;
			evt.AddMethod = AddMethod;

			if (evt.InvokeMethod is not null)
				evt.InvokeMethod.IsFire = false;
			if (InvokeMethod is not null)
				InvokeMethod.IsFire = true;
			evt.InvokeMethod = InvokeMethod;

			if (evt.RemoveMethod is not null)
				evt.RemoveMethod.IsRemoveOn = false;
			if (RemoveMethod is not null)
				RemoveMethod.IsRemoveOn = true;
			evt.RemoveMethod = RemoveMethod;

			foreach (var otherMethod in evt.OtherMethods)
				otherMethod.IsOther = false;
			foreach (var otherMethod in OtherMethods)
				otherMethod.IsOther = true;
			evt.OtherMethods.Clear();
			evt.OtherMethods.AddRange(OtherMethods);

			evt.CustomAttributes.Clear();
			evt.CustomAttributes.AddRange(CustomAttributes);
			return evt;
		}

		public EventDef CreateEventDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new EventDefUser()));

		public static EventDefOptions Create(UTF8String name, ITypeDefOrRef eventType) => new EventDefOptions {
			Attributes = 0,
			Name = name,
			EventType = eventType,
		};
	}
}
