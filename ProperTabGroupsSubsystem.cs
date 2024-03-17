using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ProperTabGroups.Scripts;

namespace ProperTabGroups.Subsystem
{
    public class TabGroupsSubsystem
    {
        private static TabGroupsSubsystem _instance;
        private ObservableCollection<TabGroup> _tabGroups;
        private DTE _dte;

        private ObservableCollection<TabInfo> _localDocumentWell;

        public ObservableCollection<TabInfo> GetDocumentWell() => _localDocumentWell;

        public CollectionViewSource LocalDocumentWell { get; set; }

        public static TabGroupsSubsystem Instance => _instance ?? (_instance = new TabGroupsSubsystem());

        private TabGroupsSubsystem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            _tabGroups = new ObservableCollection<TabGroup>();
            _localDocumentWell = new ObservableCollection<TabInfo>();
            LocalDocumentWell = new CollectionViewSource
            {
                Source = _localDocumentWell
            };
            LocalDocumentWell.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabInfo.WindowName)));
            Initialize();
        }

        private void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Initialize Tab Groups based on the current state of the IDE

            _dte.Events.SolutionEvents.Opened += SolutionOpened;
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Loop through all open document windows
            foreach (var window in _dte.Windows.Cast<Window>().Where(window => window.Kind.Equals("Document")))
            {
                _localDocumentWell.Add(new TabInfo(window, []));
            }
        }

        public ObservableCollection<TabGroup> GetAllTabGroups()
        {
            return _tabGroups;
        }

        public void CreateNewTabGroup(string groupName, List<string> filters, bool isLocked = false)
        {
            // Logic to create a new tab group
            var newTabGroup = new TabGroup()
            {
                Name = groupName,
                ThisGroupsFilters = filters,
                bIsLocked = isLocked
            };

            // Add to ObservableCollection
            _tabGroups.Add(newTabGroup);
        }

        public void DeleteTabGroup(TabGroup tabGroup)
        {
            // Logic to delete a tab group
            if (_tabGroups.Contains(tabGroup))
            {
                _tabGroups.Remove(tabGroup);
            }
        }

        public void AddTabToGroup(TabGroup tabGroup, Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Ensure the tab isn't already part of another group
            if (!_tabGroups.Any(tg => tg.TabsInGroup.Any(ti => ti.Window.Equals(window))))
            {
                tabGroup.TabsInGroup.Add(new TabInfo(window, new string[] { }));
            }
        }

        public void RemoveTabFromGroup(TabGroup tabGroup, Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var tabToRemove = tabGroup.TabsInGroup.FirstOrDefault(t => t.Window.Equals(window));
            if (tabToRemove != null)
            {
                tabGroup.TabsInGroup.Remove(tabToRemove);
            }
        }

        public void OrganizeTabsByGroup()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Logic to organize tabs by their assigned group
        }

        public void SetGroupVisibility(TabGroup tabGroup, bool isVisible)
        {
            // Logic to set visibility of a tab group
            tabGroup.IsVisible = isVisible;
            // Implement the actual UI visibility change
        }

        public void LockGroup(TabGroup tabGroup, bool isLocked)
        {
            // Logic to lock/unlock a tab group to prevent accidental changes
            tabGroup.bIsLocked = isLocked;
        }

        public void ApplyColorSchemeToGroup(TabGroup tabGroup, string colorCode)
        {
            // Apply color coding to the tab group for easy identification
            tabGroup.ColourCode = colorCode;
            // Update UI accordingly
        }



        // Additional methods as necessary for drag-and-drop, custom icons, etc.
    }
}