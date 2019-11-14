using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectShapeType
{
	Square, // radius is equal in all directions from center []
	Diamond, // classic SS13 diagonals are reduced and angled <>
	Bomberman, // plus +
	Circle // Diamond without tip
}

public abstract class EffectShape : IEnumerable<Vector3Int>
{
	public abstract IEnumerator<Vector3Int> GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public static EffectShape CreateEffectShape(EffectShapeType type, Vector3Int center, int radius)
	{
		switch (type)
		{
			case EffectShapeType.Square:
				return new SquareEffectShape(center, radius);
		}

		return null;
	}
}
