using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mono.Debugger.Soft
{
	/*
	 * Represents a value of a primitive type in the debuggee
	 */
	public class PrimitiveValue : Value {
		object value;
		ElementType etype;

		public PrimitiveValue (VirtualMachine vm, ElementType etype, object value) : base (vm, 0) {
			this.etype = etype;
			this.value = value;
		}

		public ElementType Type {
			get {
				return etype;
			}
		}

		public object Value {
			get {
				return value;
			}
		}

		public override bool Equals (object obj) {
			if (value == obj)
				return true;

			var primitive = obj as PrimitiveValue;
			if (primitive != null)
				return value == primitive.Value;

			return base.Equals (obj);
		}

		public override int GetHashCode () {
			return base.GetHashCode ();
		}

		public override string ToString () {
			object v = Value;

			return "PrimitiveValue<" + (v != null ? v.ToString () : "(null)") + ">";
		}
	}
}
