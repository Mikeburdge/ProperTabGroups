using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
                if (Equals(_tabsInGroupSource, value)) return;

                if (_tabsInGroupSource != null)
                {
                    // Unsubscribe from the CollectionChanged event of the old collection
                    _tabsInGroupSource.CollectionChanged -= TabsInGroupSource_CollectionChanged;
                }

                _tabsInGroupSource = value;
                OnPropertyChanged();

                if (_tabsInGroupSource != null)
                {
                    // Subscribe to the CollectionChanged event of the new collection
                    _tabsInGroupSource.CollectionChanged += TabsInGroupSource_CollectionChanged;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void TabsInGroupSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ProperTabGroupsSubsystem.RefreshAllTabsView();
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

        private ObservableCollection<string> _filters;
        public ObservableCollection<string> Filters
        {
            get => _filters;
            set
            {
                if (Equals(_filters, value)) return;

                if (_filters != null)
                {
                    // Unsubscribe from the CollectionChanged event of the old collection
                    _filters.CollectionChanged -= Filters_CollectionChanged;
                }

                _filters = value;
                OnPropertyChanged();

                if (_filters != null)
                {
                    // Subscribe to the CollectionChanged event of the new collection
                    _filters.CollectionChanged += Filters_CollectionChanged;
                }

            }
        }

        // Property to hold the window's name
        public string WindowName { get; set; }
        public string DocumentPath { get; set; }
        // Property to hold the window's kind
        public string ViewKind { get; set; }


        public TabInfo(Window window, ObservableCollection<string> filters)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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

        private void Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Currently calling the same thing as all we need to do is refresh but this has functionality for later in case we need to do more.
            ProperTabGroupsSubsystem.Instance.RealignTabsToFilteredGroups();
        }

    }

}
