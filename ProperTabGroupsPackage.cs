global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProperTabGroups
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.ProperTabGroupsString)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class ProperTabGroupsPackage : ToolkitPackage
    {
        private DTE dte;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken); 

            // Get the DTE service asynchronously
            dte = await GetServiceAsync(typeof(DTE)) as DTE;
            if (dte == null) return;

            // Listen for the solution opened event
            dte.Events.SolutionEvents.Opened += SolutionOpened;
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Loop through all open document windows
            foreach (Window window in dte.Windows)
            {
                if (window.Kind == "Document")
                {
                    // Perform your logic here, e.g., print window name
                    Document doc = window.Document;
                    System.Diagnostics.Debug.WriteLine($"Open Document: {doc.Name}");
                }
            }
        }
    }
}
