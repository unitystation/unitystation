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
	private TileList serverObjects;
	private TileList clientObjects;

	public TileList ServerObjects => serverObjects ?? (serverObjects = new TileList());
	public TileList ClientObjects => clientObjects ?? (clientObjects = new TileList());

	public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix, Color color)
	{
		ObjectTile objectTile = tile as ObjectTile;

		if (objectTile)
		{
			//hack to expand bounds when placing items in editor
			base.InternalSetTile(position, tile);
			base.InternalSetTile(position, null);

			objectTile.SpawnObject(position, tilemap, transformMatrix);
		}
	}

	public bool HasObject(Vector3Int position, bool isServer)
	{
		return (isServer ? ServerObjects.HasObjects(position) : ClientObjects.HasObjects(position));
	}

	public float GetObjectResistanceAt( Vector3Int position, bool isServer )
	{
		float resistance = 0; //todo: non-alloc method with ref?
		foreach ( RegisterTile t in isServer ? ServerObjects.Get( position ) : ClientObjects.Get( position ) )
		{
			var health = t.GetComponent<IHealth>();
			if ( health != null )
			{
				resistance += health.Resistance;
			}
		}

		return resistance;
	}

	public bool IsPassableAtOnThisLayer(Vector3Int origin, Vector3Int to, bool isServer, CollisionType collisionType = CollisionType.Player,
			bool inclPlayers = true, GameObject context = null, List<TileType> excludeTiles = null, bool isReach = false)
	{
		//Targeting windoors here
		foreach ( RegisterTile t in isServer ? ServerObjects.Get(origin) : ClientObjects.Get(origin) )
		{
			if (t.IsPassableFromInside(to, isServer, context) == false
				&& (context == null|| t.gameObject != context))
			{
				//Can't get outside the tile because windoor doesn't allow us
				return false;
			}
		}

		foreach ( RegisterTile o in isServer ? ServerObjects.Get(to) : ClientObjects.Get(to) )
		{
			if ((inclPlayers || o.ObjectType != ObjectType.Player)
				&& o.IsPassableFromOutside(origin, isServer, context) == false
				&& (context == null|| o.gameObject != context)
				&& (isReach == false || o.IsReachableThrough(origin, isServer, context) == false)
				)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Returns whether anything in the same square as some particular object, moving out to a particular destination
	/// would be blocked by that object
	/// </summary>
	/// <param name="to">destination of hypothetical movement</param>
	/// <param name="isServer">Whether or not being run on server</param>
	/// <param name="context">the object in question.</param>
	/// <returns></returns>
	public bool HasAnyDepartureBlockedByRegisterTile(Vector3Int to, bool isServer, RegisterTile context)
	{
		foreach (RegisterTile o in isServer ? ServerObjects.Get(context.LocalPositionServer) : ClientObjects.Get(context.LocalPositionClient) )
		{
			if (o.IsPassable(isServer,context.gameObject) == false
				&& context.IsPassableFromInside(to, isServer, o.gameObject) == false)
			{
				return true;
			}
		}

		return false;
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer)
	{
		foreach ( RegisterTile t in isServer ? ServerObjects.Get(to) : ClientObjects.Get(to) )
		{
			if (!t.IsAtmosPassable(origin, isServer))
			{
				return false;
			}
		}

		foreach ( RegisterTile t in isServer ? ServerObjects.Get(origin) : ClientObjects.Get(origin) )
		{
			if (!t.IsAtmosPassable(to, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public bool IsSpaceAt(Vector3Int position, bool isServer)
	{
		return IsAtmosPassableAt(position, position, isServer);
	}

	public override void ClearAllTiles()
	{
		for (var i = 0; i < ClientObjects.AllObjects.Count; i++)
		{
			RegisterTile obj = ClientObjects.AllObjects[i];
			if (obj != null)
			{
				DestroyImmediate(obj.gameObject);
			}
		}
		for (var i = 0; i < ServerObjects.AllObjects.Count; i++)
		{
			RegisterTile obj = ServerObjects.AllObjects[i];
			if (obj != null)
			{
				DestroyImmediate(obj.gameObject);
			}
		}
	}

	public override void RecalculateBounds()
	{

	}
}