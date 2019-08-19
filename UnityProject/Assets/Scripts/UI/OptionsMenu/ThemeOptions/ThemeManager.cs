using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Core;

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
        private static string[] folderPaths = new string[] { "/ChatBubbleThemes/" };
        //Directory info list of each folder path
        private List<DirectoryInfo> diPaths = new List<DirectoryInfo>();
        private Dictionary<ThemeType, List<ThemeHandler>> handlers = new Dictionary<ThemeType, List<ThemeHandler>>();
        //In case there are any hold ups with initialization, do init work via a queue
        private Queue<ThemeHandler> themeSetQueue = new Queue<ThemeHandler>();
        //Set to true when all the themes have been successfully loaded
        private bool themesLoaded = false;

        void Awake()
        {
            //Create DirectoryInfo's for each folder path for ease of use
            foreach (string p in folderPaths)
            {
                diPaths.Clear();
                diPaths.Add(new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, p)));
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
                        }
                    }
                }
            }
            themesLoaded = true;
        }

        void LoadThemeFile(FileInfo file)
        {
            
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
}