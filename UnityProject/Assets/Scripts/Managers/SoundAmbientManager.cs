﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AddressableReferences;
using Audio.Containers;
using Initialisation;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Audio.Managers
{
	public class SoundAmbientManager : MonoBehaviour, IInitialise
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
		private Dictionary<string, AddressableAudioSource> ambientAudioSources = new Dictionary<string, AddressableAudioSource>();

		private Dictionary<AddressableAudioSource, string> playingSource = new Dictionary<AddressableAudioSource, string>();

		[SerializeField] private AudioClipsArray ambientSoundsArray = null;

		public static void PlayAudio(string assetAddress)
		{
			var audioSource = new AddressableAudioSource(assetAddress);

			if (Instance.playingSource.ContainsKey(audioSource))
			{
				SoundManager.Stop(Instance.playingSource[audioSource]);
				Instance.playingSource.Remove(audioSource);
			}

			var guid = Guid.NewGuid().ToString();
			Instance.playingSource.Add(audioSource, guid);
			SoundManager.Play(audioSource, guid);
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
			SoundManager.Play(source, guid);
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
			if (audioSource == null || Instance.playingSource.ContainsKey(audioSource) == false) return;

			audioSource.AudioSource.loop = false;
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

		/// <summary>
		/// Sets all ambient tracks to a certain volume
		/// </summary>
		/// <param name="newVolume"></param>
		public static void SetVolumeForAllAudioSources(float newVolume)
		{
			foreach (var audioSource in Instance.ambientAudioSources)
			{
				audioSource.Value.AudioSource.volume = newVolume;
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
				SetVolumeForAllAudioSources(0.2f);
			}
		}

		public InitialisationSystems Subsystem { get; }
		public void Initialise()
		{
			LoadClips(Instance.ambientSoundsArray.AddressableAudioSource);
		}

		public async Task LoadClips(List<AddressableAudioSource> audioSources)
		{
			foreach (var source in audioSources)
			{
				var sound = await SoundManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource>{source});

				if(Instance.ambientAudioSources.ContainsKey(sound.AudioSource.clip.name)) continue;

				Instance.ambientAudioSources.Add(sound.AudioSource.clip.name, sound);
			}
		}

		#endregion
	}
}