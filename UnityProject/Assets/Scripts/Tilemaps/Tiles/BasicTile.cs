using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public struct TileState
{
	public Sprite Sprite;
	public float Damage;
}

public abstract class BasicTile : LayerTile
{
	public bool AtmosPassable;
	public bool IsSealed;
	public bool Passable;
	public bool Mineable;
	public PassableDictionary PassableException;

	public float MaxHealth;
	public TileState[] HealthStates;

	public GameObject ItemSpawn;
	public int amount;

	public LayerTile DestroyedTile;

	public override void RefreshTile(Vector3Int position, ITilemap tilemap)
	{
		foreach (Vector3Int p in new BoundsInt(-1, -1, 0, 3, 3, 1).allPositionsWithin)
		{
			tilemap.RefreshTile(position + p);
		}
	}

	/// <summary>
	/// Checks if the tile is Passable by the ColliderType
	/// It will return the default Passable bool unless an exception is avalaible in PassableException
	/// </summary>
	/// <param name="colliderType"></param>
	/// <returns>IsPassable</returns>
	public bool IsPassable(CollisionType colliderType)
	{
		if (PassableException.ContainsKey(colliderType))
		{
			return PassableException[colliderType];
		} else
		{
			return Passable;
		}
	}

	public bool IsAtmosPassable()
	{
		return AtmosPassable;
	}

	public bool IsSpace()
	{
		return IsAtmosPassable() && !IsSealed;
	}
}