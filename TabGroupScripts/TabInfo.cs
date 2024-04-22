using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ProperTabGroups.Subsystem;
using Window = EnvDTE.Window;

namespace ProperTabGroups.TabGroupScripts
{
    public class TabGroup : INotifyPropertyChanged
    {
        //////////////////////////////////////////
        // Debounce Variables
        //////////////////////////////////////////

        private DispatcherTimer debounceTimer = new DispatcherTimer();

        //////////////////////////////////////////
        // Tabs In Group Section
        //////////////////////////////////////////
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


        //////////////////////////////////////////
        // Tab Group Public Variables
        //////////////////////////////////////////
        public string Name { get; set; }
        public Guid GroupGuid { get; set; }
        public bool BIsLocked { get; set; }
        public bool BIsVisible { get; set; }
        public string ColourCode { get; set; }
        public int ItemCount => TabsInGroupSource.Count;

        //////////////////////////////////////////
        // Constructor
        //////////////////////////////////////////
        public TabGroup(string name, bool bIsLocked = false, bool bIsVisible = true, Guid inGuid = default)
        {
            Name = name;
            GroupGuid = inGuid == default ? Guid.NewGuid() : inGuid;
            BIsLocked = bIsLocked;
            BIsVisible = bIsVisible;

            TabsInGroupSource = new ObservableCollection<TabInfo>();
            TabsInGroup = new CollectionViewSource()
            {
                Source = TabsInGroupSource
            };

            TabsInGroup.SortDescriptions.Add(new SortDescription(nameof(TabInfo.WindowName), ListSortDirection.Ascending));

            //////////////////////////////////////////
            // Debounce Timer Implementation
            //////////////////////////////////////////
            debounceTimer.Interval = TimeSpan.FromMilliseconds(100); // Possibly needs adjusting.
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                RefreshUI();
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void TabsInGroupSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {   
            // Restart the timer on each collection change
            debounceTimer.Stop();
            debounceTimer.Start();
        }
        private static void RefreshUI()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => 
            {
                // Code to update the UI safely
                DocumentWellManagementSubsystem.RefreshAllTabsView();
            }), DispatcherPriority.Background);
        }
    }

    public enum TabState
    {
        Grouped,
        Unassigned,
        Invalid
    }

    public class TabInfo : INotifyPropertyChanged
    {
        // todo: probably for the best to create a non-persistent guid. one that is given at the start of the session and used to identify tabd within sessions. MAYBE
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

        private ObservableCollection<Guid> _filters;
        public ObservableCollection<Guid> Filters
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

        //////////////////////////////////////////
        // Debounce Variables
        //////////////////////////////////////////

        private DispatcherTimer debounceTimer = new DispatcherTimer();

        //////////////////////////////////////////
        // Public Tab Info Variables
        //////////////////////////////////////////
        /// 
        public string WindowName { get; set; }
        public string DocumentPath { get; set; }
        public string ViewKind { get; set; }
        public TabState State { get; set; }


        public TabInfo()
        {

        }
        public TabInfo(Window window, ObservableCollection<Guid> filters)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Window = window;
            Filters = filters;
            if (window != null)
            {
                WindowName = window.Caption;
            }
            DocumentPath = window.Document.FullName;
            ViewKind = window.Kind;
            State = TabState.Invalid;

            //////////////////////////////////////////
            // Debounce Timer Implementation
            //////////////////////////////////////////
            debounceTimer.Interval = TimeSpan.FromMilliseconds(100); // Possibly needs adjusting.
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                RealignTabs();
            };
        }

        private void RealignTabs()
        {
            DocumentWellManagementSubsystem.Instance.RealignTabsToFilteredGroups();
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
            debounceTimer.Stop();
            debounceTimer.Start();
        }
    }

    public class SerializableTabGroup
    {
        public string Name { get; set; }
        public Guid GroupGuid { get; set; }
        public bool BIsLocked { get; set; }
        public bool BIsVisible { get; set; }
        public string ColourCode { get; set; }
        public List<SerializableTabInfo> Tabs { get; set; } = new List<SerializableTabInfo>();
    }
    public class SerializableTabInfo
    {
        public bool IsSelected { get; set; }
        public string WindowName { get; set; }
        public string DocumentPath { get; set; }
        public string ViewKind { get; set; }
        public List<Guid> Filters { get; set; } = new List<Guid>();
    }

}
