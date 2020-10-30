using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;

namespace NPC
{
	public class GodAI : GenericFriendlyAI
	{
		public List<string> GenericSounds = new List<string>();

		public List<AddressableAudioSource> genericSounds = new List<AddressableAudioSource>();

		/// <summary>
		/// Changes Time that a sound has the chance to play
		/// WARNING, decreasing this time will decrease performance.
		/// </summary>
		public int PlaySoundTime = 3;

		private void Start()
		{
			PlaySound();
		}

		private void PlaySound()
		{
			if (IsDead || IsUnconscious || genericSounds.Count <= 0)
			{
				return;
			}
			if (!DMMath.Prob(30))
			{
				return;
			}
			{
				// JESTE_R
				SoundManager.PlayNetworkedAtPos(
					genericSounds.PickRandom(),
					transform.position,
					Random.Range(0.9f, 1.1f),
					sourceObj: gameObject);

			}
			Invoke(nameof(PlaySound), PlaySoundTime);
		}
	}
}
