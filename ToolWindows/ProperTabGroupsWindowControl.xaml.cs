using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using ProperTabGroups.Subsystem;
using EnvDTE;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using ProperTabGroups.DualListSelector;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using System.ComponentModel;

namespace ProperTabGroups
{
    public partial class ProperTabGroupsWindowControl : UserControl
    {
        private readonly DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;

        public DocumentWellManagementSubsystem ViewModel => DocumentWellManagementSubsystem.Instance;

        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
           
            ViewModel.ProperTabGroupWindowControlRef = this;
            ViewModel.PropertyChanged += ViewModelOnDocumentChanged;
            
            if (_dte == null) return;
            _dte.Events.SolutionEvents.Opened += SolutionOpened;
        }

        private void SolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ViewModel.SyncSelectionToActiveWindow();
        }

        private void ViewModelOnDocumentChanged(object sendder, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DocumentWellManagementSubsystem.SelectedTab))
            {
                UpdateSelectionFromViewModel();
            }   
        }

        private void UpdateSelectionFromViewModel()
        {
            TabInfo selectedTab = ViewModel.SelectedTab;
            
            _bIsSelectionChangeProgrammatic = true;
            try
            {
                foreach (ListView list in GetAllListViews(this))
                {
                    if (selectedTab != null && list.Items.Contains(selectedTab))
                    {
                        if (!Equals(list.SelectedItem, selectedTab))
                        {
                            list.SelectedItem = selectedTab;
                            list.ScrollIntoView(selectedTab);
                        }
                    }
                    else
                    {
                        // else, make sure its not selected
                        if (list.SelectedItem != null)
                        {
                            list.SelectedItem = null;
                        }
                    }
                }
            }
            finally
            {
                _bIsSelectionChangeProgrammatic = false;
            }
        }

        private IEnumerable<ListView> GetAllListViews(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is ListView listView)
                {
                    yield return listView;

                    foreach (var subList in GetAllListViews(listView))
                    {
                        yield return subList;
                    }
                }
                else
                {
                    foreach (var subList in GetAllListViews(child))
                    {
                        yield return subList;
                    }
                }
            }
        }


        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection change is programmatic and ignore it if so
            if (_bIsSelectionChangeProgrammatic)
                return;

            // ensure it's a ListView
            if (sender is not ListView sourceListView)
            {
                return;
            }

            if (sourceListView.SelectedItem is TabInfo selectedTabInfo)
            {
                ViewModel.SelectedTab = selectedTabInfo;
            }
            else
            {
                ViewModel.SelectedTab = null;
            }
        }

        private void ModifyFilters_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo selectedTabInfo = GetClickedTabInfo(sender);
            if (selectedTabInfo == null) return;

            DualListboxSelectorWindowControl selectorWindow = new DualListboxSelectorWindowControl();
            selectorWindow.Show();
            selectorWindow.PopulateInitialItems(selectedTabInfo);
        }

        private TabInfo GetClickedTabInfo(object sender)
        {
            if (sender is MenuItem menuItem)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                FrameworkElement placementTarget = contextMenu?.PlacementTarget as FrameworkElement;
                object dataContext = FindDataContextForFrameworkElement(placementTarget);
                return dataContext as TabInfo;
            }

            if (sender is ListView listView)
            {
                return listView.SelectedItem as TabInfo;
            }

            return null;
        }

        private object FindDataContextForFrameworkElement(FrameworkElement element)
        {
            if (element?.DataContext != null)
                return element.DataContext;

            FrameworkElement parent = VisualTreeHelper.GetParent(element) as FrameworkElement;
            while (parent != null)
            {
                if (parent.DataContext != null)
                    return parent.DataContext;

                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }

            return null;
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

        private void SearchTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            searchTextBox.Foreground = Brushes.Black;
            if (searchTextBox.Text == "Search...")
            {
                searchTextBox.Text = "";
            }
        }

        private async void SearchTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Foreground = Brushes.Gray;
                searchTextBox.Text = "Search...";
            }
        }

        private void SearchTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                searchTextBox.Clear();
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            searchTextBox.Clear();
            searchTextBox.Foreground = Brushes.Gray;
            searchTextBox.Text = "Search...";
        }
    }
}