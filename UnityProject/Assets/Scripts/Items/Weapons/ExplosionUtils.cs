using System.Collections.Generic;
using UnityEngine;

namespace Systems.Explosions
{
	/// <summary>
	/// Utilities for explosions
	/// </summary>
	public static class ExplosionUtils
	{
		private static readonly string[] EXPLOSION_SOUNDS = { "Explosion1", "Explosion2" };
		private static readonly string[] DISTANT_EXPLOSION_SOUNDS = { "ExplosionDistant1", "ExplosionDistant2" };
		private static readonly string[] STATION_GROAN_SOUNDS = { "ExplosionCreak1", "ExplosionCreak2", "ExplosionCreak3" };

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
			string sndName = EXPLOSION_SOUNDS[Random.Range(0, EXPLOSION_SOUNDS.Length)];
			SoundManager.PlayNetworkedAtPos(sndName, worldPosition, -1f, true, true, shakeIntensity, shakeDistance, false);

			if (shakeDistance > DISTANT_THRESHOLD)
			{
				string distantName = DISTANT_EXPLOSION_SOUNDS[Random.Range(0, DISTANT_EXPLOSION_SOUNDS.Length)];
				SoundManager.PlayNetworkedAtPos(distantName, worldPosition, global: true);
			}
			if (shakeIntensity > GROAN_THRESHOLD)
			{
				string groanName = STATION_GROAN_SOUNDS[Random.Range(0, STATION_GROAN_SOUNDS.Length)];
				SoundManager.PlayNetworkedAtPos(groanName, worldPosition, global: true);
			}
		}
	}
}
