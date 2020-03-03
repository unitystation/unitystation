﻿using System;
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
        //The theme the player has chosen to use:
        public Dictionary<ThemeType, ThemeConfig> chosenThemes = new Dictionary<ThemeType, ThemeConfig>();
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
                Instance.themeSetQueue.Enqueue(handler);
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

        /// <summary>
        /// Get a list of available configs for a 
        /// Theme Type
        /// </summary>
        public static List<string> GetThemeOptions(ThemeType themeType)
        {
            var list = new List<string>();
            if (Instance.configs.ContainsKey(themeType))
            {
                foreach (ThemeConfig config in Instance.configs[themeType])
                {
                    list.Add(config.themeName);
                }
            }
            return list;
        }

        void SetTheme(ThemeHandler handler)
        {
            if (chosenThemes.ContainsKey(handler.themeType))
            {
                handler.SetTheme(chosenThemes[handler.themeType]);
            }
        }

        [ContextMenu("Load All Configs")]
        public void LoadAllThemes()
        {
            configs.Clear();
            
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
                        }
                    }
                }
                else
                {
                    Logger.LogError($"Theme folder not found: {di.FullName}", Category.Themes);
                }
            }

            //Load the configs that the player has chosen to use:
            LoadUserPreferences();

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
                        //Get all the setting values for this config
                        var values = (YamlMappingNode) c.Value;
                        foreach (var kvp in values)
                        {
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

        // Check player prefs for the users preference
        void LoadUserPreferences()
        {
            //put all initial default preferences for your custom theme types here:
            if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleThemeKey))
            {
                PlayerPrefs.SetString(PlayerPrefKeys.ChatBubbleThemeKey, "Default");
                PlayerPrefs.Save();
            }

            //Set the chosen theme:
            //FIXME put this is some kind of loop later on when we have more themetypes
            SetPreferredTheme(ThemeType.ChatBubbles, PlayerPrefs.GetString(PlayerPrefKeys.ChatBubbleThemeKey));
        }

        /// <summary>
        /// Pass the ThemeType and Theme Name to set the peferred settings for this player
        /// Client Side only!!!
        /// </summary>
        public static void SetPreferredTheme(ThemeType themeType, string themeName)
        {
            if (!Instance.configs.ContainsKey(themeType))
            {
                Logger.LogError($"Theme Type {themeType} not found in ThemeManager", Category.Themes);
                return;
            }

            var index = Instance.configs[themeType].FindIndex(x => string.Equals(x.themeName, themeName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
            {
                Logger.LogError($"Theme not found {themeName}", Category.Themes);
                return;
            }

            Instance.SetPreferredTheme(themeType, Instance.configs[themeType][index]);
        }

        //Update the chosenThemes dictionary with the preferred theme
        void SetPreferredTheme(ThemeType themeType, ThemeConfig config)
        {
            if (!chosenThemes.ContainsKey(themeType))
            {
                chosenThemes.Add(themeType, config);
            }
            else
            {
                chosenThemes[themeType] = config;
            }

            //Save preference to player prefs:
            SavePlayerPrefs(config);
        }

        void SavePlayerPrefs(ThemeConfig config)
        {
            //As keys can be arbitrary values we need this switch case:
            var prefKey = "";
            switch (config.themeType)
            {
                case ThemeType.ChatBubbles:
                    prefKey = PlayerPrefKeys.ChatBubbleThemeKey;
                    break;
                    //Add your keys here!
            }

            PlayerPrefs.SetString(prefKey, config.themeName);
            PlayerPrefs.Save();

            //Invoke theme change events
            ThemeChangeEvent(config.themeType);
        }

        //A new theme has been chosen, update all elements
        void ThemeChangeEvent(ThemeType themeType)
        {
            if (handlers.ContainsKey(themeType))
            {
                foreach (var handler in handlers[themeType])
                {
                    handler.SetTheme(chosenThemes[themeType]);
                }
            }
        }
    }

    public enum UIElement
    {
        Text,
        Image,
		TextMeshProUGUI
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