global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using ProperTabGroups.Subsystem;
using ProperTabGroups.Subsystems;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;

namespace ProperTabGroups
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(MyToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ProperTabGroupsString)]
    public sealed class ProperTabGroupsPackage : ToolkitPackage
    {

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken, progress);

            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();
            
            // its a bit scuffed but this order initialises the SaveLoadManager first and then the document well stuff accesses it after
            var init2 = new SolutionEventsListener();
            var init3 = new SaveLoadManager();
            var init = DocumentWellManagementSubsystem.Instance;
        }
    }
}