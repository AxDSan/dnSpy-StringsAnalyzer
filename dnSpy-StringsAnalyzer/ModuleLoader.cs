//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using dnSpy.Contracts.App;
//using dnSpy.Contracts.Controls;
//using dnSpy.Contracts.Decompiler;
//using dnSpy.Contracts.Documents.Tabs;
//using dnSpy.Contracts.Extension;
//using dnSpy.Contracts.Images;
//using dnSpy.Contracts.Menus;
//using dnSpy.Contracts.Metadata;
//using dnSpy.Contracts.MVVM;
//using dnSpy.Contracts.Text;
//using dnSpy.Contracts.ToolWindows.App;
//using dnSpy.Contracts.Documents;
//using dndbg.Engine;
//using System.Diagnostics;
//using dnlib.DotNet;

//namespace dnSpy.StringsAnalyzer
//{
//    interface IModuleLoader
//    {
//        IDsDocument LoadModule(CorModule module, bool canLoadDynFile, bool isAutoLoaded);
//        IDsDocument LoadModule(DnModule module, bool canLoadDynFile, bool isAutoLoaded);
//        IDsDocument LoadModule(ModuleId moduleId, bool canLoadDynFile, bool diskFileOk, bool isAutoLoaded);
//        DnModule GetDnModule(CorModule module);
//    }

//    [Export(typeof(IModuleLoader))]
//    sealed class ModuleLoader : IModuleLoader
//    {
//        readonly IDsDocumentService documentService;
//        readonly Lazy<IInMemoryModuleService> inMemoryModuleService;
//        readonly IModuleIdProvider moduleIdProvider;

//        [ImportingConstructor]
//        ModuleLoader(Lazy<ITheDebugger> theDebugger, IDebuggerSettings debuggerSettings, IDsDocumentService documentService, Lazy<IInMemoryModuleService> inMemoryModuleService, IModuleIdProvider moduleIdProvider)
//        {
//            //this.theDebugger = theDebugger;
//            //this.debuggerSettings = debuggerSettings;
//            this.documentService = documentService;
//            this.inMemoryModuleService = inMemoryModuleService;
//            this.moduleIdProvider = moduleIdProvider;
//        }

//        //public DnModule GetDnModule(CorModule module)
//        //{
//        //    var dbg = theDebugger.Value.Debugger;
//        //    if (dbg == null)
//        //        return null;
//        //    foreach (var m in dbg.Modules)
//        //    {
//        //        if (m.CorModule == module)
//        //            return m;
//        //    }
//        //    return null;
//        //}

//        //DnModule GetDnModule(ModuleId moduleId)
//        //{
//        //    var dbg = theDebugger.Value.Debugger;
//        //    if (dbg == null)
//        //        return null;
//        //    //TODO: This method should have an AppDomain parameter.
//        //    foreach (var m in dbg.Modules)
//        //    {
//        //        if (m.DnModuleId.ToModuleId().Equals(moduleId))
//        //            return m;
//        //    }
//        //    return null;
//        //}

//        //public IDsDocument LoadModule(CorModule module, bool canLoadDynFile, bool isAutoLoaded)
//        //{
//        //    if (module == null)
//        //        return null;

//        //    var dnModule = GetDnModule(module);
//        //    Debug.Assert(dnModule != null);
//        //    if (dnModule != null)
//        //        return LoadModule(dnModule, canLoadDynFile, isAutoLoaded);

//        //    return LoadModule(module.DnModuleId.ToModuleId(), canLoadDynFile, diskFileOk: false, isAutoLoaded: isAutoLoaded);
//        //}

//        public IDsDocument LoadModule(DnModule module, bool canLoadDynFile, bool isAutoLoaded)
//        {
//            if (module == null)
//                return null;
//            if (UseMemoryModules || module.IsDynamic || module.IsInMemory)
//                return inMemoryModuleService.Value.LoadDocument(module, canLoadDynFile);
//            var file = inMemoryModuleService.Value.FindDocument(module);
//            if (file != null)
//                return file;
//            var moduleId = module.DnModuleId;
//            return LoadModule(moduleId.ToModuleId(), canLoadDynFile, diskFileOk: false, isAutoLoaded: isAutoLoaded);
//        }

//        IEnumerable<IDsDocument> AllDocuments => inMemoryModuleService.Value.AllDocuments;

