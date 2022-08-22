using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using UnityEngine;


namespace Systems.MobAIs
{
	public class GodAI : GenericFriendlyAI
	{
		public List<AddressableAudioSource> GenericSounds = new List<AddressableAudioSource>();

		/// <summary>
		/// Changes Time that a sound has the chance to play
		/// WARNING, decreasing this time will decrease performance.
		/// </summary>
		public int PlaySoundTime = 3;

		protected override void OnAIStart()
		{
			base.OnAIStart();
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
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
				SoundManager.PlayNetworkedAtPos(GenericSounds.PickRandom(),	transform.position,
					audioSourceParameters, sourceObj: gameObject);
			}
			Invoke(nameof(PlaySound), PlaySoundTime);
		}
	}
}
