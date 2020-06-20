using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Audio.Containers;
using UnityEngine.Audio;

namespace Audio.Managers
{
	public class SoundAmbientManager : MonoBehaviour
	{
		private static SoundAmbientManager soundAmbientManager;
		public static SoundAmbientManager Instance
		{
			get
			{
				if (soundAmbientManager == null)
				{
					soundAmbientManager = FindObjectOfType<SoundAmbientManager>();
				}

				return soundAmbientManager;
			}
		}

		/// <summary>
		/// Cache of audioSources on the Manager
		/// </summary>
		private List<AudioSource> ambientAudioSources = new List<AudioSource>();

		[SerializeField] private AudioClipsArray audioClips = null;
		[SerializeField] private AudioMixerGroup audioMixerGroup = null;

		public static void StopAudio(string trackName)
		{
			var audioSource = GetAudioSourceOrNull(trackName);
			if (audioSource == null) return;

			audioSource.loop = false;
			audioSource.Stop();
		}

		public static void PlayAudio(string trackName, bool isLooped = false)
		{
			var audioSource = GetAudioSourceOrNull(trackName);
			if (audioSource == null) return;

			audioSource.loop = isLooped;
			audioSource.Play();
		}

		private static AudioSource GetAudioSourceOrNull(string trackName)
		{
			if (trackName == null)
			{
				Logger.LogError("Track name is null.", Category.SoundFX);
				return null;
			}

			return TryFindAudioSource(trackName);
		}

		public static void StopAudio(AudioClip clip)
		{
			var audioSource = GetAudioSourceOrNull(clip);
			if (audioSource == null) return;

			audioSource.loop = false;
			audioSource.Stop();

		}

		public static void PlayAudio(AudioClip clip, bool isLooped = false)
		{
			var audioSource = GetAudioSourceOrNull(clip);
			if (audioSource == null) return;

			audioSource.loop = isLooped;
			audioSource.Play();
		}

		private static AudioSource GetAudioSourceOrNull(AudioClip clip)
		{
			if (clip == null)
			{
				Logger.LogError("Clip is null.", Category.SoundFX);
				return null;
			}

			return TryFindAudioSource(clip.name);
		}

		private static AudioSource TryFindAudioSource(string trackName)
		{
			var audioSource = FindAudioSource(trackName);

			if (audioSource == null)
			{
				Logger.LogWarning($"Didn't find track: {trackName}", Category.SoundFX);
				return null;
			}

			return audioSource;
		}

		private static AudioSource FindAudioSource(string trackName)
		{
			foreach (var audioSource in Instance.ambientAudioSources)
			{
				if (audioSource.name == trackName)
				{
					return audioSource;
				}
			}

			return null;
		}

		/// <summary>
		/// Stops all AudioSources on this manager
		/// </summary>
		public static void StopAllAudio()
		{
			foreach (var audioSource in Instance.ambientAudioSources)
			{
				audioSource.Stop();
			}
		}

		/// <summary>
		/// Sets all ambient tracks to a certain volume
		/// </summary>
		/// <param name="newVolume"></param>
		public static void SetVolumeForAllAudioSources(float newVolume)
		{
			foreach (var audioSource in Instance.ambientAudioSources)
			{
				audioSource.volume = newVolume;
			}

			PlayerPrefs.SetFloat(PlayerPrefKeys.AmbientVolumeKey, newVolume);
			PlayerPrefs.Save();
		}

		#region Initialization

		private void Awake()
		{
			SetVolumeWithPlayerPrefs();
		}

		private void SetVolumeWithPlayerPrefs()
		{
			if (PlayerPrefs.HasKey(PlayerPrefKeys.AmbientVolumeKey))
			{
				SetVolumeForAllAudioSources(PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey));
			}
			else
			{
				SetVolumeForAllAudioSources(1f);
			}
		}

		private void OnEnable()
		{
			CreateAudioSources();

			// Cache AudioSources on the manager
			var managerAudioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
			CacheAudioSources(managerAudioSources);
		}

		/// <summary>
		/// Cache audioSources so we can control them at runtime
		/// </summary>
		/// <param name="managerAudioSources"> AudioSources on the manager </param>
		private void CacheAudioSources(IEnumerable<AudioSource> managerAudioSources)
		{
			foreach (var audioSource in managerAudioSources)
			{
				if (ambientAudioSources.Contains(audioSource) == false
				    && audioClips.AudioClips.Any(a => a.name == audioSource.name))
				{
					ambientAudioSources.Add(audioSource);
				}
			}
		}

		/// <summary>
		/// Manager creates GameObjects with AudiosSource for each clip
		/// </summary>
		private void CreateAudioSources()
		{
			if (audioClips == null)
			{
				var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
				DestroyGameObjects(audioSources);
				ambientAudioSources.Clear();
			}
			else
			{
				// Compare AudioSources on the manager and create new
				var managerAudioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
				CreateNewAudioSources(GetMissingClips(managerAudioSources));

				// Clearing Manager children from audio sources which are not in audioClips
				DestroyGameObjects(GetRedundantAudioSources(managerAudioSources));
			}
		}

		/// <summary>
		/// Destroys GameObjects with AudioSource component
		/// </summary>
		/// <param name="audioSources"> GameObjects to destroy </param>
		private void DestroyGameObjects(IEnumerable<AudioSource> audioSources)
		{
			foreach (var source in audioSources)
			{
				DestroyImmediate(source.gameObject);
			}
		}

		/// <summary>
		/// Creates GameObjects with AudioSource component for each clip
		/// </summary>
		/// <param name="newClips"> Clips to add as AudioSources </param>
		private void CreateNewAudioSources(IEnumerable<AudioClip> newClips)
		{
			foreach (var clip in newClips)
			{
				var newGameObject = new GameObject(clip.name);
				newGameObject.transform.parent = gameObject.transform;

				var audioSource = newGameObject.AddComponent<AudioSource>();
				audioSource.outputAudioMixerGroup = audioMixerGroup;
				audioSource.clip = clip;
				audioSource.playOnAwake = false;
			}
		}

		/// <summary>
		/// Get clips which are not on the manager as AudioSources
		/// </summary>
		/// <returns> Array of new clips </returns>
		private IEnumerable<AudioClip> GetMissingClips(IEnumerable<AudioSource> managerAudioSources)
		{
			var newClips = new List<AudioClip>();

			foreach (var clip in audioClips.AudioClips)
			{
				if(managerAudioSources.Any( a => a.name == clip.name)) continue;

				newClips.Add(clip);
			}

			return newClips;
		}

		/// <summary>
		/// Compares clips names with Manager children which have AudioSource component
		/// </summary>
		/// <param name="audioSources"></param>
		/// <returns> AudiosSources on Manager which are not in audioClips</returns>
		private IEnumerable<AudioSource> GetRedundantAudioSources(IEnumerable<AudioSource> audioSources)
		{
			var redundantAudioSources = new List<AudioSource>();

			foreach (var source in audioSources)
			{
				if(audioClips.AudioClips.Any(c => c.name == source.name)) continue;
				Logger.Log(source.name);
				redundantAudioSources.Add(source);
			}

			return redundantAudioSources;
		}

		#endregion
	}
}