using Community.VisualStudio.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperTabGroups.Commands
{
    internal class WindowModifyingSubsystem
    {
        public static void CheckWindowsVisibility(IEnumerable<WindowFrame> WindowsToCheck)
        {
            foreach (var ActiveWindow in WindowsToCheck)
            {
            }
        }

        public static void CheckWindowsVisibility(Guid GuidToCheck)
        {
        }

        public static void CheckWindowsVisibility(IEnumerable<Guid> GuidsToCheck)
        {
            foreach (var Guid in GuidsToCheck)
            {
            }
        }
    }
}
