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

using System.Windows.Controls;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.TreeView;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Constants
	/// </summary>
	public static class MenuConstants {
		/// <summary>Guid of context menu</summary>
		public const string CTX_MENU_GUID = "CB53CCAF-9EE3-411E-A03A-561E7D8470EC";

		/// <summary>Guid of app menu</summary>
		public const string APP_MENU_GUID = "3D87660F-DA21-48B9-9022-C76F0E588E1F";

		/// <summary>Guid of app menu: File</summary>
		public const string APP_MENU_FILE_GUID = "DC3B8109-21BB-40E8-9999-FC6526C3DD15";

		/// <summary>Guid of app menu: Edit</summary>
		public const string APP_MENU_EDIT_GUID = "BC6AE088-F941-4F4B-B976-42A09866C94A";

		/// <summary>Guid of app menu: View</summary>
		public const string APP_MENU_VIEW_GUID = "235BDFD8-A065-4E89-B041-C40A90526AF9";

		/// <summary>Guid of app menu: Debug</summary>
		public const string APP_MENU_DEBUG_GUID = "62B311D0-D77E-4718-86C3-14BA031C47DF";

		/// <summary>Guid of app menu: Window</summary>
		public const string APP_MENU_WINDOW_GUID = "5904BD1D-1EF3-424F-B531-FE6BCF2FC9D4";

		/// <summary>Guid of app menu: Help</summary>
		public const string APP_MENU_HELP_GUID = "52504C1B-7C35-464A-A35D-6D9F59E035D9";

		/// <summary>Guid of app menu: Debug \ Windows</summary>
		public const string APP_MENU_DEBUG_WINDOWS_GUID = "7F95892B-975D-4217-A497-2EB0504489F4";

		/// <summary>Guid of app menu: View \ Bookmarks</summary>
		public const string APP_MENU_VIEW_BOOKMARKS_GUID = "69DBE925-ED20-43D5-B1EF-EBAB0BAE9E9A";

		/// <summary>Guid of glyph margin</summary>
		public const string GLYPHMARGIN_GUID = "53F9F2FF-5AF8-4FC6-B849-74AB88EFB367";

		/// <summary>App menu order: File</summary>
		public const double ORDER_APP_MENU_FILE = 0;

		/// <summary>App menu order: Edit</summary>
		public const double ORDER_APP_MENU_EDIT = 1000;

		/// <summary>App menu order: View</summary>
		public const double ORDER_APP_MENU_VIEW = 2000;

		/// <summary>App menu order: Debug</summary>
		public const double ORDER_APP_MENU_DEBUG = 10000;

		/// <summary>App menu order: Window</summary>
		public const double ORDER_APP_MENU_WINDOW = 1000000;

		/// <summary>App menu order: Help</summary>
		public const double ORDER_APP_MENU_HELP = 1001000;

		/// <summary>An unknown object</summary>
		public static readonly string GUIDOBJ_UNKNOWN_GUID = "9BD7C228-91A0-4140-8E8B-AB0450B418CA";

		/// <summary>Documents treeview</summary>
		public static readonly string GUIDOBJ_DOCUMENTS_TREEVIEW_GUID = "F64505EB-6D8B-4332-B697-73B2D1EE6C37";

		/// <summary>Analyzer's treeview</summary>
		public static readonly string GUIDOBJ_ANALYZER_TREEVIEW_GUID = "4C7D6317-C84A-42E6-A582-FCE3ED35EBE6";

		/// <summary>Search ListBox</summary>
		public static readonly string GUIDOBJ_SEARCH_GUID = "7B460F9C-424D-48B3-8FD3-72CEE8DD58E5";

		/// <summary>Treeview nodes array (<see cref="TreeNodeData"/>[])</summary>
		public static readonly string GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID = "B116BABD-BD8B-4870-968A-D1871CC21638";

		/// <summary><see cref="ISearchResult"/></summary>
		public static readonly string GUIDOBJ_SEARCHRESULT_GUID = "50CD0058-6406-4ACA-A386-1A4E07561C62";

		/// <summary><see cref="TextReference"/></summary>
		public static readonly string GUIDOBJ_CODE_REFERENCE_GUID = "751F4075-D420-4196-BCF0-A0149A8948A4";

		/// <summary>Document <see cref="TabControl"/></summary>
		public static readonly string GUIDOBJ_DOCUMENTS_TABCONTROL_GUID = "AB1B4BCE-D8C1-43BE-8822-C124FBCAC260";

		/// <summary><see cref="ITabGroup"/></summary>
		public static readonly string GUIDOBJ_TABGROUP_GUID = "87B2F94A-D80B-45FD-BB31-71E390CA6C01";

		/// <summary><see cref="IToolWindowGroup"/></summary>
		public static readonly string GUIDOBJ_TOOLWINDOWGROUP_GUID = "3E9743F1-A2E0-4C5A-B463-3E8CF6D677E4";

		/// <summary>Tool window <see cref="TabControl"/></summary>
		public static readonly string GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID = "33FEE79F-7998-4D63-8E6F-B3AD86134960";

		/// <summary><see cref="IDocumentViewer"/>'s UI control</summary>
		public static readonly string GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID = "FF1980C8-049C-4B9C-8298-5B5C30558A97";

		/// <summary><see cref="IDocumentViewer"/></summary>
		public static readonly string GUIDOBJ_DOCUMENTVIEWER_GUID = "F7088928-7BF0-4044-B631-201F6565077A";

		/// <summary><see cref="TextEditorPosition"/></summary>
		public static readonly string GUIDOBJ_TEXTEDITORPOSITION_GUID = "F093458D-C95B-4745-8388-047DE348B500";

		/// <summary><see cref="HexCaretPosition"/></summary>
		public static readonly string GUIDOBJ_HEXEDITORPOSITION_GUID = "DF87FB1F-D902-4365-BA52-655A7B27C94A";

		/// <summary>Asm editor's hex view</summary>
		public static readonly string GUIDOBJ_ASMEDITOR_HEXVIEW_GUID = "95F0CEE5-44D0-468A-B214-69F91B76A84C";

		/// <summary>Debugger's memory hex view</summary>
		public static readonly string GUIDOBJ_DEBUGGER_MEMORY_HEXVIEW_GUID = "8AD6778E-015E-4520-8B77-A6A2E23FFCFF";

		/// <summary>Glyph margin</summary>
		public static readonly string GUIDOBJ_GLYPHMARGIN_GUID = "60A3ECC3-3714-418E-8C26-D33F00EA31B4";

		/// <summary>REPL text editor control</summary>
		public static readonly string GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID = "18953907-F276-43F8-B267-DFEA192DD9B8";

		/// <summary><see cref="IReplEditor"/></summary>
		public static readonly string GUIDOBJ_REPL_EDITOR_GUID = "530F5283-6FCF-49EC-A7D1-52D456C9C846";

		/// <summary><see cref="IWpfTextView"/></summary>
		public static readonly string GUIDOBJ_WPF_TEXTVIEW_GUID = "2579A07D-C6F5-4A13-B0FB-2C8828278C0C";

		/// <summary><see cref="IWpfTextViewHost"/></summary>
		public static readonly string GUIDOBJ_WPF_TEXTVIEW_HOST_GUID = "C1F6C5AB-1E0F-4BF3-A787-39AA78F0F7A1";

		/// <summary><see cref="IWpfTextViewMargin"/></summary>
		public static readonly string GUIDOBJ_WPF_TEXTVIEW_MARGIN_GUID = "36C94DC4-05AA-4F2B-A6C4-02EFE187AAA3";

		/// <summary><see cref="WpfHexViewHost"/></summary>
		public static readonly string GUIDOBJ_WPF_HEXVIEW_HOST_GUID = "D63537FA-9D09-44E0-A345-41B7457CFD69";

		/// <summary><see cref="WpfHexView"/></summary>
		public static readonly string GUIDOBJ_WPF_HEXVIEW_GUID = "2A57190E-B129-4083-8427-EC2DC6C53D55";

		/// <summary><see cref="WpfHexViewMargin"/></summary>
		public static readonly string GUIDOBJ_WPF_HEXVIEW_MARGIN_GUID = "9CFD1794-C39A-4529-89BF-03C0C6E1714F";

		/// <summary>Point of mouse relative to a <see cref="IWpfTextViewMargin"/> or a <see cref="WpfHexViewMargin"/></summary>
		public static readonly string GUIDOBJ_MARGIN_POINT_GUID = "FEAC116C-FA91-42D9-A646-BD8F3A6A6EFD";

		/// <summary>Log text editor control</summary>
		public static readonly string GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID = "898C7BE5-EDAE-42E5-A97F-1FA73C18ED36";

		/// <summary><see cref="ILogEditor"/></summary>
		public static readonly string GUIDOBJ_LOG_EDITOR_GUID = "7ED3CA27-F8F2-4EB8-B9CA-690B27243403";

		/// <summary><see cref="IOutputService"/></summary>
		public static readonly string GUIDOBJ_OUTPUT_SERVICE_GUID = "FB4F524A-7096-46CD-BCE2-EC2550EFCC92";

		/// <summary>Active <see cref="IOutputTextPane"/></summary>
		public static readonly string GUIDOBJ_ACTIVE_OUTPUT_TEXTPANE_GUID = "5787A5D8-80DE-437F-A44A-6FD0138DBB57";

		/// <summary><see cref="ICodeEditor"/></summary>
		public static readonly string GUIDOBJ_CODE_EDITOR_GUID = "297907F8-38BE-4C8C-90D1-3400BB0EB36E";

		/// <summary>Breakpoint</summary>
		public static readonly string GUIDOBJ_BREAKPOINT_GUID = "A229BFE6-0445-4B5C-9D4B-E590995B9D93";

		/// <summary>Bookmark</summary>
		public static readonly string GUIDOBJ_BOOKMARK_GUID = "71BD1A39-50A8-43C2-B88A-8986D0C674B4";

		/// <summary>Variables window treeview (autos, locals, watch)</summary>
		public static readonly string GUIDOBJ_VARIABLES_WINDOW_TREEVIEW_GUID = "6415325D-11CC-48C7-9E7B-15D363B7D18E";

		/// <summary>Group: App Menu: File, Group: Save</summary>
		public const string GROUP_APP_MENU_FILE_SAVE = "0,557C4B2D-5966-41AF-BFCA-D0A36DB5D6D8";

		/// <summary>Group: App Menu: File, Group: Open</summary>
		public const string GROUP_APP_MENU_FILE_OPEN = "1000,636D9C45-00A9-461F-8947-E01755929A5B";

		/// <summary>Group: App Menu: File, Group: Exit</summary>
		public const string GROUP_APP_MENU_FILE_EXIT = "1000000,6EBA065B-5A1E-4DD4-B91A-339F2D2ED66E";

		/// <summary>Group: App Menu: Edit, Group: Undo/Redo</summary>
		public const string GROUP_APP_MENU_EDIT_UNDO = "0,3DFFD4E1-CFD9-442D-B1E5-E1E98AB8766B";

		/// <summary>Group: App Menu: Edit, Group: Find</summary>
		public const string GROUP_APP_MENU_EDIT_FIND = "1000,240D24B1-1A37-41B8-8A9A-94CD72C08145";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Delete</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_DELETE = "2000,F483414D-5CA0-4CE3-9FB2-BFB21987D9F4";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Misc</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_MISC = "3000,3DCA360E-3CCD-4F27-AF50-A254CD5F9C83";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor New</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_NEW = "4000,178A6FD0-2F22-466D-8F2E-664E5531F50B";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Settings</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_SETTINGS = "5000,69EA4DD7-8220-43A5-9812-F1EC221AD7D8";

		/// <summary>Group: App Menu: Edit, Group: Hex</summary>
		public const string GROUP_APP_MENU_EDIT_HEX = "6000,6D8CA476-8D3D-468E-A895-40F3A9D5A25C";

		/// <summary>Group: App Menu: Edit, Group: Hex MD</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_MD = "7000,36F0A9CA-5D14-4F56-8F64-ED3628FB5F30";

		/// <summary>Group: App Menu: Edit, Group: Hex MD Go To</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_GOTO_MD = "8000,1E0213F3-0578-43D9-A12D-14AE30EFD0EA";

		/// <summary>Group: App Menu: Edit, Group: Hex Copy</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_COPY = "9000,32791A7F-4CFC-49D2-B066-A611A9E362DB";

		/// <summary>Group: App Menu: View, Group: Options</summary>
		public const string GROUP_APP_MENU_VIEW_OPTS = "0,FCBA133F-F62B-4DB2-BEC9-5AE11C95873B";

		/// <summary>Group: App Menu: View, Group: Tool Windows</summary>
		public const string GROUP_APP_MENU_VIEW_WINDOWS = "1000,599D070A-521E-4A1B-80DB-62C9B0AB48FA";

		/// <summary>Group: App Menu: View, Group: Options dlg</summary>
		public const string GROUP_APP_MENU_VIEW_OPTSDLG = "1000000,AAA7FF98-47CD-4ABF-8824-EE20A283EEB3";

		/// <summary>Group: App Menu: View \ Bookmarks, Group: Windows</summary>
		public const string GROUP_APP_MENU_BOOKMARKS_WINDOWS = "0,73401CB5-1573-4868-96C9-6141EF39F44F";

		/// <summary>Group: App Menu: View \ Bookmarks, Group: Commands #1</summary>
		public const string GROUP_APP_MENU_BOOKMARKS_COMMANDS1 = "1000,F6008A3C-8EBF-459F-8446-6D96D02060DC";

		/// <summary>Group: App Menu: View \ Bookmarks, Group: Commands #2</summary>
		public const string GROUP_APP_MENU_BOOKMARKS_COMMANDS2 = "2000,10E117AF-68CF-47ED-AEA9-80CDDEDA3C16";

		/// <summary>Group: App Menu: View \ Bookmarks, Group: Commands #3</summary>
		public const string GROUP_APP_MENU_BOOKMARKS_COMMANDS3 = "3000,2E6C9563-FF9F-433B-B03A-A5CFA4228A78";

		/// <summary>Group: App Menu: Themes, Group: Themes</summary>
		public const string GROUP_APP_MENU_THEMES_THEMES = "0,AAE0CE90-DB6E-4E8D-9E1B-9BF7ABBDBB32";

		/// <summary>Group: App Menu: Debug, Group: Windows</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS = "-1000,B4D89733-91AC-4C2F-8808-3AEBD2A686C9";

		/// <summary>Group: App Menu: Debug, Group: Start</summary>
		public const string GROUP_APP_MENU_DEBUG_START = "0,118A7201-7560-443E-B2F6-7F6369A253A2";

		/// <summary>Group: App Menu: Debug, Group: Continue/Stop/etc commands</summary>
		public const string GROUP_APP_MENU_DEBUG_CONTINUE = "1000,E9AEB324-1425-4CBF-8998-B1796A16AA06";

		/// <summary>Group: App Menu: Debug, Group: Step (Current Process) commands</summary>
		public const string GROUP_APP_MENU_DEBUG_STEP_CURRENTPROCESS = "1999,D7C0F536-DD76-428C-8E87-A9D27D4C19A9";

		/// <summary>Group: App Menu: Debug, Group: Step commands</summary>
		public const string GROUP_APP_MENU_DEBUG_STEP = "2000,5667E48E-5E33-46E9-9661-98B979D65F5D";

		/// <summary>Group: App Menu: Debug, Group: Breakpoint commands</summary>
		public const string GROUP_APP_MENU_DEBUG_BREAKPOINTS = "3000,2684EC1B-45A7-4412-BCBF-81345845FF54";

		/// <summary>Group: App Menu: Debug, Group: Options</summary>
		public const string GROUP_APP_MENU_DEBUG_OPTIONS = "1000000,F3B4A871-C8D8-40CA-A881-7BEF2328145C";

		/// <summary>Group: App Menu: Debug \ Windows, Group: Settings</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_SETTINGS = "0,37136731-930E-4D87-8144-03DB3217668D";

		/// <summary>Group: App Menu: Debug \ Windows, Group: Values</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_VALUES = "3000,BC7F81CF-49A1-4F59-B789-56EEDAA375BE";

		/// <summary>Group: App Menu: Debug \ Windows \ Watch, Group: Watch N</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_WATCH_SUB = "0,2248FFD3-0377-4434-B293-378DDD605DF3";

		/// <summary>Group: App Menu: Debug \ Windows, Group: Info</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_INFO = "6000,2721D995-74E5-4AA8-9E32-FBB2EDCE768F";

		/// <summary>Group: App Menu: Debug \ Windows, Group: Memory</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_MEMORY = "7000,246D698C-04F2-4998-88FB-46853D62E290";

		/// <summary>Group: App Menu: Debug \ Windows \ Memory, Group: Memory N</summary>
		public const string GROUP_APP_MENU_DEBUG_WINDOWS_MEMORY_SUB = "0,3B088B70-F2D6-4388-8B5D-397372CEAC9F";

		/// <summary>Group: App Menu: Window, Group: Window</summary>
		public const string GROUP_APP_MENU_WINDOW_WINDOW = "0,27A8834B-D6BF-4267-803D-15DECAFAEA05";

		/// <summary>Group: App Menu: Window, Group: Tab Groups</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPS = "1000,3890B3CB-2DE5-4745-A8F8-61A379485345";

		/// <summary>Group: App Menu: Window, Group: Tab Groups Close commands</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE = "2000,11548593-C399-4EA8-B944-60603BE1FD4B";

		/// <summary>Group: App Menu: Window, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPSVERT = "3000,7E948EE4-59EA-47F2-B1C8-C5A5DB6F13B9";

		/// <summary>Group: App Menu: Window, Group: All Windows</summary>
		public const string GROUP_APP_MENU_WINDOW_ALLWINDOWS = "1000000,0BBFA4E5-3C54-41E9-BC74-69ADDC09CECC";

		/// <summary>Group: App Menu: Help, Group: Links</summary>
		public const string GROUP_APP_MENU_HELP_LINKS = "0,35CCC7A7-D1C0-4F70-AAFC-7E7CD90B4735";

		/// <summary>Group: App Menu: Help, Group: About</summary>
		public const string GROUP_APP_MENU_HELP_ABOUT = "1000000,835F06B5-67FB-4D01-8920-9D9E2FED9238";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Tabs</summary>
		public const string GROUP_CTX_DOCVIEWER_TABS = "0,3576E74B-8D4D-47EE-9925-462B1007C879";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Debug</summary>
		public const string GROUP_CTX_DOCVIEWER_DEBUG = "1000,46C39BDA-35F5-4416-AAE2-A2FE05645F79";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: AsmEditor Save</summary>
		public const string GROUP_CTX_DOCVIEWER_ASMED_SAVE = "2000,57ED92C1-3292-47DD-99CD-FB777DDF1276";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: AsmEditor Delete</summary>
		public const string GROUP_CTX_DOCVIEWER_ASMED_DELETE = "3000,7A3E4F42-37A5-4A85-B403-62E6CD091E1D";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: AsmEditor New</summary>
		public const string GROUP_CTX_DOCVIEWER_ASMED_NEW = "4000,15776B90-55EF-4ABE-9EC8-FB4A1E49A76F";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: AsmEditor Settings</summary>
		public const string GROUP_CTX_DOCVIEWER_ASMED_SETTINGS = "5000,4E4FF711-D262-452D-BA1A-38A6D9951CE2";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: AsmEditor IL ED</summary>
		public const string GROUP_CTX_DOCVIEWER_ASMED_ILED = "6000,5DD87F08-FB00-4D00-9503-29590A8079CE";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Tokens</summary>
		public const string GROUP_CTX_DOCVIEWER_TOKENS = "7000,096957CB-B94D-4A47-AC6D-DBF4C63C6955";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Hex</summary>
		public const string GROUP_CTX_DOCVIEWER_HEX = "9000,81BEEEAD-9498-4AD5-B387-006E93FD4014";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Hex MD</summary>
		public const string GROUP_CTX_DOCVIEWER_HEX_MD = "10000,0BE33A51-E400-4E3D-9B48-FF91E4A78303";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Hex Copy</summary>
		public const string GROUP_CTX_DOCVIEWER_HEX_COPY = "12000,E18271DD-7571-4509-9A7D-37E283BCF7C2";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Debug RT</summary>
		public const string GROUP_CTX_DOCVIEWER_DEBUGRT = "13000,5A9207C0-C0E5-464D-B7A2-FB29ADA9C090";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Other</summary>
		public const string GROUP_CTX_DOCVIEWER_OTHER = "14000,47308D41-FCAD-4518-9859-AD67C2B912BB";

		/// <summary>Group: Context Menu, Type: Document Viewer, Group: Editor</summary>
		public const string GROUP_CTX_DOCVIEWER_EDITOR = "15000,FD52ABD1-6DB2-48C3-A5DB-809ECE5EBBB2";

		/// <summary>Group: Context Menu, Type: HexView, Group: Show commands</summary>
		public const string GROUP_CTX_HEXVIEW_SHOW = "0,261BB98C-6C43-4258-9C4E-7A3702298DE0";

		/// <summary>Group: Context Menu, Type: HexView, Group: Edit</summary>
		public const string GROUP_CTX_HEXVIEW_EDIT = "1000,63AE7AC9-B507-4474-9BB3-9B64B2036D34";

		/// <summary>Group: Context Menu, Type: HexView, Group: Misc</summary>
		public const string GROUP_CTX_HEXVIEW_MISC = "10000,73D6E16B-6AF4-4F6D-8515-6D63ECDBFA3F";

		/// <summary>Group: Context Menu, Type: HexView, Group: Copy</summary>
		public const string GROUP_CTX_HEXVIEW_COPY = "99000,34DCFAE6-7D6A-428A-8E71-5151616A08A3";

		/// <summary>Group: Context Menu, Type: HexView, Group: Options</summary>
		public const string GROUP_CTX_HEXVIEW_OPTS = "100000,0794156A-1EDE-45EC-9C41-48E27DE14085";

		/// <summary>Group: Context Menu, Type: HexView, Group: Find</summary>
		public const string GROUP_CTX_HEXVIEW_FIND = "101000,8BD504DA-A927-4CBC-9E77-C873560C530F";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Close/New commands</summary>
		public const string GROUP_CTX_TABS_CLOSE = "0,FABC0864-6B57-4C49-A1AF-6015F7CFB5F4";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups</summary>
		public const string GROUP_CTX_TABS_GROUPS = "1000,1F89B1F4-8A1F-41FC-8B19-AF3F36AE806E";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups Close commands</summary>
		public const string GROUP_CTX_TABS_GROUPSCLOSE = "2000,80871274-20F2-4A51-8697-C3439781CA40";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_CTX_TABS_GROUPSVERT = "3000,15174C91-6EA8-47E3-880E-FCDF607974F1";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Close commands</summary>
		public const string GROUP_CTX_TOOLWINS_CLOSE = "0,D6F31BC9-2474-44B9-8786-D3044F6F402C";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPS = "1000,32E1C678-7889-499D-8BC3-C22160E7E2AC";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups Close commands</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPSCLOSE = "2000,61D665C4-B55D-45BF-B592-85D174C0A1E7";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPSVERT = "3000,3F438576-672F-4865-B581-759D5DC678D5";

		/// <summary>Group: Context Menu, Type: Search, Group: Tabs</summary>
		public const string GROUP_CTX_SEARCH_TABS = "0,249A0912-68BE-4468-931A-055726958EA4";

		/// <summary>Group: Context Menu, Type: Search, Group: Tokens</summary>
		public const string GROUP_CTX_SEARCH_TOKENS = "1000,8B57D21D-8109-424A-A337-DB61BE361ED4";

		/// <summary>Group: Context Menu, Type: Search, Group: Debug</summary>
		public const string GROUP_CTX_SEARCH_DEBUG = "2000,AC4A027D-7C4C-422A-A619-BF6DFF4DE7F9";

		/// <summary>Group: Context Menu, Type: Search, Group: Other</summary>
		public const string GROUP_CTX_SEARCH_OTHER = "5000,255AE50D-3638-4128-808D-FC8910BA9279";

		/// <summary>Group: Context Menu, Type: Search, Group: Options</summary>
		public const string GROUP_CTX_SEARCH_OPTIONS = "10000,2A261412-7DCD-4CD1-B936-783C67476E99";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Tabs</summary>
		public const string GROUP_CTX_ANALYZER_TABS = "0,BC8D4C75-B5BC-4964-9A3C-E9EE33F928B0";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Tokens</summary>
		public const string GROUP_CTX_ANALYZER_TOKENS = "1000,E3FB23EB-EFA8-4C80-ACCD-DCB714BBAFC7";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Debug</summary>
		public const string GROUP_CTX_ANALYZER_DEBUG = "2000,9DE7F11A-7F72-43E2-AB6E-E8E8587B956F";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Other</summary>
		public const string GROUP_CTX_ANALYZER_OTHER = "5000,A766D535-4069-4AF7-801E-F4B87A2D0F84";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Options</summary>
		public const string GROUP_CTX_ANALYZER_OPTIONS = "10000,FD6E5D84-A83C-4D0A-8A77-EE755DE76999";

		/// <summary>Group: Context Menu, Type: Documents, Group: Tabs</summary>
		public const string GROUP_CTX_DOCUMENTS_TABS = "0,3FEF128B-8320-4ED0-B03B-0932FCCDA98E";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor Save</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_SAVE = "1000,9495E6B9-0C5C-484A-9354-A5D19A5010DE";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor Delete</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_DELETE = "2000,17B24EE5-C1C0-441D-9B6F-C7632AF4C539";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor Misc</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_MISC = "3000,928EDD44-E4A9-4EA9-93FF-55709943A088";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor New</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_NEW = "4000,05FD56B0-CAF9-48E1-9CED-5221E8A13140";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor Settings</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_SETTINGS = "5000,2247C4DB-73B8-4926-96EB-1C16EAF4A3E4";

		/// <summary>Group: Context Menu, Type: Documents, Group: AsmEditor IL ED</summary>
		public const string GROUP_CTX_DOCUMENTS_ASMED_ILED = "6000,9E0E8539-751E-47EA-A0E9-EAB3A45724E3";

		/// <summary>Group: Context Menu, Type: Documents, Group: Tokens</summary>
		public const string GROUP_CTX_DOCUMENTS_TOKENS = "7000,C98101AD-1A59-42AE-B446-16545F39DC7A";

		/// <summary>Group: Context Menu, Type: Documents, Group: Debug RT</summary>
		public const string GROUP_CTX_DOCUMENTS_DEBUGRT = "9000,9A151E30-AC16-4745-A819-24AA199E82CB";

		/// <summary>Group: Context Menu, Type: Documents, Group: Debug</summary>
		public const string GROUP_CTX_DOCUMENTS_DEBUG = "10000,080A553F-F066-41DC-9CC6-B4CCF2C48675";

		/// <summary>Group: Context Menu, Type: Document, Group: Other</summary>
		public const string GROUP_CTX_DOCUMENTS_OTHER = "11000,15776535-8A1D-4255-8C3D-331163324C7C";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Copy</summary>
		public const string GROUP_CTX_BOOKMARKS_COPY = "0,1633C2A1-5A65-41FF-B83D-E6B0E1B565EC";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Code</summary>
		public const string GROUP_CTX_BOOKMARKS_CODE = "1000,3932F616-AD0B-4310-A4DE-678AF7E9C149";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Settings</summary>
		public const string GROUP_CTX_BOOKMARKS_SETTINGS = "2000,12AFA15C-7D72-41FB-A65C-92367D8091A2";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Commands</summary>
		public const string GROUP_CTX_BOOKMARKS_CMDS1 = "3000,4287B6E2-256F-4A8A-9041-EA7BE393C18A";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Export</summary>
		public const string GROUP_CTX_BOOKMARKS_EXPORT = "5000,565EDE0D-4139-486E-A486-CD6B3657E5FF";

		/// <summary>Group: Context Menu, Type: Bookmarks, Group: Options</summary>
		public const string GROUP_CTX_BOOKMARKS_OPTS = "10000,1D7EEE8F-CD29-4F4E-A4A8-6906680B0601";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Copy</summary>
		public const string GROUP_CTX_DBG_CODEBPS_COPY = "0,FB604477-5E55-4B55-91A4-0E06762FED83";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Code</summary>
		public const string GROUP_CTX_DBG_CODEBPS_CODE = "1000,5918522A-B51A-430D-8351-561FF0618AB3";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Settings</summary>
		public const string GROUP_CTX_DBG_CODEBPS_SETTINGS = "1500,466C6110-9CD4-4D64-B532-8DCFC61C01EC";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Commands</summary>
		public const string GROUP_CTX_DBG_CODEBPS_CMDS1 = "2000,3F86C3D0-9FCF-4DF8-93D7-2C1D202DC22D";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Export</summary>
		public const string GROUP_CTX_DBG_CODEBPS_EXPORT = "4000,51A2286D-423B-447D-82B7-4A8AAE9D1203";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Options</summary>
		public const string GROUP_CTX_DBG_CODEBPS_OPTS = "10000,E326374F-8D4F-4CC4-B454-BB3F2C585299";

		/// <summary>Group: Context Menu, Type: Debugger/Module Breakpoints, Group: Copy</summary>
		public const string GROUP_CTX_DBG_MODULEBPS_COPY = "0,648E5B4C-BADE-4226-9B18-EE983438728E";

		/// <summary>Group: Context Menu, Type: Debugger/Module Breakpoints, Group: Commands</summary>
		public const string GROUP_CTX_DBG_MODULEBPS_CMDS1 = "1000,F07E3763-5827-4DE1-95A3-EEBD224B711A";

		/// <summary>Group: Context Menu, Type: Debugger/Module Breakpoints, Group: Commands</summary>
		public const string GROUP_CTX_DBG_MODULEBPS_CMDS2 = "2000,2AE615AA-3786-424A-8C90-B028032DFD6C";

		/// <summary>Group: Context Menu, Type: Debugger/Module Breakpoints, Group: Export</summary>
		public const string GROUP_CTX_DBG_MODULEBPS_EXPORT = "4000,B6A97E72-7CAD-4C36-951A-A8BF1F9BCFBA";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Copy</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_COPY = "0,FA7DD7BA-CC6B-46F4-8838-F8015B586911";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Frame</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_FRAME = "1000,5F24F714-41CB-4111-89C1-BCA9734115B0";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Breakpoints</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_BPS = "1500,C3FC3901-808B-472F-BB71-9AC7E75F1413";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_HEXOPTS = "2000,66C60524-E129-491D-A8A8-7939B567BC3A";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Options</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_OPTS = "3000,8B40E062-CACD-4BF0-BFE2-6003400E9DC8";

		/// <summary>Group: Context Menu, Type: Debugger/Exceptions, Group: Copy</summary>
		public const string GROUP_CTX_DBG_EXCEPTIONS_COPY = "0,836ECA3F-DD93-4843-B752-B81D4A67F1A7";

		/// <summary>Group: Context Menu, Type: Debugger/Exceptions, Group: Add</summary>
		public const string GROUP_CTX_DBG_EXCEPTIONS_ADD = "1000,27050687-6367-48C4-A036-E6E368965BB4";

		/// <summary>Group: Context Menu, Type: Debugger/Exceptions, Group: Options</summary>
		public const string GROUP_CTX_DBG_EXCEPTIONS_OPTIONS = "5000,64A4FCD8-64BD-4435-84E3-5FD0F78BFFCF";

		/// <summary>Group: Context Menu, Type: Debugger/Variables window, Group: Copy</summary>
		public const string GROUP_CTX_DBG_VARIABLES_WINDOW_COPY = "0,5DE1C544-8079-4C4E-ABB1-7CE34BDF6A94";

		/// <summary>Group: Context Menu, Type: Debugger/Variables window, Group: Values</summary>
		public const string GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES = "1000,1A0FDB51-7DCC-4B18-A4BA-0A6A45A8B14A";

		/// <summary>Group: Context Menu, Type: Debugger/Variables window, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_VARIABLES_WINDOW_HEXOPTS = "2000,72E9B097-DD72-411E-B9FC-B01AE30EF24F";

		/// <summary>Group: Context Menu, Type: Debugger/Variables window, Group: Tree</summary>
		public const string GROUP_CTX_DBG_VARIABLES_WINDOW_TREE = "3000,877A4CC7-3074-4EFF-9C6B-96D1203F55F5";

		/// <summary>Group: Context Menu, Type: Debugger/Variables window, Group: Options</summary>
		public const string GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS = "4000,93573019-549D-40EC-8B3C-D515DACB3C47";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Copy</summary>
		public const string GROUP_CTX_DBG_MODULES_COPY = "0,A43EAAA4-2729-418A-B5B8-39237D2E998D";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Go To</summary>
		public const string GROUP_CTX_DBG_MODULES_GOTO = "1000,D981D937-B196-42F9-8AB8-FED62E3C4C43";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_MODULES_HEXOPTS = "2000,4ABA3476-C88E-47F4-B299-46FE12C38AA3";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Directories</summary>
		public const string GROUP_CTX_DBG_MODULES_DIRS = "3000,84F6531F-567B-43F8-9251-5566244F00A7";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Save</summary>
		public const string GROUP_CTX_DBG_MODULES_SAVE = "4000,1B07EE10-60B5-442A-9EC5-63C3D20F5A9E";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Copy</summary>
		public const string GROUP_CTX_DBG_THREADS_COPY = "0,F11E427D-6B88-44B9-ACFF-4D8AD8131DC0";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_THREADS_HEXOPTS = "1000,960A6F14-846D-42EE-BD1E-4C1C91ECB21F";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Commands</summary>
		public const string GROUP_CTX_DBG_THREADS_CMDS = "2000,B7B20F2D-6FE1-4415-BC4A-D92B31EE9342";

		/// <summary>Group: Context Menu, Type: Debugger/Processes, Group: Copy</summary>
		public const string GROUP_CTX_DBG_PROCESSES_COPY = "0,CC3C28D9-E9F7-448B-9299-31038143439F";

		/// <summary>Group: Context Menu, Type: Debugger/Processes, Group: Continue/Break</summary>
		public const string GROUP_CTX_DBG_PROCESSES_CONTINUE = "1000,3F9F65B7-A18C-4657-AED7-CBB521196C34";

		/// <summary>Group: Context Menu, Type: Debugger/Processes, Group: Detach/Terminate</summary>
		public const string GROUP_CTX_DBG_PROCESSES_TERMINATE = "2000,276A37FE-50C8-4B56-BF56-4A0F414207DF";

		/// <summary>Group: Context Menu, Type: Debugger/Processes, Group: Options</summary>
		public const string GROUP_CTX_DBG_PROCESSES_OPTIONS = "3000,09039E26-03A2-453C-B164-F43DF8154D3F";

		/// <summary>Group: Context Menu, Type: Debugger/Processes, Group: Attach</summary>
		public const string GROUP_CTX_DBG_PROCESSES_ATTACH = "4000,1F9D28D0-282B-46BB-9E2D-59703E28A5FF";

		/// <summary>Group: Context Menu, Type: REPL text editor, Group: Reset</summary>
		public const string GROUP_CTX_REPL_RESET = "0,407111D9-B090-4151-83FF-2C01C3816DF3";

		/// <summary>Group: Context Menu, Type: REPL text editor, Group: Copy</summary>
		public const string GROUP_CTX_REPL_COPY = "1000,E246D458-5DBD-41A6-866B-948793A1D125";

		/// <summary>Group: Context Menu, Type: REPL text editor, Group: Clear</summary>
		public const string GROUP_CTX_REPL_CLEAR = "2000,1B3A6F12-AB30-4750-AB62-BB34DE4D9D0C";

		/// <summary>Group: Context Menu, Type: REPL text editor, Group: Save</summary>
		public const string GROUP_CTX_REPL_SAVE = "3000,F2D18E19-FBA7-4DFF-A72D-B88C44DBFC43";

		/// <summary>Group: Context Menu, Type: REPL text editor, Group: Settings</summary>
		public const string GROUP_CTX_REPL_SETTINGS = "4000,0585159F-E555-433D-B854-42A36487B7C4";

		/// <summary>Group: Context Menu, Type: Output text editor, Group: Copy</summary>
		public const string GROUP_CTX_OUTPUT_COPY = "0,7E2D36F5-9F04-411C-81B6-DD92B53A9D57";

		/// <summary>Group: Context Menu, Type: Output text editor, Group: Settings</summary>
		public const string GROUP_CTX_OUTPUT_SETTINGS = "1000,DF1E714B-B1E3-4B6F-948E-36C4B69AA649";

		/// <summary>Group: Context Menu, Type: Output text editor, Group: User Commands</summary>
		public const string GROUP_CTX_OUTPUT_USER_COMMANDS = "2000,A48F98FE-5BA6-4023-A7E5-0C3D6AFCC10B";

		/// <summary>Group: Context Menu, Type: Code editor, Group: Compile</summary>
		public const string GROUP_CTX_CODEEDITOR_COMPILE = "0,3920F03E-3345-4557-AEBC-11EF272C6D62";

		/// <summary>Group: Context Menu, Type: Code editor, Group: Copy</summary>
		public const string GROUP_CTX_CODEEDITOR_COPY = "5000,3B7890A7-AF3C-4FA2-9554-B0FA65B9F767";

		/// <summary>Group: Context Menu, Type: Code editor, Group: Find</summary>
		public const string GROUP_CTX_CODEEDITOR_FIND= "6000,CDE742E8-31DA-4D96-A641-73A36CCF0DC0";

		/// <summary>Group: Glyph margin, Type: Debugger/Breakpoints, Group: Settings</summary>
		public const string GROUP_GLYPHMARGIN_DEBUG_CODEBPS_SETTINGS = "0,FB70F59F-7507-43C1-AD7B-BCBDD60375F6";

		/// <summary>Group: Glyph margin, Type: Debugger/Breakpoints, Group: Edit</summary>
		public const string GROUP_GLYPHMARGIN_DEBUG_CODEBPS_EDIT = "5000,B31BDBFD-3C44-4D14-92E8-85141167696F";

		/// <summary>Group: Glyph margin, Type: Debugger/Breakpoints, Group: Breakpoints</summary>
		public const string GROUP_GLYPHMARGIN_DEBUG_CODEBPS_EXPORT = "10000,EEC8041B-DC23-4E50-BEBC-BB71AB36631D";
	}
}
