using System;
using System.Collections.Generic;

namespace Mono.Debugger.Soft
{
	public abstract class Value : Mirror {

		// FIXME: Add a 'Value' field

		internal Value (VirtualMachine vm, long id) : base (vm, id) {
		}

		public IInvokeAsyncResult BeginInvokeMethod (ThreadMirror thread, MethodMirror method, IList<Value> arguments, InvokeOptions options, AsyncCallback callback, object state) {
			return ObjectMirror.BeginInvokeMethod (vm, thread, method, this, arguments, options, callback, state);
		}

		public InvokeResult EndInvokeMethodWithResult (IAsyncResult asyncResult) {
			var result = ObjectMirror.EndInvokeMethodInternalWithResult (asyncResult);
			var outThis = result.OutThis as StructMirror;
			if (outThis != null) {
				(this as StructMirror)?.SetFields (outThis.Fields);
			}
			return result;
		}
	}
}

