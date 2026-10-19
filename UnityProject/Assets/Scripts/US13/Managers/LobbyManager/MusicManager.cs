using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logs;
using Shared.Util;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Sound;
using US13.Messages.Server.SoundMessages;
using US13.Managers.Music;
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

		#region Robust Music Manager (bounty #6787)

		[Header("Robust Music Manager")]
		[Tooltip("Optional server-host configurable music library. Allows adding lobby, admin, and trigger music without code changes.")]
		[SerializeField] private MusicLibrarySO musicLibrary = null;

		/// <summary>
		/// The track currently managed by the scoring system (may be null).
		/// </summary>
		public MusicTrackSO CurrentTrack { get; private set; }

		/// <summary>
		/// True while a scored track is playing (used for override decisions).
		/// </summary>
		public bool IsScoredTrackPlaying => CurrentTrack != null && isMusicPlaying();

		private Coroutine fadeCoroutine;

		/// <summary>
		/// Assigns a server-host music library at runtime (e.g. loaded from config).
		/// </summary>
		public void SetMusicLibrary(MusicLibrarySO library)
		{
			musicLibrary = library;
		}

		/// <summary>
		/// Plays a scored track, respecting playback mode and the override/scoring rules.
		/// A higher-score Global track fades out and replaces a lower-score Local track.
		/// A lower-score track is ignored while a higher-score track is playing.
		/// Diagetic tracks are routed through the ambient system and do not compete here.
		/// </summary>
		/// <returns>True if the track was accepted and playback started (or is fading in).</returns>
		public async Task<bool> PlayScoredTrack(MusicTrackSO track)
		{
			if (track == null || track.Clip == null)
			{
				Loggy.Warning("MusicManager.PlayScoredTrack called with a null track or clip.", Category.Audio);
				return false;
			}

			if (GameData.IsHeadlessServer) return false;

			// Diagetic tracks are positional and handled by the ambient/sound system, not this manager.
			if (track.PlaybackMode == MusicPlaybackMode.Diagetic)
			{
				await PlayDiagetic(track);
				return true;
			}

			// Scoring: reject a track that cannot override the currently playing scored track.
			if (IsScoredTrackPlaying && track.Score < CurrentTrack.Score)
			{
				Loggy.Info($"MusicManager: ignoring \"{track.TrackName}\" (score {track.Score}) — " +
				           $"\"{CurrentTrack.TrackName}\" (score {CurrentTrack.Score}) is playing.", Category.Audio);
				return false;
			}

			// Fade out whatever is playing before starting the new track.
			if (isMusicPlaying())
			{
				var fadeOut = CurrentTrack != null ? CurrentTrack.FadeOutDuration : 1f;
				await FadeOutAndStop(fadeOut);
			}

			CurrentTrack = track;
			await PlayTrackInternal(track);
			return true;
		}

		/// <summary>
		/// Fires a registered trigger by id. Rolls the trigger's chance/cooldown and, on success,
		/// plays the chosen track via the scoring system.
		/// </summary>
		/// <returns>True if a track was selected and playback was attempted.</returns>
		public async Task<bool> FireTrigger(string triggerId)
		{
			if (musicLibrary == null)
			{
				Loggy.Warning("MusicManager.FireTrigger: no MusicLibrarySO assigned.", Category.Audio);
				return false;
			}

			var trigger = musicLibrary.GetTrigger(triggerId);
			if (trigger == null)
			{
				Loggy.Warning($"MusicManager.FireTrigger: trigger \"{triggerId}\" not found in library.", Category.Audio);
				return false;
			}

			var track = trigger.TryFire();
			if (track == null) return false; // failed the chance roll or cooldown

			return await PlayScoredTrack(track);
		}

		/// <summary>
		/// Stops the current scored track with its configured fade-out duration.
		/// </summary>
		public async Task StopScoredTrack()
		{
			var fadeOut = CurrentTrack != null ? CurrentTrack.FadeOutDuration : 1f;
			await FadeOutAndStop(fadeOut);
			CurrentTrack = null;
		}

		private async Task PlayTrackInternal(MusicTrackSO track)
		{
			var audioSource = await AudioManager.GetAddressableAudioSourceFromCache(
				new List<AddressableAudioSource> { track.Clip });
			if (audioSource == null || audioSource.AudioSource == null || audioSource.AudioSource.clip == null)
			{
				Loggy.Error("MusicManager failed to load a scored track; is Addressables loaded?", Category.Audio);
				CurrentTrack = null;
				return;
			}

			if (musicAudioSource == null) Init();

			musicAudioSource.clip = audioSource.AudioSource.clip;
			musicAudioSource.mute = isMusicMute;
			musicAudioSource.volume = MusicVolume;
			musicAudioSource.Play();

			AudioManager.MusicVolume(0f, false);
			if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
			fadeCoroutine = StartCoroutine(FadeMixerTo(track.FadeInDuration, MusicVolume));

			currentNetworkedSong = track.TrackName;
		}

		private async Task PlayDiagetic(MusicTrackSO track)
		{
			// Diagetic music is positional; route through the ambient manager at the listener's position.
			if (SoundAmbientManager.Instance == null) return;
			var audioSource = await AudioManager.GetAddressableAudioSourceFromCache(
				new List<AddressableAudioSource> { track.Clip });
			if (audioSource?.AudioSource?.clip != null)
			{
				SoundAmbientManager.PlayAudio(track.Clip.name);
			}
		}

		private async Task FadeOutAndStop(float duration)
		{
			if (duration <= 0f || !isMusicPlaying())
			{
				StopMusic();
				return;
			}

			if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
			fadeCoroutine = StartCoroutine(FadeMixerTo(duration, 0f, stopAfter: true));
			// Await roughly the fade duration so the next track starts after the fade completes.
			await Task.Delay(TimeSpan.FromSeconds(duration));
		}

		private IEnumerator FadeMixerTo(float duration, float targetVolume, bool stopAfter = false)
		{
			if (duration <= 0f)
			{
				AudioManager.MusicVolume(targetVolume, false);
				if (stopAfter) StopMusic();
				yield break;
			}

			float startVolume = musicAudioSource != null ? musicAudioSource.volume : 0f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float volume = Mathf.Lerp(startVolume, targetVolume, t);

				if (musicAudioSource != null) musicAudioSource.volume = volume;
				AudioManager.MusicVolume(volume, false);

				yield return null;
			}

			if (musicAudioSource != null) musicAudioSource.volume = targetVolume;
			AudioManager.MusicVolume(targetVolume, false);
			if (stopAfter) StopMusic();
			fadeCoroutine = null;
		}

		#endregion

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
