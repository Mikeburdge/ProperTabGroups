using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace ProperTabGroups.Scripts
{
    public class TabGroup
    {
        public List<TabInfo> TabsInGroup { get; set; }
        public List<string> ThisGroupsFilters { get; set; }
        public string Name { get; set; }
        public bool bIsLocked { get; set; }
        public bool IsVisible { get; set; }
        public string ColourCode { get; set; }
    }

    public class TabInfo
    {
        public Window Window { get; set; }
        public string[] Filters { get; set; }
        // Property to hold the window's name
        public string WindowName => Window.Caption;

        public TabInfo(Window window, string[] filters)
        {
            Window = window;
            Filters = filters;
        }
    }

}
