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
using ProperTabGroups.DualListSelector;
using ProperTabGroups.TabGroupScripts;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using Window = EnvDTE.Window;
using System.Windows.Data;

namespace ProperTabGroups
{
    public partial class ProperTabGroupsWindowControl : UserControl
    {
        private readonly DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;

        private TabInfo _filterModificationCurrentTabInfo = null;

        private bool isSelectionHandling;

        public DocumentWellManagementSubsystem ViewModel => DocumentWellManagementSubsystem.Instance;

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
                //ClearSelection();
                SelectTabGroupInListView(GotFocus.Caption);
            }
        }
        private IEnumerable<ListView> GetAllListViews(DependencyObject parent)
        {
            // This function iteratively searches through the children of the provided DependencyObject,
            // looking for ListView instances. If it finds one, it yields it. If it finds a container
            // (something that can have children), it recursively searches through its children.

            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListView view)
                {
                    yield return view;
                }

                foreach (ListView childOfChild in GetAllListViews(child))
                {
                    yield return childOfChild;
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSelectionHandling) return;

            isSelectionHandling = true;
            try
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

                if (localListView?.SelectedItem is not TabInfo selectedTabInfo) return; // Safety check

                if (selectedTabInfo.Window != null && selectedTabInfo.Window == _dte.ActiveWindow)
                {
                    return;
                }

                ViewModel.OpenFileSafely(selectedTabInfo);

                // Old Show File, only reason im keeping this here is because I think this also worked for opening files
                //_dte.ExecuteCommand("File.OpenFile", selectedTabInfo.DocumentPath);
            }
            finally
            {
                isSelectionHandling = false;
            }
        }

        private void HandleClearingAllOtherSelectedItemsListViewOnly(object sender)
        {
            TabInfo selectedTabInfo = GetClickedTabInfo(sender);
            if (selectedTabInfo == null) return;

            DeselectAllTabsExceptOne(selectedTabInfo);

            //IEnumerable<ListView> allListViews = GetAllListViews(this);

            //// Loop through each ListView
            //foreach (ListView currentListView in allListViews)
            //{
            //    // Skip the ListView that initiated the call to prevent altering its state
            //    if (currentListView == sender as ListView) continue;

            //    foreach (object item in currentListView.Items)
            //    {
            //        // Skip items that are not of type TabInfo
            //        if (item is not TabInfo currentTabInfo) continue;

            //        // Conditions to skip the deselection:
            //        // 1. The TabInfo corresponds to a visible, non-null window object.
            //        // 2. The TabInfo is the one that was selected.
            //        if (/*currentTabInfo.Window is { Object: not null, Visible: true } ||*/ false)
            //        {
            //            continue;
            //        }

            //        bool shouldTabBeSelected = currentTabInfo == selectedTabInfo;

            //        // Retrieve the ListViewItem corresponding to the currentTabInfo
            //        if (currentListView.ItemContainerGenerator.ContainerFromItem(currentTabInfo) is ListViewItem listViewItem)
            //        {
            //            // Deselect the ListViewItem
            //            listViewItem.IsSelected = shouldTabBeSelected;
            //        }
            //    }
            //}
        }

        private void DeselectAllTabsExceptOne(TabInfo tabToModify)
        {
            foreach (TabInfo tabInfo in ViewModel.AllTabInfos)
            {
                bool shouldTabBeSelected = tabInfo.Equals(tabToModify);

                SetIfTabIsSelected(tabInfo, shouldTabBeSelected);
            }
        }

        private void SetIfTabIsSelected(TabInfo tabToModify, bool shouldBeSelected)
        {
            // Assuming there's a method to retrieve all ListViews in the current context
            IEnumerable<ListView> allListViews = GetAllListViews(this);

            // Loop through each ListView in the application or window
            foreach (ListView currentListView in allListViews)
            {
                // Check if the ListView contains the TabInfo item
                foreach (object item in currentListView.Items)
                {
                    if (item is not TabInfo currentTabInfo || currentTabInfo != tabToModify) continue;

                    // Retrieve the ListViewItem corresponding to the currentTabInfo
                    if (currentListView.ItemContainerGenerator.ContainerFromItem(currentTabInfo) is ListViewItem listViewItem)
                    {
                        // Set the selection state of the ListViewItem
                        listViewItem.IsSelected = shouldBeSelected;
                    }
                    // Since TabInfo was found and handled, no need to continue checking this ListView
                    break;
                }
            }
        }

        private void SelectTabGroupInListView(string activatedTabName)
        {
            List<TabInfo> documentWell = DocumentWellManagementSubsystem.Instance.AllTabInfos;

            TabInfo matchingTabInfo = documentWell.FirstOrDefault(tabInfo => tabInfo.WindowName.Equals(activatedTabName));

            if (matchingTabInfo == null) return;

            // Set the flag before changing the selection
            _bIsSelectionChangeProgrammatic = true;

            try
            {
                SetIfTabIsSelected(matchingTabInfo, true);
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

        private void ModifyFilters_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo selectedTabInfo = GetClickedTabInfo(sender);

            //TabInfo selectedTabInfo = ViewModel.GetFirstSelectedTabInfoInGroups();
            if (selectedTabInfo == null) return;

            DualListboxSelectorWindowControl selectorWindow = new();
            selectorWindow.Show();
            selectorWindow.PopulateInitialItems(selectedTabInfo);
        }

        private TabInfo GetClickedTabInfo(object sender)
        {
            if (sender is MenuItem menuItem)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;

                FrameworkElement placementTarget = contextMenu.PlacementTarget as FrameworkElement;
                object dataContext = FindDataContextForFrameworkElement(placementTarget);

                TabInfo selectedTabInfo = dataContext as TabInfo;
                return selectedTabInfo;
            }

            ListView listView = sender as ListView;

            if (listView == null) return null;

            TabInfo listViewTabInfo = listView.SelectedItem as TabInfo;

            if (listViewTabInfo == null) return null;

            return listViewTabInfo;
        }

        private void DeleteGroup_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement placementTarget = contextMenu.PlacementTarget as FrameworkElement;
            object dataContext = FindDataContextForFrameworkElement(placementTarget);

            // Check if the dataContext is a CollectionViewGroup
            if (dataContext is CollectionViewGroup collectionViewGroup)
            {
                // Access the items in the group
                foreach (object item in collectionViewGroup.Items)
                {
                    // Now you can work with each item, which might be of the type TabGroup
                    TabGroup tabGroup = item as TabGroup;
                    if (tabGroup == null) continue;
                    ViewModel.DeleteTabGroup(tabGroup);
                    return;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DataContext is not a CollectionViewGroup.");
            }

            // Rest of your method...
        }

        private void RefreshAll(object sender, RoutedEventArgs e)
        {
            DocumentWellManagementSubsystem.RefreshAll();
        }
    }
}