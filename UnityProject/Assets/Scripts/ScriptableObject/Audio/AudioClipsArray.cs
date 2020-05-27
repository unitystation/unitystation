using UnityEngine;

namespace Audio
{
	[CreateAssetMenu(fileName = "AudioClipsArray", menuName = "ScriptableObjects/Audio/AudioClipsArray", order = 0)]
	public class AudioClipsArray : ScriptableObject
	{
		public AudioClip[] audioClips;

		public AudioClip GetRandomClip()
		{
			return audioClips[Random.Range(0, audioClips.Length)];
		}
	}
}