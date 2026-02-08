using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logs;
using Shared.Managers;
using UnityEngine;
using UnityEngine.Audio;
using US13.Core.Addressables.Types;
using US13.Core.Utils;
using US13.PlayerPrefs;
using Util;

namespace US13.Managers
{
    public class AudioManager : SingletonManager<AudioManager>
    {
        /// <summary>
        /// Library of AddressableAudioSource.  Might be loaded or not.
        /// </summary>
        /// <remarks>Always use GetAddressableAudioSourceFromCache if you want a loaded version</remarks>
        [HideInInspector] public readonly List<AddressableAudioSource> AudioLibrary = new List<AddressableAudioSource>();

        [SerializeField] private AudioMixer audioMixer;
        public AudioMixerGroup MasterMixer;
        public AudioMixerGroup MusicMixer;
        public AudioMixerGroup JukeboxMixer;
        public AudioMixerGroup SFXMixer;
        public AudioMixerGroup SFXMuffledMixer;
        public AudioMixerGroup AmbientMixer;
        public AudioMixerGroup TTSMixer;
        public AudioMixerGroup TTSMixerRadio;
        public AudioMixerGroup TTSMixerRobot;
        public AudioMixerGroup GameplayMixer; //Affected by deafness and air pressure and all that stuff

        public event Action<bool> AudioReflectionsToggled;
        private bool enableAudioReflections = true;

        public bool EnableAudioReflections
        {
	        get => enableAudioReflections;
	        set => ToggleAudioReflections(value);
        }

        private void ToggleAudioReflections(bool value)
        {
	        AudioReflectionsToggled?.Invoke(value);
	        enableAudioReflections = value;
        }

        private float GameplayVolumeLevel = 1;

        public float gameplayVolumeLevel
        {
	        set
	        {
		        if (value > 1) //No earap please
		        {
			        GameplayVolumeLevel = 1;
		        }
		        else if ( value == 0)
		        {
			        GameplayVolumeLevel = 0.0001f; //Mathf.Log10(0) = Invalid number
		        }

		        else
		        {
			        GameplayVolumeLevel = value;
		        }

		        GameplayMixer.audioMixer.SetFloat("GameplayAudio_Volume", Mathf.Log10(GameplayVolumeLevel) * 20);
	        }
        }

        public MultiInterestFloat MultiInterestFloat = new MultiInterestFloat( 1,MultiInterestFloat.RegisterBehaviour.Register0, MultiInterestFloat.FloatBehaviour.ReturnOn1 );

        private void OnSetGameplayVolume(float vall)
        {
	        gameplayVolumeLevel = vall;
        }

