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

// Simple hack to decompile code from the command line.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using dnlib.DotNet;
using dnSpy.Console.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;
using dnSpy.Decompiler.MSBuild;

namespace dnSpy_Console {
	[Serializable]
	sealed class ErrorException : Exception {
		public ErrorException(string s)
			: base(s) {
		}
	}

	static class Program {
		static int Main(string[] args) {
			if (!dnlib.Settings.IsThreadSafe) {
				Console.WriteLine("dnlib wasn't compiled with THREAD_SAFE defined");
				return 1;
			}

			var oldEncoding = Console.OutputEncoding;
			try {
				// Make sure russian and chinese characters are shown correctly
				Console.OutputEncoding = Encoding.UTF8;

				return new DnSpyDecompiler().Run(args);
			}
			catch (Exception ex) {
				Console.Error.WriteLine(ex.ToString());
				return 1;
			}
			finally {
				Console.OutputEncoding = oldEncoding;
			}
		}
	}

	readonly struct ConsoleColorPair {
		public ConsoleColor? Foreground { get; }
		public ConsoleColor? Background { get; }
		public ConsoleColorPair(ConsoleColor? foreground, ConsoleColor? background) {
			Foreground = foreground;
			Background = background;
		}
	}

	sealed class ColorProvider {
		readonly Dictionary<TextColor, ConsoleColorPair> colors = new Dictionary<TextColor, ConsoleColorPair>();

		public void Add(TextColor color, ConsoleColor? foreground, ConsoleColor? background = null) {
			if (foreground is not null || background is not null)
				colors[color] = new ConsoleColorPair(foreground, background);
		}

		public ConsoleColorPair? GetColor(TextColor? color) {
			if (color is null)
				return null;
			return colors.TryGetValue(color.Value, out var ccPair) ? ccPair : (ConsoleColorPair?)null;
		}
	}

	sealed class ConsoleColorizerOutput : IDecompilerOutput {
		readonly ColorProvider colorProvider;
		readonly TextWriter writer;
		readonly Indenter indenter;
		bool addIndent = true;
		int position;

		public int Length => position;
		public int NextPosition => position + (addIndent ? indenter.String.Length : 0);

		bool IDecompilerOutput.UsesCustomData => false;

		public ConsoleColorizerOutput(TextWriter writer, ColorProvider colorProvider, Indenter indenter) {
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			this.colorProvider = colorProvider ?? throw new ArgumentNullException(nameof(colorProvider));
			this.indenter = indenter ?? throw new ArgumentNullException(nameof(indenter));
		}

		void IDecompilerOutput.AddCustomData<TData>(string id, TData data) { }
		public void IncreaseIndent() => indenter.IncreaseIndent();
		public void DecreaseIndent() => indenter.DecreaseIndent();

		public void WriteLine() {
			var nlArray = newLineArray;
			writer.Write(nlArray);
			position += nlArray.Length;
			addIndent = true;
		}
		static readonly char[] newLineArray = Environment.NewLine.ToCharArray();

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			var s = indenter.String;
			writer.Write(s);
			position += s.Length;
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			var colorPair = colorProvider.GetColor(color as TextColor?);
			if (colorPair is not null) {
				if (colorPair.Value.Foreground is not null)
					Console.ForegroundColor = colorPair.Value.Foreground.Value;
				if (colorPair.Value.Background is not null)
					Console.BackgroundColor = colorPair.Value.Background.Value;
				writer.Write(text);
				Console.ResetColor();
			}
			else
				writer.Write(text);

			position += text.Length;
		}

		void AddText(string text, int index, int length, object color) {
			if (index == 0 && length == text.Length)
				AddText(text, color);
			else
				AddText(text.Substring(index, length), color);
		}

