using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;

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

		public string currentNetworkedSong = "";

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

			//Mute Music Preference
			if (PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				isMusicMute = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
			}
		}

		private void Start()
		{
			currentLobbyAudioSource.outputAudioMixerGroup = AudioManager.Instance.MusicMixer;
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

		/// <summary>
		/// Plays music for all clients.
		/// </summary>
		/// <param name="addressableAudioSource">The sound to be played.</param>
		/// <param name="audioSourceParameters">Extra parameters of the audio source</param>
		/// <param name="polyphonic">Is the sound to be played polyphonic</param>
		/// <param name="shakeParameters">Extra parameters that define the sound's associated shake</param>
		public static void PlayNetworked(AddressableAudioSource addressableAudioSource,
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
			ShakeParameters shakeParameters = new ShakeParameters())
		{
			if (Instance.currentNetworkedSong != "")
			{
				StopNetworked(Instance.currentNetworkedSong);
			}
			audioSourceParameters.MixerType = MixerType.Music;
			Instance.currentNetworkedSong = PlaySoundMessage.SendToAll(addressableAudioSource, TransformState.HiddenPos, polyphonic, null, shakeParameters, audioSourceParameters);
		}

		/// <summary>
		/// Tell all clients to stop playing a song
		/// </summary>
		/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the sound to be stopped</returns>
		public static void StopNetworked(string songToken)
		{
			StopSoundMessage.SendToAll(songToken);
		}
	}
}
