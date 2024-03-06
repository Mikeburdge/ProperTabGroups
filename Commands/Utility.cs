using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace ProperTabGroups.Commands
{
    internal class Utility
    {

        public static class ProperTabGroupHelpers
        {
            public static void WriteToOutputWindow(string message)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Get the SVsOutputWindow service which provides access to the Output window
                IVsOutputWindow outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

                // Define a unique GUID for your Output window pane
                Guid paneGuid = new Guid("YOUR-UNIQUE-GUID-HERE");
                string paneTitle = "My Custom Pane";

                // Check if the pane already exists, if not, create it
                IVsOutputWindowPane pane;
                if (ErrorHandler.Failed(outputWindow.GetPane(ref paneGuid, out pane)) || pane == null)
                {
                    // Create a new pane
                    ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(ref paneGuid, paneTitle, 1, 0));
                    ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref paneGuid, out pane));
                }

                // Activate the pane and write the message
                ErrorHandler.ThrowOnFailure(pane.Activate());
                ErrorHandler.ThrowOnFailure(pane.OutputString(message + Environment.NewLine));
            }

        }

    }
}
