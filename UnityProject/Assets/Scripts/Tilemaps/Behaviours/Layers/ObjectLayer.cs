using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// ObjectLayer holds all the objects on all the tiles in the game world - specifically the RegisterTile components of those objects.
/// It provides functionality for checking what should occur on given tiles, such as if a tile at a specific location should be passable.
/// </summary>
[ExecuteInEditMode]
public class ObjectLayer : Layer
{
	private TileList _objects;

	public TileList Objects => _objects ?? (_objects = new TileList());

	public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
	{
		ObjectTile objectTile = tile as ObjectTile;

		if (objectTile)
		{
			if (!objectTile.IsItem)
			{
				tilemap.SetTile(position, null);
			}

			objectTile.SpawnObject(position, tilemap, transformMatrix);
		}
		else
		{
			base.SetTile(position, tile, transformMatrix);
		}
	}

	public override bool HasTile(Vector3Int position)
	{
		return Objects.HasObjects(position) || base.HasTile(position);
	}

	public override void RemoveTile(Vector3Int position, bool removeAll = false)
	{
		foreach ( RegisterTile obj in Objects.Get(position) )
		{
			DestroyImmediate(obj.gameObject);
		}

		base.RemoveTile(position, removeAll);
	}

	public override bool IsPassableAt(Vector3Int origin, Vector3Int to, CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null)
	{
		//Targeting windoors here
		foreach ( RegisterTile t in Objects.Get(origin) )
		{
			if (!t.IsPassableTo(to) && (!context || t.gameObject != context))
			{
				//Can't get outside the tile because windoor doesn't allow us
				return false;
			}
		}

		foreach ( RegisterTile o in Objects.Get(to) )
		{
			if ((inclPlayers || o.ObjectType != ObjectType.Player) && !o.IsPassable(origin) && (!context || o.gameObject != context))
			{
				return false;
			}
		}

		return base.IsPassableAt(origin, to, collisionType: collisionType, inclPlayers: inclPlayers, context: context);
	}

	public override bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to)
	{
		foreach ( RegisterTile t in Objects.Get(to) )
		{
			if (!t.IsAtmosPassable(origin))
			{
				return false;
			}
		}

		foreach ( RegisterTile t in Objects.Get(origin) )
		{
			if (!t.IsAtmosPassable(to))
			{
				return false;
			}
		}

		return base.IsAtmosPassableAt(origin, to);
	}

	public override bool IsSpaceAt(Vector3Int position)
	{
		return IsAtmosPassableAt(position, position) && base.IsSpaceAt(position);
	}

	public override void ClearAllTiles()
	{
		for (var i = 0; i < Objects.AllObjects.Count; i++)
		{
			RegisterTile obj = Objects.AllObjects[i];
			if (obj != null)
			{
				DestroyImmediate(obj.gameObject);
			}
		}

		base.ClearAllTiles();
	}
}