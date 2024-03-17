using System.Collections.Generic;
using ProperTabGroups.Scripts;
using System.Windows.Controls;
using ProperTabGroups.Subsystem;
using System.Collections.ObjectModel;
using EnvDTE;
using System.Linq;

namespace ProperTabGroups
{
    //public class MainWindowViewModel
    //{
    //    public List<TabInfo> Tabs { get; set; }

    //    public MainWindowViewModel()
    //    {
    //        // Initialize the ObservableCollection
    //        Tabs = new List<TabInfo>();

    //        var documentWell = TabGroupsSubsystem.Instance._localDocumentWell;

    //        foreach (var tabInfo in documentWell)
    //        {
    //            Tabs.Add(tabInfo);
    //        }
    //    }
    //}

    public partial class ProperTabGroupsWindowControl : UserControl
    {
        private DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;

        public TabGroupsSubsystem ViewModel => TabGroupsSubsystem.Instance;

        public List<TabGroup> TabGroups { get; set; }
        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            _dte.Events.SolutionEvents.Opened += SolutionOpened;
            _dte.Events.WindowEvents.WindowActivated += WindowActivated;
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
            if (GotFocus != null && GotFocus.Kind.Equals("Document"))
            {
                SelectTabGroupInListView(GotFocus.Caption);
            }
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Check if the selection change is programmatic and ignore it if so
            if (_bIsSelectionChangeProgrammatic)
            {
                return;
            }
            var listView = sender as ListView;
            if (listView == null) return; // Safety check

            var selectedTabInfo = listView.SelectedItem as TabInfo;
            if (selectedTabInfo == null) return; // Safety check


            selectedTabInfo.Window.Activate();

        }

        private void ListViewItem_Selected(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        public void SelectTabGroupInListView(string activatedTabName)
        {
            ObservableCollection<TabInfo> documentWell = TabGroupsSubsystem.Instance.GetDocumentWell();

            TabInfo matchingTabInfo = documentWell.FirstOrDefault(tabInfo => tabInfo.WindowName.Equals(activatedTabName));
            if (matchingTabInfo != null)
            {
                // Set the flag before changing the selection
                _bIsSelectionChangeProgrammatic = true;

                try
                {
                    // Find the index
                    int index = documentWell.IndexOf(matchingTabInfo);
                    if (index >= 0)
                    {
                        // Set the selected item
                        tabGroupsListView.SelectedIndex = index;

                        // Ensure the item is visible
                        tabGroupsListView.ScrollIntoView(tabGroupsListView.SelectedItem);
                    }
                }
                finally
                {
                    // Reset the flag after changing the selection
                    _bIsSelectionChangeProgrammatic = false;
                }
            }
        }

    }
}