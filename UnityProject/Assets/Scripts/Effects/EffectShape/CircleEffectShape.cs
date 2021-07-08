using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Diamond without tip
/// </summary>
public class CircleEffectShape : EffectShape
{
	public readonly Vector3Int Center;
	public readonly int Radius;

	public CircleEffectShape(Vector3Int center, int radius)
	{
		Center = center;
		Radius = radius;
	}

	public override IEnumerator<Vector3Int> GetEnumerator()
    {
	    for (int i = -Radius; i <= Radius; i++)
	    {
		    int f = Radius - Mathf.Abs(i) + 1;
		    for (int j = -Radius; j <= Radius; j++)
		    {
			    if (j <= 0 && j >= (-f) || j >= 0 && j <= (0 + f))
			    {
				    Vector3Int circlePos = new Vector3Int(Center.x + i, Center.y + j, 0);
				    yield return circlePos;
			    }
		    }
	    }
    }
}
