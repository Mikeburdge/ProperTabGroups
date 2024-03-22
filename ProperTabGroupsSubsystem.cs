using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using EnvDTE;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using ProperTabGroups.TabGroupScripts;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using Window = EnvDTE.Window;

namespace ProperTabGroups.Subsystem
{
    public class TabGroupsSubsystem : INotifyPropertyChanged
    {
        private static TabGroupsSubsystem _instance;
        private DTE _dte;

        private ObservableCollection<TabGroup> _groupsDocumentWellSource;

        public ObservableCollection<TabGroup> GroupsDocumentWellSource
        {
            get => _groupsDocumentWellSource;
            set
            {
                if (!Equals(_groupsDocumentWellSource, value))
                {
                    _groupsDocumentWellSource = value;
                    OnPropertyChanged();

                    OnGroupsDocumentWellSourceChanged();
                }
            }
        }

        private void OnGroupsDocumentWellSourceChanged()
        {
            RefreshAllGroupsAndTabs();
        }

        public CollectionViewSource GroupsDocumentWell { get; set; }

        public List<TabInfo> AllOpenDocuments;

        public ListView GroupsListView { get; set; }

        public static TabGroupsSubsystem Instance => _instance ?? (_instance = new TabGroupsSubsystem());



        private TabGroupsSubsystem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            AllOpenDocuments = new List<TabInfo>();

            GroupsDocumentWellSource = new ObservableCollection<TabGroup>();
            GroupsDocumentWell = new CollectionViewSource
            {
                Source = GroupsDocumentWellSource
            };

            Initialize();
        }

