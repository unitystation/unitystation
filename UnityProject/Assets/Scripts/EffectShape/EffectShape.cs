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

	/// <summary>
	/// Creates new center-based Effect Shape
	/// </summary>
	/// <param name="type">The type of a new shape</param>
	/// <param name="center"></param>
	/// <param name="radius"></param>
	/// <returns></returns>
	public static EffectShape CreateEffectShape(EffectShapeType type, Vector3Int center, int radius)
	{
		switch (type)
		{
			case EffectShapeType.Square:
				return new SquareEffectShape(center, radius);
			case EffectShapeType.Bomberman:
				return new BombermanEffectShape(center, radius);
			case EffectShapeType.Diamond:
				return new DiamondEffectShape(center, radius);
			case EffectShapeType.Circle:
				return new CircleEffectShape(center, radius);
		}

		throw new System.NotImplementedException("Unknown type " + type);
	}
}
