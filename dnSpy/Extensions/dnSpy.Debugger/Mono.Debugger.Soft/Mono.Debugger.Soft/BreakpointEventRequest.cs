using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Debugger.Soft
{
	public sealed class BreakpointEventRequest : EventRequest {

		public object Tag { get; set; }

		MethodMirror method;
		long location;
		
		internal BreakpointEventRequest (VirtualMachine vm, MethodMirror method, long location) : base (vm, EventType.Breakpoint) {
			if (method == null)
				throw new ArgumentNullException ("method");
			CheckMirror (vm, method);
			this.method = method;
			this.location = location;
		}

		public override void Enable () {
			var mods = new List <Modifier> ();
			mods.Add (new LocationModifier () { Method = method.Id, Location = location });
			SendReq (mods);
		}
	}
}