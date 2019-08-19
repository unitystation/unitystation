using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

        }

        public static void RegisterHandler(ThemeHandler handler)
        {

        }

        public static void UnregisterHandler(ThemeHandler handler)
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