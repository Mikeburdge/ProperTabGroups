using ProperTabGroups.Subsystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProperTabGroups.ToolWindows.SearchBar
{
    /// <summary>
    /// Interaction logic for SearchBarControl.xaml
    /// </summary>
    public partial class SearchBarControl : UserControl
    {
        public SearchBarControl()
        {
            InitializeComponent();
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchItem = searchTextBox.Text;

            DocumentWellManagementSubsystem.Instance.SearchBoxText = searchItem;
        }
    }
}
