global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using ProperTabGroups.Commands;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using static ProperTabGroups.Commands.IVsWindowFrameUtilities;

namespace ProperTabGroups
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ProperTabGroupsString)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class ProperTabGroupsPackage : /*ToolkitPackage*/ AsyncPackage
    {
        IVsRunningDocumentTable rdt;
        private uint _rdtEventsCookie;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Switch to main thread asynchronously
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);


            // Get the running document table and set up the RunningDocTableEventHandler
            rdt = (IVsRunningDocumentTable)await GetServiceAsync(typeof(SVsRunningDocumentTable));
            RunningDocTableEventsHandler myRdtEventHandler = new RunningDocTableEventsHandler(this);
            rdt.AdviseRunningDocTableEvents(myRdtEventHandler, out _rdtEventsCookie);
             
            // Inital Check for open windows
            IEnumerable<WindowFrame> WindowsToCheck = await VS.Windows.GetAllDocumentWindowsAsync();

            WindowModifyingSubsystem.LocalWindowsStateChecker = new WindowStateChecker(this);
            WindowModifyingSubsystem.CheckWindowsVisibility(WindowsToCheck);

        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            rdt.UnadviseRunningDocTableEvents(_rdtEventsCookie);
            base.Dispose(disposing);
        }
    }
}