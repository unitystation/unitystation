

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Outline of a square shape with center position and equal radius in all directions from center
/// </summary>
public class OutlineSquareEffectShape  : EffectShape
{
	public readonly Vector3Int Center;
	public readonly int Radius;
	public readonly bool WithCorners;

	/// <summary>
	/// Creates square shape with center position and radius in all directions from center
	/// Tiles iteration starting from left bottom
	/// </summary>
	/// <param name="center">Square center</param>
	/// <param name="radius">Radius from square center</param>
	public OutlineSquareEffectShape(Vector3Int center, int radius, bool withCorners = true)
	{
		this.Center = center;
		this.Radius = radius;
		this.WithCorners = withCorners;
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
			if (i == -Radius || i == Radius)
			{
				for (int j = -Radius; j <= Radius; j++)
				{
					//Ignore corners is set to
					if (!WithCorners && ((i == -Radius && (j == -Radius || j == Radius)) || (i == Radius && (j == -Radius || j == Radius))))
					{
						continue;
					}

					Vector3Int checkPos = new Vector3Int(Center.x + i, Center.y + j, 0);
					yield return checkPos;
				}
			}
			else
			{
				Vector3Int checkPosTop = new Vector3Int(Center.x + i, Center.y + Radius, 0);
				yield return checkPosTop;

				Vector3Int checkPosBottom = new Vector3Int(Center.x + i, Center.y - Radius, 0);
				yield return checkPosBottom;
			}
		}
	}
}
