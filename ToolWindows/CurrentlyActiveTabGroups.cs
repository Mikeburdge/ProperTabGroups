using System.Collections.ObjectModel;
using System.Windows.Controls;
using ProperTabGroups.Scripts;

namespace ProperTabGroups.ToolWindows
{
    public class CurrentlyActiveTabGroups : ObservableCollection<TabGroup>
    {
        public CurrentlyActiveTabGroups()
        {
            var newTabGroup = new TabGroup();

            foreach (var tabInfo in TabGroupsSubsystem.Instance.LocalDocumentWell)
            {
                newTabGroup.TabsInGroup.Add(tabInfo);
            }

            Add(newTabGroup);
        }
    }
}
