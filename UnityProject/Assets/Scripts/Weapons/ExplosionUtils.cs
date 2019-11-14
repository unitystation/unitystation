using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilities for explosions
/// </summary>
public static class ExplosionUtils
{
	private static readonly string[] EXPLOSION_SOUNDS = { "Explosion1", "Explosion2" };


	/// <summary>
	/// Play explosion sound and shake ground
	/// </summary>
	/// <param name="worldPosition">position explosion is centered at</param>
	/// <param name="shakeIntensity">intensity of shaking</param>
	/// <param name="shakeDistance">how far away the shaking can be felt</param>
	public static void PlaySoundAndShake(Vector3Int worldPosition, byte shakeIntensity, int shakeDistance)
	{
		string sndName = EXPLOSION_SOUNDS[Random.Range(0, EXPLOSION_SOUNDS.Length)];
		SoundManager.PlayNetworkedAtPos( sndName, worldPosition, -1f, true, true, shakeIntensity, (int)shakeDistance);
	}
}