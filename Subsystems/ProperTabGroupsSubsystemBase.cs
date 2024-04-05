using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ProperTabGroups.Subsystem;
using EnvDTE;

namespace ProperTabGroups.Subsystems
{
    public class ProperTabGroupsSubsystemBase
    {

        protected static ProperTabGroupsSubsystemBase _instance;

        private SaveLoadManager _saveLoadManager;

        protected DTE _dte;

        public static ProperTabGroupsSubsystemBase Instance => _instance ??= new ProperTabGroupsSubsystemBase();
        protected ProperTabGroupsSubsystemBase()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = (DTE)Package.GetGlobalService(typeof(DTE));

            Initialise();
        }

        // Example of a common method
        protected virtual void Initialise()
        {
            // Initialization logic here
        }
    }

    // Note: Adapt and extend this base class with specific properties, methods, and logic common across your subsystems.
}
