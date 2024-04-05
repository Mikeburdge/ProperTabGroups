using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using ProperTabGroups.Subsystems;
using Constants = EnvDTE.Constants;

namespace ProperTabGroups.Subsystem
{
    public class DocumentWellManagementSubsystem : ProperTabGroupsSubsystemBase, INotifyPropertyChanged
    {
        public new static DocumentWellManagementSubsystem Instance => (DocumentWellManagementSubsystem)(_instance ??= new DocumentWellManagementSubsystem());


        private ObservableCollection<TabGroup> _groupsDocumentWellSource;

        public string UnassignedTabsGroupName = "Unassigned Tabs";
        public Guid UnassignedTabsGroupGuid = Guid.NewGuid();

        public Package package;
        public ObservableCollection<TabGroup> GroupsDocumentWellSource
        {
            get => _groupsDocumentWellSource;
            set
            {
                if (Equals(_groupsDocumentWellSource, value)) return;

                if (_groupsDocumentWellSource != null)
                {
                    // Unsubscribe from the CollectionChanged event of the old collection
                    _groupsDocumentWellSource.CollectionChanged -= OnGroupsDocumentWellSourceChanged;
                }

                _groupsDocumentWellSource = value;
                OnPropertyChanged();

                if (_groupsDocumentWellSource != null)
                {
                    // Subscribe to the CollectionChanged event of the new collection
                    _groupsDocumentWellSource.CollectionChanged += OnGroupsDocumentWellSourceChanged;
                }
            }
        }

        private void OnGroupsDocumentWellSourceChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RefreshAllGroupsView();
        }

        public CollectionViewSource GroupsDocumentWell { get; set; }

        public readonly List<TabInfo> AllOpenDocuments;

        public ListView GroupsListView { get; set; }


        private DocumentWellManagementSubsystem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            AllOpenDocuments = new List<TabInfo>();

