using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
{
	/// <summary>
	/// Handles the UI for Theme Options in the
	/// Options Screen.false ThemeManager for
	/// Theme switching operations
	/// </summary>
	public class ThemeOptions : MonoBehaviour
	{
		public Dropdown chatBubbleDropDown;

		[SerializeField]
		private Toggle HighlightToggle = null;

		[SerializeField]
		private Toggle chatHighlightToggle = null;

		[SerializeField]
		private Toggle mentionSoundToggle = null;

		[SerializeField]
		private TMP_Dropdown mentionSoundDropdown = null;

		void OnEnable()
		{
			Refresh();
			ConstructChatBubbleOptions();
		}

		void Refresh()
		{
			//Reload all the themes as there might be
			//updates
			ThemeManager.Instance.LoadAllThemes();
			HighlightToggle.isOn = Highlight.HighlightEnabled;
			chatHighlightToggle.isOn = ThemeManager.ChatHighlight;
			mentionSoundToggle.isOn = ThemeManager.MentionSound;

			var newOptions = new List<TMP_Dropdown.OptionData>();

			foreach (var sound in ThemeManager.Instance.MentionSounds)
			{
				newOptions.Add(new TMP_Dropdown.OptionData(sound.AudioSource.name));
			}

			mentionSoundDropdown.options = newOptions;

			mentionSoundDropdown.value = ThemeManager.MentionSoundIndex;
		}

		void ConstructChatBubbleOptions()
		{
			var options = ThemeManager.GetThemeOptions(ThemeType.ChatBubbles);
			if (options.Count > 0)
			{
				chatBubbleDropDown.interactable = true;

				List<Dropdown.OptionData> optionData = new List<Dropdown.OptionData>();
				foreach (string option in options)
				{
					var optData = new Dropdown.OptionData();
					optData.text = option;
					optionData.Add(optData);
				}
				var currentPref = optionData.FindIndex(x => string.Equals(x.text, ThemeManager.Instance.chosenThemes[ThemeType.ChatBubbles].themeName));
				chatBubbleDropDown.options = optionData;
				chatBubbleDropDown.value = currentPref;
			}
			else
			{
				chatBubbleDropDown.interactable = false;
				Logger.LogError("No Options found for ChatBubbles", Category.Themes);
			}
		}

		public void HighlightSetPreference()
		{
			Highlight.SetPreference(HighlightToggle.isOn);
			Refresh();
		}

		public void ChatHighlightSetPreference()
		{
			ThemeManager.Instance.ChatHighlightToggle(chatHighlightToggle.isOn);
			Refresh();
		}

		public void MentionSoundSetPreference()
		{
			ThemeManager.Instance.MentionSoundToggle(mentionSoundToggle.isOn);
			Refresh();
		}

		public void OnMentionSoundIndexChange()
		{
			ThemeManager.Instance.MentionSoundIndexChange(mentionSoundDropdown.value);
			Refresh();
		}


		//Changing the value of the preferred Chat Bubble Theme from drop down list
		public void OnChatBubbleChange()
		{
			ThemeManager.SetPreferredTheme(ThemeType.ChatBubbles, chatBubbleDropDown.options[chatBubbleDropDown.value].text);
		}
	}
}