using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using UnityEngine;


namespace Systems.Explosions
{
	/// <summary>
	/// Utilities for explosions
	/// </summary>
	public static class ExplosionUtils
	{
		private static readonly AddressableAudioSource[] EXPLOSION_SOUNDS = { CommonSounds.Instance.Explosion1, CommonSounds.Instance.Explosion2 };
		private static readonly AddressableAudioSource[] DISTANT_EXPLOSION_SOUNDS = { CommonSounds.Instance.ExplosionDistant1, CommonSounds.Instance.ExplosionDistant2 };
		private static readonly AddressableAudioSource[] STATION_GROAN_SOUNDS = { CommonSounds.Instance.ExplosionCreak1, CommonSounds.Instance.ExplosionCreak2, CommonSounds.Instance.ExplosionCreak3 };

		private static readonly int DISTANT_THRESHOLD = 63;
		private static readonly int GROAN_THRESHOLD = 29;

		/// <summary>
		/// Play explosion sound and shake ground
		/// </summary>
		/// <param name="worldPosition">position explosion is centered at</param>
		/// <param name="shakeIntensity">intensity of shaking</param>
		/// <param name="shakeDistance">how far away the shaking can be felt</param>
		public static void PlaySoundAndShake(Vector3Int worldPosition, byte shakeIntensity, int shakeDistance)
		{
			AddressableAudioSource sndName = EXPLOSION_SOUNDS[Random.Range(0, EXPLOSION_SOUNDS.Length)];
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(0f, 100f);
			ShakeParameters shakeParameters = new ShakeParameters(true, shakeIntensity, shakeDistance);
			_ = SoundManager.PlayNetworkedAtPosAsync(sndName, worldPosition, audioSourceParameters, true, false, shakeParameters: shakeParameters);

			if (shakeDistance > DISTANT_THRESHOLD)
			{
				AddressableAudioSource distantName = DISTANT_EXPLOSION_SOUNDS[Random.Range(0, DISTANT_EXPLOSION_SOUNDS.Length)];
				_ = SoundManager.PlayNetworkedAtPosAsync(distantName, worldPosition, global: true);
			}
			if (shakeIntensity > GROAN_THRESHOLD)
			{
				AddressableAudioSource groanName = STATION_GROAN_SOUNDS[Random.Range(0, STATION_GROAN_SOUNDS.Length)];
				_ = SoundManager.PlayNetworkedAtPosAsync(groanName, worldPosition, global: true);
			}
		}
	}
}
