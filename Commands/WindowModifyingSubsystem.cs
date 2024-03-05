using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperTabGroups.Commands
{
    internal class WindowModifyingSubsystem
    {
        public static WindowStateChecker LocalWindowsStateChecker;

        public static void CheckWindowsVisibility(IEnumerable<WindowFrame> WindowsToCheck)
        {
            foreach (var ActiveWindow in WindowsToCheck)
            {
                LocalWindowsStateChecker.CheckWindowState(ActiveWindow.Editor);
            }
        }

        public static void CheckWindowsVisibility(Guid GuidToCheck)
        {
            LocalWindowsStateChecker.CheckWindowState(GuidToCheck);
        }

        public static void CheckWindowsVisibility(IEnumerable<Guid> GuidsToCheck)
        {
            foreach (var Guid in GuidsToCheck)
            {
                LocalWindowsStateChecker.CheckWindowState(Guid);
            }
        }
    }
}
