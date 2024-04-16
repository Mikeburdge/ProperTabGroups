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
using ProperTabGroups.TabGroupScripts;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using Window = EnvDTE.Window;
using System.Diagnostics;
using ProperTabGroups.Subsystems;
using Constants = EnvDTE.Constants;
using System.Globalization;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Experimentation;
using System.Net.NetworkInformation;

namespace ProperTabGroups.Subsystem
{
    public class DocumentWellManagementSubsystem : INotifyPropertyChanged
    {
        private static DocumentWellManagementSubsystem _instance;
        public static DocumentWellManagementSubsystem Instance => _instance ??= new DocumentWellManagementSubsystem();

        private DTE _dte;

        private const string UnassignedTabsGroupName = "Unassigned Tabs";
        public static Guid UnassignedTabsGroupGuid = Guid.NewGuid();
        public static Guid ClosedFileGuid = new Guid("045ca0af-b76a-4222-9958-12a287a26e68");

        public CollectionViewSource GroupsDocumentWell { get; set; }
        private ObservableCollection<TabGroup> _groupsDocumentWellSource;
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

        public bool IsUnassignedListBoxVisible = true;
        private ObservableCollection<TabInfo> _unassignedTabsGroupSource;
        public ObservableCollection<TabInfo> UnassignedTabsGroupSource
        {
            get => _unassignedTabsGroupSource;
            set
            {
                if (Equals(_groupsDocumentWellSource, value)) return;

                if (_unassignedTabsGroupSource != null)
                {
                    // Unsubscribe from the CollectionChanged event of the old collection
                    _unassignedTabsGroupSource.CollectionChanged -= OnUnassignedTabsSourceChanged;
                }

                _unassignedTabsGroupSource = value;
                OnPropertyChanged();

                if (_unassignedTabsGroupSource != null)
                {
                    // Subscribe to the CollectionChanged event of the new collection
                    _unassignedTabsGroupSource.CollectionChanged += OnUnassignedTabsSourceChanged;
                }
            }
        }

        public CollectionViewSource UnassignedTabsGroup { get; set; }

        private void OnUnassignedTabsSourceChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            //IsUnassignedListBoxVisible = UnassignedTabsGroupSource.Any();

            RefreshUnassignedGroupsListView();
        }


        public readonly List<TabInfo> AllTabInfos;

        public ListView GroupsListView { get; set; }


        private DocumentWellManagementSubsystem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte = (DTE)Package.GetGlobalService(typeof(DTE));

            AllTabInfos = new List<TabInfo>();

            UnassignedTabsGroupSource = new ObservableCollection<TabInfo>();
            UnassignedTabsGroup = new CollectionViewSource
            {
                Source = UnassignedTabsGroupSource
            };

            GroupsDocumentWellSource = new ObservableCollection<TabGroup>();
            GroupsDocumentWell = new CollectionViewSource
            {
                Source = GroupsDocumentWellSource
            };

