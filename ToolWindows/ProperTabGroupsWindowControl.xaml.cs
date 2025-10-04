using System.Windows.Controls;
using ProperTabGroups.Subsystem;
using EnvDTE;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using ProperTabGroups.DualListSelector;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;

namespace ProperTabGroups
{
    public partial class ProperTabGroupsWindowControl : UserControl
    {
        private readonly DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;
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
                // Check if the selection change is programmatic and ignore it if so
                if (_bIsSelectionChangeProgrammatic)
                    return;

                ListView sourceListView = sender as ListView;
                if (sourceListView?.SelectedItem is not TabInfo selectedTabInfo)
                    return;

                // clear selection in ALL the other listviews
                ClearOtherListViewSelections(sourceListView);
                
                ViewModel.SelectOnlyOneTab(selectedTabInfo);

                // Open the file, skip if it's the active window
                ThreadHelper.ThrowIfNotOnUIThread();
                if (selectedTabInfo.Window == null || selectedTabInfo.Window != _dte.ActiveWindow)
                {
                    ViewModel.OpenFileSafely(selectedTabInfo);
                }
            }
            finally
            {
                isSelectionHandling = false;
            }
        }

        /// <summary>
        /// Deselects items in every ListView except the one the user interacted with.
        /// </summary>
        private void ClearOtherListViewSelections(ListView source)
        {
            _bIsSelectionChangeProgrammatic = true;
            try
            {
                foreach (ListView list in ViewModel.GetAllListViews(this))
                {
                    if (!ReferenceEquals(list, source))
                    {
                        list.SelectedItem = null;
                        list.UnselectAll();
                    }
                }
            }
            finally
            {
                _bIsSelectionChangeProgrammatic = false;
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