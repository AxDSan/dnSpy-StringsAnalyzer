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
using System.Windows.Controls;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Constants
	/// </summary>
	public static class ControlConstants {
		/// <summary>Guid of main window</summary>
		public static readonly Guid GUID_MAINWINDOW = new Guid("6C6DF6A3-2681-4A17-B81C-7EF8ABAC845C");

		/// <summary>Guid of <see cref="IDocumentViewer"/> UI control</summary>
		public static readonly Guid GUID_DOCUMENTVIEWER_UICONTEXT = new Guid("38542BD9-D8E0-4281-8B71-6470F1342689");

		/// <summary>Guid of document <see cref="ITreeView"/></summary>
		public static readonly Guid GUID_DOCUMENT_TREEVIEW = new Guid("E0ABA20F-5CD7-4CFD-A9D4-F9F3C655DD4A");

		/// <summary>Guid of analyzer <see cref="ITreeView"/></summary>
		public static readonly Guid GUID_ANALYZER_TREEVIEW = new Guid("6C62342D-8CBE-4EC4-9E05-828DDCCFE934");

		/// <summary>Guid of search control</summary>
		public static readonly Guid GUID_SEARCH_CONTROL = new Guid("D2699C68-1A08-4522-9A2D-C5DF6002F5FC");

		/// <summary>Guid of search <see cref="ListBox"/></summary>
		public static readonly Guid GUID_SEARCH_LISTBOX = new Guid("651FC97F-A9A7-4649-97AC-FC942168E6E2");

		/// <summary>Guid of bookmarks control</summary>
		public static readonly Guid GUID_BOOKMARKS_CONTROL = new Guid("0222FABA-6344-4DAE-966F-3FA5EA19598F");

		/// <summary>Guid of bookmarks <see cref="ListView"/></summary>
		public static readonly Guid GUID_BOOKMARKS_LISTVIEW = new Guid("2A3AE539-9666-486C-98AC-AA5679DD9F54");

		/// <summary>Guid of debugger breakpoints control</summary>
		public static readonly Guid GUID_DEBUGGER_CODEBREAKPOINTS_CONTROL = new Guid("00EC8F82-086C-4305-A07D-CC43CB035905");

		/// <summary>Guid of debugger breakpoints <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_CODEBREAKPOINTS_LISTVIEW = new Guid("E178917C-199C-4A99-95F9-9724806E528F");

		/// <summary>Guid of debugger module breakpoints control</summary>
		public static readonly Guid GUID_DEBUGGER_MODULEBREAKPOINTS_CONTROL = new Guid("D64862B4-6282-4579-BE84-41B1D629F980");

		/// <summary>Guid of debugger module breakpoints <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_MODULEBREAKPOINTS_LISTVIEW = new Guid("DAB850F0-BA82-454E-9C00-2EF9C12CCF7F");

		/// <summary>Guid of debugger call stack control</summary>
		public static readonly Guid GUID_DEBUGGER_CALLSTACK_CONTROL = new Guid("D0EDBB27-8367-4806-BB03-03B6990A7D32");

		/// <summary>Guid of debugger call stack <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_CALLSTACK_LISTVIEW = new Guid("7E39E2DD-666C-4309-867E-9460D97361D2");

		/// <summary>Guid of debugger exceptions control</summary>
		public static readonly Guid GUID_DEBUGGER_EXCEPTIONS_CONTROL = new Guid("FD139D3D-2C84-40C1-B088-11BD99840956");

		/// <summary>Guid of debugger exceptions <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_EXCEPTIONS_LISTVIEW = new Guid("02BC921F-A601-456A-8C4F-84256C34A2A0");

		/// <summary>Guid of debugger threads control</summary>
		public static readonly Guid GUID_DEBUGGER_THREADS_CONTROL = new Guid("19AB4CCD-65AB-46B1-9855-79BDABBCDFFB");

		/// <summary>Guid of debugger threads <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_THREADS_LISTVIEW = new Guid("44EB5AF6-D9D3-44AD-ABB0-288C6F95EE29");

		/// <summary>Guid of debugger modules control</summary>
		public static readonly Guid GUID_DEBUGGER_MODULES_CONTROL = new Guid("131B8A8D-771B-46DE-A8D4-20D4BBEBF2B1");

		/// <summary>Guid of debugger modules <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_MODULES_LISTVIEW = new Guid("F91D9EA8-614D-4B36-AE27-B4EA541F6992");

		/// <summary>Guid of debugger processes control</summary>
		public static readonly Guid GUID_DEBUGGER_PROCESSES_CONTROL = new Guid("418382F3-596E-485E-BF56-6FAF156EAA34");

		/// <summary>Guid of debugger processes <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_PROCESSES_LISTVIEW = new Guid("A26EA8A2-64EE-4311-B365-789C26D86711");

		/// <summary>Guid of debugger memory control</summary>
		public static readonly Guid GUID_DEBUGGER_MEMORY_CONTROL = new Guid("D638F6E0-EA1E-4E2C-9969-A14751C800D1");

		/// <summary>Guid of debugger memory <see cref="WpfHexViewHost"/></summary>
		public static readonly Guid GUID_DEBUGGER_MEMORY_WPFHEXVIEWHOST = new Guid("9A82A54B-B5FC-4EA4-B825-45DD32C1695D");

		/// <summary>Guid of output control</summary>
		public static readonly Guid GUID_OUTPUT_CONTROL = new Guid("0DD9693D-DA25-40A0-A9AC-4393D5819969");
	}
}