            GroupsDocumentWellSource = new ObservableCollection<TabGroup>();
            GroupsDocumentWell = new CollectionViewSource
            {
                Source = GroupsDocumentWellSource
            };
        }

        private void HandleGroupFunctionality()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GroupsDocumentWell.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabGroup.Name)));

            // Sorting documents by FileName
            GroupsDocumentWell.SortDescriptions.Add(new SortDescription(nameof(TabInfo.WindowName), ListSortDirection.Ascending));
        }

        protected override void Initialise()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Initialise Tab Groups based on the current state of the IDE

            HandleGroupFunctionality();







            ThreadHelper.ThrowIfNotOnUIThread();

            List<TabGroup> TabGroups = SaveLoadManager.Instance.InitSaveLoadManager(package, _dte);

            _groupsDocumentWellSource.Clear();
            foreach (TabGroup tabGroup in TabGroups)
            {
                _groupsDocumentWellSource.Add(tabGroup);
            }

            //const string document1Name = "Test Documents";
            //TabGroup document1Group = CreateNewTabGroup(document1Name);
            //CreateNewTabGroup("Test Documents 2");
            //// Loop through all open document windows
            //foreach (Window window in _dte.Windows.Cast<Window>().Where(window => window.Kind.Equals("Document")))
            //{
            //    AllOpenDocuments.Add(new TabInfo(window, [document1Group.GroupGuid]));
            //}

            RealignTabsToFilteredGroups();

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
                TabGroup unassignedGroup = GroupsDocumentWellSource.FirstOrDefault(g => g.GroupGuid.Equals(UnassignedTabsGroupGuid));

                if (unassignedGroup == null)
                {
                    unassignedGroup = new TabGroup(UnassignedTabsGroupName, false, true, UnassignedTabsGroupGuid);
                    GroupsDocumentWellSource.Add(unassignedGroup);
                }

                unassignedGroup.TabsInGroupSource.Add(tabToIntegrate);

                // If no matching group, consider creating a new group or adding to a default group
                return;
            }

            // Determine the correct group for each tab based on its filters
            foreach (Guid filter in tabToIntegrate.Filters)
            {
                // Collect all groups this tag should be in
                IEnumerable<TabGroup> matchingGroups = GroupsDocumentWellSource.Where(g => g.GroupGuid == filter);

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

        public void RefreshAllGroupsAndTabsView()
        {
            RefreshAllGroupsView();

            RefreshAllTabsView();
        }

        public static void RefreshAllGroupsView()
        {
            CollectionViewSource.GetDefaultView(Instance.GroupsDocumentWellSource).Refresh();
        }

        public static void RefreshAllTabsView()
        {
            foreach (TabGroup tabGroup in Instance.GroupsDocumentWellSource)
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
                // If it contains the unassigned filter then ensure the group exists
                if (tab.Filters.Count == 0)
                {
                    TabGroup unassignedGroup = GroupsDocumentWellSource.FirstOrDefault(g => g.GroupGuid.Equals(UnassignedTabsGroupGuid));

                    if (unassignedGroup == null)
                    {
                        unassignedGroup = new TabGroup(UnassignedTabsGroupName, false, true, UnassignedTabsGroupGuid);
                        GroupsDocumentWellSource.Add(unassignedGroup);
                    }

                    unassignedGroup.TabsInGroupSource.Add(tab);
                    continue;
                }

                // Determine the correct group for each tab based on its filters
                foreach (Guid filter in tab.Filters)
                {
                    // Collect all groups this tag should be in
                    IEnumerable<TabGroup> matchingGroups = GroupsDocumentWellSource.Where(g => g.GroupGuid == filter);

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
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                for (int i = tabGroup.TabsInGroupSource.Count - 1; i >= 0; i--)
                {
                    TabInfo tabInfo = tabGroup.TabsInGroupSource[i];
                    if (!tabInfo.Filters.Contains(tabGroup.GroupGuid))
                    {
                        tabGroup.TabsInGroupSource.RemoveAt(i);
                    }
                }
            }

            TabGroup unassignedGroup = GroupsDocumentWellSource.FirstOrDefault(x => x.GroupGuid == UnassignedTabsGroupGuid);

            if (unassignedGroup != null && !unassignedGroup.TabsInGroupSource.Any())
            {
                DeleteTabGroup(unassignedGroup);
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

            // Use LINQ to filter out groups with empty names directly
            List<TabGroup> groupsToRemove = GroupsDocumentWellSource.Where(group => string.IsNullOrEmpty(group.Name)).ToList();
            foreach (TabGroup group in groupsToRemove)
            {
                GroupsDocumentWellSource.Remove(group);
            }

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
            return (from @group in GroupsDocumentWellSource where !tabInfo.Filters.Contains(@group.GroupGuid) select @group.Name).ToList();
        }

        public IEnumerable<TabGroup> GetTabGroupsFromNames(List<string> inStrings)
        {
            List<TabGroup> list = new();
            foreach (TabGroup group in GroupsDocumentWellSource)
            {
                if (inStrings.Contains(group.Name))
                {
                    list.Add(group);
                }
            }

            return list;
        }

        public string GetGroupNameFromGuid(Guid inGuid)
        {
            return (from @group in GroupsDocumentWellSource where inGuid.Equals(@group.GroupGuid) select @group.Name).FirstOrDefault();
        }

        public IEnumerable<TabGroup> GetAllTabGroups()
        {
            return GroupsDocumentWellSource;
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

        public void AddFilterToTab(TabInfo tabInfo, Guid filter)
        {
            if (tabInfo.Filters.Contains(filter)) return;

            tabInfo.Filters.Add(filter);

            if (tabInfo.Filters.Count > 1)
            {
                // Remove from unassigned if it's a part of this group
                tabInfo.Filters.Remove(UnassignedTabsGroupGuid);
            }
        }

        public void RemoveFilterFromTab(TabInfo tabInfo, Guid filter)
        {
            if (!tabInfo.Filters.Contains(filter)) return;

            tabInfo.Filters.Remove(filter);

            // If this tab contains no filters add it to the unassigned group
            if (!tabInfo.Filters.Any())
            {
                AddFilterToTab(tabInfo, UnassignedTabsGroupGuid);
            }
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

        public void OpenFileSafely(TabInfo selectedTabInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (selectedTabInfo == null || string.IsNullOrWhiteSpace(selectedTabInfo.DocumentPath))
            {
                Debug.WriteLine("Selected tab info is null or path is empty.");
                // Optionally, show a user-friendly message or log this incident.
                return;
            }

            string filePath = selectedTabInfo.DocumentPath;

            // Ensure the file path exists to prevent exceptions when trying to open it.
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"File not found: {filePath}");
                // Optionally, inform the user that the file could not be found.
                return;
            }

            try
            {
                // Open the file with a specific view kind if necessary. Here, using the default text view.
                const string fileKind = Constants.vsViewKindCode; // This is typically for text files.
                _dte.ItemOperations.OpenFile(filePath, fileKind);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open file '{filePath}': {ex.Message}");
                // Log the error or inform the user through a dialog, depending on your application's needs.
            }
        }

    }
}