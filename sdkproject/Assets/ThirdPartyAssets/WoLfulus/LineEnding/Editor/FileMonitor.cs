using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

//#pragma warning disable 0414
namespace WoLfulus.LineEnding
{
    /// <summary>
    /// Initialize on load
    /// </summary>
    [InitializeOnLoad]
    public class FileMonitor
    {

        private const string WindowsStyle = "\r\n";
        private const string UnixStyle = "\n";
        private const string MacStyle = "\r";

        private const string MenuPrefix = "Tools/Line Endings Fixer/";

        private const string MenuWindows = MenuPrefix + "Windows";
        private const string MenuUnix = MenuPrefix + "Unix";
        private const string MenuMac = MenuPrefix + "Mac OSX";

        private const string ConfigurationId = "WoLfulus_LEF_Type";

        /// <summary>
        /// Initializer
        /// </summary>
        static FileMonitor()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.HasKey(ConfigurationId))
                {
                    EditorPrefs.SetString(ConfigurationId, "win");
                }

                Menu.SetChecked(MenuWindows, false);
                Menu.SetChecked(MenuUnix, false);
                Menu.SetChecked(MenuMac, false);

                var type = EditorPrefs.GetString(ConfigurationId);
                if (type == "win")
                {
                    Menu.SetChecked(MenuWindows, true);
                }
                else if (type == "unix")
                {
                    Menu.SetChecked(MenuUnix, true);
                }
                else if (type == "mac")
                {
                    Menu.SetChecked(MenuMac, true);
                }
            };
        }

        /// <summary>
        /// Windows style
        /// </summary>
        [MenuItem(MenuWindows)]
        private static void SetWindows()
        {
            Menu.SetChecked(MenuWindows, true);
            Menu.SetChecked(MenuUnix, false);
            Menu.SetChecked(MenuMac, false);
            Debug.Log("Line endings changed to Windows");
            EditorPrefs.SetString(ConfigurationId, "win");
        }

        /// <summary>
        /// Windows style
        /// </summary>
        [MenuItem(MenuUnix)]
        private static void SetUnix()
        {
            Menu.SetChecked(MenuWindows, false);
            Menu.SetChecked(MenuUnix, true);
            Menu.SetChecked(MenuMac, false);
            Debug.Log("Line endings changed to Unix");
            EditorPrefs.SetString(ConfigurationId, "unix");
        }

        /// <summary>
        /// Windows style
        /// </summary>
        [MenuItem(MenuMac)]
        private static void SetMac()
        {
            Menu.SetChecked(MenuWindows, false);
            Menu.SetChecked(MenuUnix, false);
            Menu.SetChecked(MenuMac, true);
            Debug.Log("Line endings changed to Mac OSX");
            EditorPrefs.SetString(ConfigurationId, "mac");
        }

        /// <summary>
        /// Collect files from log entries
        /// </summary>
        private static IEnumerable<string> Collect()
        {
            var files = new List<string>();
            var flags = LogEntries.consoleFlags;

            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, false);
            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, true);
            LogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, false);

            LogEntries.StartGettingEntries();

            var count = LogEntries.GetCount();
            for (int i = 0; i < count; i++)
            {
                LogEntries.GetEntryInternal(i, LogEntry.instance);
                if ((LogEntry.mode & Mode.AssetImportWarning) != 0)
                {
                    var condition = LogEntry.condition;
                    if (!string.IsNullOrEmpty(condition) && condition.Contains("inconsistent line endings"))
                    {
                        files.Add(LogEntry.file);
                    }
                }
            }

            LogEntries.EndGettingEntries();
            LogEntries.consoleFlags = flags;

            return files.ToArray();
        }

        /// <summary>
        /// Fix stuff
        /// </summary>
        static void FixFiles()
        {
            var files = Collect();

            var endingType = EditorPrefs.GetString(ConfigurationId);
            var ending = "\r\n";

            if (endingType == "win")
            {
                ending = WindowsStyle;
            }
            else if (endingType == "unix")
            {
                ending = UnixStyle;
            }
            else if (endingType == "mac")
            {
                ending = MacStyle;
            }
            else
            {
                Debug.Log("Line Endings Fixer settings not detected. You might want to select a line ending style.");
                return;
            }

            int filesFixed = 0;

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    Debug.LogError("File '" + file + "' is reported to have wrong line endings but the file itself couldn't be found.");
                    continue;
                }

                var fileContents = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", ending);
                File.WriteAllText(file, fileContents);

                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
                };

                filesFixed++;
            }

            if (filesFixed > 0)
            {
                Debug.Log("Fixed " + filesFixed + " files with mixed line endings.");
            }
        }

        /// <summary>
        /// Scripts reloaded
        /// </summary>
        [DidReloadScripts]
        static void ScriptsReloaded()
        {
            if (!EditorApplication.isPlaying)
            {
                FixFiles();
            }
        }
    }
}
