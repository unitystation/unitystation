using System.Collections.Generic;
using UnityEngine;

namespace Systems.MobAIs
{
	public class GodAI : GenericFriendlyAI
	{
		public List<string> GenericSounds = new List<string>();

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
			if (IsDead || IsUnconscious || GenericSounds.Count <= 0)
			{
				return;
			}
			if (!DMMath.Prob(30))
			{
				return;
			}
			{
				SoundManager.PlayNetworkedAtPos(
					GenericSounds.PickRandom(),
					transform.position,
					Random.Range(0.9f, 1.1f),
					sourceObj: gameObject);
			}
			Invoke(nameof(PlaySound), PlaySoundTime);
		}
	}
}
