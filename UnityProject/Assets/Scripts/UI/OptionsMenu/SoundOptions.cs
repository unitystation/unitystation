﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
		private Slider masterSlider = null;

		void OnEnable()
        {
            Refresh();

		}

        public void OnAmbientVolumeChange()
        {
            SoundManager.AmbientVolume(ambientSlider.value);
        }

		public void OnMasterVolumeChange()
		{
			SoundManager.MasterVolume(masterSlider.value);
		}

		public void TTSToggle()
        {
            UIManager.ToggleTTS(ttsToggle.isOn);
        }

        void Refresh()
        {
            ambientSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey);
            ttsToggle.isOn = PlayerPrefs.GetInt(PlayerPrefKeys.TTSToggleKey) == 1;
			masterSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey);

		}

        public void ResetDefaults()
        {
            ModalPanelManager.Instance.Confirm(
                "Are you sure?",
                () =>
                {
                    UIManager.ToggleTTS(false);
                    SoundManager.AmbientVolume(1f);
					AudioListener.volume = 1;
                    Refresh();
                },
                "Reset"
            );
        }
    }
}