using ProperTabGroups.Subsystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ProperTabGroups.ToolWindows.OptionsWindow
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : UserControl
    {
        public OptionsWindow()
        {
            InitializeComponent();
            //try
            //{
            //    ChevronDown = new BitmapImage(new Uri("/pack://application:,,,/Resources/ChevronDown.png"));
            //    ChevronUp = new BitmapImage(new Uri("/pack://application:,,,/Resources//ChevronUp.png"));
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("Error loading images: " + ex.Message);
            //}

        }


        //private BitmapImage ChevronDown;
        //private BitmapImage ChevronUp;


        //private bool isChevronDown = true; // State tracking variable
        private void ToggleImageButton_Click(object sender, RoutedEventArgs e)
        {
            //OpenCloseButtonImage.Source = isChevronDown ? ChevronUp : ChevronDown;
            //isChevronDown = !isChevronDown; // Toggle the state

            OptionsPanel.Visibility = OptionsPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
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

            DocumentWellManagementSubsystem.Instance.CreateNewTabGroup(userInput);
        }

        private void RefreshAll(object sender, RoutedEventArgs e)
        {
            DocumentWellManagementSubsystem.RefreshAll();
        }
    }
}
