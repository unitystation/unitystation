using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Audio.Containers
{
	[CreateAssetMenu(fileName = "AudioClipsArray", menuName = "ScriptableObjects/Audio/AudioClipsArray", order = 0)]
	public class AudioClipsArray : ScriptableObject
	{
		[FormerlySerializedAs("audioClips")] [SerializeField]
		private List<AddressableAudioSource> addressableAudioSource = new List<AddressableAudioSource>();

		[NonSerialized] private List<AddressableAudioSource> shuffledClips = new List<AddressableAudioSource>();

		public List<AddressableAudioSource> AddressableAudioSource => addressableAudioSource;

		private int i = 0;

		/// <summary>
		/// Return null if there are no clips
		/// </summary>
		/// <returns> Random clip in range </returns>
		public AddressableAudioSource GetRandomClip()
		{
			if (shuffledClips.Count == 0)
			{
				var hasTracks = Shuffle();
				if (!hasTracks)
				{
					return null;
				}
			}

			return shuffledClips.Wrap(i++);
		}

		private bool Shuffle()
		{
			if (addressableAudioSource.Count == 0)
			{
				return false;
			}

			shuffledClips = addressableAudioSource.OrderBy(clip => Random.value).ToList();
			return true;
		}
	}
}