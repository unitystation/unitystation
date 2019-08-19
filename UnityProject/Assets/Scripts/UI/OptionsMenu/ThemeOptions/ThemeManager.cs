using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace Unitystation.Options
{
    /// <summary>
    /// Handles everything to do with Themes
    /// Add a ThemeHandler to your customizable UI elements
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        private static ThemeManager themeManager;
        public static ThemeManager Instance
        {
            get
            {
                if (themeManager == null)
                {
                    themeManager = FindObjectOfType<ThemeManager>();
                }
                return themeManager;
            }
        }

        //Add the root folder paths for each config type here:
        private static string[] folderPaths = new string[] { "ChatBubbleThemes" };
        //Directory info list of each folder path
        private List<DirectoryInfo> diPaths = new List<DirectoryInfo>();
        private Dictionary<ThemeType, List<ThemeHandler>> handlers = new Dictionary<ThemeType, List<ThemeHandler>>();
        //In case there are any hold ups with initialization, do init work via a queue
        private Queue<ThemeHandler> themeSetQueue = new Queue<ThemeHandler>();
        //All the loaded configs:
        private Dictionary<ThemeType, List<ThemeConfig>> configs = new Dictionary<ThemeType, List<ThemeConfig>>();
        //Set to true when all the themes have been successfully loaded
        private bool themesLoaded = false;

        void Awake()
        {
            //Create DirectoryInfo's for each folder path for ease of use
            diPaths.Clear();
            foreach (string p in folderPaths)
            {
                diPaths.Add(new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, $"Themes/{p}")));
            }

            LoadAllThemes();
        }

        public static void RegisterHandler(ThemeHandler handler)
        {
            if (!Instance.handlers.ContainsKey(handler.themeType))
            {
                Instance.handlers.Add(handler.themeType, new List<ThemeHandler>());
            }

            if (!Instance.handlers[handler.themeType].Contains(handler))
            {
                Instance.handlers[handler.themeType].Add(handler);
            }
        }

        public static void UnregisterHandler(ThemeHandler handler)
        {
            if (Instance.handlers.ContainsKey(handler.themeType))
            {
                if (Instance.handlers[handler.themeType].Contains(handler))
                {
                    Instance.handlers[handler.themeType].Remove(handler);
                }
            }
        }

        void Update()
        {
            if (themesLoaded)
            {
                if (themeSetQueue.Count > 0)
                {
                    SetTheme(themeSetQueue.Dequeue());
                }
            }
        }

        void SetTheme(ThemeHandler handler)
        {

        }

        [ContextMenu("Load All Configs")]
        void LoadAllThemes()
        {
            foreach (DirectoryInfo di in diPaths)
            {
                if (di.Exists)
                {
                    var files = di.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.Equals(".yaml", System.StringComparison.OrdinalIgnoreCase))
                        {
                            LoadThemeFile(file);
                            Debug.Log("Load theme: " + file.Name);
                        }
                    }
                }
                else
                {
                    Logger.LogError($"Theme folder not found: {di.FullName}", Category.Themes);
                }
            }
            themesLoaded = true;
        }

        //Loads one theme file into the config dictionary
        void LoadThemeFile(FileInfo file)
        {
            var config = file.OpenText();
            var yaml = new YamlStream();
            yaml.Load(config);

            //Examine the yaml file:
            var mapping = (YamlMappingNode) yaml.Documents[0].RootNode;
            foreach (var entry in mapping.Children)
            {
                var nodeName = ((YamlScalarNode) entry.Key).Value;
                Debug.Log(nodeName);
                if (string.IsNullOrEmpty(nodeName))
                {
                    Logger.LogError($"No Theme Type found for {nodeName}", Category.Themes);
                    continue;
                }

                //Get the theme type from the node name
                ThemeType theme = (ThemeType) Enum.Parse(typeof(ThemeType), nodeName, true);
                //See if we have a list set up for this theme type
                if (!configs.ContainsKey(theme))
                {
                    configs.Add(theme, new List<ThemeConfig>());
                }

                //Get all the config names and their settings associated with this Theme type in this file
                var settings = (YamlMappingNode) entry.Value;
                foreach (var c in settings.Children)
                {
                    var cfg = new ThemeConfig();
                    cfg.themeType = theme;
                    cfg.themeName = ((YamlScalarNode) c.Key).Value;

                    //check to see if that name is already in use:
                    var index = configs[theme].FindIndex(x => x.themeName == cfg.themeName);
                    if (index == -1)
                    {
                        Debug.Log($"{cfg.themeName} theme config added for {cfg.themeType.ToString()}");

                        //Get all the setting values for this config
                        var values = (YamlMappingNode) c.Value;
                        foreach (var kvp in values)
                        {
                            Debug.Log($"Key: {kvp.Key.ToString()} Value: {kvp.Value.ToString()}");

                            //Load hex colors into Color
                            if (kvp.Key.ToString().Contains("ImageColor"))
                            {
                                if (!ColorUtility.TryParseHtmlString(kvp.Value.ToString(), out cfg.imageColor))
                                {
                                    Logger.LogError($"Failed to parse html color {kvp.Value.ToString()}", Category.Themes);
                                }
                            }

                            if (kvp.Key.ToString().Contains("TextColor"))
                            {

                                if (!ColorUtility.TryParseHtmlString(kvp.Value.ToString(), out cfg.textColor))
                                {
                                    Logger.LogError($"Failed to parse html color {kvp.Value.ToString()}", Category.Themes);
                                }
                            }
                        }
                        configs[theme].Add(cfg);
                    }
                    else
                    {
                        Logger.LogError($"There is already a config named {cfg.themeName} in {theme.ToString()}", Category.Themes);
                    }
                }
            }
        }
    }

    public enum UIElement
    {
        Text,
        Image
        //Add new elements that can be customized here
        //Then you need to open ThemeHandlerEditor.cs
        //and add the inspector gui for it (follow examples already there)
    }

    /// <summary>
    /// Add more fields as needed
    /// Values are loaded into the config which is then
    /// Stored in a dictionary on ThemeManager
    /// </summary>
    public class ThemeConfig
    {
        public ThemeType themeType;
        public string themeName;
        public Color imageColor;
        public Color textColor;
    }
}