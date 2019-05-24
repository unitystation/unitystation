using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Behavior which indicates a matrix - a contiguous grid of tiles.
///
/// If a matrix can move / rotate, the parent gameobject will have a MatrixMove component. Not this gameobject.
/// </summary>
public class Matrix : MonoBehaviour
{
	private MetaTileMap metaTileMap;
	private MetaTileMap MetaTileMap => metaTileMap ? metaTileMap : metaTileMap = GetComponent<MetaTileMap>();
	private TileList serverObjects;
	private TileList ServerObjects => serverObjects ?? (serverObjects = ((ObjectLayer) MetaTileMap.Layers[LayerType.Objects] ).ServerObjects);
	private TileList clientObjects;
	private TileList ClientObjects => clientObjects ?? (clientObjects = ((ObjectLayer) MetaTileMap.Layers[LayerType.Objects]).ClientObjects);
	private Vector3Int initialOffset;
	public Vector3Int InitialOffset => initialOffset;
	public int Id { get; set; } = 0;

	private void Awake()
	{
		initialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
	}

	public bool IsPassableAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position, isServer);
	}

	/// <summary>
	/// Checks if door can be closed at this tile
	/// – isn't occupied by solid objects and has no living beings
	/// </summary>
	public bool CanCloseDoorAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position, isServer) && GetFirst<LivingHealthBehaviour>( position, isServer ) == null;
	}

	/// Can one pass from `origin` to adjacent `position`?
	/// <param name="origin">Position object is at now</param>
	/// <param name="position">Adjacent position object wants to move to</param>
	/// <param name="includingPlayers">Set this to false to ignore players from check</param>
	/// <param name="context">Is excluded from passable check</param>
	/// <returns></returns>
	public bool IsPassableAt(Vector3Int origin, Vector3Int position, bool isServer, CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null)
	{
		return MetaTileMap.IsPassableAt(origin, position, isServer, collisionType: collisionType, inclPlayers: includingPlayers, context: context);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsAtmosPassableAt(origin, position, isServer);
	}

	public bool IsSpaceAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsSpaceAt(position, isServer);
	}

	public bool IsEmptyAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsEmptyAt(position, isServer);
	}

	/// Is this position and surrounding area completely clear of solid objects?
	public bool IsFloatingAt(Vector3Int position, bool isServer)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!MetaTileMap.IsEmptyAt(pos, isServer))
			{
				return false;
			}
		}

		return true;
	}

	/// Is current position NOT a station tile? (Objects not taken into consideration)
	public bool IsNoGravityAt( Vector3Int position, bool isServer )
	{
		return MetaTileMap.IsNoGravityAt(position, isServer);
	}

	/// Should player NOT stick to the station at this position?
	public bool IsNonStickyAt( Vector3Int position, bool isServer )
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!MetaTileMap.IsNoGravityAt(pos, isServer))
			{
				return false;
			}
		}

		return true;
	}

	/// Is this position and surrounding area completely clear of solid objects except for provided one?
	public bool IsFloatingAt(GameObject[] context, Vector3Int position, bool isServer)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!MetaTileMap.IsEmptyAt(context, pos, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public IEnumerable<T> Get<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		if ( !(isServer ? ServerObjects : ClientObjects).HasObjects( position ) )
		{
			return Enumerable.Empty<T>(); //?
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(position) )
		{
			T x = t.GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public T GetFirst<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		//This has been checked in the profiler. 0% CPU and 0kb garbage, so should be fine
		foreach ( RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(position) )
		{
			T c = t.GetComponent<T>();
			if (c != null)
			{
				return c;
			}
		}

		return null;
	}

	public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type, bool isServer) where T : MonoBehaviour
	{
		if ( !(isServer ? ServerObjects : ClientObjects).HasObjects( position ) )
		{
			return Enumerable.Empty<T>();
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(position, type) )
		{
			T x = t.GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public bool HasTile( Vector3Int position, bool isServer )
	{
		return MetaTileMap.HasTile( position, isServer );
	}

	public bool IsClearUnderfloorConstruction(Vector3Int position, bool isServer)
	{
		if (MetaTileMap.HasTile(position, LayerType.Floors, isServer))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Walls, isServer)){
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Windows, isServer))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Grills, isServer))
		{
			return (false);
		}
		return (true);
	}

	public IEnumerable<ElectricalOIinheritance> GetElectricalConnections(Vector3Int position)
	{
		if (ServerObjects != null)
		{
			return ServerObjects.Get(position).Select(x => x.GetComponent<ElectricalOIinheritance>()).Where(x => x != null);
		}
		else
		{
			return null;
		}
	}
}