		public void Write(string text, object color) => AddText(text, color);
		public void Write(string text, int index, int length, object color) => AddText(text, index, length, color);
		public void Write(string text, object? reference, DecompilerReferenceFlags flags, object color) => AddText(text, color);
		public void Write(string text, int index, int length, object? reference, DecompilerReferenceFlags flags, object color) => AddText(text, index, length, color);
		public override string ToString() => writer.ToString()!;
		public void Dispose() => writer.Dispose();
	}

	sealed class DnSpyDecompiler : IMSBuildProjectWriterLogger {
		bool isRecursive = false;
		bool useGac = true;
		bool addCorlibRef = true;
		bool createSlnFile = true;
		bool unpackResources = true;
		bool createResX = true;
		bool decompileBaml = true;
		bool colorizeOutput;
		Guid projectGuid = Guid.NewGuid();
		int numThreads;
		int mdToken;
		int spaces;
		string? typeName;
		ProjectVersion projectVersion = ProjectVersion.VS2010;
		string? outputDir;
		string slnName = "solution.sln";
		readonly List<string> files;
		readonly List<string> asmPaths;
		readonly List<string> userGacPaths;
		readonly List<string> gacFiles;
		string language = DecompilerConstants.LANGUAGE_CSHARP.ToString();
		readonly DecompilationContext decompilationContext;
		readonly ModuleContext moduleContext;
		readonly AssemblyResolver assemblyResolver;
		readonly IBamlDecompiler? bamlDecompiler;
		readonly HashSet<string> reservedOptions;
#if NET
		readonly dnSpy.MainApp.DotNetAssemblyLoader dotNetAssemblyLoader = new dnSpy.MainApp.DotNetAssemblyLoader(System.Runtime.Loader.AssemblyLoadContext.Default);
#endif

		static readonly char PATHS_SEP = Path.PathSeparator;

		public DnSpyDecompiler() {
#if NET
			// This assembly is always in the bin sub dir if one exists
			dotNetAssemblyLoader.AddSearchPath(Path.GetDirectoryName(typeof(ILSpan).Assembly.Location)!);
#endif
			files = new List<string>();
			asmPaths = new List<string>();
			userGacPaths = new List<string>();
			gacFiles = new List<string>();
			decompilationContext = new DecompilationContext();
			moduleContext = ModuleDef.CreateModuleContext(); // Same as dnSpy.exe
			assemblyResolver = (AssemblyResolver)moduleContext.AssemblyResolver;
			assemblyResolver.EnableFrameworkRedirect = false; // Same as dnSpy.exe
			assemblyResolver.FindExactMatch = true; // Same as dnSpy.exe
			assemblyResolver.EnableTypeDefCache = true;
			bamlDecompiler = TryLoadBamlDecompiler();
			decompileBaml = bamlDecompiler is not null;
			reservedOptions = GetReservedOptions();
			colorizeOutput = !Console.IsOutputRedirected;

			var langs = new List<IDecompiler>();
			langs.AddRange(GetAllLanguages());
			langs.Sort((a, b) => a.OrderUI.CompareTo(b.OrderUI));
			allLanguages = langs.ToArray();
		}

		static IEnumerable<IDecompiler> GetAllLanguages() {
			var asmNames = new string[] {
				"dnSpy.Decompiler.ILSpy.Core",
			};
			foreach (var asmName in asmNames) {
				foreach (var l in GetLanguagesInAssembly(asmName))
					yield return l;
			}
		}

		static IEnumerable<IDecompiler> GetLanguagesInAssembly(string asmName) {
			var asm = TryLoad(asmName);
			if (asm is not null) {
				foreach (var type in asm.GetTypes()) {
					if (!type.IsAbstract && !type.IsInterface && typeof(IDecompilerProvider).IsAssignableFrom(type)) {
						var p = (IDecompilerProvider)Activator.CreateInstance(type)!;
						foreach (var l in p.Create())
							yield return l;
					}
				}
			}
		}

		static IBamlDecompiler? TryLoadBamlDecompiler() => TryCreateType<IBamlDecompiler>("dnSpy.BamlDecompiler.x", "dnSpy.BamlDecompiler.BamlDecompiler");

		static Assembly? TryLoad(string asmName) {
			try {
				return Assembly.Load(asmName);
			}
			catch {
			}
			return null;
		}

		static T? TryCreateType<T>(string asmName, string typeFullName) where T : class {
			var asm = TryLoad(asmName);
			var type = asm?.GetType(typeFullName);
			return type is null ? default! : (T)Activator.CreateInstance(type)!;
		}

		public int Run(string[] args) {
			try {
				ParseCommandLine(args);
				if (allLanguages.Length == 0)
					throw new ErrorException(dnSpy_Console_Resources.NoLanguagesFound);
				if (GetLanguageOrNull() is null)
					throw new ErrorException(string.Format(dnSpy_Console_Resources.LanguageXDoesNotExist, language));
				Decompile();
			}
			catch (ErrorException ex) {
				PrintHelp();
				Console.WriteLine();
				Console.WriteLine(dnSpy_Console_Resources.Error1, ex.Message);
				return 1;
			}
			catch (Exception ex) {
				Dump(ex);
				return 1;
			}
			return errors == 0 ? 0 : 1;
		}

		void PrintHelp() {
			var progName = GetProgramBaseName();
			Console.WriteLine(progName + " " + dnSpy_Console_Resources.UsageHeader, progName);
			Console.WriteLine();
			foreach (var info in usageInfos) {
				var arg = info.Option;
				if (info.OptionArgument is not null)
					arg = arg + " " + info.OptionArgument;
				Console.WriteLine("  {0,-12}   {1}", arg, string.Format(info.Description, PATHS_SEP));
			}
			Console.WriteLine();
			Console.WriteLine(dnSpy_Console_Resources.Languages);
			foreach (var lang in AllLanguages)
				Console.WriteLine("  {0} ({1})", lang.UniqueNameUI, lang.UniqueGuid.ToString("B"));

			var langLists = GetLanguageOptions().Where(a => a[0].Settings.Options.Any()).ToArray();
			if (langLists.Length > 0) {
				Console.WriteLine();
				Console.WriteLine(dnSpy_Console_Resources.LanguageOptions);
				Console.WriteLine(dnSpy_Console_Resources.LanguageOptionsDesc);
				foreach (var langList in langLists) {
					Console.WriteLine();
					foreach (var lang in langList)
						Console.WriteLine("  {0} ({1})", lang.UniqueNameUI, lang.UniqueGuid.ToString("B"));
					foreach (var opt in langList[0].Settings.Options)
						Console.WriteLine("    {0}\t({1} = {2}) {3}", GetOptionName(opt), opt.Type.Name, opt.Value, opt.Description);
				}
			}
			Console.WriteLine();
			Console.WriteLine(dnSpy_Console_Resources.ExamplesHeader);
			foreach (var info in helpInfos) {
				Console.WriteLine("  " + progName + " " + info.CommandLine);
				Console.WriteLine("      " + info.Description);
			}
		}

		readonly struct UsageInfo {
			public string Option { get; }
			public string? OptionArgument { get; }
			public string Description { get; }
			public UsageInfo(string option, string? optionArgument, string description) {
				Option = option;
				OptionArgument = optionArgument;
				Description = description;
			}
		}
		static readonly UsageInfo[] usageInfos = new UsageInfo[] {
			new UsageInfo("--asm-path", dnSpy_Console_Resources.CmdLinePath, dnSpy_Console_Resources.CmdLineDescription_AsmPath),
			new UsageInfo("--user-gac", dnSpy_Console_Resources.CmdLinePath, dnSpy_Console_Resources.CmdLineDescription_UserGAC),
			new UsageInfo("--no-gac", null, dnSpy_Console_Resources.CmdLineDescription_NoGAC),
			new UsageInfo("--no-stdlib", null, dnSpy_Console_Resources.CmdLineDescription_NoStdLib),
			new UsageInfo("--no-sln", null, dnSpy_Console_Resources.CmdLineDescription_NoSLN),
			new UsageInfo("--sln-name", dnSpy_Console_Resources.CmdLineName, dnSpy_Console_Resources.CmdLineDescription_SlnName),
			new UsageInfo("--threads", "N", dnSpy_Console_Resources.CmdLineDescription_NumberOfThreads),
			new UsageInfo("--no-resources", null, dnSpy_Console_Resources.CmdLineDescription_NoResources),
			new UsageInfo("--no-resx", null, dnSpy_Console_Resources.CmdLineDescription_NoResX),
			new UsageInfo("--no-baml", null, dnSpy_Console_Resources.CmdLineDescription_NoBAML),
			new UsageInfo("--no-color", null, dnSpy_Console_Resources.CmdLineDescription_NoColor),
			new UsageInfo("--spaces", "N", dnSpy_Console_Resources.CmdLineDescription_Spaces),
			new UsageInfo("--vs", "N", string.Format(dnSpy_Console_Resources.CmdLineDescription_VSVersion, 2017)),
			new UsageInfo("--project-guid", "N", dnSpy_Console_Resources.CmdLineDescription_ProjectGUID),
			new UsageInfo("-t", dnSpy_Console_Resources.CmdLineName, dnSpy_Console_Resources.CmdLineDescription_Type1),
			new UsageInfo("--type", dnSpy_Console_Resources.CmdLineName, dnSpy_Console_Resources.CmdLineDescription_Type2),
			new UsageInfo("--md", "N", dnSpy_Console_Resources.CmdLineDescription_MDToken),
			new UsageInfo("--gac-file", dnSpy_Console_Resources.CmdLineAssembly, dnSpy_Console_Resources.CmdLineDescription_GACFile),
			new UsageInfo("-r", null, dnSpy_Console_Resources.CmdLineDescription_RecursiveSearch),
			new UsageInfo("-o", dnSpy_Console_Resources.CmdLineOutputDir, dnSpy_Console_Resources.CmdLineDescription_OutputDirectory),
			new UsageInfo("-l", dnSpy_Console_Resources.CmdLineLanguage, dnSpy_Console_Resources.CmdLineDescription_Language),
		};

		readonly struct HelpInfo {
			public string CommandLine { get; }
			public string Description { get; }
			public HelpInfo(string description, string commandLine) {
				CommandLine = commandLine;
				Description = description;
			}
		}
		static readonly HelpInfo[] helpInfos = new HelpInfo[] {
			new HelpInfo(dnSpy_Console_Resources.ExampleDescription1, @"-o C:\out\path C:\some\path"),
			new HelpInfo(dnSpy_Console_Resources.ExampleDescription2, @"-o C:\out\path -r C:\some\path"),
			new HelpInfo(dnSpy_Console_Resources.ExampleDescription3, @"-o C:\out\path C:\some\path\*.dll"),
			new HelpInfo(dnSpy_Console_Resources.ExampleDescription4, @"--md 0x06000123 file.dll"),
			new HelpInfo(dnSpy_Console_Resources.ExampleDescription5, @"-t system.int32 --gac-file ""mscorlib, Version=4.0.0.0"""),
		};

		string GetOptionName(IDecompilerOption opt, string? extraPrefix = null) {
			var prefix = "--" + extraPrefix;
			var o = prefix + FixInvalidSwitchChars((opt.Name is not null ? opt.Name : opt.Guid.ToString()));
			if (reservedOptions.Contains(o))
				o = prefix + FixInvalidSwitchChars(opt.Guid.ToString());
			return o;
		}

		static string FixInvalidSwitchChars(string s) => s.Replace(' ', '-');

		List<List<IDecompiler>> GetLanguageOptions() {
			var list = new List<List<IDecompiler>>();
			var dict = new Dictionary<object, List<IDecompiler>>();
			foreach (var lang in AllLanguages) {
				if (!dict.TryGetValue(lang.Settings, out var opts)) {
					dict.Add(lang.Settings, opts = new List<IDecompiler>());
					list.Add(opts);
				}
				opts.Add(lang);
			}
			return list;
		}

		void Dump(Exception? ex) {
			while (ex is not null) {
				Console.WriteLine(dnSpy_Console_Resources.Error1, ex.GetType());
				Console.WriteLine("  {0}", ex.Message);
				Console.WriteLine("  {0}", ex.StackTrace);
				ex = ex.InnerException;
			}
		}

		string GetProgramBaseName() => GetBaseName(Environment.GetCommandLineArgs()[0]);

		string GetBaseName(string name) {
			int index = name.LastIndexOf(Path.DirectorySeparatorChar);
			if (index < 0)
				return name;
			return name.Substring(index + 1);
		}

		const string BOOLEAN_NO_PREFIX = "no-";
		const string BOOLEAN_DONT_PREFIX = "dont-";
		HashSet<string> GetReservedOptions() {
			var hash = new HashSet<string>(StringComparer.Ordinal);
			foreach (var a in ourOptions) {
				hash.Add("--" + a);
				hash.Add("--" + BOOLEAN_NO_PREFIX + a);
				hash.Add("--" + BOOLEAN_DONT_PREFIX + a);
			}
			return hash;
		}
		static readonly string[] ourOptions = new string[] {
			// Don't include 'no-' and 'dont-'
			"recursive",
			"output-dir",
			"lang",
			"asm-path",
			"user-gac",
			"gac",
			"stdlib",
			"sln",
			"sln-name",
			"threads",
			"vs",
			"resources",
			"resx",
			"baml",
			"color",
			"spaces",
			"type",
			"md",
			"gac-file",
			"project-guid",
		};

		void ParseCommandLine(string[] args) {
			if (args.Length == 0)
				throw new ErrorException(dnSpy_Console_Resources.MissingOptions);

			bool canParseCommands = true;
			IDecompiler? lang = null;
			Dictionary<string, (IDecompilerOption setOption, Action<string> setOptionValue)>? langDict = null;
			for (int i = 0; i < args.Length; i++) {
				if (lang is null) {
					lang = GetLanguage();
					langDict = CreateDecompilerOptionsDictionary(lang);
				}
				var arg = args[i];
				var next = i + 1 < args.Length ? args[i + 1] : null;
				if (arg.Length == 0)
					continue;

				// **********************************************************************
				// If you add more '--' options here, also update 'string[] ourOptions'
				// **********************************************************************

				if (canParseCommands && arg[0] == '-') {
					string? error;
					switch (arg) {
					case "--":
						canParseCommands = false;
						break;

					case "-r":
					case "--recursive":
						isRecursive = true;
						break;

					case "-o":
					case "--output-dir":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingOutputDir);
						outputDir = Path.GetFullPath(next);
						i++;
						break;

					case "-l":
					case "--lang":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingLanguageName);
						language = next;
						i++;
						if (GetLanguageOrNull() is null)
							throw new ErrorException(string.Format(dnSpy_Console_Resources.LanguageDoesNotExist, language));
						lang = null;
						langDict = null;
						break;

					case "--asm-path":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingAsmSearchPath);
						asmPaths.AddRange(next.Split(new char[] { PATHS_SEP }, StringSplitOptions.RemoveEmptyEntries));
						i++;
						break;

					case "--user-gac":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingUserGacPath);
						userGacPaths.AddRange(next.Split(new char[] { PATHS_SEP }, StringSplitOptions.RemoveEmptyEntries));
						i++;
						break;

					case "--no-gac":
						useGac = false;
						break;

					case "--no-stdlib":
						addCorlibRef = false;
						break;

					case "--no-sln":
						createSlnFile = false;
						break;

					case "--sln-name":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingSolutionName);
						slnName = next;
						i++;
						if (Path.IsPathRooted(slnName))
							throw new ErrorException(string.Format(dnSpy_Console_Resources.InvalidSolutionName, slnName));
						break;

					case "--threads":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingNumberOfThreads);
						i++;
						numThreads = SimpleTypeConverter.ParseInt32(next, int.MinValue, int.MaxValue, out error);
						if (!string2.IsNullOrEmpty(error))
							throw new ErrorException(error);
						break;

					case "--vs":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingVSVersion);
						i++;
						int vsVer;
						vsVer = SimpleTypeConverter.ParseInt32(next, int.MinValue, int.MaxValue, out error);
						if (!string2.IsNullOrEmpty(error))
							throw new ErrorException(error);
						switch (vsVer) {
						case 2005: projectVersion = ProjectVersion.VS2005; break;
						case 2008: projectVersion = ProjectVersion.VS2008; break;
						case 2010: projectVersion = ProjectVersion.VS2010; break;
						case 2012: projectVersion = ProjectVersion.VS2012; break;
						case 2013: projectVersion = ProjectVersion.VS2013; break;
						case 2015: projectVersion = ProjectVersion.VS2015; break;
						case 2017: projectVersion = ProjectVersion.VS2017; break;
						case 2019: projectVersion = ProjectVersion.VS2019; break;
						case 2022: projectVersion = ProjectVersion.VS2022; break;
						default: throw new ErrorException(string.Format(dnSpy_Console_Resources.InvalidVSVersion, vsVer));
						}
						break;

					case "--no-resources":
						unpackResources = false;
						break;

					case "--no-resx":
						createResX = false;
						break;

					case "--no-baml":
						decompileBaml = false;
						break;

					case "--no-color":
						colorizeOutput = false;
						break;

					case "--spaces":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingArgument);
						const int MIN_SPACES = 0, MAX_SPACES = 100;
						if (!int.TryParse(next, out spaces) || spaces < MIN_SPACES || spaces > MAX_SPACES)
							throw new ErrorException(string.Format(dnSpy_Console_Resources.InvalidSpacesArgument, MIN_SPACES, MAX_SPACES));
						i++;
						break;

					case "-t":
					case "--type":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingTypeName);
						i++;
						typeName = next;
						break;

					case "--md":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingMDToken);
						i++;
						mdToken = SimpleTypeConverter.ParseInt32(next, int.MinValue, int.MaxValue, out error);
						if (!string2.IsNullOrEmpty(error))
							throw new ErrorException(error);
						break;

					case "--gac-file":
						if (next is null)
							throw new ErrorException(dnSpy_Console_Resources.MissingGacFile);
						i++;
						gacFiles.Add(next);
						break;

					case "--project-guid":
						if (next is null || !Guid.TryParse(next, out projectGuid))
							throw new ErrorException(dnSpy_Console_Resources.InvalidGuid);
						i++;
						break;

					default:
						(IDecompilerOption option, Action<string> setOptionValue) tuple;
						Debug2.Assert(langDict is not null);
						if (langDict.TryGetValue(arg, out tuple)) {
							bool hasArg = tuple.option.Type != typeof(bool);
							if (hasArg && next is null)
								throw new ErrorException(dnSpy_Console_Resources.MissingOptionArgument);
							if (hasArg)
								i++;
							tuple.setOptionValue(next ?? string.Empty);
							break;
						}

						throw new ErrorException(string.Format(dnSpy_Console_Resources.InvalidOption, arg));
					}
				}
				else
					files.Add(arg);
			}
		}

		static int ParseInt32(string s) {
			var v = SimpleTypeConverter.ParseInt32(s, int.MinValue, int.MaxValue, out var error);
			if (!string2.IsNullOrEmpty(error))
				throw new ErrorException(error);
			return v;
		}

		static string ParseString(string s) => s;

		Dictionary<string, (IDecompilerOption option, Action<string> setOptionValue)> CreateDecompilerOptionsDictionary(IDecompiler decompiler) {
			var dict = new Dictionary<string, (IDecompilerOption, Action<string>)>();

			if (decompiler is null)
				return dict;

			foreach (var tmp in decompiler.Settings.Options) {
				var opt = tmp;
				if (opt.Type == typeof(bool)) {
					dict[GetOptionName(opt)] = (opt, new Action<string?>(a => opt.Value = true));
					dict[GetOptionName(opt, BOOLEAN_NO_PREFIX)] = (opt, new Action<string?>(a => opt.Value = false));
					dict[GetOptionName(opt, BOOLEAN_DONT_PREFIX)] = (opt, new Action<string?>(a => opt.Value = false));
				}
				else if (opt.Type == typeof(int))
					dict[GetOptionName(opt)] = (opt, new Action<string>(a => opt.Value = ParseInt32(a)));
				else if (opt.Type == typeof(string))
					dict[GetOptionName(opt)] = (opt, new Action<string>(a => opt.Value = ParseString(a)));
				else
					Debug.Fail($"Unsupported type: {opt.Type}");
			}

			return dict;
		}

		void AddSearchPath(string dir) {
			if (Directory.Exists(dir) && !addedPaths.Contains(dir)) {
				addedPaths.Add(dir);
				assemblyResolver.PreSearchPaths.Add(dir);
			}
		}
		readonly HashSet<string> addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		void Decompile() {
			foreach (var dir in asmPaths)
				AddSearchPath(dir);
			foreach (var dir in userGacPaths)
				AddSearchPath(dir);
			assemblyResolver.UseGAC = useGac;

			var files = new List<ProjectModuleOptions>(GetDotNetFiles());
			string guidStr = projectGuid.ToString();
			int guidNum = int.Parse(guidStr.Substring(36 - 8, 8), NumberStyles.HexNumber);
			string guidFormat = guidStr.Substring(0, 36 - 8) + "{0:X8}";
			foreach (var file in files.OrderBy(a => a.Module.Location, StringComparer.InvariantCultureIgnoreCase))
				file.ProjectGuid = new Guid(string.Format(guidFormat, guidNum++));

			if (mdToken != 0 || typeName is not null) {
				if (files.Count == 0)
					throw new ErrorException(dnSpy_Console_Resources.MissingDotNetFilename);
				if (files.Count != 1)
					throw new ErrorException(dnSpy_Console_Resources.OnlyOneFileCanBeDecompiled);

				IMemberDef? member;
				if (typeName is not null)
					member = FindType(files[0].Module, typeName);
				else
					member = files[0].Module.ResolveToken(mdToken) as IMemberDef;
				if (member is null) {
					if (typeName is not null)
						throw new ErrorException(string.Format(dnSpy_Console_Resources.CouldNotFindTypeX, typeName));
					throw new ErrorException(dnSpy_Console_Resources.InvalidToken);
				}

				var writer = Console.Out;
				IDecompilerOutput output;
				if (colorizeOutput)
					output = new ConsoleColorizerOutput(writer, CreateColorProvider(), GetIndenter());
				else
					output = new TextWriterDecompilerOutput(writer, GetIndenter());

				var lang = GetLanguage();
				if (member is MethodDef)
					lang.Decompile((MethodDef)member, output, decompilationContext);
				else if (member is FieldDef)
					lang.Decompile((FieldDef)member, output, decompilationContext);
				else if (member is PropertyDef)
					lang.Decompile((PropertyDef)member, output, decompilationContext);
				else if (member is EventDef)
					lang.Decompile((EventDef)member, output, decompilationContext);
				else if (member is TypeDef)
					lang.Decompile((TypeDef)member, output, decompilationContext);
				else
					throw new ErrorException(dnSpy_Console_Resources.InvalidMemberToDecompile);
			}
			else {
				if (string2.IsNullOrEmpty(outputDir))
					throw new ErrorException(dnSpy_Console_Resources.MissingOutputDir);
				if (GetLanguage().ProjectFileExtension is null)
					throw new ErrorException(string.Format(dnSpy_Console_Resources.LanguageXDoesNotSupportProjects, GetLanguage().UniqueNameUI));

				decompilationContext.AsyncMethodBodyDecompilation = false;
				var options = new ProjectCreatorOptions(outputDir, decompilationContext.CancellationToken);
				options.Logger = this;
				options.ProjectVersion = projectVersion;
				options.NumberOfThreads = numThreads;
				options.ProjectModules.AddRange(files);
				options.UserGACPaths.AddRange(userGacPaths);
				options.CreateDecompilerOutput = textWriter => new TextWriterDecompilerOutput(textWriter, GetIndenter());
				if (createSlnFile && !string.IsNullOrEmpty(slnName))
					options.SolutionFilename = slnName;
				var creator = new MSBuildProjectCreator(options);
				creator.Create();
			}
		}

		Indenter GetIndenter() {
			if (spaces <= 0)
				return new Indenter(4, 4, true);
			return new Indenter(spaces, spaces, false);
		}

		static TypeDef? FindType(ModuleDef module, string name) =>
			FindTypeFullName(module, name, StringComparer.Ordinal) ??
			FindTypeFullName(module, name, StringComparer.OrdinalIgnoreCase) ??
			FindTypeName(module, name, StringComparer.Ordinal) ??
			FindTypeName(module, name, StringComparer.OrdinalIgnoreCase);

		static TypeDef? FindTypeFullName(ModuleDef module, string name, StringComparer comparer) {
			var sb = new StringBuilder();
			return module.GetTypes().FirstOrDefault(a => {
				sb.Clear();
				string s1, s2;
				if (comparer.Equals(s1 = FullNameFactory.FullName(a, false, null, sb), name))
					return true;
				sb.Clear();
				if (comparer.Equals(s2 = FullNameFactory.FullName(a, true, null, sb), name))
					return true;
				sb.Clear();
				if (comparer.Equals(CleanTypeName(s1), name))
					return true;
				sb.Clear();
				return comparer.Equals(CleanTypeName(s2), name);
			});
		}

		static TypeDef? FindTypeName(ModuleDef module, string name, StringComparer comparer) {
			var sb = new StringBuilder();
			return module.GetTypes().FirstOrDefault(a => {
				sb.Clear();
				string s1, s2;
				if (comparer.Equals(s1 = FullNameFactory.Name(a, false, sb), name))
					return true;
				sb.Clear();
				if (comparer.Equals(s2 = FullNameFactory.Name(a, true, sb), name))
					return true;
				sb.Clear();
				if (comparer.Equals(CleanTypeName(s1), name))
					return true;
				sb.Clear();
				return comparer.Equals(CleanTypeName(s2), name);
			});
		}

		static string CleanTypeName(string s) {
			int i = s.LastIndexOf('`');
			if (i < 0)
				return s;
			return s.Substring(0, i);
		}

		IEnumerable<ProjectModuleOptions> GetDotNetFiles() {
			foreach (var file in files) {
				if (File.Exists(file)) {
					var info = OpenNetFile(file);
					if (info is null)
						throw new Exception(string.Format(dnSpy_Console_Resources.NotDotNetFile, file));
					yield return info;
				}
				else if (Directory.Exists(file)) {
					foreach (var info in DumpDir(file, null))
						yield return info;
				}
				else {
					var path = Path.GetDirectoryName(file);
					var name = Path.GetFileName(file);
					if (Directory.Exists(path)) {
						Debug2.Assert(path is not null);
						foreach (var info in DumpDir(path, name))
							yield return info;
					}
					else
						throw new ErrorException(string.Format(dnSpy_Console_Resources.FileOrDirDoesNotExist, file));
				}
			}

			// Don't use exact matching here so the user can tell us to load eg. "mscorlib, Version=4.0.0.0" which
			// is easier to type than the full assembly name
			var oldFindExactMatch = assemblyResolver.FindExactMatch;
			assemblyResolver.FindExactMatch = false;
			foreach (var asmName in gacFiles) {
				var asm = assemblyResolver.Resolve(new AssemblyNameInfo(asmName), null);
				if (asm is null)
					throw new ErrorException(string.Format(dnSpy_Console_Resources.CouldNotResolveGacFileX, asmName));
				yield return CreateProjectModuleOptions(asm.ManifestModule);
			}
			assemblyResolver.FindExactMatch = oldFindExactMatch;
		}

		IEnumerable<ProjectModuleOptions> DumpDir(string path, string? pattern) {
			pattern ??= "*";
			Stack<string> stack = new Stack<string>();
			stack.Push(path);
			while (stack.Count > 0) {
				path = stack.Pop();
				foreach (var info in DumpDir2(path, pattern))
					yield return info;
				if (isRecursive) {
					foreach (var di in GetDirs(path))
						stack.Push(di.FullName);
				}
			}
		}

		IEnumerable<DirectoryInfo> GetDirs(string path) {
			IEnumerable<FileSystemInfo>? fsysIter = null;
			try {
				fsysIter = new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			if (fsysIter is null)
				yield break;

			foreach (var info in fsysIter) {
				if ((info.Attributes & System.IO.FileAttributes.Directory) == 0)
					continue;
				DirectoryInfo? di = null;
				try {
					di = new DirectoryInfo(info.FullName);
				}
				catch (IOException) {
				}
				catch (UnauthorizedAccessException) {
				}
				catch (SecurityException) {
				}
				if (di is not null)
					yield return di;
			}
		}

		IEnumerable<ProjectModuleOptions> DumpDir2(string path, string pattern) {
			pattern ??= "*";
			foreach (var fi in GetFiles(path, pattern)) {
				var info = OpenNetFile(fi.FullName);
				if (info is not null)
					yield return info;
			}
		}

		IEnumerable<FileInfo> GetFiles(string path, string pattern) {
			IEnumerable<FileSystemInfo>? fsysIter = null;
			try {
				fsysIter = new DirectoryInfo(path).EnumerateFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			if (fsysIter is null)
				yield break;

			foreach (var info in fsysIter) {
				if ((info.Attributes & System.IO.FileAttributes.Directory) != 0)
					continue;
				FileInfo? fi = null;
				try {
					fi = new FileInfo(info.FullName);
				}
				catch (IOException) {
				}
				catch (UnauthorizedAccessException) {
				}
				catch (SecurityException) {
				}
				if (fi is not null)
					yield return fi;
			}
		}

		ProjectModuleOptions? OpenNetFile(string file) {
			try {
				file = Path.GetFullPath(file);
				if (!File.Exists(file))
					return null;
				return CreateProjectModuleOptions(ModuleDefMD.Load(file, moduleContext));
			}
			catch {
			}
			return null;
		}

		ProjectModuleOptions CreateProjectModuleOptions(ModuleDef mod) {
			mod.EnableTypeDefFindCache = true;
			((AssemblyResolver)moduleContext.AssemblyResolver).AddToCache(mod);
			AddSearchPath(Path.GetDirectoryName(mod.Location)!);
			var proj = new ProjectModuleOptions(mod, GetLanguage(), decompilationContext);
			proj.DontReferenceStdLib = !addCorlibRef;
			proj.UnpackResources = unpackResources;
			proj.CreateResX = createResX;
			proj.DecompileXaml = decompileBaml && bamlDecompiler is not null;
			var o = BamlDecompilerOptions.Create(GetLanguage());
			var outputOptions = new XamlOutputOptions {
				IndentChars = "\t",
				NewLineChars = Environment.NewLine,
				NewLineOnAttributes = true,
			};
			if (bamlDecompiler is not null)
				proj.DecompileBaml = (a, b, c, d) => bamlDecompiler.Decompile(a, b, c, o, d, outputOptions);
			return proj;
		}

		IDecompiler GetLanguage() => GetLanguageOrNull() ?? throw new InvalidOperationException();
		IDecompiler? GetLanguageOrNull() {
			bool hasGuid = Guid.TryParse(language, out var guid);
			return AllLanguages.FirstOrDefault(a => {
				if (StringComparer.OrdinalIgnoreCase.Equals(language, a.UniqueNameUI))
					return true;
				if (hasGuid && (guid.Equals(a.UniqueGuid) || guid.Equals(a.GenericGuid)))
					return true;
				return false;
			});
		}

		IDecompiler[] AllLanguages => allLanguages;
		readonly IDecompiler[] allLanguages;

		public void Error(string message) {
			errors++;
			Console.Error.WriteLine(string.Format(dnSpy_Console_Resources.Error1, message));
		}
		int errors;

		ColorProvider CreateColorProvider() {
			var provider = new ColorProvider();
			provider.Add(TextColor.Operator, null, null);
			provider.Add(TextColor.Punctuation, null, null);
			provider.Add(TextColor.Number, null, null);
			provider.Add(TextColor.Comment, ConsoleColor.Green, null);
			provider.Add(TextColor.Keyword, ConsoleColor.Cyan, null);
			provider.Add(TextColor.String, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.VerbatimString, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.Char, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.Namespace, ConsoleColor.Yellow, null);
			provider.Add(TextColor.Type, ConsoleColor.Magenta, null);
			provider.Add(TextColor.SealedType, ConsoleColor.Magenta, null);
			provider.Add(TextColor.StaticType, ConsoleColor.Magenta, null);
			provider.Add(TextColor.Delegate, ConsoleColor.Magenta, null);
			provider.Add(TextColor.Enum, ConsoleColor.Magenta, null);
			provider.Add(TextColor.Interface, ConsoleColor.Magenta, null);
			provider.Add(TextColor.ValueType, ConsoleColor.Green, null);
			provider.Add(TextColor.Module, ConsoleColor.DarkMagenta, null);
			provider.Add(TextColor.TypeGenericParameter, ConsoleColor.Magenta, null);
			provider.Add(TextColor.MethodGenericParameter, ConsoleColor.Magenta, null);
			provider.Add(TextColor.InstanceMethod, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.StaticMethod, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.ExtensionMethod, ConsoleColor.DarkYellow, null);
			provider.Add(TextColor.InstanceField, ConsoleColor.Magenta, null);
			provider.Add(TextColor.EnumField, ConsoleColor.Magenta, null);
			provider.Add(TextColor.LiteralField, ConsoleColor.Magenta, null);
			provider.Add(TextColor.StaticField, ConsoleColor.Magenta, null);
			provider.Add(TextColor.InstanceEvent, ConsoleColor.Magenta, null);
			provider.Add(TextColor.StaticEvent, ConsoleColor.Magenta, null);
			provider.Add(TextColor.InstanceProperty, ConsoleColor.Magenta, null);
			provider.Add(TextColor.StaticProperty, ConsoleColor.Magenta, null);
			provider.Add(TextColor.Local, ConsoleColor.White, null);
			provider.Add(TextColor.Parameter, ConsoleColor.White, null);
			provider.Add(TextColor.PreprocessorKeyword, ConsoleColor.Blue, null);
			provider.Add(TextColor.PreprocessorText, null, null);
			provider.Add(TextColor.Label, ConsoleColor.DarkRed, null);
			provider.Add(TextColor.OpCode, ConsoleColor.Cyan, null);
			provider.Add(TextColor.ILDirective, ConsoleColor.Cyan, null);
			provider.Add(TextColor.ILModule, ConsoleColor.DarkMagenta, null);
			provider.Add(TextColor.ExcludedCode, null, null);
			provider.Add(TextColor.XmlDocCommentAttributeName, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentAttributeQuotes, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentAttributeValue, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentCDataSection, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentComment, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentDelimiter, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentEntityReference, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentName, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentProcessingInstruction, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.XmlDocCommentText, ConsoleColor.DarkGreen, null);
			provider.Add(TextColor.Error, ConsoleColor.Red, null);
			return provider;
		}
	}
}
