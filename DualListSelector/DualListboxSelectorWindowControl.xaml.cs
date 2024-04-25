using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProperTabGroups.Subsystem;
using System.Windows;
using System.Windows.Input;
using ProperTabGroups.TabGroupScripts;
using TabInfo = ProperTabGroups.TabGroupScripts.TabInfo;
using System.Windows.Controls;

namespace ProperTabGroups.DualListSelector
{
    public partial class DualListboxSelectorWindowControl
    {
        public ObservableCollection<TabGroup> LeftItems { get; set; } = new ObservableCollection<TabGroup>();
        public ObservableCollection<TabGroup> RightItems { get; set; } = new ObservableCollection<TabGroup>();
        
        private List<TabGroup> LeftItemsOriginal;
        private List<TabGroup> RightItemsOriginal;

        private TabInfo currentTabInfo;

        private DocumentWellManagementSubsystem ViewModel => DocumentWellManagementSubsystem.Instance;

        public DualListboxSelectorWindowControl()
        {
            InitializeComponent();


            //Loaded += (sender, e) =>
            //{
            //    // Getting the mouse position in WPF
            //    Point position = Mouse.GetPosition(this);
            //    Point screenPosition = PointToScreen(position);
            //    Left = screenPosition.X;
            //    Top = screenPosition.Y;
            //};

            LeftListBox.ItemsSource = LeftItems;
            RightListBox.ItemsSource = RightItems;
        }

        public void PopulateInitialItems(TabInfo inTabInfo)
        {
            List<TabGroup> tabGroups = ViewModel.GroupsDocumentWellSource.ToList();
            
            foreach (TabGroup tabGroup in tabGroups.OrderBy(group => group.Name))
            {
                if (inTabInfo.Filters.Contains(tabGroup.GroupGuid))
                {
                    RightItems.Add(tabGroup);
                }
                else
                {
                    LeftItems.Add(tabGroup);
                }
            }

            RightItemsOriginal = RightItems.ToList();
            LeftItemsOriginal = LeftItems.ToList();

            currentTabInfo = inTabInfo;
            TitleTextBlock.Text = string.Concat("Filters for " + currentTabInfo.WindowName);
        }

        private void MoveRight(object sender, RoutedEventArgs e)
        {
            if (LeftListBox.SelectedItem is not TabGroup selectedItem) return;

            LeftItems.Remove(selectedItem);
            RightItems.Add(selectedItem);
        }

        private void MoveLeft(object sender, RoutedEventArgs e)
        {
            if (RightListBox.SelectedItem is not TabGroup selectedItem) return;

            RightItems.Remove(selectedItem);
            LeftItems.Add(selectedItem);
        }

        private void DualListboxSelectorWindowControl_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           DragMove();
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
            Close();
        }

        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
        }

        private void ApplyChanges()
        {
            if (LeftItemsOriginal == LeftItems.OrderBy(group => group.Name).ToList() 
                && RightItemsOriginal == RightItems.OrderBy(group => group.Name).ToList())
            {
                return;
            }

            foreach (TabGroup leftItem in LeftItems)
            {
                ViewModel.RemoveFilterFromTab(currentTabInfo, leftItem.GroupGuid);
            }

            foreach (TabGroup rightItem in RightItems)
            {
                ViewModel.AddFilterToTab(currentTabInfo, rightItem.GroupGuid);
            }
        }
    }
}