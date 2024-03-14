using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace ProperTabGroups.Scripts
{
    public class TabGroup()
    {
        public TabInfo[] TabsInGroup;
        public string[] ThisGroupsFilters;

    }

    public class TabInfo(Window window, string[] filters)
    {
        public Window Window = window;
        public string[] Filters = filters;
    }
}
