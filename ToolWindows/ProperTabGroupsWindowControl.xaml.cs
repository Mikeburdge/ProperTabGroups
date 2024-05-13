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
using System.Diagnostics;

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

            ViewModel.ProperTabGroupWindowControlRef = this;

        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.ActiveDocument is not { ActiveWindow: not null }) return;

            TabInfo tab = ViewModel.GetTabInfoFromWindow(_dte.ActiveDocument.ActiveWindow);
            ViewModel.SelectOnlyOneTab(tab);
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

            ViewModel.SelectOnlyOneTab(selectedTabInfo);
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

        private void ClearFilters_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo tabInfo = GetClickedTabInfo(sender);

            if (tabInfo == null) return;

            tabInfo.Filters.Clear();
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchTextBoxText = searchTextBox.Text;
        }
    }
}