        public override void Start()
        {
	        base.Start();
	        MultiInterestFloat.OnFloatChange.AddListener(OnSetGameplayVolume);
	        MasterVolume(
		        UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MasterVolumeKey)
			        ? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey)
			        : 1f
	        );
	        AmbientVolume(
		        UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.AmbientVolumeKey)
			        ? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey)
			        : 0.8f
	        );
	        SoundFXVolume(
		        UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.SoundFXVolumeKey)
			        ? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.SoundFXVolumeKey)
			        : 0.8f
	        );
	        MusicVolume(
		        UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey)
			        ? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey)
			        : 0.8f
	        );
	        TtsVolume(
		        UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.TtsVolumeKey)
			        ? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.TtsVolumeKey)
			        : 0.8f
	        );

	        // ReSharper disable once SimplifyConditionalTernaryExpression
	        EnableAudioReflections = UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.AudioReflectionsToggleKey)
		        ? UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.AudioReflectionsToggleKey) == 1
		        : true;
        }

        /// <summary>
        /// Sets all Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void MasterVolume(float volume, bool overwritePrefs = true)
        {
            Instance.audioMixer.SetFloat("Master_Volume", Mathf.Log10(volume) * 20);
            if (overwritePrefs)
            {
                UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.MasterVolumeKey, volume);
                UnityEngine.PlayerPrefs.Save();
            }
        }


        /// <summary>
        /// Sets Ambient Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void RadioChatterVolume(float volume, bool overwritePrefs = true)
        {
	        if (overwritePrefs)
	        {
		        UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.RadioVolumeKey, volume);
		        UnityEngine.PlayerPrefs.Save();
	        }
        }


        /// <summary>
        /// Sets Ambient Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void CommonRadioChatter(bool Value, bool overwritePrefs = true)
        {
	        if (overwritePrefs)
	        {
		        UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.CommonRadioToggleKey, Value ? 1 : 0);
		        UnityEngine.PlayerPrefs.Save();
	        }
        }

        /// <summary>
        /// Sets Ambient Sounds volume
        /// </summary>
        /// <param name="volume"></param>
        public static void AmbientVolume(float volume, bool overwritePrefs = true)
        {
            Instance.audioMixer.SetFloat("Ambient_Volume", Mathf.Log10(volume) * 20);
            if (overwritePrefs)
            {
                UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.AmbientVolumeKey, volume);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Sets Sound FX volume
        /// </summary>
        /// <param name="volume"></param>
        public static void SoundFXVolume(float volume, bool overwritePrefs = true)
        {
            Instance.audioMixer.SetFloat("SoundFX_Volume", Mathf.Log10(volume) * 20);
            if (overwritePrefs)
            {
                UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.SoundFXVolumeKey, volume);
                UnityEngine.PlayerPrefs.Save();
            }

        }

        /// <summary>
        /// Sets Music volume
        /// </summary>
        /// <param name="volume"></param>
        public static void MusicVolume(float volume, bool overwritePrefs = true)
        {
            Instance.audioMixer.SetFloat("Music_Volume", Mathf.Log10(volume) * 20);
            if (overwritePrefs)
            {
                UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.MusicVolumeKey, volume);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Sets TTS volume
        /// </summary>
        /// <param name="volume"></param>
        public static void TtsVolume(float volume, bool overwritePrefs = true)
        {
            Instance.audioMixer.SetFloat("TTS_Volume", Mathf.Log10(volume) * 20);
            if (overwritePrefs)
            {
                UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.TtsVolumeKey, volume);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Get a fully loaded addressableAudioSource from the loaded cache.  This ensures that everything is ready to use.
        /// </summary>
        /// <param name="addressableAudioSources">The audio to be played.</param>
        /// <returns>A fully loaded and ready to use AddressableAudioSource</returns>
        public static async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache(AddressableAudioSource addressableAudioSource)
        {
        //Make sure it is a valid Addressable AudioSource
        if (addressableAudioSource == null || addressableAudioSource == default(AddressableAudioSource))
            {
                Loggy.Warning("AudioManager recieved a null Addressable audio source, look at log trace for responsible component", Category.Audio);
                return null;
            }
            if (string.IsNullOrEmpty(addressableAudioSource.AssetAddress))
            {
                Loggy.Warning("AudioManager received a null address for an addressable, look at log trace for responsible component", Category.Audio);
                return null;
            }
            if (addressableAudioSource.AssetAddress == "null")
            {
                Loggy.Warning("AudioManager received an addressable with an address set to the string 'null', look at log trace for responsible component", Category.Audio);
                return null;
            }

            //Try to get the Audio Source from cache, if its not there load it into cache
            AddressableAudioSource addressableAudioSourceFromCache = null;
            lock (Instance.AudioLibrary)
            {
                addressableAudioSourceFromCache =
                    Instance.AudioLibrary.FirstOrDefault(p => p.AssetAddress == addressableAudioSource.AssetAddress);
            }
            if (addressableAudioSourceFromCache == null)
            {
                lock (Instance.AudioLibrary)
                {
                    Instance.AudioLibrary.Add(addressableAudioSource);
                }
                addressableAudioSourceFromCache = addressableAudioSource;
            }

            //Ensure that the audio source is loaded
            GameObject gameObject = await addressableAudioSourceFromCache.Load();

            if (gameObject == null)
            {
                Loggy.Error(
                    $"AddressableAudioSource in AudioManager failed to load from address: {addressableAudioSourceFromCache.AssetAddress}",
                    Category.Audio, LogOption.NoStacktrace);
                return null;
            }

            if (gameObject.TryGetComponent(out AudioSource audioSource) == false)
            {
                Loggy.Error(
                    $"AddressableAudioSource in AudioManager doesn't contain an AudioSource: {addressableAudioSourceFromCache.AssetAddress}",
                    Category.Audio, LogOption.NoStacktrace);
                return null;
            }

            return addressableAudioSourceFromCache;
        }

        /// <summary>
        /// Get a fully loaded addressableAudioSource from the loaded cache.  This ensures that everything is ready to use.
        /// If more than one addressableAudioSource is provided, one will be picked at random.
        /// </summary>
        /// <param name="addressableAudioSources">A list containing audio to be played. If more than one is specified, one will be picked at random.</param>
        /// <returns>A fully loaded and ready to use AddressableAudioSource</returns>
        public static async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache(List<AddressableAudioSource> addressableAudioSources)
        {
            AddressableAudioSource addressableAudioSource = EnumerableExt.PickRandom(addressableAudioSources);
            addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);
            return addressableAudioSource;
        }

        public async Task FadeMixerGroup(string exposedParam, float duration, float targetVolume)
		{
			float currentTimeMs = 0;
			audioMixer.GetFloat(exposedParam, out float currentVol);
			currentVol = Mathf.Pow(10, currentVol / 20);
			float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
			while (currentTimeMs < duration)
			{
				float newVol = Mathf.Lerp(currentVol, targetValue, currentTimeMs / duration);
                currentTimeMs += 16f;
				audioMixer.SetFloat(exposedParam, Mathf.Log10(newVol) * 20);
                await Task.Delay(16); // Sleep for approx one frame (16 * 60 fps ~= 1000ms)
			}
            return;
		}
    }
}
