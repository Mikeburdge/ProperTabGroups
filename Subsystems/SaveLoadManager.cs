
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using ProperTabGroups.Subsystem;
using ProperTabGroups.TabGroupScripts;

using EnvDTE;

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
        private DTE dte;
        public Package package;

        private bool _isInitialised = false;

        private SaveLoadManager() { }

        public List<TabGroup> InitSaveLoadManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string extensionFolder = Path.Combine(appDataPath, "ProperTabGroups");
            if (!Directory.Exists(extensionFolder))
            {
                Directory.CreateDirectory(extensionFolder);
            }
            settingsFilePath = Path.Combine(extensionFolder, "settings.json");

            // Listen for Visual Studio shutdown event
            dte.Events.DTEEvents.OnBeginShutdown += SaveTabGroupsOnShutdown;

            _isInitialised = true;

            return LoadTabGroups();
        }

        private void SaveTabGroupsOnShutdown()
        {
            System.Diagnostics.Debug.Assert(_isInitialised, "SaveLoadManager has not been initialised");

            List<TabGroup> tabGroups = DocumentWellManagementSubsystem.Instance.GetAllTabGroups().ToList();
            SaveTabGroups(tabGroups);
        }

        private void SaveTabGroups(IReadOnlyCollection<TabGroup> tabGroups)
        {
            System.Diagnostics.Debug.Assert(_isInitialised, "SaveLoadManager has not been initialised");
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                string json = JsonConvert.SerializeObject(tabGroups, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving tab groups: {ex.Message}");
            }
        }

        public List<TabGroup> LoadTabGroups()
        {
            System.Diagnostics.Debug.Assert(_isInitialised, "SaveLoadManager has not been initialised");
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    return JsonConvert.DeserializeObject<List<TabGroup>>(json) ?? new List<TabGroup>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tab groups: {ex.Message}");
            }

            return new List<TabGroup>();
        }
    }
}
