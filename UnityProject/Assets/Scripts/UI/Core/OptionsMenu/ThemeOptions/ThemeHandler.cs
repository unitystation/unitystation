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
		public TMPro.TextMeshProUGUI textMeshProUGUI;

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
	        if (this == null || this.gameObject == null)
	        {
		        return;
	        }
            switch (targetElement)
            {
                case UIElement.Image:
                    image.color = new Color(config.imageColor.r, config.imageColor.g, config.imageColor.b, image.color.a); // Use original alpha (Fixes #2567)
					break;
                case UIElement.Text:
					text.color = new Color(config.textColor.r, config.textColor.g, config.textColor.b, text.color.a); // Use original alpha (Fixes #2567)
					break;
				case UIElement.TextMeshProUGUI:
					textMeshProUGUI.color = new Color(config.textColor.r, config.textColor.g, config.textColor.b, textMeshProUGUI.color.a);
					break;
			}
        }
    }
}