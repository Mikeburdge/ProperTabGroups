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
using ProperTabGroups.ToolWindows;

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
            await base.InitializeAsync(cancellationToken, progress);

            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();

            TabGroupsSubsystem tabsSubsystem = TabGroupsSubsystem.Instance;
        }
    }
}