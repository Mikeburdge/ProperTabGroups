using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using EnvDTE;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using ProperTabGroups.TabGroupScripts;
using Debugger = Community.VisualStudio.Toolkit.Debugger;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;

namespace ProperTabGroups.Subsystem
{
    public class TabGroupsSubsystem
    {
        private static TabGroupsSubsystem _instance;
        private DTE _dte;

        public ObservableCollection<TabGroup> _groupsDocumentWell { get; set; }

        public CollectionViewSource GroupsDocumentWell { get; set; }

        public List<TabInfo> AllOpenDocuments;

        public static TabGroupsSubsystem Instance => _instance ?? (_instance = new TabGroupsSubsystem());

        private TabGroupsSubsystem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            _groupsDocumentWell = new ObservableCollection<TabGroup>();
            GroupsDocumentWell = new CollectionViewSource
            {
                Source = _groupsDocumentWell
            };
            AllOpenDocuments = new List<TabInfo>();

            HandleGroupFunctionality();
            Initialize();
        }

        private void HandleGroupFunctionality()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Temporarily Grouping by WindowName until I can figure out a better way of grouping
            GroupsDocumentWell.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabGroup.Name)));

            // Sorting documents by FileName
            GroupsDocumentWell.SortDescriptions.Add(new SortDescription(nameof(TabInfo.WindowName), ListSortDirection.Ascending));
        }

        private void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Initialize Tab Groups based on the current state of the IDE

            _dte.Events.SolutionEvents.Opened += SolutionOpened;

            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                while (true)
                {
                    bool allowBreakpoint = false;

                    if (allowBreakpoint)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
            });
            t.Start();
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            TabGroup allDocumentsGroup = new("All Documents", true);
            // Loop through all open document windows
            foreach (Window window in _dte.Windows.Cast<Window>().Where(window => window.Kind.Equals("Document")))
            {
                AllOpenDocuments.Add(new TabInfo(window, [nameof(allDocumentsGroup.Name)]));
            }

            // Trigger Sort Groups Action / Setup Constant Group Sorting based on filters
            // For now I'm going to add it manually until I get shit up and running

            foreach (TabInfo tab in AllOpenDocuments)
            {
                allDocumentsGroup._tabsInGroup.Add(tab);
            }

            _groupsDocumentWell.Add(allDocumentsGroup);
            allDocumentsGroup.Name = "AllDocuments2";
            _groupsDocumentWell.Add(allDocumentsGroup);
        }

        public ObservableCollection<object> FilterAndGroupTabs(IEnumerable<TabInfo> tabs, string filter)
        {
            ObservableCollection<object> filteredAndGroupedTabs = new ObservableCollection<object>();

            // Filter tabs based on the filter criteria
            IEnumerable<TabInfo> filteredTabs = string.IsNullOrEmpty(filter) ? tabs : tabs.Where(tab => tab.Filters.Contains(filter));

            IEnumerable<string> categories = filteredTabs
                .SelectMany(tab => tab.Filters)
                .Distinct();

            foreach (string category in categories)
            {
                var group = new
                {
                    Category = category,
                    Tabs = filteredTabs.Where(tab => tab.Filters.Contains(category)).ToList()
                };

                filteredAndGroupedTabs.Add(group);
            }

            return filteredAndGroupedTabs;
        }

        public void CreateNewTabGroup(string groupName, bool isLocked = false)
        {
            // Logic to create a new tab group
            TabGroup newTabGroup = new TabGroup(name: groupName, bIsLocked: isLocked);

            // Add to ObservableCollection
            _groupsDocumentWell.Add(newTabGroup);
        }

        public void DeleteTabGroup(TabGroup tabGroup)
        {
            // Logic to delete a tab group
            if (_groupsDocumentWell.Contains(tabGroup))
            {
                _groupsDocumentWell.Remove(tabGroup);
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
            tabGroup.BIsVisible = isVisible;
            // Implement the actual UI visibility change
        }

        public void LockGroup(TabGroup tabGroup, bool isLocked)
        {
            // Logic to lock/unlock a tab group to prevent accidental changes
            tabGroup.BIsLocked = isLocked;
        }

        public void ApplyColorSchemeToGroup(TabGroup tabGroup, string colorCode)
        {
            // Apply color coding to the tab group for easy identification
            tabGroup.ColourCode = colorCode;
            // Update UI accordingly
        }

        public bool AddFilter(TabInfo tabInfo, string Filter)
        {
            if (tabInfo.Filters.Contains(Filter))
            {
                return false;
            }
            
            tabInfo.Filters.Add(Filter);
            return true;
        }

        // Additional methods as necessary for drag-and-drop, custom icons, etc.
    }
}