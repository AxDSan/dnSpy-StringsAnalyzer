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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.DnSpy.Properties;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.MVVM.Dialogs;
using Ookii.Dialogs.Wpf;
using WF = System.Windows.Forms;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Saves resources
	/// </summary>
	public static class SaveResources {
		static readonly HashSet<char> invalidFileNameChar = new HashSet<char>();
		static SaveResources() {
			foreach (var c in Path.GetInvalidFileNameChars())
				invalidFileNameChar.Add(c);
			foreach (var c in Path.GetInvalidPathChars())
				invalidFileNameChar.Add(c);
		}

		/// <summary>
		/// Gets all resource data
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <param name="resourceDataType">Type of data to get</param>
		/// <returns></returns>
		public static ResourceData[] GetResourceData(IResourceDataProvider[]? nodes, ResourceDataType resourceDataType) {
			if (nodes is null)
				return Array.Empty<ResourceData>();
			return nodes.SelectMany(a => a.GetResourceData(resourceDataType)).ToArray();
		}

		/// <summary>
		/// Saves the nodes
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <param name="useSubDirs">true to create sub directories, false to dump everything in the same folder</param>
		/// <param name="resourceDataType">Type of data to save</param>
		/// <param name="ownerWindow">Owner window</param>
		public static void Save(IResourceDataProvider[]? nodes, bool useSubDirs, ResourceDataType resourceDataType, Window? ownerWindow = null) {
			if (nodes is null)
				return;

			(ResourceData data, string filename)[] files;
			try {
				files = GetFiles(GetResourceData(nodes, resourceDataType), useSubDirs).ToArray();
			}
			catch (Exception ex) {
				MsgBox.Instance.Show(ex);
				return;
			}
			if (files.Length == 0)
				return;

			var data = new ProgressVM(Dispatcher.CurrentDispatcher, new ResourceSaver(files));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? Application.Current.MainWindow;
			win.Title = files.Length == 1 ? dnSpy_Contracts_DnSpy_Resources.SaveResource : dnSpy_Contracts_DnSpy_Resources.SaveResources;
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			MsgBox.Instance.Show(string.Format(dnSpy_Contracts_DnSpy_Resources.AnErrorOccurred, data.ErrorMessage));
		}

		static IEnumerable<(ResourceData data, string filename)> GetFiles(ResourceData[] infos, bool useSubDirs) {
			if (infos.Length == 1) {
				var info = infos[0];
				var name = FixFileNamePart(GetFileName(info.Name));
				var dlg = new WF.SaveFileDialog {
					Filter = PickFilenameConstants.AnyFilenameFilter,
					RestoreDirectory = true,
					ValidateNames = true,
					FileName = name,
				};
				var ext = Path.GetExtension(name);
				dlg.DefaultExt = string.IsNullOrEmpty(ext) ? string.Empty : ext.Substring(1);
				if (dlg.ShowDialog() != WF.DialogResult.OK)
					yield break;
				yield return (info, dlg.FileName);
			}
			else {
				var dlg = new VistaFolderBrowserDialog();
				if (dlg.ShowDialog() != true)
					yield break;
				string baseDir = dlg.SelectedPath;
				foreach (var info in infos) {
					var name = GetCleanedPath(info.Name, useSubDirs);
					var pathName = Path.Combine(baseDir, name);
					yield return (info, pathName);
				}
			}
		}

		static string GetFileName(string s) {
			int index = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
			if (index < 0)
				return s;
			return s.Substring(index + 1);
		}

		static string FixFileNamePart(string s) {
			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (invalidFileNameChar.Contains(c))
					sb.Append('_');
				else
					sb.Append(c);
			}

			return sb.ToString();
		}

		static string GetCleanedPath(string s, bool useSubDirs) {
			if (!useSubDirs)
				return FixFileNamePart(GetFileName(s));

			string res = string.Empty;
			foreach (var part in s.Replace('/', '\\').Split('\\'))
				res = Path.Combine(res, FixFileNamePart(part));
			return res;
		}
	}

	sealed class ResourceSaver : IProgressTask {
		public bool IsIndeterminate => false;
		public double ProgressMinimum => 0;
		public double ProgressMaximum => fileInfos.Length;

		readonly (ResourceData data, string filename)[] fileInfos;

		public ResourceSaver((ResourceData data, string filename)[] files) => fileInfos = files;

		public void Execute(IProgress progress) {
			var buf = new byte[64 * 1024];
			for (int i = 0; i < fileInfos.Length; i++) {
				progress.ThrowIfCancellationRequested();
				var info = fileInfos[i];
				progress.SetDescription(info.filename);
				progress.SetTotalProgress(i);
				Directory.CreateDirectory(Path.GetDirectoryName(info.filename)!);
				var file = File.Create(info.filename);
				try {
					var stream = info.data.GetStream(progress.Token);
					stream.Position = 0;
					for (;;) {
						int len = stream.Read(buf, 0, buf.Length);
						if (len <= 0) {
							if (stream.Position != stream.Length)
								throw new Exception("Could not read all bytes");
							break;
						}
						file.Write(buf, 0, len);
					}
				}
				catch {
					file.Dispose();
					try { File.Delete(info.filename); }
					catch { }
					throw;
				}
				finally {
					file.Dispose();
				}
			}
			progress.SetTotalProgress(fileInfos.Length);
		}
	}
}
