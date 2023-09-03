using System.Collections.Generic;
using System.Linq;
using Logs;
using Objects;
using Tilemaps.Utils;
using Tiles;
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

	private EnterTileBaseList enterTileBaseList;
	public EnterTileBaseList EnterTileBaseList => enterTileBaseList ?? (enterTileBaseList = new EnterTileBaseList());

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


	public bool IsPassableAtOnThisLayerV2(Vector3Int localOrigin, Vector3Int localTo, bool isServer,
		UniversalObjectPhysics Incontext,
		List<UniversalObjectPhysics> Pushings, List<IBumpableObject> Bumps, Vector3Int? originalFrom = null,
		Vector3Int? originalTo = null, List<UniversalObjectPhysics> Hits = null)
	{
		if (CanLeaveTileV2(localOrigin, localTo, isServer, Pushings, Bumps, Incontext, originalFrom, originalTo,
			    Hits) == false)
		{
			return false;
		}

		if (CanEnterTileV2(localOrigin, localTo, isServer, Pushings, Bumps, Incontext, originalFrom, originalTo,
			    Hits) == false)
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

	public bool PushingCalculation(RegisterTile o, Vector3Int originalFrom, Vector3Int originalTo,
		List<UniversalObjectPhysics> Pushings, List<IBumpableObject> Bumps, ref bool PushObjectSet,
		ref bool CanPushObjects, UniversalObjectPhysics context, List<UniversalObjectPhysics> Hits = null) //True equal return False
	{
		if (o.ObjectPhysics.HasComponent == false)
		{
			Loggy.LogError(o.name + " Is missing UniversalObjectPhysics");
		}
		if (PushObjectSet == false)
		{
			PushObjectSet = true;
			var Worldorigin = (originalFrom).ToWorld(context.registerTile.Matrix);
			var WorldTo = (originalTo).ToWorld(context.registerTile.Matrix);
			CanPushObjects = o.ObjectPhysics.Component.CanPush((WorldTo - Worldorigin).RoundTo2Int());
		}
		else
		{
			if (o.ObjectPhysics.Component.IsNotPushable)
			{
				CanPushObjects = false;
				Pushings.Clear();
			}
		}

		if (CanPushObjects)
		{
			if (Hits != null) Hits.Add(o.ObjectPhysics.Component);
			if (Pushings.Contains(o.ObjectPhysics.Component) == false)
			{
				Pushings.Add(o.ObjectPhysics.Component);
			}

			return false;
		}
		else
		{
			foreach (var objectOnTile in Matrix.Get<UniversalObjectPhysics>(originalTo, CustomNetworkManager.IsServer))
			{
				if(objectOnTile.Intangible) continue;

				var bumpAbles = objectOnTile.GetComponents<IBumpableObject>();
				foreach (var bump in bumpAbles)
				{
					Bumps.Add(bump);
				}
			}

			//If you can't bump anything on an adjacent tile, you may be blocked by a bumpable object on your current tile (probably a windoor)
			if (Bumps.Any() == false)
			{
				foreach (var objectOnTile in Matrix.Get<UniversalObjectPhysics>(originalFrom, CustomNetworkManager.IsServer))
				{
					if (objectOnTile.Intangible) continue;
					//Prevents living creatures from bumping themselves (or other living creatures) if on the same tile.
					if (objectOnTile.GetComponent<HealthV2.HealthStateController>() != null) continue;

					var bumpAbles = objectOnTile.GetComponents<IBumpableObject>();
					foreach (var bump in bumpAbles)
					{
						Bumps.Add(bump);
					}
				}
			}

			if (Hits != null) Hits.Add(o.ObjectPhysics.Component);

			Pushings.Clear();
			return true;
		}
	}

	public bool CanLeaveTileV2(Vector3Int origin, Vector3Int to, bool isServer, List<UniversalObjectPhysics> Pushings,
		List<IBumpableObject> Bumps,
		UniversalObjectPhysics context, Vector3Int? originalFrom = null, Vector3Int? originalTo = null,
		List<UniversalObjectPhysics> Hits = null)
	{
		bool PushObjectSet = false;
		bool CanPushObjects = false;

		//Targeting windoors here
		foreach (RegisterTile t in GetTileList(isServer).Get(origin))
		{
			if (t.IsPassableFromInside(to, isServer, context.OrNull().gameObject) == false
			    && (context == null || t.gameObject != context.gameObject))
			{
				if (context != null)
				{
					if (context.Pulling.HasComponent)
					{
						if (t.gameObject == context.Pulling.Component.gameObject)
						{
							return false;
						}
					}
					else if (context.PulledBy.HasComponent)
					{
						if (t.gameObject == context.PulledBy.Component.gameObject)
						{
							return false;
						}
					}
				}

				if (PushingCalculation(t, originalFrom ?? origin, originalTo ?? to, Pushings, Bumps, ref PushObjectSet,
					    ref CanPushObjects, context, Hits))
				{
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


	public bool CanEnterTileV2(Vector3Int origin, Vector3Int to, bool isServer, List<UniversalObjectPhysics> Pushings,
		List<IBumpableObject> Bumps, UniversalObjectPhysics context,
		Vector3Int? originalFrom = null, Vector3Int? originalTo = null, List<UniversalObjectPhysics> Hits = null)
	{
		bool PushObjectSet = false;
		bool CanPushObjects = false;

		//Targeting windoors here
		var List = GetTileList(isServer).Get(to);
		foreach (RegisterTile o in List)
		{
			if (context != null)
			{
				if (context.Pulling.HasComponent)
				{
					if (o.gameObject == context.Pulling.Component.gameObject)
					{
						context.StopPulling(false);
						return false;
					}
				}
				else if (context.PulledBy.HasComponent)
				{
					if (o.gameObject == context.PulledBy.Component.gameObject)
					{
						return false;
					}
				}
			}

			if (o.IsPassableFromOutside(origin, isServer, context.OrNull().gameObject) == false
			    && (context == null || o.OrNull()?.gameObject != context.OrNull()?.gameObject)  )
			{

				var PushDirection = (o.transform.localPosition - (originalFrom ?? origin)).RoundTo2Int();
				if (PushDirection == Vector2Int.zero)
				{
					PushDirection = ((originalTo ?? to) - (originalFrom ?? origin)).To2Int();
				}

				var theOriginal = originalFrom ?? origin;

				if (PushingCalculation(o, originalFrom ?? origin,(theOriginal + (Vector3Int) PushDirection) , Pushings, Bumps, ref PushObjectSet,
					    ref CanPushObjects, context, Hits))
				{
					if (o is RegisterPlayer) //yay Swapping Is Dumb
						//This is because the player can't be pushed into a wall However the swap is still initiated but the move is not, Meaning that server doesn't receive message for move
					{
						var Movement = (o.ObjectPhysics.Component as MovementSynchronisation);
						if (Movement.CanSwap(context.gameObject, out var move))
						{
							if (Pushings.Contains(o.ObjectPhysics.Component) == false)
							{
								Pushings.Add(o.ObjectPhysics.Component);
							}
							return true;
						}
					}
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