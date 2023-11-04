using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AddressableReferences;
using SecureStuff;
using Initialisation;
using Logs;
using Shared.Util;
using UnityEngine;
using UnityEngine.UI;
using Util;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Unitystation.Options
{
    /// <summary>
    /// Handles everything to do with Themes
    /// Add a ThemeHandler to your customizable UI elements
    /// </summary>
    public class ThemeManager : MonoBehaviour, IInitialise
    {
        private static ThemeManager themeManager;
        public static ThemeManager Instance => FindUtils.LazyFindObject(ref themeManager);

        [SerializeField]
        private List<AddressableAudioSource> mentionSounds = new List<AddressableAudioSource>();

        public List<AddressableAudioSource> MentionSounds => mentionSounds;

        //Add the root folder paths for each config type here:
        private static string[] folderPaths = new string[] { "ChatBubbleThemes" };
        //Directory info list of each folder path
        private List<string> diPaths = new List<string>();
        private Dictionary<ThemeType, List<ThemeHandler>> handlers = new Dictionary<ThemeType, List<ThemeHandler>>();
        //In case there are any hold ups with initialization, do init work via a queue
        private Queue<ThemeHandler> themeSetQueue = new Queue<ThemeHandler>();
        //All the loaded configs:
        private Dictionary<ThemeType, List<ThemeConfig>> Setconfigs = new Dictionary<ThemeType, List<ThemeConfig>>();
        //The theme the player has chosen to use:
        public Dictionary<ThemeType, ThemeConfig> chosenThemes = new Dictionary<ThemeType, ThemeConfig>();
        //Set to true when all the themes have been successfully loaded
        private bool themesLoaded = false;

        public static bool ChatHighlight;
        public static bool MentionSound;
        public static int MentionSoundIndex;

        public static AddressableAudioSource CurrentMentionSound;

        public InitialisationSystems Subsystem => InitialisationSystems.ThemeManager;

        void IInitialise.Initialise()
        {
	        //Create DirectoryInfo's for each folder path for ease of use
	        diPaths.Clear();
	        foreach (string p in folderPaths)
	        {
		        diPaths.Add($"Themes/{p}");
	        }

	        LoadAllThemes();

	        if (PlayerPrefs.HasKey(PlayerPrefKeys.HighlightChat) == false)
	        {
		        PlayerPrefs.SetInt(PlayerPrefKeys.HighlightChat, 1);
	        }

	        if (PlayerPrefs.HasKey(PlayerPrefKeys.MentionSound) == false)
	        {
		        PlayerPrefs.SetInt(PlayerPrefKeys.MentionSound, 1);
	        }

	        if (PlayerPrefs.HasKey(PlayerPrefKeys.MentionSoundIndex) == false)
	        {
		        PlayerPrefs.SetInt(PlayerPrefKeys.MentionSoundIndex, 0);
	        }

	        ChatHighlight = PlayerPrefs.GetInt(PlayerPrefKeys.HighlightChat) == 1;
	        MentionSound = PlayerPrefs.GetInt(PlayerPrefKeys.MentionSound) == 1;

	        foreach (var sound in mentionSounds)
	        {
		        sound.Preload();
	        }

	        MentionSoundIndex = PlayerPrefs.GetInt(PlayerPrefKeys.MentionSoundIndex);
	        CurrentMentionSound = MentionSounds[MentionSoundIndex];
        }

        public void ChatHighlightToggle(bool toggle)
        {
	        ChatHighlight = toggle;
	        PlayerPrefs.SetInt(PlayerPrefKeys.HighlightChat, toggle ? 1 : 0);
	        PlayerPrefs.Save();
        }

        public void MentionSoundToggle(bool toggle)
        {
	        MentionSound = toggle;
	        PlayerPrefs.SetInt(PlayerPrefKeys.MentionSound, toggle ? 1 : 0);
	        PlayerPrefs.Save();
        }

        public void MentionSoundIndexChange(int newValue)
        {
	        MentionSoundIndex = newValue;
	        CurrentMentionSound = MentionSounds[MentionSoundIndex];
	        PlayerPrefs.SetInt(PlayerPrefKeys.MentionSoundIndex, newValue);
	        PlayerPrefs.Save();
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

        private void OnEnable()
        {
	        UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
        }

        private void OnDisable()
        {
	        UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
        }

        void UpdateMe()
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
            if (Instance.Setconfigs.ContainsKey(themeType))
            {
                foreach (ThemeConfig config in Instance.Setconfigs[themeType])
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
            Setconfigs.Clear();

            foreach (string di in diPaths)
            {
                if (AccessFile.Exists(di, userPersistent: true, isFile: false))
                {
	                var files = AccessFile.DirectoriesOrFilesIn(di, userPersistent: true);
                    foreach (string file in files)
                    {
                        if (file.EndsWith(".yaml"))
                        {
                            LoadThemeFile(AccessFile.Load(Path.Combine(di, file), userPersistent: true));
                        }
                    }
                }
                else
                {
	                AccessFile.Save(Path.Combine(di, "ThemeBubble.yaml"),
@"ChatBubbles:
  Default:
    ImageColor: ""#FFFFFF""
    TextColor: ""#000000""
  DarkTheme:
    ImageColor: ""#222222""
    TextColor: ""#FFFFFF""
# Either add to this file or start a new
# one for your custom theme
", userPersistent: true);
	                LoadThemeFile(AccessFile.Load(Path.Combine(di, "ThemeBubble.yaml"), userPersistent: true));
                }
            }

            //Load the configs that the player has chosen to use:
            LoadUserPreferences();

            themesLoaded = true;
        }

        //Loads one theme file into the config dictionary
        void LoadThemeFile(string data)
        {
	        string yamlString = data;

	        var deserializer = new DeserializerBuilder().Build();
	        var yamlStream = new YamlStream();

	        using (var reader = new StringReader(yamlString))
	        {
		        try
		        {
			        yamlStream.Load(reader);
		        }
		        catch (Exception e)
		        {

			        //YML is God damned Stupid, No one cares if it has a tab just pass it Anyways You stupid pile of S****
			        Loggy.LogError(e.ToString());
			        return;
		        }

	        }

	        //Examine the yaml file:all gold dust buckets this all or
            var mapping = (YamlMappingNode) yamlStream.Documents[0].RootNode;
            foreach (var entry in mapping.Children)
            {
                var nodeName = ((YamlScalarNode) entry.Key).Value;

                if (string.IsNullOrEmpty(nodeName))
                {
                    Loggy.LogError($"No Theme Type found for {nodeName}", Category.Themes);
                    continue;
                }

                //Get the theme type from the node name
                ThemeType theme = (ThemeType) Enum.Parse(typeof(ThemeType), nodeName, true);
                //See if we have a list set up for this theme type
                if (!Setconfigs.ContainsKey(theme))
                {
                    Setconfigs.Add(theme, new List<ThemeConfig>());
                }

                //Get all the config names and their settings associated with this Theme type in this file
                var settings = (YamlMappingNode) entry.Value;
                foreach (var c in settings.Children)
                {
                    var cfg = new ThemeConfig();
                    cfg.themeType = theme;
                    cfg.themeName = ((YamlScalarNode) c.Key).Value;

                    //check to see if that name is already in use:
                    var index = Setconfigs[theme].FindIndex(x => x.themeName == cfg.themeName);
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
                                    Loggy.LogError($"Failed to parse html color {kvp.Value.ToString()}", Category.Themes);
                                }
                            }

                            if (kvp.Key.ToString().Contains("TextColor"))
                            {
                                if (!ColorUtility.TryParseHtmlString(kvp.Value.ToString(), out cfg.textColor))
                                {
                                    Loggy.LogError($"Failed to parse html color {kvp.Value.ToString()}", Category.Themes);
                                }
                            }
                        }
                        Setconfigs[theme].Add(cfg);
                    }
                    else
                    {
                        Loggy.LogError($"There is already a config named {cfg.themeName} in {theme.ToString()}", Category.Themes);
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
            if (!Instance.Setconfigs.ContainsKey(themeType))
            {
                Loggy.LogError($"Theme Type {themeType} not found in ThemeManager", Category.Themes);
                return;
            }

            var index = Instance.Setconfigs[themeType].FindIndex(x => string.Equals(x.themeName, themeName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
            {
                Loggy.LogError($"Theme not found {themeName}", Category.Themes);
                return;
            }

            Instance.SetPreferredTheme(themeType, Instance.Setconfigs[themeType][index]);
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
