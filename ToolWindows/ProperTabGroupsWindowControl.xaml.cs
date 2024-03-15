using System.Collections.Generic;
using ProperTabGroups.Scripts;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;
using Window = EnvDTE.Window;
using ProperTabGroups.ToolWindows;

namespace ProperTabGroups
{
    public class MainWindowViewModel
    {
        public List<TabInfo> Tabs { get; set; }

        public MainWindowViewModel()
        {
            // Initialize the ObservableCollection
            Tabs = new List<TabInfo>();

            var documentWell = TabGroupsSubsystem.Instance.LocalDocumentWell;

            foreach (var tabInfo in documentWell)
            {
                Tabs.Add(tabInfo);
            }
        }
    }

    public partial class ProperTabGroupsWindowControl : UserControl
    {
        public List<TabGroup> TabGroups { get; set; }
        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            TabGroupsSubsystem tabsSubsystem = TabGroupsSubsystem.Instance;

            // Set the DataContext for the window
            DataContext = tabsSubsystem;
        }


        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}