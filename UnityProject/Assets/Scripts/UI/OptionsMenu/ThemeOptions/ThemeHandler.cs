using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
{
    /// <summary>
    /// The ThemeHandler allows UI elements
    /// to be customized by a user defined theme 
    /// configuration. Add your own types to the ThemeType 
    /// list if they are not available
    /// </summary>
    public class ThemeHandler : MonoBehaviour
    {
        [Tooltip("Add your own type if needed")]
        public ThemeType themeType;
        [Tooltip("Is this a Text, Image or something else?" +
            " Add a ThemeHandler for each UIElement you want to" +
            " customize")]
        public UIElement targetElement;
        public Image image;
        public Text text;

        void OnEnable()
        {
            StartCoroutine(Register());
        }

        void OnDestroy()
        {
            if (ThemeManager.Instance != null)
            {
                ThemeManager.UnregisterHandler(this);
            }
        }

        IEnumerator Register()
        {
            while (ThemeManager.Instance == null)
            {
                yield return WaitFor.EndOfFrame;
            }
            ThemeManager.RegisterHandler(this);
        }

        /// <summary>
        /// Set the theme preferences.
        /// Called via ThemeManager
        /// </summary>
        public void SetTheme(ThemeConfig config)
        {
            switch (targetElement)
            {
                case UIElement.Image:
                    image.color = config.imageColor;
                    break;
                case UIElement.Text:
                    text.color = config.textColor;
                    break;
            }
        }
    }
}