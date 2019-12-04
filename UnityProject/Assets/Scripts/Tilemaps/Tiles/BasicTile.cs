using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

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

	public Resistances Resistances;
	public Armor Armor;

	[Tooltip("Interactions which can occur on this tile.")]
	[SerializeField]
	private List<TileInteraction> tileInteractions;
	/// <summary>
	/// Interactions which can occur on this tile.
	/// </summary>
	public List<TileInteraction> TileInteractions => tileInteractions;

	[Tooltip("What object to spawn when it's deconstructed or destroyed.")]
	[SerializeField]
	private GameObject spawnOnDeconstruct;
	/// <summary>
	/// Object to spawn when deconstructed.
	/// </summary>
	public GameObject SpawnOnDeconstruct => spawnOnDeconstruct;

	[Tooltip("How much of the object to spawn when it's deconstructed.")]
	[SerializeField]
	private int spawnAmountOnDeconstruct;
	/// <summary>
	/// How many of the object to spawn when it's deconstructed.
	/// </summary>
	public int SpawnAmountOnDeconstruct => spawnAmountOnDeconstruct;



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
