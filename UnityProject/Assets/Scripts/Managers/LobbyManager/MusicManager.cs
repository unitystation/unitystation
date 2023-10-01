using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using AddressableReferences;
using Logs;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;
using Shared.Util;
using UnityEngine.Audio;
using Util;

namespace Audio.Containers
{
	public class MusicManager : MonoBehaviour
	{
		private static MusicManager musicManager;
		public static MusicManager Instance => FindUtils.LazyFindObject(ref musicManager);

		public string currentNetworkedSong = "";

		[SerializeField] private SongTracker songTracker = null;
		/// <summary>
		/// For controlling the song play list. Includes random shuffle and auto play
		/// </summary>
		public static SongTracker SongTracker => Instance.songTracker;

		private bool isMusicMute;
		[Range(0f, 1f)] public float MusicVolume = 0.5f;

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
			if (PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				isMusicMute = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
			}
		}

		private void Start()
		{
			musicAudioSource.outputAudioMixerGroup = AudioManager.Instance.MusicMixer;
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
			StopMusic();
			if (musicAudioSource == null) Init();
			var audioSource = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource>{audioClips.GetRandomClip()});
			if(audioSource == null)
			{
				Loggy.LogError("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
				return null;
			}
			musicAudioSource.clip = audioSource.AudioSource.clip;
			musicAudioSource.mute = isMusicMute;
			musicAudioSource.volume = Instance.MusicVolume;
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
				Loggy.LogError("MusicManager failed to load a song, is Addressables loaded?", Category.Audio);
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

			float targetVolume = PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey)
				? PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey)
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
