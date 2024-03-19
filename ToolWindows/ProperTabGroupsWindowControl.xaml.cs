using System.Collections.Generic;
using System.Windows.Controls;
using ProperTabGroups.Subsystem;
using System.Collections.ObjectModel;
using EnvDTE;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using ProperTabGroups.TabGroupScripts;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using Window = EnvDTE.Window;

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
        private readonly DTE _dte;

        private bool _bIsSelectionChangeProgrammatic;

        public TabGroupsSubsystem ViewModel => TabGroupsSubsystem.Instance;

        public List<TabGroup> TabGroups { get; set; }
        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (_dte == null) return;
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
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child != null && child is ListView)
                    {
                        yield return (ListView)child;
                    }

                    foreach (var childOfChild in GetAllListViews(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Assuming you have access to a collection of all ListView instances or can retrieve them
            foreach (var currentListView in GetAllListViews(this))
            {
                if (currentListView == sender) continue; // Check to avoid clearing the selection of the current ListView

                foreach (var item in currentListView.Items)
                {
                    TabInfo currentTabInfo = item as TabInfo;

                    if (currentTabInfo == null) continue;

                    if (currentTabInfo.IsSelected) continue;

                    currentTabInfo.IsSelected = false;
                }
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            //// Check if the selection change is programmatic and ignore it if so
            //if (_bIsSelectionChangeProgrammatic)
            //{
            //    return;
            //}
            ListView localListView = sender as ListView;
            if (localListView == null) return; // Safety check

            TabInfo selectedTabInfo = localListView.SelectedItem as TabInfo;
            if (selectedTabInfo == null) return; // Safety check

            if (selectedTabInfo.Window.Object == null)
            {
                _dte.ItemOperations.OpenFile(selectedTabInfo.DocumentPath, Constants.vsViewKindTextView);
            }
            else
            {
                selectedTabInfo.Window.Activate();
            }
        }
        private void ListViewItem_Selected(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void ClearSelection()
        {

        }

        private void SelectTabGroupInListView(string activatedTabName)
        {

            //List<TabInfo> documentWell = TabGroupsSubsystem.Instance.AllOpenDocuments;


            //TabInfo matchingTabInfo = documentWell.FirstOrDefault(tabInfo => tabInfo.WindowName.Equals(activatedTabName));


            //if (matchingTabInfo == null) return;


            //// Set the flag before changing the selection
            //_bIsSelectionChangeProgrammatic = true;

            //try
            //{
            //    // Find the index
            //    int index = documentWell.IndexOf(matchingTabInfo);

            //    if (index < 0) return;
            //    // Set the selected item
            //    TabGroupsListView.SelectedIndex = index;

            //    // Ensure the item is visible
            //    TabGroupsListView.ScrollIntoView(TabGroupsListView.SelectedItem);
            //}
            //finally
            //{
            //    // Reset the flag after changing the selection
            //    _bIsSelectionChangeProgrammatic = false;
            //}
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

        private void AddFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TabInfo tab = sender as TabInfo;
            if (tab == null) return; // return if this is not of type TabInfo



            //ViewModel.AddFilter(tab, )


        }
    }
}