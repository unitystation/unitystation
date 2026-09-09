using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logs;
using Shared.Util;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Sound;
using US13.Messages.Server.SoundMessages;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.PlayerPrefs;
using US13.ScriptableObjects.Audio;

namespace US13.Managers.LobbyManager
{
	public class MusicManager : MonoBehaviour
	{
		private static MusicManager musicManager;
		public static MusicManager Instance => FindUtils.LazyFindObject(ref musicManager);

		public string currentNetworkedSong = "";

		private bool isMusicMute;
		[Range(0f, 1f)] public float MusicVolume = 0.5f;

		private readonly List<AddressableAudioSource> trackHistory = new List<AddressableAudioSource>();
		private int historyIndex = -1;
		private const int MAX_HISTORY = 20;

		private bool playingRandomPlayList;
		private float currentWaitTime;
		private const float TIME_BETWEEN_SONGS = 2f;

		private bool isPaused;

		public enum PlaybackState
		{
			Stopped,
			Playing,
			Paused
		}

		public PlaybackState State
		{
			get
			{
				if (isPaused)
				{
					return PlaybackState.Paused;
				}
				if (isMusicPlaying())
				{
					return PlaybackState.Playing;
				}
				return PlaybackState.Stopped;
			}
		}

		public string[] CurrentTrackInfo
		{
			get
			{
				if (musicAudioSource == null) return null;
				if (musicAudioSource.clip == null) return null;
				return musicAudioSource.clip.name.Split('_');
			}
		}

		[SerializeField] private AudioSource musicAudioSource = null;

		[SerializeField] private AudioClipsArray audioClips = null;

		private void Awake()
		{
			Init();
		}

		private void Init()
		{
			if (musicAudioSource == null)
			{
				musicAudioSource = GetComponent<AudioSource>();
			}

			//Mute Music Preference
			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				isMusicMute = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
			}

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey))
			{
				MusicVolume = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey);
			}
		}

		private void Start()
		{
			musicAudioSource.outputAudioMixerGroup = AudioManager.Instance.MusicMixer;
		}

		private void OnEnable()
		{
			UpdateManager.UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (playingRandomPlayList == false || CustomNetworkManager.IsHeadless) return;
			if (State != PlaybackState.Stopped) return;

			currentWaitTime += Time.deltaTime;
			if (currentWaitTime < TIME_BETWEEN_SONGS) return;

			currentWaitTime = 0f;
			_ = PlayRandomTrack();
		}

		public void StartPlayingRandomPlaylist()
		{
			if (CustomNetworkManager.IsHeadless) return;

			playingRandomPlayList = true;
		}

		public void StopPlaylist()
		{
			playingRandomPlayList = false;
			StopMusic();
		}

		public static void StopMusic()
		{
			Instance.musicAudioSource.Stop();
			Synth.Instance.StopMusic();
		}

		/// <summary>
		/// Plays a random music track.
		/// <returns>String[] that represents the picked song's name.</returns>
		/// </summary>
		public async Task<String[]> PlayRandomTrack()
		{
			var clip = audioClips.GetRandomClip();
			if (historyIndex < trackHistory.Count - 1)
			{
				trackHistory.RemoveRange(historyIndex + 1, trackHistory.Count - historyIndex - 1);
			}
			trackHistory.Add(clip);
			if (trackHistory.Count > MAX_HISTORY)
			{
				trackHistory.RemoveAt(0);
			}
			historyIndex = trackHistory.Count - 1;
			return await PlayClip(clip);
		}

		public async Task<String[]> PlayPreviousTrack()
		{
			if (historyIndex <= 0)
			{
				if (musicAudioSource == null || musicAudioSource.clip == null) return null;
				musicAudioSource.time = 0f;
				ResumeMusic();
				musicAudioSource.Play();
				return musicAudioSource.clip.name.Split('_');
			}
			historyIndex--;
			return await PlayClip(trackHistory[historyIndex]);
		}

		public void PauseMusic()
		{
			isPaused = true;
			musicAudioSource.Pause();
		}

		public void ResumeMusic()
		{
			isPaused = false;
			musicAudioSource.UnPause();
		}

		private async Task<String[]> PlayClip(AddressableAudioSource clip)
		{
			StopMusic();
			if (musicAudioSource == null) Init();
			isPaused = false;
			var audioSource = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource>{clip});
			if(audioSource == null)
			{
				Loggy.Error("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
				return null;
			}
			musicAudioSource.clip = audioSource.AudioSource.clip;
			musicAudioSource.mute = isMusicMute;
			musicAudioSource.volume = Instance.MusicVolume;
			AudioManager.MusicVolume(Instance.MusicVolume, false);
			musicAudioSource.Play();
			if (musicAudioSource.clip == null) return new string[]{ "ERROR",  "ERROR" , "ERROR",  "ERROR"};;
			return musicAudioSource.clip.name.Split('_');
		}

		/// <summary>
		/// Plays specific music track.
		/// <returns>String[] that represents the picked song's name.</returns>
		/// </summary>
		public async Task<string[]> PlayTrack(AddressableAudioSource addressableAudioSource)
		{
			if(addressableAudioSource == null)
			{
				Loggy.Error("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
				return null;
			}

			if(GameData.IsHeadlessServer)
				return null;

			addressableAudioSource = await AudioManager.GetAddressableAudioSourceFromCache(addressableAudioSource);

			if (isMusicPlaying())
			{
				await AudioManager.Instance.FadeMixerGroup("Music_Volume", 1000f, 0f);
				StopMusic();
			}
			AudioManager.MusicVolume(0f, false);
			musicAudioSource.clip = addressableAudioSource.AudioSource.clip;
			musicAudioSource.mute = isMusicMute;
			musicAudioSource.volume = Instance.MusicVolume;
			musicAudioSource.Play();

			float targetVolume = UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey)
				? UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey)
				: 0.8f
			;
			await AudioManager.Instance.FadeMixerGroup("Music_Volume", 1000f, targetVolume);

			return musicAudioSource.clip.name.Split('_');
		}

		public void ToggleMusicMute(bool mute)
		{
			isMusicMute = mute;
			musicAudioSource.mute = mute;
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
		/// Checks if music is being played or not.
		/// <returns> true if music is being played.</returns>
		/// </summary>
		public static bool isMusicPlaying()
		{
			if (Instance.musicAudioSource != null
			    && Instance.musicAudioSource.isPlaying
			    || (SunVox.SunVox.sv_end_of_song((int) Slot.Music) != 0))
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
			if (musicAudioSource != null)
			{
				musicAudioSource.volume = newVolume;
			}
			AudioManager.MusicVolume(newVolume);

			SaveNewVolume(newVolume);
		}

		private void SaveNewVolume(float newVolume)
		{
			UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.MusicVolumeKey, newVolume);
			UnityEngine.PlayerPrefs.Save();
		}

		/// <summary>
		/// Plays music for all clients.
		/// </summary>
		/// <param name="addressableAudioSource">The sound to be played.</param>
		/// <param name="audioSourceParameters">Extra parameters of the audio source</param>
		public static void PlayNetworked(AddressableAudioSource addressableAudioSource,
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
		{
			audioSourceParameters.MixerType = MixerType.Music;
			PlayMusicMessage.SendToAll(addressableAudioSource, audioSourceParameters);
		}

		/// <summary>
		/// Tell all clients to stop playing a song
		/// </summary>
		/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the sound to be stopped</returns>
		public static void StopNetworked(string songToken)
		{
			StopMusicMessage.SendToAll(songToken);
		}
	}
}
