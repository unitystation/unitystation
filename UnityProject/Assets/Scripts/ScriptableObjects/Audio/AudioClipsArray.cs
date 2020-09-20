using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio.Containers
{
	[CreateAssetMenu(fileName = "AudioClipsArray", menuName = "ScriptableObjects/Audio/AudioClipsArray", order = 0)]
	public class AudioClipsArray : ScriptableObject
	{
		[SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();

		[NonSerialized] private List<AudioClip> shuffledClips = new List<AudioClip>();

		public List<AudioClip> AudioClips => audioClips;

		private int i = 0;

		/// <summary>
		/// Return null if there are no clips
		/// </summary>
		/// <returns> Random clip in range </returns>
		public AudioClip GetRandomClip()
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
			if (audioClips.Count == 0)
			{
				return false;
			}

			shuffledClips = audioClips.OrderBy(clip => Random.value).ToList();
			return true;
		}

		#region Editor

		private void OnValidate()
		{
			RemoveNulls();
			RemoveDuplicates();
		}

		private void RemoveNulls()
		{
			if (audioClips.Count == 0) return;
			var audioList = audioClips.ToList();
			for (int i = audioList.Count - 1; i >= 0; i--)
			{
				if (audioList[i] == null)
				{
					audioList.RemoveAt(i);
				}
			}

			audioClips = audioList;
		}

		private void RemoveDuplicates()
		{
			if (audioClips.Count == 0) return;
			var audioList = audioClips.ToList();
			if (audioList.GroupBy(x => x.name).Any(g => g.Count() > 1) == false) return;
			audioList = audioList.OrderBy(c => c.name).ToList();
			for (int i = audioList.Count - 2; i >= 0; i--)
			{
				if (audioList[i] == audioList[i + 1])
				{
					audioList.RemoveAt(i);
				}
			}

			audioClips = audioList;
		}

		#endregion
	}
}