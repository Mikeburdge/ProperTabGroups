using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using EnvDTE;
using ProperTabGroups.Subsystem;

namespace ProperTabGroups.TabGroupScripts
{
    public class TabGroup : INotifyPropertyChanged
    {
        private ObservableCollection<TabInfo> _tabsInGroupSource;

        public ObservableCollection<TabInfo> TabsInGroupSource
        {
            get => _tabsInGroupSource;
            set
            {
                if (!Equals(_tabsInGroupSource, value))
                {

                    _tabsInGroupSource = value;
                    OnPropertyChanged(nameof(TabsInGroupSource));

                    OnTabsInGroupSourceChanged();
                }
            }
        }

        public CollectionViewSource TabsInGroup { get; set; }
        // Used for filter matching
        public string Name { get; set; }
        public bool BIsLocked { get; set; }
        public bool BIsVisible { get; set; }
        public string ColourCode { get; set; }

        public TabGroup(string name, bool bIsLocked = false, bool bIsVisible = true)
        {
            Name = name;
            BIsLocked = bIsLocked;
            BIsVisible = bIsVisible;

            TabsInGroupSource = new ObservableCollection<TabInfo>();
            TabsInGroup = new CollectionViewSource()
            {
                Source = TabsInGroupSource
            };
        }


        private void OnTabsInGroupSourceChanged()
        {
            TabGroupsSubsystem.Instance.ValidateCurrentGroups();
            TabGroupsSubsystem.Instance.RefreshAllGroupsAndTabs();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TabInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(); // Notify the UI of the change
                }
            }
        }
        public Window Window { get; set; }
        public List<string> Filters { get; set; }

        // Property to hold the window's name
        public string WindowName { get; set; }
        public string DocumentPath { get; set; }
        // Property to hold the window's kind
        public string ViewKind { get; set; }


        public TabInfo(Window window, List<string> filters)
        {
            Window = window;
            Filters = filters;
            WindowName = window.Caption;
            DocumentPath = window.Document.FullName;
            ViewKind = window.Kind;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}
