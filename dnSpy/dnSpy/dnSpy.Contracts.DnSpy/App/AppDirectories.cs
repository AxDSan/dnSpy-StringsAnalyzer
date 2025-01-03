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

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Application directories
	/// </summary>
	public static class AppDirectories {
		const string DNSPY_SETTINGS_FILENAME = "dnSpy.xml";

		/// <summary>
		/// Base directory of dnSpy binaries. If all files have been moved to a 'bin' sub dir,
		/// this is the path of the bin sub dir.
		/// </summary>
		public static string BinDirectory { get; }

		/// <summary>
		/// Base directory of data directory. Usually %APPDATA%\dnSpy but could be identical to
		/// <see cref="BinDirectory"/>.
		/// </summary>
		public static string DataDirectory { get; }

		/// <summary>
		/// dnSpy settings filename
		/// </summary>
		public static string SettingsFilename => settingsFilename;
		static string settingsFilename;

		internal static void SetSettingsFilename(string? filename) {
			if (hasCalledSetSettingsFilename)
				throw new InvalidOperationException();
			hasCalledSetSettingsFilename = true;
			if (!string2.IsNullOrEmpty(filename))
				settingsFilename = filename;
		}
		static bool hasCalledSetSettingsFilename = false;

		static AppDirectories() {
			// This assembly is always in the bin sub dir if one exists
			BinDirectory = Path.GetDirectoryName(typeof(AppDirectories).Assembly.Location)!;
			settingsFilename = Path.Combine(BinDirectory, DNSPY_SETTINGS_FILENAME);
			if (File.Exists(settingsFilename))
				DataDirectory = BinDirectory;
			else {
				DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnSpy");
				settingsFilename = Path.Combine(DataDirectory, DNSPY_SETTINGS_FILENAME);
			}
		}

		/// <summary>
		/// Returns directories relative to <see cref="BinDirectory"/> and <see cref="DataDirectory"/>
		/// in that order. If they're identical, only one path is returned.
		/// </summary>
		/// <param name="subDir">Sub directory</param>
		/// <returns></returns>
		public static IEnumerable<string> GetDirectories(string subDir) {
			yield return Path.Combine(BinDirectory, subDir);
			if (!StringComparer.OrdinalIgnoreCase.Equals(BinDirectory, DataDirectory))
				yield return Path.Combine(DataDirectory, subDir);
		}
	}
}
