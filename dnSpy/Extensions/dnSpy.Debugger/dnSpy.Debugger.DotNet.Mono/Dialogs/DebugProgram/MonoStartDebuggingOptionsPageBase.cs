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
using System.IO;
using System.Windows.Input;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	abstract class MonoStartDebuggingOptionsPageBase : StartDebuggingOptionsPage, IDataErrorInfo {
		public override object? UIObject => this;

		public string Filename {
			get => filename;
			set {
				if (filename != value) {
					filename = value;
					OnPropertyChanged(nameof(Filename));
					UpdateIsValid();
					var path = GetPath(filename);
					if (path is not null)
						WorkingDirectory = path;
				}
			}
		}
		string filename = string.Empty;

		public string CommandLine {
			get => commandLine;
			set {
				if (commandLine != value) {
					commandLine = value;
					OnPropertyChanged(nameof(CommandLine));
					UpdateIsValid();
				}
			}
		}
		string commandLine = string.Empty;

		public string WorkingDirectory {
			get => workingDirectory;
			set {
				if (workingDirectory != value) {
					workingDirectory = value;
					OnPropertyChanged(nameof(WorkingDirectory));
					UpdateIsValid();
				}
			}
		}
		string workingDirectory = string.Empty;

		public UInt16VM ConnectionPort { get; }
		public UInt32VM ConnectionTimeout { get; }

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(BreakProcessKindsUtils.BreakProcessKindList);

		public string? BreakKind {
			get => (string)BreakProcessKindVM.SelectedItem!;
			set => BreakProcessKindVM.SelectedItem = value;
		}

		public ICommand PickFilenameCommand => new RelayCommand(a => PickNewFilename());
		public ICommand PickWorkingDirectoryCommand => new RelayCommand(a => PickNewWorkingDirectory());

		public override bool IsValid => isValid;
		bool isValid;

		protected void UpdateIsValid() {
			var newIsValid = CalculateIsValid();
			if (newIsValid == isValid)
				return;
			isValid = newIsValid;
			OnPropertyChanged(nameof(IsValid));
		}

		protected readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		protected MonoStartDebuggingOptionsPageBase(IPickFilename pickFilename, IPickDirectory pickDirectory) {
			this.pickFilename = pickFilename ?? throw new ArgumentNullException(nameof(pickFilename));
			this.pickDirectory = pickDirectory ?? throw new ArgumentNullException(nameof(pickDirectory));
			ConnectionPort = new UInt16VM(a => UpdateIsValid(), useDecimal: true);
			ConnectionTimeout = new UInt32VM(a => UpdateIsValid(), useDecimal: true);
		}

		static string? GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
		}

		static string FilterBreakKind(string? breakKind) {
			foreach (var info in BreakProcessKindsUtils.BreakProcessKindList) {
				if (StringComparer.Ordinal.Equals(breakKind, (string)info.Value))
					return breakKind!;
			}
			return PredefinedBreakKinds.DontBreak;
		}

		protected static void Initialize(string filename, MonoStartDebuggingOptionsBase options) {
			options.Filename = filename;
			options.WorkingDirectory = GetPath(options.Filename);
		}

		void PickNewFilename() {
			var newFilename = pickFilename.GetFilename(Filename, "dll", PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			if (newFilename is null)
				return;

			Filename = newFilename;
		}

		void PickNewWorkingDirectory() {
			var newDir = pickDirectory.GetDirectory(WorkingDirectory);
			if (newDir is null)
				return;

			WorkingDirectory = newDir;
		}

		protected void Initialize(MonoStartDebuggingOptionsBase options) {
			Filename = options.Filename ?? string.Empty;
			CommandLine = options.CommandLine ?? string.Empty;
			// Must be init'd after Filename since it also overwrites this property
			WorkingDirectory = options.WorkingDirectory ?? string.Empty;
			ConnectionPort.Value = options.ConnectionPort;
			ConnectionTimeout.Value = (uint)options.ConnectionTimeout.TotalSeconds;
			BreakKind = FilterBreakKind(options.BreakKind);
		}

		protected void InitializeDefault(MonoStartDebuggingOptionsBase options, string breakKind) {
			options.BreakKind = FilterBreakKind(breakKind);
		}

		protected void GetOptions(MonoStartDebuggingOptionsBase options) {
			options.Filename = Filename;
			options.CommandLine = CommandLine;
			options.WorkingDirectory = WorkingDirectory;
			options.ConnectionPort = ConnectionPort.Value;
			options.ConnectionTimeout = TimeSpan.FromSeconds(ConnectionTimeout.Value);
			options.BreakKind = FilterBreakKind(BreakKind);
		}

		string IDataErrorInfo.Error => throw new NotImplementedException();
		string IDataErrorInfo.this[string columnName] => Verify(columnName);

		protected static string VerifyFilename(string filename) {
			if (!File.Exists(filename)) {
				if (string.IsNullOrWhiteSpace(filename))
					return dnSpy_Debugger_DotNet_Mono_Resources.Error_MissingFilename;
				return dnSpy_Debugger_DotNet_Mono_Resources.Error_FileDoesNotExist;
			}
			return string.Empty;
		}

		bool CalculateIsValid() =>
			!ConnectionPort.HasError &&
			!ConnectionTimeout.HasError &&
			string.IsNullOrEmpty(Verify(nameof(Filename))) &&
			CalculateIsValidCore();

		protected abstract bool CalculateIsValidCore();

		protected string Verify(string columnName) {
			var error = VerifyCore(columnName);
			if (!string.IsNullOrEmpty(error))
				return error;
			if (columnName == nameof(Filename))
				return VerifyFilename(Filename);

			return string.Empty;
		}

		protected abstract string VerifyCore(string columnName);
	}
}
