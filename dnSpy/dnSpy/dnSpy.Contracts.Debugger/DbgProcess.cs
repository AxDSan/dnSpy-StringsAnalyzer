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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A debugged process
	/// </summary>
	public abstract class DbgProcess : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the owner debug manager
		/// </summary>
		public abstract DbgManager DbgManager { get; }

		/// <summary>
		/// Process id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets all runtimes
		/// </summary>
		public abstract DbgRuntime[] Runtimes { get; }

		/// <summary>
		/// Raised when <see cref="Runtimes"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgRuntime>>? RuntimesChanged;

		/// <summary>
		/// Gets the process bitness (32 or 64)
		/// </summary>
		public abstract int Bitness { get; }

		/// <summary>
		/// Gets the size of a pointer
		/// </summary>
		public int PointerSize => Bitness / 8;

		/// <summary>
		/// Gets the architecture
		/// </summary>
		public abstract DbgArchitecture Architecture { get; }

		/// <summary>
		/// Gets the operating system
		/// </summary>
		public abstract DbgOperatingSystem OperatingSystem { get; }

		/// <summary>
		/// Gets the process state
		/// </summary>
		public abstract DbgProcessState State { get; }

		/// <summary>
		/// Gets the filename or an empty string if it's unknown
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// Gets the process name or an empty string if it's unknown
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// What is being debugged. This is shown in the UI (eg. Processes window)
		/// </summary>
		public abstract ReadOnlyCollection<string> Debugging { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Raised when <see cref="Threads"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgThread>>? ThreadsChanged;

		/// <summary>
		/// true if the process is running, false if it's paused or terminated (see <see cref="State"/>)
		/// </summary>
		public abstract bool IsRunning { get; }

		/// <summary>
		/// Raised when <see cref="IsRunning"/> is changed, see also <see cref="DelayedIsRunningChanged"/>
		/// </summary>
		public abstract event EventHandler? IsRunningChanged;

		/// <summary>
		/// Raised when the process has been running for a little while, eg. 1 second.
		/// </summary>
		public abstract event EventHandler? DelayedIsRunningChanged;

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination address</param>
		/// <param name="size">Number of bytes to read</param>
		public unsafe abstract void ReadMemory(ulong address, void* destination, int size);

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination buffer</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="size">Number of bytes to read</param>
		public abstract void ReadMemory(ulong address, byte[] destination, int destinationIndex, int size);

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination buffer</param>
		public void ReadMemory(ulong address, byte[] destination) {
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			ReadMemory(address, destination, 0, destination.Length);
		}

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public byte[] ReadMemory(ulong address, int size) {
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (size == 0)
				return Array.Empty<byte>();
			var res = new byte[size];
			ReadMemory(address, res, 0, size);
			return res;
		}

		/// <summary>
		/// Writes memory.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source address</param>
		/// <param name="size">Number of bytes to write</param>
		public unsafe abstract void WriteMemory(ulong address, void* source, int size);

		/// <summary>
		/// Writes memory.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source buffer</param>
		/// <param name="sourceIndex">Source index</param>
		/// <param name="size">Number of bytes to write</param>
		public abstract void WriteMemory(ulong address, byte[] source, int sourceIndex, int size);

		/// <summary>
		/// Writes memory.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source buffer</param>
		public void WriteMemory(ulong address, byte[] source) {
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			WriteMemory(address, source, 0, source.Length);
		}

		/// <summary>
		/// true if the process gets detached when debugging stops (<see cref="StopDebugging"/>),
		/// false if the process gets terminated.
		/// </summary>
		public abstract bool ShouldDetach { get; set; }

		/// <summary>
		/// Stops debugging. This will either detach from the process or terminate it depending on <see cref="ShouldDetach"/>
		/// </summary>
		public void StopDebugging() {
			if (ShouldDetach)
				Detach();
			else
				Terminate();
		}

		/// <summary>
		/// Detaches the process, if possible, else it will be terminated
		/// </summary>
		public abstract void Detach();

		/// <summary>
		/// Terminates the process
		/// </summary>
		public abstract void Terminate();

		/// <summary>
		/// Pauses the process
		/// </summary>
		public abstract void Break();

		/// <summary>
		/// Lets the process run again
		/// </summary>
		public abstract void Run();
	}

	/// <summary>
	/// Architecture
	/// </summary>
	public enum DbgArchitecture {
		/// <summary>
		/// x86, 32-bit
		/// </summary>
		X86,

		/// <summary>
		/// x64, 64-bit
		/// </summary>
		X64,

		/// <summary>
		/// 32-bit ARM
		/// </summary>
		Arm,

		/// <summary>
		/// 64-bit ARM
		/// </summary>
		Arm64,
	}

	/// <summary>
	/// Operating system
	/// </summary>
	public enum DbgOperatingSystem {
		/// <summary>
		/// Windows OS
		/// </summary>
		Windows,

		/// <summary>
		/// OSX/MacOS OS
		/// </summary>
		MacOS,

		/// <summary>
		/// Linux OS
		/// </summary>
		Linux,

		/// <summary>
		/// FreeBSD OS
		/// </summary>
		FreeBSD,
	}

	/// <summary>
	/// Process state
	/// </summary>
	public enum DbgProcessState {
		/// <summary>
		/// The process is running
		/// </summary>
		Running,

		/// <summary>
		/// The process is paused
		/// </summary>
		Paused,

		/// <summary>
		/// The process is terminated
		/// </summary>
		Terminated,
	}
}
