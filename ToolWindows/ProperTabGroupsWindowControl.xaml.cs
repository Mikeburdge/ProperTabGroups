using System.Collections.Generic;
using ProperTabGroups.Scripts;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Window = EnvDTE.Window;

namespace ProperTabGroups
{
    public partial class ProperTabGroupsWindowControl : UserControl
    {
        public ObservableCollection<TabGroup> TabGroups { get; set; }
        public ProperTabGroupsWindowControl()
        {
            InitializeComponent();

            // Set the DataContext for the window
            DataContext = new MainWindowViewModel();
        }

        // Define the ViewModel for your window
        public class MainWindowViewModel
        {
            public ObservableCollection<TabInfo> Tabs { get; set; }

            public MainWindowViewModel()
            {
                // Initialize the ObservableCollection
                Tabs = new ObservableCollection<TabInfo>();

                var listOfWindows = ProperTabGroupsPackage.AllOpenDocumentWindows;
                
                foreach (var window in listOfWindows)
                {
                    Tabs.Add(new TabInfo(window, []));
                }
            }
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}