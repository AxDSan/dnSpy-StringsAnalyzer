using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace Plugin.StringAnalyzer
{
    [Export(typeof(IToolWindowContentProvider))]
    sealed class MainToolWindowContentProvider : IToolWindowContentProvider {
        // Caches the created tool window
        public ToolWindowContent ToolWindowContent => _myToolWindowContent ?? (_myToolWindowContent = new ToolWindowContent());

        private ToolWindowContent _myToolWindowContent;

        // Add any deps to the constructor if needed, else remove the constructor
        [ImportingConstructor]
        private MainToolWindowContentProvider(DeppDep deppDep) {
            deppDep.Hello();
        }

        // Lets dnSpy know which tool windows it can create and their default locations
        public IEnumerable<ToolWindowContentInfo> ContentInfos {
            get { yield return new ToolWindowContentInfo(ToolWindowContent.TheGuid, ToolWindowContent.DefaultLocation, 0, false); }
        }

        // Called by dnSpy. If it's your tool window guid, return the instance. Make sure it's
        // cached since it can be called multiple times.
        public IToolWindowContent GetOrCreate(Guid guid)
        {
            return guid == ToolWindowContent.TheGuid ? ToolWindowContent : null;
        }
    }
}