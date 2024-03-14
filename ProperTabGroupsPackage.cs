global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using EnvDTE;
using System.Runtime.InteropServices;
using System.Threading;
using ProperTabGroups.Scripts;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows.Controls;

namespace ProperTabGroups
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(MyToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ProperTabGroupsString)]
    public sealed class ProperTabGroupsPackage : ToolkitPackage
    {
        private DTE dte;

        static public List<Window> AllOpenDocumentWindows { get; set; }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialise Variables

            // Get the DTE service asynchronously
            dte = await GetServiceAsync(typeof(DTE)) as DTE;
            if (dte == null) return;

            AllOpenDocumentWindows = new List<Window>();    

            // Listen for the solution opened event
            dte.Events.SolutionEvents.Opened += SolutionOpened;
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Loop through all open document windows
            foreach (Window window in dte.Windows)
            {
                if (window.Kind.Equals("Document"))
                {
                    AllOpenDocumentWindows.Add(window);
                }
            }
        }
    }
}


//global using Community.VisualStudio.Toolkit;
//global using Microsoft.VisualStudio.Shell;
//global using System;
//global using Task = System.Threading.Tasks.Task;
//using System.Collections.Generic;
//using EnvDTE;
//using Microsoft.VisualStudio.Shell.Interop;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Windows.Documents;
//using ProperTabGroups.Scripts;

//namespace ProperTabGroups
//{

//    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
//    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
//    //[ProvideToolWindow(typeof(ProperTabsMainWindow.Pane), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
//    ////[ProvideMenuResource("Menus.ctmenu", 1)]
//    //[Guid(PackageGuids.ProperTabGroupsString)]
//    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
//    public sealed class ProperTabGroupsPackage : ToolkitPackage
//    {
//        private DTE dte;
//        private List<TabInfo> AllOpenWindows = [];

//        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
//        {
//            await base.InitializeAsync(cancellationToken, progress);

//            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

//            // Get the DTE service asynchronously
//            dte = await GetServiceAsync(typeof(DTE)) as DTE;
//            if (dte == null) return;

//            // Listen for the solution opened event
//            dte.Events.SolutionEvents.Opened += SolutionOpened;
//        }

//        private void SolutionOpened()
//        {
//            ThreadHelper.ThrowIfNotOnUIThread();

//            // Loop through all open document windows
//            foreach (Window window in dte.Windows)
//            {
//                if (window.Kind.Equals("Document"))
//                {
//                    var newTab = new TabInfo(window, []);

//                    AllOpenWindows.Add(newTab);
//                }
//            }
//        }
//    }
//}
