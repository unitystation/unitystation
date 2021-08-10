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
            chatSlider.value = ChatUI.Instance.maxLogLength;
            chatSliderValueLabel.text = chatSlider.value.ToString();
        }

        public void UpdateChatLogMaximumSize()
        {
            ChatUI.Instance.maxLogLength = chatSlider.value.RoundToLargestInt();
            chatSliderValueLabel.text = chatSlider.value.ToString();
        }
    }
}