        private void HandleGroupFunctionality()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GroupsDocumentWell.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabGroup.Name)));

            // Sorting documents by FileName
            GroupsDocumentWell.SortDescriptions.Add(new SortDescription(nameof(TabInfo.WindowName), ListSortDirection.Ascending));
        }

        private void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Initialize Tab Groups based on the current state of the IDE

            HandleGroupFunctionality();
            _dte.Events.SolutionEvents.Opened += SolutionOpened;
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            const string document1Name = "Test Documents";
            CreateNewTabGroup(document1Name);
            CreateNewTabGroup("Test Documents 2");
            // Loop through all open document windows
            foreach (Window window in _dte.Windows.Cast<Window>().Where(window => window.Kind.Equals("Document")))
            {
                AllOpenDocuments.Add(new TabInfo(window, [document1Name]));
            }

            RealignTabsToFilteredGroups();
            // Trigger Sort Groups Action / Setup Constant Group Sorting based on filters
            // For now I'm going to add it manually until I get shit up and running

            _dte.Events.WindowEvents.WindowCreated += WindowCreated;
        }

        private void WindowCreated(Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //if (window.Object == null) return;

            if (IsWindowContainedInAnyGroup(window)) return;

            IntegrateNewTabIntoGroups(new TabInfo(window, []));
        }

        private void IntegrateNewTabIntoGroups(TabInfo tabToIntegrate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If there is no filters for the document then add it to the unassigned tabs group
            if (tabToIntegrate.Filters.Count.Equals(0))
            {
                const string unassignedTabsGroupName = "Unassigned Tabs";
                TabGroup unassignedGroup = GroupsDocumentWellSource.FirstOrDefault(g => g.Name.Equals(unassignedTabsGroupName));

                if (unassignedGroup == null)
                {
                    unassignedGroup = new TabGroup(unassignedTabsGroupName, false);
                    GroupsDocumentWellSource.Add(unassignedGroup);
                }

                unassignedGroup.TabsInGroupSource.Add(tabToIntegrate);

                // If no matching group, consider creating a new group or adding to a default group
                return;
            }

            // Determine the correct group for each tab based on its filters
            foreach (string filter in tabToIntegrate.Filters)
            {
                // Collect all groups this tag should be in
                IEnumerable<TabGroup> matchingGroups = GroupsDocumentWellSource.Where(g => g.Name == filter);

                foreach (TabGroup group in matchingGroups)
                {
                    // If the tag isn't already in the group then add it
                    if (!group.TabsInGroupSource.Contains(tabToIntegrate))
                    {
                        group.TabsInGroupSource.Add(tabToIntegrate);
                    }
                }
            }
        }

        public void RefreshAllGroupsAndTabs()
        {
            CollectionViewSource.GetDefaultView(GroupsDocumentWellSource).Refresh();

            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                CollectionViewSource.GetDefaultView(tabGroup.TabsInGroupSource).Refresh();
            }
        }

        public void RealignTabsToFilteredGroups()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ValidateCurrentGroups();

            // Iterate over all open documents
            foreach (TabInfo tab in AllOpenDocuments)
            {
                // If there is no filters for the document then add it to the unassigned tabs group
                if (tab.Filters.Count == 0)
                {
                    const string unassignedTabsGroupName = "Unassigned Tabs";
                    TabGroup unassignedGroup = GroupsDocumentWellSource.FirstOrDefault(g => g.Name.Equals(unassignedTabsGroupName));

                    if (unassignedGroup == null)
                    {
                        unassignedGroup = new TabGroup(unassignedTabsGroupName, true);
                    }
                    GroupsDocumentWellSource.Add(unassignedGroup);
                    // If no matching group, consider creating a new group or adding to a default group
                    continue;
                }

                // Determine the correct group for each tab based on its filters
                foreach (string filter in tab.Filters)
                {
                    // Collect all groups this tag should be in
                    IEnumerable<TabGroup> matchingGroups = GroupsDocumentWellSource.Where(g => g.Name == filter);

                    foreach (TabGroup group in matchingGroups)
                    {
                        // If the tag isn't already in the group then add it
                        if (!group.TabsInGroupSource.Contains(tab))
                        {
                            group.TabsInGroupSource.Add(tab);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates the tabs within each group in the document well, ensuring each tab is supposed to be in its current group. 
        /// Tabs not matching their group's criteria (based on filters) are removed from the group. Note: this does not add tabs to groups, simply removes tabs from groups they aren't meant to be in.
        /// </summary>
        /// <remarks>
        /// This method iterates through all tab groups within the document well (GroupsDocumentWellSource). 
        /// For each tab in a group, it checks if the group's name exists within the tab's Filters. 
        /// If a tab's Filters do not contain the name of the group it's in, the tab is removed from the group.
        /// This ensures that only tabs that meet the group's criteria remain, maintaining the integrity of the groups.
        /// </remarks>
        /// <returns>Void. The method does not return a value but modifies the GroupsDocumentWellSource by potentially removing tabs from groups.</returns>
        public void ValidateCurrentGroups()
        {
            // Use LINQ to filter out groups with empty names directly
            List<TabGroup> groupsToRemove = GroupsDocumentWellSource.Where(group => string.IsNullOrEmpty(group.Name)).ToList();
            foreach (TabGroup group in groupsToRemove)
            {
                GroupsDocumentWellSource.Remove(group);
            }

            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                for (int i = tabGroup.TabsInGroupSource.Count - 1; i >= 0; i--)
                {
                    TabInfo tabInfo = tabGroup.TabsInGroupSource[i];
                    if (!tabInfo.Filters.Contains(tabGroup.Name))
                    {
                        tabGroup.TabsInGroupSource.RemoveAt(i);
                    }
                }
            }
        }

        private bool IsWindowContainedInAnyGroup(Window windowToFind)
        {
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                foreach (TabInfo tabInfo in tabGroup.TabsInGroupSource)
                {
                    if (tabInfo.Window == windowToFind) return true;
                }
            }

            return false;
        }
        private bool IsTabContainedInAnyGroup(TabInfo tabInfoToFind)
        {
            return GroupsDocumentWellSource.Any(tabGroup => tabGroup.TabsInGroupSource.Contains(tabInfoToFind));
        }

        public TabGroup CreateNewTabGroup(string groupName, bool isLocked = false)
        {
            // Logic to create a new tab group
            TabGroup newTabGroup = new TabGroup(name: groupName, bIsLocked: isLocked);

            // Add to ObservableCollection
            GroupsDocumentWellSource.Add(newTabGroup);

            return newTabGroup;
        }

        public void DeleteTabGroup(TabGroup tabGroup)
        {
            // Logic to delete a tab group
            if (GroupsDocumentWellSource.Contains(tabGroup))
            {
                GroupsDocumentWellSource.Remove(tabGroup);
            }
        }

        public IEnumerable<string> GetAvailableTabGroupNames(TabInfo tabInfo)
        {
            return (from @group in GroupsDocumentWellSource where !tabInfo.Filters.Contains(@group.Name) select @group.Name).ToList();
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

        public static void AddFilter(TabInfo tabInfo, string filter)
        {
            if (!tabInfo.Filters.Contains(filter)) tabInfo.Filters.Add(filter);
        }

        // Additional methods as necessary for drag-and-drop, custom icons, etc.
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public TabInfo GetFirstSelectedTabInfoInGroups()
        {
            return GroupsDocumentWellSource.Select(tabGroup => tabGroup.TabsInGroupSource.FirstOrDefault(tab => tab.IsSelected)).FirstOrDefault(selectedTab => selectedTab != null);
        }
    }
}