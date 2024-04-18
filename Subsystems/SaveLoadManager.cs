using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProperTabGroups.Subsystem;
using ProperTabGroups.TabGroupScripts;

using EnvDTE;
using Solution = EnvDTE.Solution;

namespace ProperTabGroups.Subsystems
{
    /// <summary>
    /// Handles persistence of tab group configurations for the ProperTabGroups extension.
    /// </summary>
    internal class SaveLoadManager
    {

        private static SaveLoadManager _instance;
        public static SaveLoadManager Instance => _instance ??= new SaveLoadManager();

        private string settingsFilePath;
        private DTE _dte;

        public SaveLoadManager()
        {
            Initialise();
        }

        private void Initialise()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = (DTE)Package.GetGlobalService(typeof(DTE));
            _dte.Events.SolutionEvents.Opened += SolutionOpened;
        }


        public void SolutionOpened()
        {
            InitSaveLoadManager();
        }
        public void InitSaveLoadManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string solutionName = Path.GetFileNameWithoutExtension(_dte.Solution.FullName);
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string extensionFolder = Path.Combine(appDataPath, "ProperTabGroups", solutionName);
            if (!Directory.Exists(extensionFolder))
            {
                Directory.CreateDirectory(extensionFolder);
            }
            settingsFilePath = Path.Combine(extensionFolder, "settings.json");
        }

        public void SaveTabGroups()
        {
            SaveTabGroups(DocumentWellManagementSubsystem.Instance.GroupsDocumentWellSource);
        }

        private void SaveTabGroups(IEnumerable<TabGroup> tabGroups)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                List<SerializableTabGroup> serializableTabGroups = tabGroups.Select(tg => new SerializableTabGroup
                {
                    Name = tg.Name,
                    GroupGuid = tg.GroupGuid,
                    BIsLocked = tg.BIsLocked,
                    BIsVisible = tg.BIsVisible,
                    ColourCode = tg.ColourCode,
                    Tabs = tg.TabsInGroupSource.Select(ti => new SerializableTabInfo
                    {
                        WindowName = ti.WindowName,
                        DocumentPath = ti.DocumentPath,
                        Filters = ti.Filters.ToList() // Assuming this is serializable as is
                    }).ToList()
                }).ToList();

                string json = JsonConvert.SerializeObject(serializableTabGroups, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving tab groups: {ex.Message}");
            }
        }

        public List<TabGroup> LoadTabGroupsFromJson()
        {
            // Deserialize the JSON back into the list of serializable TabGroups
            List<TabGroup> tabGroups = new List<TabGroup>();

            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);

                    List<SerializableTabGroup> serializableTabGroups = JsonConvert.DeserializeObject<List<SerializableTabGroup>>(json);

                    if (serializableTabGroups == null)
                    {
                        return new List<TabGroup>();
                    }

                    IEnumerable<Window> allActiveDocuments = _dte.Windows.Cast<Window>().Where(window => window.Kind is "Document");

                    foreach (SerializableTabGroup serializableTabGroup in serializableTabGroups)
                    {
                        TabGroup tabGroup = new(serializableTabGroup.Name, serializableTabGroup.BIsLocked,
                            serializableTabGroup.BIsVisible, serializableTabGroup.GroupGuid)
                        {
                            ColourCode = serializableTabGroup.ColourCode
                        };

                       

                        foreach (SerializableTabInfo serializableTabInfo in serializableTabGroup.Tabs)
                        {
                            Window matchingTabWindow = allActiveDocuments.FirstOrDefault(x => x.Caption == serializableTabInfo.WindowName);

                            TabInfo tabInfo = new TabInfo()
                            {
                                //IsSelected = serializableTabInfo.IsSelected,
                                WindowName = serializableTabInfo.WindowName,
                                Window = matchingTabWindow, // If this is null anyway it means that when we click to open it it "should" open it safely
                                DocumentPath = serializableTabInfo.DocumentPath,
                                ViewKind = serializableTabInfo.ViewKind,
                                Filters = new ObservableCollection<Guid>(serializableTabInfo.Filters)
                            };

                            tabGroup.TabsInGroupSource.Add(tabInfo);
                        }

                        tabGroups.Add(tabGroup);
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tab groups: {ex.Message}");
            }
            return tabGroups;
        }
    }
}
