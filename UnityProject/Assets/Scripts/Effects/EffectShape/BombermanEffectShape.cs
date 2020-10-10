using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plus + effect
/// </summary>
public class BombermanEffectShape : EffectShape
{
	public readonly Vector3Int Center;
    public readonly int Radius;

    public BombermanEffectShape(Vector3Int center, int radius)
    {
	    Center = center;
	    Radius = radius;
    }

    public override IEnumerator<Vector3Int> GetEnumerator()
	{
		for (int i = -Radius; i <= Radius; i++)
		{
			Vector3Int xPos = new Vector3Int(Center.x + i, Center.y, 0);
			yield return xPos;
		}
		for (int j = -Radius; j <= Radius; j++)
		{
			Vector3Int yPos = new Vector3Int(Center.x, Center.y + j, 0);
			yield return yPos;
		}
	}
}
