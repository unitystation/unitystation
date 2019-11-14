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


	/// <summary>
	/// Creates square shape with center position and margin on each side
	/// The size of the square will always be 2 * margin + 1
	/// </summary>
	/// <param name="center">Square center</param>
	/// <param name="margin">Margin from square center</param>
	/// <returns>All tiles in a shape, starting from left bottom</returns>
	public static List<Vector3Int> CreateSquareShapeMargin(Vector3Int center, int margin)
	{
		var square = new List<Vector3Int>();
		if (margin > 0)
		{
			for (int i = -margin; i <= margin; i++)
			{
				for (int j = -margin; j <= margin; j++)
				{
					Vector3Int checkPos = new Vector3Int(center.x + i, center.y + j, 0);
					square.Add(checkPos);
				}
			}
		}
		else
		{
			// leave only center
			square.Add(center);
		}


		return square;
	}
}