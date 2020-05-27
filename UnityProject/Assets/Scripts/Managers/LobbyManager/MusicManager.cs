using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio
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
		[Range(0f, 1f)] public float MusicVolume = 1;

		private AudioSource currentLobbyAudioSource;

		[SerializeField] private AudioClipsArray audioClips;

		private void Awake()
		{
			Init();
		}

		private void Init()
		{
			currentLobbyAudioSource = GetComponent<AudioSource>();
			//Mute Music Preference
			if (PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				isMusicMute = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
			}

			if (PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolume))
			{
				MusicVolume = PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolume);
				currentLobbyAudioSource.volume =  PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolume);
			}
			else
			{
				SaveNewVolume(1f);
			}
		}

		public static void StopMusic()
		{
			/*foreach (AudioSource track in Instance.musicTracks)
			{
				track.Stop();
			}*/

			Synth.Instance.StopMusic();
		}

		/// <summary>
		/// Plays a random music track.
		/// Using two diiferent ways to play tracks, some tracks are normal audio and some are tracker files played by sunvox.
		/// <returns>String[] that represents the picked song's name.</returns>
		/// </summary>
		public String[] PlayRandomTrack()
		{
			StopMusic();
			String[] songInfo;

			// To make sure not to play the last song that just played,
			// every time a track is played, it's either a normal audio or track played by sunvox, alternatively.

				//Traditional music
			var randTrack = audioClips.GetRandomClip();
			currentLobbyAudioSource.clip = randTrack;
			var volume = Instance.MusicVolume;

			currentLobbyAudioSource.mute = isMusicMute;
			currentLobbyAudioSource.volume = volume;
			currentLobbyAudioSource.Play();
			songInfo = currentLobbyAudioSource.clip.name.Split('_'); // Spliting to get the song and artist name

			/*else
			{
				currentLobbyAudioSource = null;
				//Tracker music
				var trackerMusic = new[]
				{
					"Spaceman_HERB.xm",
					"Echo sound_4mat.xm",
					"Tintin on the Moon_Jeroen Tel.xm"
				};
				var songPicked = trackerMusic.Wrap(Random.Range(1, 100));
				var vol = 255 * Instance.MusicVolume;

				Synth.Instance.PlayMusic(songPicked, false, (byte) (int) vol);
				songPicked = songPicked.Split('.')[0]; // Throwing away the .xm extension in the string
				songInfo = songPicked.Split('_'); // Spliting to get the song and artist name
			}*/

			return songInfo;
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
			// Checks if an audiosource or a track by sunvox is being played(Since there are two diiferent ways to play tracks)
			if (Instance.currentLobbyAudioSource != null && Instance.currentLobbyAudioSource.isPlaying ||
			    !(SunVox.sv_end_of_song((int) Slot.Music) == 1))
				return true;

			return false;
		}

		public void ChangeVolume(float newVolume)
		{
			MusicVolume = newVolume;
			currentLobbyAudioSource.volume = newVolume;

			SaveNewVolume(newVolume);
		}

		private void SaveNewVolume(float newVolume)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.MusicVolume, newVolume);
			PlayerPrefs.Save();
		}
	}
}