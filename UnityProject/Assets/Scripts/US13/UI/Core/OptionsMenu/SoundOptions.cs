using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using US13.Managers;
using US13.Managers.AddressableManager;
using US13.PlayerPrefs;
using US13.UI.Systems;
using US13.UI.Systems.IngameMenu;
using Util;

namespace US13.UI.Core.OptionsMenu
{
	public class SoundOptions : MonoBehaviour
	{
		[SerializeField]
		private Slider ambientSlider = null;

		[SerializeField]
		private Toggle ttsToggle = null;

		[SerializeField]
		private Toggle audioReflectionsToggle = null;

		[SerializeField]
		private Slider masterSlider = null;

		[SerializeField]
		private Slider soundFXSlider = null;

		[SerializeField]
		private Slider musicSlider = null;

		[SerializeField]
		private Slider TTSSlider = null;

		[SerializeField]
		private Slider RadioSlider = null;

		[SerializeField]
		private Toggle CommonRadioToggle = null;

		[SerializeField]
		private Toggle VoiceChatToggle = null;

		[SerializeField]
		private Toggle PushToTalkToggle = null;

		[SerializeField] private TMP_Text cacheVersionText;

		void OnEnable()
		{
			Refresh();
		}

		public void OnMasterVolumeChange()
		{
			AudioManager.MasterVolume(masterSlider.value);
		}

		public void OnSoundFXVolumeChange()
		{
			AudioManager.SoundFXVolume(soundFXSlider.value);
		}

		public void OnMusicVolumeChange()
		{
			AudioManager.MusicVolume(musicSlider.value);
		}

		public void OnAmbientVolumeChange()
		{
			AudioManager.AmbientVolume(ambientSlider.value);
		}


		public void OnRadioChatterChange()
		{
			AudioManager.RadioChatterVolume(RadioSlider.value);
		}

		public void OnCommonRadioChatterChange()
		{
			AudioManager.CommonRadioChatter(CommonRadioToggle.isOn);
		}

		public void TTSToggle()
		{
			UIManager.ToggleTTS(ttsToggle.isOn);
			TTSSlider.transform.parent.SetActive(ttsToggle.isOn);
		}

		public void AudioReflectionsToggle()
		{
			AudioManager.Instance.EnableAudioReflections = audioReflectionsToggle.isOn;
			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.AudioReflectionsToggleKey, audioReflectionsToggle.isOn ? 1 : 0);
			UnityEngine.PlayerPrefs.Save();
		}

		public void OnTtsVolumeChange()
		{
			AudioManager.TtsVolume(TTSSlider.value);
		}


		public void OnPushToTalkChange()
		{
			VoiceChatManager.Instance.ClientPushToTalk = PushToTalkToggle.isOn;
			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.PushToTalkToggle, PushToTalkToggle.isOn ? 1 : 0);
			UnityEngine.PlayerPrefs.Save();
		}

		public void OnVoiceChatToggle()
		{
			VoiceChatManager.Instance.ClientEnabled = VoiceChatToggle.isOn;
			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.VoiceChatToggle, VoiceChatToggle.isOn ? 1 : 0);
			UnityEngine.PlayerPrefs.Save();
		}

		void Refresh()
		{
			musicSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey, 0.75f);
			soundFXSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.SoundFXVolumeKey, 1f);
			ambientSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey, 0.35f);
			TTSSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.TtsVolumeKey);
			ttsToggle.isOn = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.TTSToggleKey) == 1;
			audioReflectionsToggle.isOn = !UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.AudioReflectionsToggleKey) || UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.AudioReflectionsToggleKey) == 1;
			masterSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey);
			TTSSlider.transform.parent.SetActive(ttsToggle.isOn);

			CommonRadioToggle.isOn = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.CommonRadioToggleKey) == 1;
			RadioSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.RadioVolumeKey);


			VoiceChatToggle.isOn = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.VoiceChatToggle, 1) == 1;
			PushToTalkToggle.isOn = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.PushToTalkToggle, 1) == 1;
			cacheVersionText.text = "";
		}

		public void OnClearSoundCacheOption()
		{
			Addressables.CleanBundleCache();
			Caching.ClearCache();
			AddressableCatalogueManager.LoadHostCatalogues();
			cacheVersionText.text = "<color=green>Cache cleared. Please restart the game.</color>";
		}


		public void ResetDefaults()
		{
			ModalPanelManager.Instance.Confirm(
				"Are you sure?",
				() =>
				{
					UIManager.ToggleTTS(false);
					AudioManager.AmbientVolume(0.2f);
					AudioManager.SoundFXVolume(0.2f);
					AudioManager.MusicVolume(0.2f);
					AudioManager.TtsVolume(0.2f);
					AudioListener.volume = 1;
					AudioManager.CommonRadioChatter(false);
					AudioManager.RadioChatterVolume(0.2f);
					Refresh();
				},
				"Reset"
			);
		}
	}
}
