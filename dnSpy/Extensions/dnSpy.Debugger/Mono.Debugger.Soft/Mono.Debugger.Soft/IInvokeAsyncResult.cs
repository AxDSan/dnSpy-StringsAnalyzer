using System;
using System.Collections.Generic;

namespace Mono.Debugger.Soft
{
	public interface IInvokeAsyncResult : IAsyncResult, IDisposable
	{
		void Abort ();
	}
}