//        IEnumerable<IDsDocument> AllActiveDocuments
//        {
//            get
//            {
//                foreach (var file in AllDocuments)
//                {
//                    var cmdf = file as CorModuleDefFile;
//                    if (cmdf != null)
//                    {
//                        if (cmdf.DnModule.Process.HasExited || cmdf.DnModule.Debugger.ProcessState == DebuggerProcessState.Terminated)
//                            continue;
//                        yield return cmdf;
//                        continue;
//                    }

//                    var mmdf = file as MemoryModuleDefFile;
//                    if (mmdf != null)
//                    {
//                        if (mmdf.Process.HasExited || mmdf.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
//                            continue;
//                        yield return mmdf;
//                        continue;
//                    }

//                    yield return file;
//                }
//            }
//        }

//        IDsDocument LoadNonDiskFile(ModuleId moduleId, bool canLoadDynFile)
//        {
//            if (UseMemoryModules || moduleId.IsDynamic || moduleId.IsInMemory)
//            {
//                var dnModule = GetDnModule(moduleId);
//                if (dnModule != null)
//                    return inMemoryModuleService.Value.LoadDocument(dnModule, canLoadDynFile);
//            }

//            return null;
//        }

//        IDsDocument LoadExisting(ModuleId moduleId)
//        {
//            foreach (var file in AllActiveDocuments)
//            {
//                var otherId = moduleIdProvider.Create(file.ModuleDef);
//                if (otherId.Equals(moduleId))
//                    return file;
//            }

//            foreach (var file in AllDocuments)
//            {
//                var moduleIdFile = moduleIdProvider.Create(file.ModuleDef);
//                if (moduleIdFile.Equals(moduleId))
//                    return file;
//            }

//            return null;
//        }

//        public IDsDocument LoadModule(ModuleId moduleId, bool canLoadDynFile, bool diskFileOk, bool isAutoLoaded)
//        {
//            IDsDocument document;
//            if (diskFileOk)
//            {
//                document = LoadExisting(moduleId) ?? LoadNonDiskFile(moduleId, canLoadDynFile);
//                if (document != null)
//                    return document;
//            }
//            else {
//                document = LoadNonDiskFile(moduleId, canLoadDynFile) ?? LoadExisting(moduleId);
//                if (document != null)
//                    return document;
//            }

//            if (moduleId.IsDynamic || moduleId.IsInMemory)
//                return null;

//            string moduleFilename = moduleId.ModuleName;
//            if (!File.Exists(moduleFilename))
//                return null;
//            string asmFilename = GetAssemblyFilename(moduleFilename, moduleId.AssemblyFullName, moduleId.ModuleNameOnly);

//            if (!string.IsNullOrEmpty(asmFilename))
//            {
//                document = documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(asmFilename), isAutoLoaded);
//                if (document == null)
//                    document = documentService.Resolve(new AssemblyNameInfo(moduleId.AssemblyFullName), null);
//                if (document != null)
//                {
//                    // Common case is a one-file assembly or first module of a multifile assembly
//                    if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
//                        return document;

//                    foreach (var child in document.Children)
//                    {
//                        if (child.Filename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
//                            return child;
//                    }
//                }
//            }

//            return documentService.TryGetOrCreate(DsDocumentInfo.CreateDocument(moduleFilename), isAutoLoaded);
//        }

//        static string GetAssemblyFilename(string moduleFilename, string assemblyFullName, bool moduleNameOnly)
//        {
//            if (string.IsNullOrEmpty(moduleFilename) || (string.IsNullOrEmpty(assemblyFullName) && !moduleNameOnly))
//                return null;
//            if (moduleNameOnly)
//                return moduleFilename;
//            try
//            {
//                var asm = new AssemblyNameInfo(assemblyFullName);
//                if (Path.GetFileName(asm.Name) != asm.Name || asm.Name.Length == 0)
//                    return null;
//                var fn = Path.Combine(Path.GetDirectoryName(moduleFilename), asm.Name);
//                var fnDll = fn + ".dll";
//                if (File.Exists(fnDll))
//                    return fnDll;
//                var fnExe = fn + ".exe";
//                if (File.Exists(fnExe))
//                    return fnExe;
//            }
//            catch
//            {
//            }
//            return null;
//        }
//    }
//}
