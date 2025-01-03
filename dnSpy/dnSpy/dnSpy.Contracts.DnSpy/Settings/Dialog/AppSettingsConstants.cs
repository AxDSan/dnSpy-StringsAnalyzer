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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Constants
	/// </summary>
	public static class AppSettingsConstants {
		/// <summary>
		/// Code using <see cref="IDecompiler.ShowMember(IMemberRef)"/> must be refreshed.
		/// </summary>
		public static readonly Guid REFRESH_LANGUAGE_SHOWMEMBER = new Guid("64819A35-1DA1-4485-BFF0-C9C702147550");

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		public static readonly Guid DISABLE_MEMORY_MAPPED_IO = new Guid("D34E66D2-524C-4B6C-87CE-ED8ECCC32C59");

		/// <summary>
		/// Order of Environment settings page
		/// </summary>
		public const double ORDER_ENVIRONMENT = 0;

		/// <summary>
		/// Order of decompiler settings page
		/// </summary>
		public const double ORDER_DECOMPILER = 1000;

		/// <summary>
		/// Order of compiler settings page
		/// </summary>
		public const double ORDER_COMPILER = 1500;

		/// <summary>
		/// Order of debugger settings page
		/// </summary>
		public const double ORDER_DEBUGGER = 2000;

		/// <summary>
		/// Order of disassembler settings page
		/// </summary>
		public const double ORDER_DISASSEMBLER = 2500;

		/// <summary>
		/// Order of Text Viewer settings page
		/// </summary>
		public const double ORDER_DOCUMENT_VIEWER = 3000;

		/// <summary>
		/// Order of Code Editor settings page
		/// </summary>
		public const double ORDER_CODE_EDITOR = 4000;

		/// <summary>
		/// Order of REPL settings page
		/// </summary>
		public const double ORDER_REPL = 5000;

		/// <summary>
		/// Order of Output Window settings page
		/// </summary>
		public const double ORDER_OUTPUT = 6000;

		/// <summary>
		/// Order of Assembly Explorer settings page
		/// </summary>
		public const double ORDER_ASSEMBLY_EXPLORER = 7000;

		/// <summary>
		/// Order of baml settings page
		/// </summary>
		public const double ORDER_BAML = 8000;

		/// <summary>
		/// Order of hex editor settings page
		/// </summary>
		public const double ORDER_HEXEDITOR = 9000;

		/// <summary>
		/// Order of background image settings page
		/// </summary>
		public const double ORDER_BACKGROUNDIMAGE = 10000;

		/// <summary>
		/// Order of bookmarks settings page
		/// </summary>
		public const double ORDER_BOOKMARKS = 11000;

		/// <summary>
		/// Order of <see cref="IDsDocumentService"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DOCUMENTMANAGER = double.MinValue;	// It must be first since it disables mmap'd I/O

		/// <summary>
		/// Order of decompiler's <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DECOMPILER = 1000;

		/// <summary>
		/// Order of <see cref="IDocumentTreeView"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DOCUMENTTREEVIEW = 2000;

		/// <summary>
		/// Guid of Environment settings page
		/// </summary>
		public const string GUID_ENVIRONMENT = "66B8E553-3961-4B0D-8948-F399FA78A809";

		/// <summary>
		/// Guid of Decompiler settings page
		/// </summary>
		public const string GUID_DECOMPILER = "E380FC93-BACB-4125-8AF1-ADFAEA4D1307";

		/// <summary>
		/// Guid of Compiler settings page
		/// </summary>
		public const string GUID_COMPILER = "3ABE6BCF-06FE-4D75-AFA0-1E75577D8BCD";

		/// <summary>
		/// Guid of Disassembler settings page
		/// </summary>
		public const string GUID_DISASSEMBLER = "9B8D176A-7BEE-46B4-995A-727C82FF7DAF";

		/// <summary>
		/// Guid of Disassembler / Code Style page
		/// </summary>
		public const string GUID_DISASSEMBLER_CODESTYLE = "48477544-423A-46CB-B9C4-AFBC73A1DBD3";

		/// <summary>
		/// Guid of Code Editor settings page
		/// </summary>
		public const string GUID_CODE_EDITOR = "E1BA254B-C5EA-4305-801A-9336BDFA33FB";

		/// <summary>
		/// Guid of C# (Roslyn) code editor settings
		/// </summary>
		public const string GUID_CODE_EDITOR_CSHARP_ROSLYN = "A5170115-EC2C-495E-BBAC-0A67E311CAF6";

		/// <summary>
		/// Guid of Visual Basic (Roslyn) code editor settings
		/// </summary>
		public const string GUID_CODE_EDITOR_VISUAL_BASIC_ROSLYN = "A863FFDA-2AD2-4DB4-B5D4-B2363F3D450F";

		/// <summary>
		/// Guid of Text Viewer settings page
		/// </summary>
		public const string GUID_DOCUMENT_VIEWER = "DEFBC59F-8FB1-4DC1-8067-B006BE07B6DA";

		/// <summary>
		/// Guid of Output Window settings page
		/// </summary>
		public const string GUID_OUTPUT = "26FAD694-0964-476C-AA73-7EE94FBAFFA3";

		/// <summary>
		/// Guid of REPL settings page
		/// </summary>
		public const string GUID_REPL = "6AF674BC-0AB4-4A0C-9F41-6F83ED992DAB";

		/// <summary>
		/// Guid of C# (Roslyn) REPL settings
		/// </summary>
		public const string GUID_REPL_CSHARP_ROSLYN = "4A391B5A-8A9F-485E-9E48-76FEEAD0176B";

		/// <summary>
		/// Guid of Visual Basic (Roslyn) REPL settings
		/// </summary>
		public const string GUID_REPL_VISUAL_BASIC_ROSLYN = "7C275A4E-348E-4620-A292-60DAC69AFF7D";

		/// <summary>
		/// Guid of Hex Editor settings page
		/// </summary>
		public const string GUID_HEX_EDITOR = "81B4DA71-3489-4A2D-BE18-9D0BA0D49E71";

		/// <summary>
		/// GUID of Hex Editor / Default page
		/// </summary>
		public const string GUID_HEX_EDITOR_DEFAULT = "1B88E778-3CF6-4E06-BCD1-12FC0E651089";

		/// <summary>
		/// GUID of Hex Editor / Debugger page
		/// </summary>
		public const string GUID_HEX_EDITOR_DEBUGGER_MEMORY = "6C458D3B-4288-47F4-BD34-4F96AE98CEE6";

		/// <summary>
		/// Order of Environment / General
		/// </summary>
		public const double ORDER_ENVIRONMENT_GENERAL = 0;

		/// <summary>
		/// Order of Environment / Font
		/// </summary>
		public const double ORDER_ENVIRONMENT_FONT = 1000;

		/// <summary>
		/// Order of Decompiler / ILSpy C#/VB
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_CSHARP = 10000;

		/// <summary>
		/// Order of Decompiler / ILSpy IL
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_IL = 11000;

		/// <summary>
		/// Order of Compiler / C#
		/// </summary>
		public const double ORDER_COMPILER_SETTINGS_CSHARP = 10000;

		/// <summary>
		/// Order of Compiler / Visual Basic
		/// </summary>
		public const double ORDER_COMPILER_SETTINGS_VISUALBASIC = 11000;

		/// <summary>
		/// Start of language options
		/// </summary>
		public const double ORDER_CODE_EDITOR_LANGUAGES = 10000;

		/// <summary>
		/// Order of General language settings
		/// </summary>
		public const double ORDER_CODE_EDITOR_LANGUAGES_GENERAL = 1000;

		/// <summary>
		/// Order of Scroll Bars language settings
		/// </summary>
		public const double ORDER_CODE_EDITOR_LANGUAGES_SCROLLBARS = 2000;

		/// <summary>
		/// Order of Tabs language settings
		/// </summary>
		public const double ORDER_CODE_EDITOR_LANGUAGES_TABS = 3000;

		/// <summary>
		/// Order of Advanced language settings
		/// </summary>
		public const double ORDER_CODE_EDITOR_LANGUAGES_ADVANCED = 4000;

		/// <summary>
		/// Order of Text Viewer / General page
		/// </summary>
		public const double ORDER_DOCUMENT_VIEWER_DEFAULT_GENERAL = 1000;

		/// <summary>
		/// Order of Text Viewer / Scroll Bars page
		/// </summary>
		public const double ORDER_DOCUMENT_VIEWER_DEFAULT_SCROLLBARS = 2000;

		/// <summary>
		/// Order of Text Viewer / Tabs page
		/// </summary>
		public const double ORDER_DOCUMENT_VIEWER_DEFAULT_TABS = 3000;

		/// <summary>
		/// Order of Text Viewer / Advanced page
		/// </summary>
		public const double ORDER_DOCUMENT_VIEWER_DEFAULT_ADVANCED = 4000;

		/// <summary>
		/// Order of Output Window / General page
		/// </summary>
		public const double ORDER_OUTPUT_DEFAULT_GENERAL = 1000;

		/// <summary>
		/// Order of Output Window / Scroll Bars page
		/// </summary>
		public const double ORDER_OUTPUT_DEFAULT_SCROLLBARS = 2000;

		/// <summary>
		/// Order of Output Window / Tabs page
		/// </summary>
		public const double ORDER_OUTPUT_DEFAULT_TABS = 3000;

		/// <summary>
		/// Order of Output Window / Advanced page
		/// </summary>
		public const double ORDER_OUTPUT_DEFAULT_ADVANCED = 4000;

		/// <summary>
		/// Start of language options
		/// </summary>
		public const double ORDER_REPL_LANGUAGES = 10000;

		/// <summary>
		/// Order of General language settings
		/// </summary>
		public const double ORDER_REPL_LANGUAGES_GENERAL = 1000;

		/// <summary>
		/// Order of Scroll Bars language settings
		/// </summary>
		public const double ORDER_REPL_LANGUAGES_SCROLLBARS = 2000;

		/// <summary>
		/// Order of Tabs language settings
		/// </summary>
		public const double ORDER_REPL_LANGUAGES_TABS = 3000;

		/// <summary>
		/// Order of Advanced language settings
		/// </summary>
		public const double ORDER_REPL_LANGUAGES_ADVANCED = 4000;

		/// <summary>
		/// Start of group options
		/// </summary>
		public const double ORDER_HEX_EDITOR_NAMES = 10000;

		/// <summary>
		/// Order of General settings
		/// </summary>
		public const double ORDER_HEX_EDITOR_NAMES_GENERAL = 1000;

		/// <summary>
		/// Order of Scroll Bars settings
		/// </summary>
		public const double ORDER_HEX_EDITOR_NAMES_SCROLLBARS = 2000;

		/// <summary>
		/// Order of Advanced settings
		/// </summary>
		public const double ORDER_HEX_EDITOR_NAMES_ADVANCED = 3000;
	}
}
