using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Square shape with center position and equal radius in all directions from center
/// </summary>
public class SquareEffectShape  : EffectShape
{
	public readonly Vector3Int Center;
	public readonly int Radius;

	/// <summary>
	/// Creates square shape with center position and radius in all directions from center
	/// Tiles iteration starting from left bottom
	/// </summary>
	/// <param name="center">Square center</param>
	/// <param name="radius">Radius from square center</param>
	public SquareEffectShape(Vector3Int center, int radius)
	{
		this.Center = center;
		this.Radius = radius;
	}

	/// <summary>
	/// The size of the square will always be 2 * radius + 1
	/// </summary>
	public int Size
	{
		get { return this.Radius * 2 + 1; }
	}

	public override IEnumerator<Vector3Int> GetEnumerator()
	{
		if (Radius <= 0)
		{
			yield return Center;
			yield break;
		}

		// starting from left bottom
		for (int i = -Radius; i <= Radius; i++)
		{
			for (int j = -Radius; j <= Radius; j++)
			{
				Vector3Int checkPos = new Vector3Int(Center.x + i, Center.y + j, 0);
				yield return checkPos;
			}
		}
	}
}
