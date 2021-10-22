using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Messages.Server.SoundMessages;
using AddressableReferences;
using Audio.Managers;

namespace Audio.Containers
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager audioManager;
        public static AudioManager Instance
        {
            get
            {
                if (audioManager == null)
                {
                    audioManager = FindObjectOfType<AudioManager>();
                }

                return audioManager;
            }
        }
        
        public AudioMixer audioMixer;
        public AudioMixerGroup MasterMixer;
        public AudioMixerGroup MusicMixer;
        public AudioMixerGroup SFXMixer;
        public AudioMixerGroup SFXMuffledMixer;
        public AudioMixerGroup AmbientMixer;
        public AudioMixerGroup TTSMixer;

        private void Start()
        {
            MasterVolume(
                PlayerPrefs.HasKey(PlayerPrefKeys.MasterVolumeKey) 
                    ? PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey)
                    : 1f
                );
            AmbientVolume(
                PlayerPrefs.HasKey(PlayerPrefKeys.AmbientVolumeKey)
                    ? PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey)
                    : 0.8f
                );
            SoundFXVolume(
                PlayerPrefs.HasKey(PlayerPrefKeys.SoundFXVolumeKey)
                    ? PlayerPrefs.GetFloat(PlayerPrefKeys.SoundFXVolumeKey)
                    : 0.8f
                );
            MusicVolume(
                PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey)
                    ? PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey)
                    : 0.8f
                );
            TtsVolume(
                PlayerPrefs.HasKey(PlayerPrefKeys.TtsVolumeKey)
                    ? PlayerPrefs.GetFloat(PlayerPrefKeys.TtsVolumeKey)
                    : 0.8f
                );
        }


        /// <summary>
        /// Sets all Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void MasterVolume(float volume)
        {
            Instance.audioMixer.SetFloat("Master_Volume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat(PlayerPrefKeys.MasterVolumeKey, volume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets Ambient Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void AmbientVolume(float volume)
        {
            Instance.audioMixer.SetFloat("Ambient_Volume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat(PlayerPrefKeys.AmbientVolumeKey, volume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets Sound FX volume
        /// </summary>
        /// <param name="volume"></param>
        public static void SoundFXVolume(float volume)
        {
            Instance.audioMixer.SetFloat("SoundFX_Volume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat(PlayerPrefKeys.SoundFXVolumeKey, volume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets Music volume
        /// </summary>
        /// <param name="volume"></param>
        public static void MusicVolume(float volume)
        {
            Instance.audioMixer.SetFloat("Music_Volume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat(PlayerPrefKeys.MusicVolumeKey, volume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets TTS volume
        /// </summary>
        /// <param name="volume"></param>
        public static void TtsVolume(float volume)
        {
            Instance.audioMixer.SetFloat("TTS_Volume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat(PlayerPrefKeys.TtsVolumeKey, volume);
            PlayerPrefs.Save();
        }
    }
}
