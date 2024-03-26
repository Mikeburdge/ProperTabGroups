using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using ProperTabGroups.Subsystem;
using EnvDTE;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using ProperTabGroups.TabGroupScripts;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using Window = EnvDTE.Window;

namespace ProperTabGroups
{
    public partial class ProperTabGroupsWindowControl : UserControl
    {
        private readonly DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;

        private TabInfo _filterModificationCurrentTabInfo = null;

        public ProperTabGroupsSubsystem ViewModel => ProperTabGroupsSubsystem.Instance;

        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (_dte == null) return;
            _dte.Events.SolutionEvents.Opened += SolutionOpened;
            _dte.Events.WindowEvents.WindowActivated += WindowActivated;

            ViewModel.GroupsListView = TabGroupsListView;
        }


        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte.ActiveDocument != null)
            {
                SelectTabGroupInListView(_dte.ActiveDocument.Name);
            }
        }
        private void WindowActivated(Window GotFocus, Window LostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // This "Pattern" below also checks if GotFocus is null
            if (GotFocus is { Kind: "Document" })
            {
                ClearSelection();
                SelectTabGroupInListView(GotFocus.Caption);
            }
        }
        private IEnumerable<ListView> GetAllListViews(DependencyObject parent)
        {
            // This function iteratively searches through the children of the provided DependencyObject,
            // looking for ListView instances. If it finds one, it yields it. If it finds a container
            // (something that can have children), it recursively searches through its children.

            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child != null && child is ListView)
                    {
                        yield return (ListView)child;
                    }

                    foreach (ListView childOfChild in GetAllListViews(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            // Handles all selection changed directly from the ListViews
            HandleClearingAllOtherSelectedItemsListViewOnly(sender);

            ThreadHelper.ThrowIfNotOnUIThread();
            // Check if the selection change is programmatic and ignore it if so
            if (_bIsSelectionChangeProgrammatic)
            {
                return;
            }
            ListView localListView = sender as ListView;
            if (localListView == null) return; // Safety check

            TabInfo selectedTabInfo = localListView.SelectedItem as TabInfo;
            if (selectedTabInfo == null) return; // Safety check

            ViewModel.OpenFileSafely(selectedTabInfo);

            // Show File
            //_dte.ExecuteCommand("File.OpenFile", selectedTabInfo.DocumentPath);
        }

        private void HandleClearingAllOtherSelectedItemsListViewOnly(object sender)
        {
            foreach (ListView currentListView in GetAllListViews(this))
            {
                if (currentListView == sender) continue; // Check to avoid clearing the selection of the current ListView

                foreach (object item in currentListView.Items)
                {
                    TabInfo currentTabInfo = item as TabInfo;

                    if (currentTabInfo == null) continue;

                    if (currentTabInfo.Window.Visible)
                    {
                        continue;
                    }

                    currentTabInfo.IsSelected = false;
                }
            }
        }

        private void ListViewItem_Selected(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void ClearSelection()
        {
            foreach (ListView currentListView in GetAllListViews(this))
            {
                foreach (object item in currentListView.Items)
                {
                    TabInfo currentTabInfo = item as TabInfo;

                    if (currentTabInfo == null) continue;

                    currentTabInfo.IsSelected = false;
                }
            }
        }

        private void SelectTabGroupInListView(string activatedTabName)
        {
            List<TabInfo> documentWell = ProperTabGroupsSubsystem.Instance.AllOpenDocuments;

            TabInfo matchingTabInfo = documentWell.FirstOrDefault(tabInfo => tabInfo.WindowName.Equals(activatedTabName));

            if (matchingTabInfo == null) return;

            // Set the flag before changing the selection
            _bIsSelectionChangeProgrammatic = true;

            try
            {
                matchingTabInfo.IsSelected = true;
            }
            finally
            {
                // Reset the flag after changing the selection
                _bIsSelectionChangeProgrammatic = false;
            }
        }

        private void AddNewGroup(object sender, RoutedEventArgs e)
        {
            // Open the popup
            InputPopup.IsOpen = true;
        }

        private void SubmitPopup_Click(object sender, RoutedEventArgs e)
        {
            // Capture the input string
            string userInput = InputTextBox.Text;


            // Close the popup
            InputPopup.IsOpen = false;

            // Clear the TextBox for the next input
            InputTextBox.Text = string.Empty;

            ViewModel.CreateNewTabGroup(userInput);
        }

        private void TabGroupsListView_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = FindResource("TabInfoContextMenu") as ContextMenu;
            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }
        private object FindDataContextForFrameworkElement(FrameworkElement element)
        {
            if (element.DataContext != null)
            {
                return element.DataContext;
            }

            FrameworkElement parent = VisualTreeHelper.GetParent(element) as FrameworkElement;
            while (parent != null)
            {
                if (parent.DataContext != null)
                {
                    return parent.DataContext;
                }
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }

            return null;
        }

        private void AddFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo selectedTabInfo = GetClickedTabInfo(sender);

            if (selectedTabInfo == null) return;

            // Storing this to use in the next window (@see AddFiltersList_OnSelectionChanged)
            _filterModificationCurrentTabInfo = selectedTabInfo;

            // Proceed to remove the filter from tabInfo
            List<string> itemSource = GetAddFilterItemSource(selectedTabInfo);

            if (!itemSource.Any()) return;

            AddFiltersList.ItemsSource = itemSource;
            ListOfAddFiltersPopup.IsOpen = true;
        }

        private void RemoveFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo selectedTabInfo = GetClickedTabInfo(sender);

            if (selectedTabInfo == null) return;

            // Storing this to use in the next window (@see RemoveFiltersList_OnSelectionChanged)
            _filterModificationCurrentTabInfo = selectedTabInfo;

            // Proceed to remove the filter from tabInfo
            List<string> itemSource = RetrievePurifiedItemSource(selectedTabInfo);

            if (!itemSource.Any()) return;

            RemoveFiltersList.ItemsSource = itemSource;

            ListOfRemoveFiltersPopup.IsOpen = true;
        }
        private TabInfo GetClickedTabInfo(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement placementTarget = contextMenu.PlacementTarget as FrameworkElement;
            object dataContext = FindDataContextForFrameworkElement(placementTarget);

            TabInfo selectedTabInfo = dataContext as TabInfo;
            return selectedTabInfo;
        }

        private void AddFiltersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AddFiltersList.SelectedItem is not string filter) return;

            TabInfo selectedTabInfo = _filterModificationCurrentTabInfo;
            if (selectedTabInfo == null) return;

            ProperTabGroupsSubsystem.AddFilterToTab(selectedTabInfo, filter);

            AddFiltersList.SelectedItem = null;

            List<string> itemSource = GetAddFilterItemSource(selectedTabInfo);

            if (itemSource != null && itemSource.Any())
            {
                AddFiltersList.ItemsSource = itemSource;
            }
            else
            {
                // Get rid of it when we are done
                _filterModificationCurrentTabInfo = null;
                ListOfAddFiltersPopup.IsOpen = false;
            }
        }

        private void RemoveFiltersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RemoveFiltersList.SelectedItem is not string filter) return;

            TabInfo selectedTabInfo = _filterModificationCurrentTabInfo;
            if (selectedTabInfo == null) return;

            ProperTabGroupsSubsystem.RemoveFilterFromTab(selectedTabInfo, filter);

            RemoveFiltersList.SelectedItem = null;

            List<string> itemSource = RetrievePurifiedItemSource(selectedTabInfo);

            if (itemSource.Any())
            {
                RemoveFiltersList.ItemsSource = itemSource;
            }
            else
            {
                // Get rid of it when we are done
                _filterModificationCurrentTabInfo = null;
                ListOfRemoveFiltersPopup.IsOpen = false;
            }
        }

        private List<string> GetAddFilterItemSource(TabInfo selectedTabInfo)
        {
            List<string> itemSource = ViewModel.GetAvailableTabGroupNames(selectedTabInfo).ToList();
            // Remove from unassigned if it's a part of this group
            itemSource.Remove(ProperTabGroupsSubsystem.UnassignedTabsGroupName);
            return itemSource;
        }

        private List<string> RetrievePurifiedItemSource(TabInfo selectedTabInfo)
        {
            // Remove from unassigned if it's a part of this group
            List<string> itemSource = selectedTabInfo.Filters.ToList();

            itemSource.Remove(ProperTabGroupsSubsystem.UnassignedTabsGroupName);
            return itemSource;
        }
    }
}