using System.Collections.Generic;
using System.Linq;
using Objects;
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

	public TileList GetTileList(bool isServer)
	{
		if (isServer)
		{
			return ServerObjects;
		}
		else
		{
			return ClientObjects;
		}
	}

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
		return GetTileList(isServer).HasObjects(position);
	}

	public float GetObjectResistanceAt(Vector3Int position, bool isServer)
	{
		float resistance = 0; //todo: non-alloc method with ref?
		foreach (RegisterTile t in GetTileList(isServer).Get(position))
		{
			var health = t.GetComponent<IHealth>();
			if (health != null)
			{
				resistance += health.Resistance;
			}
		}

		return resistance;
	}


	public bool IsPassableAtOnThisLayerV2(Vector3Int origin, Vector3Int to, bool isServer, GameObject Incontext,
		 List<PushPull> Pushings,  List<IBumpableObject> Bumps)
	{
		if (CanLeaveTileV2(origin, to, isServer, Pushings, context: Incontext) == false)
		{
			return false;
		}

		if (CanEnterTileV2(origin, to, isServer, Pushings,Bumps ,  context: Incontext) == false)
		{
			return false;
		}

		return true;
	}

	public bool IsPassableAtOnThisLayer(Vector3Int origin, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player,
		bool inclPlayers = true, GameObject Incontext = null, List<TileType> excludeTiles = null, bool isReach = false)
	{
		if (CanLeaveTile(origin, to, isServer, context: Incontext) == false)
		{
			return false;
		}

		if (CanEnterTile(origin, to, isServer, collisionType, inclPlayers, Incontext, isReach) == false)
		{
			return false;
		}

		return true;
	}

	public bool CanLeaveTileV2(Vector3Int origin, Vector3Int to, bool isServer, List<PushPull> Pushings,
		GameObject context = null)
	{
		bool PushObjectSet = false;
		bool CanPushObjects = false;

		//Targeting windoors here
		foreach (RegisterTile t in GetTileList(isServer).Get(origin))
		{
			if (t.IsPassableFromInside(to, isServer, context) == false
			    && (context == null || t.gameObject != context))
			{

				if (t.PushPull.HasComponent)
				{
					if (PushObjectSet == false)
					{
						PushObjectSet = true;
						CanPushObjects = t.PushPull.Component.CanPushServer(to, (origin - to).To2Int());

					}

					if (CanPushObjects)
					{
						Pushings.Add(t.PushPull.Component);
					}
					else
					{
						Pushings.Clear();
						return false;
					}
				}
				else
				{
					Pushings.Clear();
					return false;
				}
			}
		}

		return true;
	}

	public bool CanLeaveTile(Vector3Int origin, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player,
		bool inclPlayers = true, GameObject context = null, List<TileType> excludeTiles = null, bool isReach = false)
	{

		//Targeting windoors here
		foreach (RegisterTile t in GetTileList(isServer).Get(origin))
		{
			if (t.IsPassableFromInside(to, isServer, context) == false
			    && (context == null || t.gameObject != context))
			{
				//Can't get outside the tile because windoor doesn't allow us
				return false;
			}
		}

		return true;
	}


	public bool CanEnterTileV2(Vector3Int origin, Vector3Int to, bool isServer, List<PushPull> Pushings, List<IBumpableObject> Bumps,  GameObject context = null)
	{
		bool PushObjectSet = false;
		bool CanPushObjects = false;

		//Targeting windoors here
		foreach (RegisterTile o in GetTileList(isServer).Get(to))
		{
			if (o.IsPassableFromOutside(origin, isServer, context) == false
			    && (context == null || o.gameObject != context))
			{
				if (o.PushPull.HasComponent)
				{
					if (PushObjectSet == false)
					{
						PushObjectSet = true;
						var Worldorigin = origin.ToWorld(matrix);
						var WorldTo = to.ToWorld(matrix);
						CanPushObjects = o.PushPull.Component.CanPushServer(WorldTo.RoundToInt(), (WorldTo - Worldorigin).To2Int());
					}

					if (CanPushObjects)
					{
						Pushings.Add(o.PushPull.Component);
						continue;
					}
					else
					{
						foreach (var objectOnTile in matrix.Get<ObjectBehaviour>(to, CustomNetworkManager.IsServer))
						{
							var bumpAbles = objectOnTile.GetComponents<IBumpableObject>();
							foreach (var bump in bumpAbles)
							{
								Bumps.Add(bump);
							}
						}

						Pushings.Clear();
						return false;
					}
				}
				else
				{
					foreach (var objectOnTile in matrix.Get<ObjectBehaviour>(to, true))
					{
						var bumpAbles = objectOnTile.GetComponents<IBumpableObject>();
						foreach (var bump in bumpAbles)
						{
							Bumps.Add(bump);
						}
					}

					Pushings.Clear();
					return false;
				}
			}
		}

		return true;
	}

	public bool CanEnterTile(Vector3Int origin, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player,
		bool inclPlayers = true, GameObject context = null, bool isReach = false)
	{
		//Targeting windoors here
		foreach (RegisterTile o in GetTileList(isServer).Get(to))
		{
			if ((inclPlayers || o.ObjectType != ObjectType.Player)
			    && o.IsPassableFromOutside(origin, isServer, context) == false
			    && (context == null || o.gameObject != context)
			    && (isReach == false || o.IsReachableThrough(origin, isServer, context) == false)
			    && (collisionType != CollisionType.Click || o.DoesNotBlockClick(origin, isServer) == false)
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
		foreach (RegisterTile o in GetTileList(isServer).Get(context.LocalPositionClient))
		{
			if (o.IsPassable(isServer, context.gameObject) == false
			    && context.IsPassableFromInside(to, isServer, o.gameObject) == false)
			{
				return true;
			}
		}

		return false;
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to, bool isServer)
	{
		foreach (RegisterTile t in GetTileList(isServer).Get(to))
		{
			if (!t.IsAtmosPassable(origin, isServer))
			{
				return false;
			}
		}

		foreach (RegisterTile t in GetTileList(isServer).Get(origin))
		{
			if (!t.IsAtmosPassable(to, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public override void RecalculateBounds()
	{
	}
}