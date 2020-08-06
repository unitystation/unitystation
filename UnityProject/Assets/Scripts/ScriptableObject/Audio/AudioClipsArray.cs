using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio.Containers
{
	[CreateAssetMenu(fileName = "AudioClipsArray", menuName = "ScriptableObjects/Audio/AudioClipsArray", order = 0)]
	public class AudioClipsArray : ScriptableObject
	{
		[SerializeField] private AudioClip[] audioClips = null;

		public AudioClip[] AudioClips => audioClips;

		/// <summary>
		/// Return null if there are no clips
		/// </summary>
		/// <returns> Random clip in range </returns>
		public AudioClip GetRandomClip()
		{
			if (audioClips == null || audioClips.Length == 0)
			{
				return null;
			}
			else
			{
				return audioClips[Random.Range(0, audioClips.Length)];
			}
		}

		#region Editor

		private void OnValidate()
		{
			RemoveNulls();
			RemoveDuplicates();
		}

		private void RemoveNulls()
		{
			if (audioClips.Length == 0) return;
			var audioList = audioClips.ToList();
			for (int i = audioList.Count - 1; i >= 0 ; i--)
			{
				if (audioList[i] == null)
				{
					audioList.RemoveAt(i);
				}
			}

			audioClips = audioList.ToArray();
		}

		private void RemoveDuplicates()
		{
			if (audioClips.Length == 0) return;
			var audioList = audioClips.ToList();
			if (audioList.GroupBy(x => x.name).Any(g => g.Count() > 1) == false) return;
			audioList = audioList.OrderBy(c => c.name).ToList();
			for (int i = audioList.Count - 2; i >= 0 ; i--)
			{
				if (audioList[i] == audioList[i + 1])
				{
					audioList.RemoveAt(i);
				}
			}

			audioClips = audioList.ToArray();
		}

		#endregion
	}
}