            Initialise();
        }

        private void HandleGroupFunctionality()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GroupsDocumentWell.SortDescriptions.Add(new SortDescription(nameof(TabGroup.Name), ListSortDirection.Ascending));
            GroupsDocumentWell.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabGroup.Name)));

            CollectionViewSource.GetDefaultView(GroupsDocumentWellSource).Refresh();

            UnassignedTabsGroup.SortDescriptions.Add(new SortDescription(nameof(TabGroup.Name), ListSortDirection.Ascending));
            UnassignedTabsGroup.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TabGroup.Name)));

            CollectionViewSource.GetDefaultView(UnassignedTabsGroupSource).Refresh();
        }

        private void Initialise()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Initialize Tab Groups based on the current state of the IDE

            HandleGroupFunctionality();
            _dte.Events.SolutionEvents.Opened += SolutionOpened;

        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<TabGroup> tabGroups = SaveLoadManager.Instance.LoadTabGroupsFromJson();

            AllTabInfos.Clear();
            _groupsDocumentWellSource.Clear();
            foreach (TabGroup tabGroup in tabGroups)
            {
                AllTabInfos.AddRange(tabGroup.TabsInGroupSource);

                _groupsDocumentWellSource.Add(tabGroup);
            }

            foreach (Window window in _dte.Windows.Cast<Window>().Where(window => window.Kind is "Document"))
            {
                if (AllTabInfos.Any(x => x.WindowName.Equals(window.Caption))) continue;

                AddUniqueTabToAllTabs(new TabInfo(window, []));
            }

            RealignTabsToFilteredGroups();

            _dte.Events.WindowEvents.WindowCreated += WindowCreated;
            _dte.Events.WindowEvents.WindowClosing += WindowClosing;
        }
        private void WindowCreated(Window newWindow)
        {
            TabInfo tabInfo = FindOrCreateTabInfo(newWindow);

            switch (tabInfo.State)
            {
                case TabState.Grouped:
                    HandleInGroup(tabInfo, newWindow);
                    break;
                case TabState.Unassigned:
                    HandleUngrouped(tabInfo, newWindow);
                    break;
                case TabState.Invalid:
                    HandleInvalid(tabInfo, newWindow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!AllTabInfos.Contains(tabInfo))
            {
                AllTabInfos.Add(tabInfo);
            }
            RealignTabsToFilteredGroups();
        }

        private void HandleInGroup(TabInfo tabInfo, Window newWindow)
        {
            TabInfo activeTabInfo = new();
            if (IsTabContainedInAnyGroupByName(tabInfo.WindowName, ref activeTabInfo))
            {
                activeTabInfo.Window = newWindow;
                activeTabInfo.State = TabState.Grouped;
            }
            else
            {
                tabInfo.State = TabState.Unassigned;
                HandleUngrouped(tabInfo, newWindow);
            }
        }

        private void HandleUngrouped(TabInfo tabInfo, Window newWindow)
        {
            if (UnassignedTabsGroupSource.Contains(tabInfo) || UnassignedTabsGroupSource.Any(x => x.WindowName == newWindow.Caption))
            {
                tabInfo.Window = newWindow;
                tabInfo.State = TabState.Unassigned;
                return;
            }
            
            if (!tabInfo.Filters.Any())
            {
                tabInfo.Filters.Add(UnassignedTabsGroupGuid);
            }
        }

        private void HandleInvalid(TabInfo tabInfo, Window newWindow)
        {
            // Check if the window or TabInfo can be validated or corrected
            if (IsWindowValid(newWindow) && IsTabInfoCorrectable(tabInfo))
            {
                // Attempt to correct the TabInfo based on the new window information
                CorrectTabInfo(tabInfo, newWindow);
                // After correction, re-evaluate the state
                TabState newState = EvaluateTabState(tabInfo);
                tabInfo.State = newState;
                // Call the appropriate method based on the new state
                switch (newState)
                {
                    case TabState.Grouped:
                        HandleInGroup(tabInfo, newWindow);
                        break;
                    case TabState.Unassigned:
                        HandleUngrouped(tabInfo, newWindow);
                        break;
                    default:
                        // If state remains invalid, log and remove
                        LogAndRemoveInvalidTabInfo(tabInfo);
                        break;
                }
            }
            else
            {
                // If not correctable, log the issue and remove TabInfo from management
                LogAndRemoveInvalidTabInfo(tabInfo);
            }
        }

        // Helper methods that might be used in HandleInvalid()
        private bool IsWindowValid(Window window)
        {
            // Implement validation logic for the window, e.g., check for non-null, correct type, etc.
            return window != null && !string.IsNullOrWhiteSpace(window.Caption);
        }

        private bool IsTabInfoCorrectable(TabInfo tabInfo)
        {
            // Implement logic to determine if the TabInfo can be corrected, e.g., check attributes, linked data, etc.
            return tabInfo != null && !string.IsNullOrWhiteSpace(tabInfo.WindowName);
        }

        private void CorrectTabInfo(TabInfo tabInfo, Window window)
        {
            // Implement logic to correct the TabInfo based on new or existing window information
            tabInfo.Window = window;
            tabInfo.WindowName = window.Caption; // Update TabInfo with correct window caption
        }

        private TabState EvaluateTabState(TabInfo tabInfo)
        {
            // Logic to evaluate the new state of the tabInfo after correction
            return IsTabContainedInAnyGroup(tabInfo) ? TabState.Grouped : TabState.Unassigned;
        }

        private void LogAndRemoveInvalidTabInfo(TabInfo tabInfo)
        {
            Debug.WriteLine($"Invalid TabInfo detected and removed: {tabInfo.WindowName}");
            AllTabInfos.Remove(tabInfo);
            if (UnassignedTabsGroupSource.Contains(tabInfo))
            {
                UnassignedTabsGroupSource.Remove(tabInfo);
            }
        }

        private TabInfo FindOrCreateTabInfo(Window window)
        {
            TabInfo tabInfoByWindow = GetTabInfoFromWindow(window);
            if (tabInfoByWindow != null)
            {
                return tabInfoByWindow;
            }

            TabInfo tabInfoByName = GetTabInfoByName(window.Caption);

            if (tabInfoByName != null)
            {
                return tabInfoByName;
            }


            return new TabInfo(window, []) { State = TabState.Unassigned };
        }

        private void WindowClosing(Window closingWindow)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Retrieve the associated TabInfo using the window reference
            TabInfo tabInfo = GetTabInfoFromWindow(closingWindow);

            if (tabInfo == null)
            {
                // If no TabInfo is found by window reference, try finding it by name
                tabInfo = GetTabInfoByName(closingWindow.Caption);
                if (tabInfo == null)
                {
                    // If still not found, simply return as there's nothing to close
                    Debug.WriteLine($"No TabInfo found for closing window: {closingWindow.Caption}");
                    return;
                }
            }

            // Process the TabInfo based on its current group assignment
            if (tabInfo.Filters.Contains(UnassignedTabsGroupGuid) || UnassignedTabsGroupSource.Contains(tabInfo))
            {
                // Remove from unassigned tabs if it's listed there
                UnassignedTabsGroupSource.Remove(tabInfo);
                Debug.WriteLine("TabInfo removed from unassigned tabs.");

                // Check if the TabInfo is contained in any group
                if (!IsTabContainedInAnyGroup(tabInfo))
                {
                    // Only remove from AllTabInfos if not contained in any group
                    AllTabInfos.Remove(tabInfo);
                    Debug.WriteLine("TabInfo removed from all tabs.");
                }
            }
            else
            {
                //RemoveTabInfoFromGroups(tabInfo);
            }

            // Optionally, perform additional cleanup or state updates
            PostTabInfoRemovalCleanup(tabInfo);
        }

        // Helper methods used in WindowClosing
        private void RemoveTabInfoFromGroups(TabInfo tabInfo)
        {
            foreach (TabGroup group in GroupsDocumentWellSource)
            {
                if (group.TabsInGroupSource.Remove(tabInfo))
                {
                    Debug.WriteLine($"TabInfo removed from group: {group.Name}");
                    //break;  // Assuming a TabInfo can only be in one group at a time
                }
            }
        }

        private void PostTabInfoRemovalCleanup(TabInfo tabInfo)
        {
            // This method can handle any additional logic needed after a tab is removed
            // For example, saving state, updating UI, logging, etc.
            Debug.WriteLine($"Cleanup performed for TabInfo: {tabInfo.WindowName}");
        }

        private void IntegrateNewTabIntoGroups(TabInfo tabToIntegrate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();



        }

        private static void RefreshUnassignedGroupsListView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CollectionViewSource.GetDefaultView(Instance.UnassignedTabsGroupSource).Refresh();
        }

        private static void RefreshAllGroupsView()
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

            // Create a dictionary for quick lookup
            Dictionary<Guid, TabGroup> groupLookup = GroupsDocumentWellSource.ToDictionary(g => g.GroupGuid, g => g);

            // Preprocess all tabs and organize them by required groups
            Dictionary<TabGroup, HashSet<TabInfo>> tabsToGroup = new Dictionary<TabGroup, HashSet<TabInfo>>();

            foreach (TabInfo tab in AllTabInfos)
            {
                bool isUnassigned = true;

                foreach (Guid filter in tab.Filters)
                {
                    if (!groupLookup.TryGetValue(filter, out TabGroup group)) continue;

                    isUnassigned = false;

                    // Initialize the hash set for this group if it doesn't exist
                    if (!tabsToGroup.TryGetValue(group, out HashSet<TabInfo> tabs))
                    {
                        tabs = new HashSet<TabInfo>();
                        tabsToGroup[group] = tabs;
                    }

                    // Add tab to this group's set
                    tabs.Add(tab);
                }

                // If no filters or no groups matched, add to unassigned
                if (!isUnassigned && !tab.Filters.Contains(UnassignedTabsGroupGuid)) continue;

                //// new code idk if this gets in the way or not
                //if (!tab.Filters.Contains(UnassignedTabsGroupGuid))
                //{
                //    tab.Filters.Add(UnassignedTabsGroupGuid);
                //}

                if (!UnassignedTabsGroupSource.Contains(tab))
                {
                    UnassignedTabsGroupSource.Add(tab);
                }
            }

            // Add tabs to their groups if they are not already there
            foreach (KeyValuePair<TabGroup, HashSet<TabInfo>> group in tabsToGroup)
            {
                foreach (TabInfo tab in group.Value.Where(tab => !group.Key.TabsInGroupSource.Contains(tab)))
                {
                    group.Key.TabsInGroupSource.Add(tab);
                }
            }
        }

        private void ValidateCurrentGroups()
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
            for (int i = UnassignedTabsGroupSource.Count - 1; i >= 0; i--)
            {
                TabInfo tab = UnassignedTabsGroupSource[i];
                if (!tab.Filters.Contains(UnassignedTabsGroupGuid))
                {
                    UnassignedTabsGroupSource.RemoveAt(i);
                    continue;
                }

                if (!tab.Filters.Contains(ClosedFileGuid)) continue;
                AllTabInfos.Remove(tab);
                UnassignedTabsGroupSource.RemoveAt(i);
            }
            //IsUnassignedListBoxVisible = UnassignedTabsGroupSource.Any();
        }

        private void AddUniqueTabToAllTabs(TabInfo tabInfo)
        {
            if (AllTabInfos.Contains(tabInfo))
            {
                Debug.WriteLine("ProperTabGroups ERROR: Tried adding an existing tab info to AllTabs");
                return;
            }

            if (AllTabInfos.Any(currentTabInfo => currentTabInfo.WindowName == tabInfo.WindowName))
            {
                Debug.WriteLine("ProperTabGroups ERROR: Tried adding an existing tab name info to AllTabs");
                return;
            }

            AllTabInfos.Add(tabInfo);
        }

        private TabInfo GetTabInfoByName(string inName)
        {
            return AllTabInfos.FirstOrDefault(x => x.WindowName == inName);
        }
        private bool IsWindowContainedInAnyGroup(Window windowToFind)
        {
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                foreach (TabInfo tabInfo in tabGroup.TabsInGroupSource)
                {
                    if (tabInfo.Window == windowToFind /*|| tabInfo.WindowName == windowToFind.Caption*/) return true;
                }
            }

            return false;
        }
        private bool IsTabContainedInAnyGroup(TabInfo tabInfoToFind)
        {
            return GroupsDocumentWellSource.Any(tabGroup => tabGroup.TabsInGroupSource.Contains(tabInfoToFind));
        }
        private bool IsTabContainedInAnyGroupByName(TabInfo tabInfoToFind, ref TabInfo outContainedTab)
        {
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                foreach (TabInfo x in tabGroup.TabsInGroupSource)
                {
                    if (x.WindowName != tabInfoToFind.WindowName) continue;
                    outContainedTab = x;
                    return true;
                }
            }

            foreach (TabInfo tabInfo in UnassignedTabsGroupSource)
            {
                if (tabInfo.WindowName != tabInfoToFind.WindowName) continue;
                outContainedTab = tabInfo;
                return true;
            }

            return false;
        }
        private bool IsTabContainedInAnyGroupByName(string tabInfoToFind, ref TabInfo outContainedTab)
        {
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                foreach (TabInfo x in tabGroup.TabsInGroupSource)
                {
                    if (x.WindowName != tabInfoToFind) continue;
                    outContainedTab = x;
                    return true;
                }
            }

            foreach (TabInfo tabInfo in UnassignedTabsGroupSource)
            {
                if (tabInfo.WindowName != tabInfoToFind) continue;
                outContainedTab = tabInfo;
                return true;
            }

            return false;
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

        public bool GetTabGroupFromGuid(Guid inGuid, ref TabGroup tabGroup)
        {
            foreach (TabGroup group in GroupsDocumentWellSource)
            {
                if (group.GroupGuid == inGuid)
                {
                    tabGroup = group;
                    return true;
                }
            }

            return false;
        }

        public TabInfo GetTabInfoFromWindow(Window window)
        {
            foreach (TabInfo currentTabInfo in AllTabInfos)
            {
                if (currentTabInfo.Window == window)
                {
                    return currentTabInfo;
                }
            }
            return null;
        }

        public string GetGroupNameFromGuid(Guid inGuid)
        {
            return (from @group in GroupsDocumentWellSource where inGuid.Equals(@group.GroupGuid) select @group.Name).FirstOrDefault();
        }

        public static void AddFilterToTab(TabInfo tabInfo, Guid filter)
        {
            if (tabInfo == null)
            {
                Debug.WriteLine("Tab info is null");
                return;
            }
            if (tabInfo.Filters.Contains(filter)) return;

            tabInfo.Filters.Add(filter);

            if (tabInfo.Filters.Count > 1 && tabInfo.Filters.Contains(UnassignedTabsGroupGuid))
            {
                // Remove from unassigned if it's a part of this group
                tabInfo.Filters.Remove(UnassignedTabsGroupGuid);
            }
        }

        public void RemoveFilterFromTab(TabInfo tabInfo, Guid filter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!tabInfo.Filters.Contains(filter)) return;

            tabInfo.Filters.Remove(filter);

            // If this tab contains no filters add it to the unassigned group
            if (!tabInfo.Filters.Any())
            {
                tabInfo.Filters.Add(UnassignedTabsGroupGuid);
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
            foreach (TabGroup tabGroup in GroupsDocumentWellSource)
            {
                TabInfo selectedTab = tabGroup.TabsInGroupSource.FirstOrDefault(tab => tab.IsSelected);
                if (selectedTab != null) return selectedTab;
            }

            return null;
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