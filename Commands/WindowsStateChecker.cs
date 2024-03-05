using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProperTabGroups.Commands;
using System;

public class WindowStateChecker
{
    private readonly IVsUIShell _uiShell;

    public WindowStateChecker(IServiceProvider serviceProvider)
    {
        _uiShell = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
    }

    public void CheckWindowState(Guid toolWindowGuid)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        IVsWindowFrame windowFrame;
        _uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out windowFrame);

        if (windowFrame != null)
        {
            // Check if the window is visible
            int visible = windowFrame.IsVisible();
            bool isVisible = visible > 0;


            // Assuming you want to check for more specific states, like docked or floating,
            // you'll need to dive into the window frame properties and possibly cast
            // to other interfaces for detailed state.

            Utility.ProperTabGroupHelpers.WriteToOutputWindow($"Window visibility: {isVisible}");

            // Additional properties and methods can be used here to check other states
        }
    }
}
