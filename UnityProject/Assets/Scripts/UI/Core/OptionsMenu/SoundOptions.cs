using Audio.Managers;
using Audio.Containers;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Unitystation.Options
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
			PlayerPrefs.SetInt(PlayerPrefKeys.AudioReflectionsToggleKey, audioReflectionsToggle.isOn ? 1 : 0);
			PlayerPrefs.Save();
		}

		public void OnTtsVolumeChange()
		{
			AudioManager.TtsVolume(TTSSlider.value);
		}

		void Refresh()
		{
			musicSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey);
			soundFXSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.SoundFXVolumeKey);
			ambientSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey);
			TTSSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.TtsVolumeKey);
			ttsToggle.isOn = PlayerPrefs.GetInt(PlayerPrefKeys.TTSToggleKey) == 1;
			audioReflectionsToggle.isOn = !PlayerPrefs.HasKey(PlayerPrefKeys.AudioReflectionsToggleKey) || PlayerPrefs.GetInt(PlayerPrefKeys.AudioReflectionsToggleKey) == 1;
			masterSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey);
			TTSSlider.transform.parent.SetActive(ttsToggle.isOn);

			CommonRadioToggle.isOn = PlayerPrefs.GetInt(PlayerPrefKeys.CommonRadioToggleKey) == 1;
			RadioSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.RadioVolumeKey);

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