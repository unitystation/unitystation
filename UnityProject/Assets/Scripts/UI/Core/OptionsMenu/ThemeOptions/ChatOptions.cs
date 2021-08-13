using System.Collections;
using System.Collections.Generic;
using Managers.SettingsManager;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
{
	/// <summary>
	/// Controller for the chat options
	/// <summary>
	public class ChatOptions : MonoBehaviour
	{
        [SerializeField] private Slider chatSlider;
        [SerializeField] private Text chatSliderValueLabel;

        private void OnEnable() 
        {
            PresistOptions(1);
            chatSlider.value = ChatUI.Instance.maxLogLength;
            chatSliderValueLabel.text = chatSlider.value.ToString();
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
            ChatUI.Instance.maxLogLength = chatSlider.value.RoundToLargestInt();
            chatSliderValueLabel.text = chatSlider.value.ToString();
            PresistOptions();
        }
    }
}