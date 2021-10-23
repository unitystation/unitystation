using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio.Containers
{
	public class MusicManager : MonoBehaviour
	{
		private static MusicManager musicManager;
		public static MusicManager Instance
		{
			get
			{
				if (musicManager == null)
				{
					musicManager = FindObjectOfType<MusicManager>();
				}

				return musicManager;
			}
		}

		[SerializeField] private SongTracker songTracker = null;
		/// <summary>
		/// For controlling the song play list. Includes random shuffle and auto play
		/// </summary>
		public static SongTracker SongTracker => Instance.songTracker;

		private bool isMusicMute;
		[Range(0f, 1f)] public float MusicVolume = 0.5f;

		[SerializeField] private AudioSource currentLobbyAudioSource = null;

		[SerializeField] private AudioClipsArray audioClips = null;

		private void Awake()
		{
			Init();
		}

		private void Init()
		{
			if (currentLobbyAudioSource == null)
			{
				currentLobbyAudioSource = GetComponent<AudioSource>();
			}

			currentLobbyAudioSource.outputAudioMixerGroup = AudioManager.Instance.MusicMixer;

			//Mute Music Preference
			if (PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				isMusicMute = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
			}
		}

		public static void StopMusic()
		{
			Instance.currentLobbyAudioSource.Stop();
			Synth.Instance.StopMusic();
		}

		/// <summary>
		/// Plays a random music track.
		/// <returns>String[] that represents the picked song's name.</returns>
		/// </summary>
		public async Task<String[]> PlayRandomTrack()
		{
			StopMusic();
			if (currentLobbyAudioSource == null) Init();
			var audioSource = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource>{audioClips.GetRandomClip()});
			if(audioSource == null)
			{
				Logger.LogError("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
				return null;
			}
			currentLobbyAudioSource.clip = audioSource.AudioSource.clip;
			currentLobbyAudioSource.mute = isMusicMute;
			currentLobbyAudioSource.volume = Instance.MusicVolume;
			currentLobbyAudioSource.Play();
			if (currentLobbyAudioSource.clip == null) return new string[]{ "ERROR",  "ERROR" , "ERROR",  "ERROR"};;
			return currentLobbyAudioSource.clip.name.Split('_');
		}

		/// <summary>
		/// Plays specific music track.
		/// <returns>String[] that represents the picked song's name.</returns>
		/// </summary>
		public async Task<String[]> PlayTrack(AddressableAudioSource audioSource)
		{
			StopMusic();
			if (currentLobbyAudioSource == null) Init();
			if(audioSource == null)
			{
				Logger.LogError("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
				return null;
			}
			currentLobbyAudioSource.clip = audioSource.AudioSource.clip;
			currentLobbyAudioSource.mute = isMusicMute;
			currentLobbyAudioSource.volume = Instance.MusicVolume;
			currentLobbyAudioSource.Play();
			if (currentLobbyAudioSource.clip == null) return new string[]{ "ERROR",  "ERROR" , "ERROR",  "ERROR"};;
			return currentLobbyAudioSource.clip.name.Split('_');
		}

		public void ToggleMusicMute(bool mute)
		{
			isMusicMute = mute;
			currentLobbyAudioSource.mute = mute;
			if (mute)
			{
				Synth.Instance.SetMusicVolume(Byte.MinValue);
			}
			else
			{
				var vol = 255 * Instance.MusicVolume;
				Synth.Instance.SetMusicVolume((byte) (int) vol);
			}
		}

		/// <summary>
		/// Checks if music in lobby is being played or not.
		/// <returns> true if music is being played.</returns>
		/// </summary>
		public static bool isLobbyMusicPlaying()
		{
			if (Instance.currentLobbyAudioSource != null
			    && Instance.currentLobbyAudioSource.isPlaying
			    || (SunVox.sv_end_of_song((int) Slot.Music) != 0))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public void ChangeVolume(float newVolume)
		{
			MusicVolume = newVolume;
			AudioManager.MusicVolume(newVolume);

			SaveNewVolume(newVolume);
		}

		private void SaveNewVolume(float newVolume)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.MusicVolumeKey, newVolume);
			PlayerPrefs.Save();
		}
	}
}
