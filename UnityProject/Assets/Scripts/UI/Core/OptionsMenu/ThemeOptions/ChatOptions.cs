using UnityEngine;
using UnityEngine.UI;
using UI.Chat_UI;
using UnityEngine.Serialization;

namespace Unitystation.Options
{
	/// <summary>
	/// Controller for the chat options
	/// <summary>
	public class ChatOptions : MonoBehaviour
	{
		[FormerlySerializedAs("chatSlider")] [SerializeField] private Slider chatLogSlider;
		[SerializeField] private Slider chatFontSizeSlider;
        [SerializeField] private Text chatSliderValueLabel;
        [SerializeField] private Text chatFontSizeSliderValueLabel;

        public static string FONTSCALE_KEY = "fontscale";
        public static int FONTSCALE_KEY_DEFAULT = 1;

        private void OnEnable()
        {
            PresistOptions(1);
            if (chatLogSlider != null && chatSliderValueLabel)
            {
	            chatSliderValueLabel.text = chatLogSlider.value.ToString();
            }
            if (chatFontSizeSlider != null && chatFontSizeSliderValueLabel != null)
            {
	            var inty = PlayerPrefs.GetInt(FONTSCALE_KEY, FONTSCALE_KEY_DEFAULT);;
	            chatFontSizeSlider.value = inty;
	            chatFontSizeSliderValueLabel.text = chatFontSizeSlider.value.ToString();
            }
        }

        private void OnDisable()
        {
            PresistOptions();
        }

        /// <summary>
        /// Saves chat settings, 0 is set and 1 is get values from PlayerPrefs.
        /// </summary>
        private void PresistOptions(int GetterSetter = 0)
        {
            if(GetterSetter == 0)
            {
                PlayerPrefs.SetInt(PlayerPrefKeys.ChatLogSize, ChatUI.Instance.maxLogLength);
            }
            else
            {
                ChatUI.Instance.maxLogLength = PlayerPrefs.GetInt(PlayerPrefKeys.ChatLogSize, 100);
            }
        }

        public void UpdateChatLogMaximumSize()
        {
            ChatUI.Instance.maxLogLength = chatLogSlider.value.RoundToLargestInt();
            chatSliderValueLabel.text = chatLogSlider.value.ToString();
            PresistOptions();
        }

        public void UpdateChatFontSize()
        {
	        PlayerPrefs.SetInt(FONTSCALE_KEY, (int)chatFontSizeSlider.value);
	        chatFontSizeSliderValueLabel.text = chatFontSizeSlider.value.ToString();
        }
    }
}