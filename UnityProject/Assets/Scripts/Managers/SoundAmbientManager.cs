using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AddressableReferences;
using Audio.Containers;
using Initialisation;
using Logs;
using Messages.Server.SoundMessages;
using Shared.Util;
using Util;

namespace Audio.Managers
{
	public class SoundAmbientManager : MonoBehaviour, IInitialise
	{
		private static SoundAmbientManager soundAmbientManager;
		public static SoundAmbientManager Instance => FindUtils.LazyFindObject(ref soundAmbientManager);

		/// <summary>
		/// Cache of audioSources on the Manager
		/// </summary>
		private Dictionary<string, AddressableAudioSource> ambientAudioSources = new Dictionary<string, AddressableAudioSource>();

		private Dictionary<AddressableAudioSource, string> playingSource = new Dictionary<AddressableAudioSource, string>();

		[SerializeField] private AudioClipsArray ambientSoundsArray = null;

		private static AudioSourceParameters parameters = new AudioSourceParameters();

		#region Initialization

		private void Awake()
		{
			parameters.MixerType = MixerType.Ambient;
		}

		public InitialisationSystems Subsystem { get; }

		public void Initialise()
		{
			_ = LoadClips(Instance.ambientSoundsArray.AddressableAudioSource);
		}

		public async Task LoadClips(List<AddressableAudioSource> audioSources)
		{
			foreach (var source in audioSources)
			{
				var sound = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource> { source });

				if (Instance.ambientAudioSources.ContainsKey(sound.AudioSource.clip.name)) continue;

				Instance.ambientAudioSources.Add(sound.AudioSource.clip.name, sound);
			}
		}

		#endregion

		public static void PlayAudio(string assetAddress)
		{
			if(string.IsNullOrEmpty(assetAddress))
			{
				Loggy.LogError("Cannot play ambient noise because asset address is empty or null");
				return;
			}
			var audioSource = new AddressableAudioSource(assetAddress);

			if (Instance.playingSource.ContainsKey(audioSource))
			{
				SoundManager.Stop(Instance.playingSource[audioSource]);
				Instance.playingSource.Remove(audioSource);
			}

			var guid = Guid.NewGuid().ToString();
			Instance.playingSource.Add(audioSource, guid);
			_ = SoundManager.Play(audioSource, guid, parameters);
		}

		public static void PlayAudio(AddressableAudioSource source)
		{
			if (source == null)
			{
				return;
			}

			if (Instance.playingSource.ContainsKey(source))
			{
				SoundManager.Stop(Instance.playingSource[source]);
				Instance.playingSource.Remove(source);
			}

			var guid = Guid.NewGuid().ToString();
			Instance.playingSource.Add(source, guid);
			_ = SoundManager.Play(source, guid, parameters);
		}

		public static void StopAudio(string assetAddress)
		{
			var audioSource = new AddressableAudioSource(assetAddress);

			if (Instance.playingSource.ContainsKey(audioSource) == false) return;

			audioSource.AudioSource.loop = false;
			SoundManager.Stop(Instance.playingSource[audioSource]);
		}

		public static void StopAudio(AddressableAudioSource audioSource)
		{
			if (audioSource == null  || Instance.playingSource.ContainsKey(audioSource) == false) return;

			if (audioSource.AudioSource != null)
			{
				audioSource.AudioSource.loop = false;
			}

			SoundManager.Stop(Instance.playingSource[audioSource]);
		}

		/// <summary>
		/// Stops all AudioSources on this manager
		/// </summary>
		public static void StopAllAudio()
		{
			foreach (var audioSource in Instance.playingSource)
			{
				SoundManager.Stop(audioSource.Value);
			}
		}
	}
}
