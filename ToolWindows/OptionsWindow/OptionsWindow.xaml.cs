using ProperTabGroups.Subsystem;
using ProperTabGroups.Subsystems;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = System.Windows.Forms.MessageBox;

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
            DocumentWellManagementSubsystem.Instance.RefreshAll();
        }

        private void SaveAll_Clicked(object sender, RoutedEventArgs e)
        {
            SaveLoadManager.Instance.SaveTabGroups();
        }

        private void LoadAll_Clicked(object sender, RoutedEventArgs e)
        {
            DocumentWellManagementSubsystem.Instance.LoadAllIntoDocumentWell();
        }

        private void Export_Clicked(object sender, RoutedEventArgs e)
        {
            string path = OpenSaveFileDialog();

            if (!string.IsNullOrEmpty(path))
            {
                SaveLoadManager.Instance.SaveTabGroups(path);
            }
        }
        private void Import_Clicked(object sender, RoutedEventArgs e)
        {
            string path = SelectJsonFilePath();
            if (!string.IsNullOrEmpty(path))
            {
                DocumentWellManagementSubsystem.Instance.LoadAllIntoDocumentWell(path);
            }
        }

        private static string SelectJsonFilePath()
        {
            string projectAppDataDirectory = SaveLoadManager.Instance.GetDefaultDirectory();

            // Create a new instance of CommonOpenFileDialog
            using CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select a JSON File";
            dialog.Filters.Add(new CommonFileDialogFilter("Settings Files", "*.json"));
            dialog.EnsureFileExists = true;  // Ensure the file must exist
            dialog.InitialDirectory = projectAppDataDirectory;


            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }
        private static string OpenSaveFileDialog()
        {
            using (var dialog = new CommonSaveFileDialog())
            {

                string projectAppDataDirectory = SaveLoadManager.Instance.GetDefaultDirectory();

                dialog.Title = "Save Tab Groups";
                dialog.DefaultFileName = "TabGroups";  // Default file name
                dialog.DefaultExtension = "json";  // Default file extension
                dialog.Filters.Add(new CommonFileDialogFilter("JSON Files", "*.json"));
                dialog.AlwaysAppendDefaultExtension = true;  // Ensure .json is always appended
                dialog.InitialDirectory = projectAppDataDirectory;

                return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
            }
        }
    }
}
