using RedditScraper.AppSettings;
using RedditScraper.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RedditScraper.Services
{
    public class FileService : IFileService
    {
        private string programFolder;

        public FileService()
        {
            programFolder = AppDomain.CurrentDomain.BaseDirectory;

        }

        // Check and create required folders and files
        public void SetupFolders()
        {
            // List folders to be created
            List<string> folders = new List<string>
            {
                "Data", // For data
                "App", // General app stuff, settings etc
                "Browsers" // Browser installation location
            };

            // Loop through all folders
            for (int i = 0; i != folders.Count; i++)
            {
                string folder = $"{programFolder}{folders[i]}";

                // If folder doesn't exist, create it 
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory($"{programFolder}{folders[i]}");
                }

            }
        }

        // Add all required files & folders under corresponding folder lists
        public void SetupFiles()
        {
            // List of required files
            List<string> dataFolderFiles = new List<string>
            {
                "content.csv"
            };

            List<string> appFolderFiles = new List<string>
            {
                "settings.json",
            };

            // Loop through all FolderFiles lists
            Dictionary<string, List<String>> folderDictionary = new Dictionary<string, List<String>>
            {
                { "Data", dataFolderFiles },
                { "App", appFolderFiles },
            };

            foreach (var entry in folderDictionary)
            {
                foreach (var file in entry.Value)
                {
                    var foundFiles = Directory.GetFiles($"{programFolder}{entry.Key}", file);

                    if (foundFiles.Count() == 0)
                    {
                        using (File.Create($"{programFolder}{entry.Key}\\{file}")) ;

                        // Further prepare certain files
                        switch (file)
                        {
                            // Write basic json object to file
                            case "settings.json":
                                // Get file location
                                var jsonFile = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}{entry.Key}", file, SearchOption.AllDirectories).FirstOrDefault();

                                if (jsonFile == null)
                                    continue;

                                // Convert settings object to json string
                                var jsonString = JsonSerializer.Serialize(new Settings());

                                // Write json string to file
                                File.WriteAllText(jsonFile, jsonString);

                                break;
                        }
                    }
                }
            }
        }

        public string LocateTor()
        {
            // Check if tor has already been located
            Settings settings = SettingsManager.ReadSettings();

            if (settings.Enviroment.TorPath != null && settings.Enviroment.TorPath != "")
                return settings.Enviroment.TorPath;

            var drives = Directory.GetLogicalDrives();

            // Scan each drive found
            foreach (var drive in drives)
            {
                // Scan each drive ignoring inaccessible folders
                var files = Directory.GetFiles(drive, "firefox.exe", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                });

                // Make sure we found tor instead of firefox by locating TorBorwser folder in the same folder as firefox.exe
                foreach (var file in files)
                {
                    var filepath = file.Substring(0, file.IndexOf(".exe") - 7);
                    var foldersInDirectory = Directory.GetDirectories(filepath);

                    foreach (var folder in foldersInDirectory)
                    {
                        if (folder.Contains("TorBrowser"))
                        {
                            settings.Enviroment.TorPath = folder;
                            SettingsManager.SaveSettings(settings);

                            return file;
                        }
                    }
                }
            }

            return "";
        }


    }
}
