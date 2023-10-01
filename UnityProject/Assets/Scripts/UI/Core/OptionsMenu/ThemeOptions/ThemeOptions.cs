using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using Logs;
using Managers.SettingsManager;
using TMPro;
using UI;
using UI.Chat_UI;
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
		private Slider chatBubbleSizeSlider = null;

		[SerializeField]
		private Toggle chatBubbleInstantToggle = null;

		[SerializeField]
		private Slider chatBubblePopInSpeedSlider = null;

		[SerializeField]
		private Slider chatBubbleAdditionalTimeSlider = null;

		[SerializeField]
		private Toggle chatBubbleClownColourToggle = null;

		[SerializeField]
		private Toggle HighlightToggle = null;

		[SerializeField]
		private Toggle chatHighlightToggle = null;

		[SerializeField]
		private Toggle mentionSoundToggle = null;

		[SerializeField]
		private TMP_Dropdown mentionSoundDropdown = null;

		[SerializeField]
		private Slider chatAlphaFadeMinimum;

		[SerializeField]
		private Slider chatContentAlphaFadeMinimum;

		[SerializeField]
		private Slider hoverTooltipDelaySlider;

		[SerializeField]
		private Text hoverTooltipDelaySliderValueText;

		[SerializeField]
		private Dropdown fontDropdown = null;

		[SerializeField]
		private Dropdown RightClickropdown = null;


		[SerializeField]
		private Toggle ThrowPreferenceToggle = null;

		void OnEnable()
		{
			Refresh();
			ConstructChatBubbleOptions();
		}

		void Refresh()
		{
			//Reload all the themes as there might be
			//updates
			try
			{
				ThemeManager.Instance.LoadAllThemes();
			}
			catch (Exception e)
			{
				Loggy.LogError($"[ThemeOptions/Refresh()] - Failed to Load themes.\n {e}");
			}

			HighlightToggle.isOn = Highlight.HighlightEnabled;
			chatHighlightToggle.isOn = ThemeManager.ChatHighlight;
			mentionSoundToggle.isOn = ThemeManager.MentionSound;

			chatBubbleSizeSlider.value = DisplaySettings.Instance.ChatBubbleSize;
			chatBubbleInstantToggle.isOn = DisplaySettings.Instance.ChatBubbleInstant == 1;
			chatBubblePopInSpeedSlider.value = DisplaySettings.Instance.ChatBubblePopInSpeed;
			chatBubbleAdditionalTimeSlider.value = DisplaySettings.Instance.ChatBubbleAdditionalTime;
			chatBubbleClownColourToggle.isOn = DisplaySettings.Instance.ChatBubbleClownColour == 1;

			try
			{
				var newOptions = new List<TMP_Dropdown.OptionData>();
				foreach (var sound in ThemeManager.Instance.MentionSounds)
				{
					newOptions.Add(new TMP_Dropdown.OptionData(sound.AudioSource.name));
				}
				mentionSoundDropdown.options = newOptions;
				mentionSoundDropdown.value = ThemeManager.MentionSoundIndex;
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

			try
			{
				fontDropdown.ClearOptions();
				var fontNames = ChatUI.Instance.Fonts.Select(font => font.name).ToList();
				fontDropdown.AddOptions(fontNames);



				var value = PlayerPrefs.GetString("fontPref", "LiberationSans SDF");
				fontDropdown.SetValueByName(value);
			}
			catch (Exception e)
			{
				var chatUIHasNoFonts = ChatUI.Instance.Fonts?.Count == 0;
				Loggy.LogError($"[ThemeOptions/Refresh()] - Failed to setup font options. " +
				                $"\n chat has no fonts: {chatUIHasNoFonts} \n {e}");
			}

			try
			{
				RightClickropdown.ClearOptions();

				var Options = RightClickManager.AvailableRightClickOptions.Keys.ToList();

				RightClickropdown.AddOptions(Options);
				var value = RightClickManager.GetRightClickPreference();
				RightClickropdown.SetValueByName(value);
			}
			catch (Exception e)
			{
				Loggy.LogError($"[ThemeOptions/Refresh()] - Failed to setup RightClick options. " );
			}

			ThrowPreferenceToggle.isOn = ControlAction.GetHoldThrowPreference();

			chatAlphaFadeMinimum.value = UI.Chat_UI.ChatUI.Instance.GetPreferenceChatBackground();
			chatContentAlphaFadeMinimum.value =  UI.Chat_UI.ChatUI.Instance.GetPreferenceChatContent();
			hoverTooltipDelaySlider.value = UIManager.Instance.HoverTooltipUI.GetSavedTooltipDelay();
			hoverTooltipDelaySliderValueText.text = UIManager.Instance.HoverTooltipUI.GetSavedTooltipDelay().ToString();
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
				Loggy.LogError("No Options found for ChatBubbles", Category.Themes);
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

		public void OnChatBubbleSizeChange()
		{
			DisplaySettings.Instance.ChatBubbleSize = chatBubbleSizeSlider.value;
		}

		public void OnChatBubbleInstantChange()
		{
			DisplaySettings.Instance.ChatBubbleInstant = chatBubbleInstantToggle.isOn ? 1 : 0;
		}

		public void OnChatBubblePopInSpeedChange()
		{
			DisplaySettings.Instance.ChatBubblePopInSpeed = chatBubblePopInSpeedSlider.value;
		}

		public void OnChatBubbleAdditionalTimeChange()
		{
			DisplaySettings.Instance.ChatBubbleAdditionalTime = chatBubbleAdditionalTimeSlider.value;
		}

		public void OnChatBubbleClownColourChange()
		{
			DisplaySettings.Instance.ChatBubbleClownColour = chatBubbleClownColourToggle.isOn ? 1 : 0;
		}

		public void OnChatMinimumAlphaColorChange()
		{
			UI.Chat_UI.ChatUI.Instance.SetPreferenceChatBackground(chatAlphaFadeMinimum.value);
		}

		public void OnChatContentMinimumAlphaColorChange()
		{
			UI.Chat_UI.ChatUI.Instance.SetPreferenceChatContent(chatContentAlphaFadeMinimum.value);
		}

		public void OnHoverTooltipDelayValueChange()
		{
			UIManager.Instance.HoverTooltipUI.HoverDelay = hoverTooltipDelaySlider.value;
			PlayerPrefs.SetFloat(PlayerPrefKeys.HoverTooltipDelayKey, hoverTooltipDelaySlider.value);
			PlayerPrefs.Save();
			Refresh();
		}

		public void OnFontPreferenceChange()
		{
			ChatUI.Instance.FontIndexToUse = fontDropdown.value;
			PlayerPrefs.SetString("fontPref", fontDropdown.GetValueName());
		}

		public void OnRightClickPreferenceChange()
		{
			RightClickManager.SetRightClickPreference(RightClickropdown.GetValueName());
		}

		public void OnThrowHoldPreferenceChange()
		{
			ControlAction.SetPreferenceThrowHoldPreference(ThrowPreferenceToggle.isOn);
		}
	}
}