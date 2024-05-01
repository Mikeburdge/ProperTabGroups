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

        private string defaultSettingsFilePath;
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

            defaultSettingsFilePath = Path.Combine(extensionFolder, "settings.json");
        }

        public void SaveTabGroups()
        {
            SaveTabGroups(defaultSettingsFilePath);
        }

        public void SaveTabGroups(string pathToSave)
        {
            IEnumerable<TabGroup> tabGroups = DocumentWellManagementSubsystem.Instance.GroupsDocumentWellSource;
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                List<SerializableTabGroup> serializableTabGroupsObject = tabGroups.Select(tg => new SerializableTabGroup
                {
                    Name = tg.Name,
                    GroupGuid = tg.GroupGuid,
                    BIsLocked = tg.BIsLocked,
                    BIsVisible = tg.BIsVisible,
                    ColourCode = tg.ColourCode,
                }).ToList();

                List<SerializableTabInfo> serializableTabInfosObject = DocumentWellManagementSubsystem.Instance
                    .AllTabInfos
                    .Where(ti => ti.Filters.Any()).Select(tabInfo => new SerializableTabInfo()
                    {
                        WindowName = tabInfo.WindowName,
                        DocumentPath = tabInfo.DocumentPath,
                        ViewKind = tabInfo.ViewKind,
                        Filters = tabInfo.Filters.ToList()
                    }).ToList();

                SerializableProperTabCollection serializableProperTabCollection = new SerializableProperTabCollection()
                {
                    serializableTabGroups = serializableTabGroupsObject,
                    serializableTabInfos = serializableTabInfosObject
                };

                string json = JsonConvert.SerializeObject(serializableProperTabCollection, Formatting.Indented);
                File.WriteAllText(pathToSave, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving tab groups: {ex.Message}");
            }
        }

        public bool LoadTabGroupsFromJson(ref List<TabGroup> outTabGroups, ref List<TabInfo> outTabInfos)
        {
            return LoadTabGroupsFromJson(ref outTabGroups, ref outTabInfos, defaultSettingsFilePath);
        }

        public bool LoadTabGroupsFromJson(ref List<TabGroup> outTabGroups, ref List<TabInfo> outTabInfos, string pathToUse)
        {
            // Deserialize the JSON back into the list of serializable TabGroups
            List<TabGroup> tabGroups = new List<TabGroup>();
            List<TabInfo> allLoadedTabs = new List<TabInfo>();

            try
            {
                if (File.Exists(pathToUse))
                {
                    string json = File.ReadAllText(pathToUse);

                    SerializableProperTabCollection tabGroupsObject =
                        JsonConvert.DeserializeObject<SerializableProperTabCollection>(json);

                    if (tabGroupsObject == null)
                    {
                        return false;
                    }

                    List<SerializableTabGroup> serializableTabGroups = tabGroupsObject.serializableTabGroups;
                    List<SerializableTabInfo> serializableTabInfos = tabGroupsObject.serializableTabInfos;

                    if (serializableTabGroups != null)
                    {
                        foreach (SerializableTabGroup serializableTabGroup in serializableTabGroups)
                        {
                            TabGroup tabGroup = new(serializableTabGroup.Name, serializableTabGroup.BIsLocked,
                                serializableTabGroup.BIsVisible, serializableTabGroup.GroupGuid)
                            {
                                ColourCode = serializableTabGroup.ColourCode
                            };

                            tabGroups.Add(tabGroup);
                        }
                    }

                    List<Window> allActiveDocuments = _dte.Windows.Cast<Window>().Where(window => window.Kind is "Document").ToList();


                    foreach (SerializableTabInfo serializableTabInfo in serializableTabInfos)
                    {
                        Window matchingTabWindow =
                            allActiveDocuments.FirstOrDefault(x => x.Caption == serializableTabInfo.WindowName);

                        TabInfo tabInfo;

                        if (matchingTabWindow == null)
                        {
                            tabInfo = new TabInfo()
                            {
                                //IsSelected = serializableTabInfo.IsSelected
                                WindowName = serializableTabInfo.WindowName,
                                Window =
                                    null, // If this is null anyway it means that when we click to open it, it "should" open it safely
                                DocumentPath = serializableTabInfo.DocumentPath,
                                ViewKind = serializableTabInfo.ViewKind,
                                Filters = new ObservableCollection<Guid>(serializableTabInfo.Filters)
                            };
                        }
                        else
                        {
                            tabInfo = new TabInfo()
                            {
                                WindowName = matchingTabWindow.Caption,
                                Window = matchingTabWindow,
                                DocumentPath = matchingTabWindow.Document.FullName,
                                ViewKind = matchingTabWindow.Kind,
                                Filters = new ObservableCollection<Guid>(serializableTabInfo.Filters)
                            };
                        }

                        allLoadedTabs.Add(tabInfo);
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tab groups: {ex.Message}");
            }

            outTabGroups = tabGroups;
            outTabInfos = allLoadedTabs;
            return true;
        }
    }
}
