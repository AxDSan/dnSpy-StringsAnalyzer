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

namespace dnSpy.Contracts.Debugger.StartDebugging.Dialog {
	/// <summary>
	/// A page shown in the 'debug an application' dialog box. It provides a UI object
	/// and creates a <see cref="StartDebuggingOptions"/> instance that is used to start
	/// the application.
	/// </summary>
	public abstract class StartDebuggingOptionsPage : INotifyPropertyChanged {
		/// <summary>
		/// Raised after a property is changed
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Guid of this page
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Display order of the UI object compared to other instances, see <see cref="PredefinedStartDebuggingOptionsPageDisplayOrders"/>
		/// </summary>
		public abstract double DisplayOrder { get; }

		/// <summary>
		/// Name of debug engine shown in the UI, eg. ".NET Framework" or ".NET" or "Mono"
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Gets the UI object
		/// </summary>
		public abstract object? UIObject { get; }

		/// <summary>
		/// true if all options are valid and <see cref="GetOptions"/> can be called.
		/// <see cref="PropertyChanged"/> gets raised when this property is changed.
		/// </summary>
		public abstract bool IsValid { get; }

		/// <summary>
		/// Initializes this instance to previous options
		/// </summary>
		/// <param name="options">Options</param>
		public abstract void InitializePreviousOptions(StartDebuggingOptions options);

		/// <summary>
		/// Initializes this instance to default options. If <paramref name="filename"/> is not
		/// an EXE file, then <paramref name="options"/> should be used to initialize this instance,
		/// else <paramref name="options"/> should be ignored.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="breakKind">Default break kind, see <see cref="PredefinedBreakKinds"/></param>
		/// <param name="options">Options or null</param>
		public abstract void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions? options);

		/// <summary>
		/// Gets all options. This method is only called if <see cref="IsValid"/> returns true
		/// </summary>
		/// <returns></returns>
		public abstract StartDebuggingOptionsInfo GetOptions();

		/// <summary>
		/// Returns true if this is a debug engine page that is compatible with a debug engine
		/// (see eg. <see cref="PredefinedGenericDebugEngineGuids"/>)
		/// </summary>
		/// <param name="engineGuid">Generic debug engine guid (see <see cref="PredefinedGenericDebugEngineGuids"/>)</param>
		/// <param name="order">Only used if the method returns true and is the order to use if more than
		/// one instance returns true. (see <see cref="PredefinedGenericDebugEngineOrders"/>)</param>
		/// <returns></returns>
		public abstract bool SupportsDebugEngine(Guid engineGuid, out double order);

		/// <summary>
		/// Called when the dialog box gets closed
		/// </summary>
		public virtual void OnClose() { }
	}

	/// <summary>
	/// Contains the options and an optional filename
	/// </summary>
	public readonly struct StartDebuggingOptionsInfo {
		/// <summary>
		/// Gets the options
		/// </summary>
		public StartDebuggingOptions Options { get; }

		/// <summary>
		/// Filename or null
		/// </summary>
		public string? Filename { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public StartDebuggingOptionsInfoFlags Flags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="filename">Filename or null</param>
		/// <param name="flags">Flags</param>
		public StartDebuggingOptionsInfo(StartDebuggingOptions options, string? filename, StartDebuggingOptionsInfoFlags flags) {
			Options = options ?? throw new ArgumentNullException(nameof(options));
			Filename = filename;
			Flags = flags;
		}
	}

	/// <summary>
	/// Extra start options
	/// </summary>
	[Flags]
	public enum StartDebuggingOptionsInfoFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// The file extension is not the normal extension
		/// </summary>
		WrongExtension			= 0x00000001,
	}

	/// <summary>
	/// Returned by <see cref="StartDebuggingOptionsPage.SupportsDebugEngine(Guid, out double)"/>
	/// </summary>
	public static class PredefinedGenericDebugEngineOrders {
		/// <summary>
		/// .NET Framework
		/// </summary>
		public const double DotNetFramework = 1000000;

		/// <summary>
		/// .NET
		/// </summary>
		public const double DotNet = 1000000;

		/// <summary>
		/// .NET Mono
		/// </summary>
		public const double DotNetMono = DotNetFramework + 1;

		/// <summary>
		/// .NET Unity
		/// </summary>
		public const double DotNetUnity = 1000000;
	}
}
