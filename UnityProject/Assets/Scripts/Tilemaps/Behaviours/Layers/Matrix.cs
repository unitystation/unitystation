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
	private TileList serverObjects;
	private TileList clientObjects;
	private Vector3Int initialOffset;
	public Vector3Int InitialOffset => initialOffset;
	public int Id { get; set; } = 0;

	private void Awake()
	{
		initialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
		metaTileMap = GetComponent<MetaTileMap>();
	}

	private void Start()
	{
		try
		{
			serverObjects = ((ObjectLayer) metaTileMap.Layers[LayerType.Objects]).ServerObjects;
			clientObjects = ((ObjectLayer) metaTileMap.Layers[LayerType.Objects]).ClientObjects;
		}
		catch
		{
			Logger.LogError("CAST ERROR: Make sure everything is in its proper layer type.", Category.Matrix);
		}
	}

	public bool IsPassableAt(Vector3Int position)
	{
		return IsPassableAt(position, position);
	}

	/// <summary>
	/// Checks if door can be closed at this tile
	/// – isn't occupied by solid objects and has no living beings
	/// </summary>
	public bool CanCloseDoorAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position) && GetFirst<LivingHealthBehaviour>( position, isServer ) == null;
	}

	/// Can one pass from `origin` to adjacent `position`?
	/// <param name="origin">Position object is at now</param>
	/// <param name="position">Adjacent position object wants to move to</param>
	/// <param name="includingPlayers">Set this to false to ignore players from check</param>
	/// <param name="context">Is excluded from passable check</param>
	/// <returns></returns>
	public bool IsPassableAt(Vector3Int origin, Vector3Int position, CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null)
	{
		return metaTileMap.IsPassableAt(origin, position, collisionType: collisionType, inclPlayers: includingPlayers, context: context);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int position)
	{
		return metaTileMap.IsAtmosPassableAt(origin, position);
	}

	public bool IsSpaceAt(Vector3Int position)
	{
		return metaTileMap.IsSpaceAt(position);
	}

	public bool IsEmptyAt(Vector3Int position, bool isServer)
	{
		return metaTileMap.IsEmptyAt(position, isServer);
	}

	/// Is this position and surrounding area completely clear of solid objects?
	public bool IsFloatingAt(Vector3Int position, bool isServer)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!metaTileMap.IsEmptyAt(pos, isServer))
			{
				return false;
			}
		}

		return true;
	}

	/// Is current position NOT a station tile? (Objects not taken into consideration)
	public bool IsNoGravityAt( Vector3Int position, bool isServer )
	{
		return metaTileMap.IsNoGravityAt(position, isServer);
	}

	/// Should player NOT stick to the station at this position?
	public bool IsNonStickyAt( Vector3Int position, bool isServer )
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!metaTileMap.IsNoGravityAt(pos, isServer))
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
			if (!metaTileMap.IsEmptyAt(context, pos, isServer))
			{
				return false;
			}
		}

		return true;
	}

	public IEnumerable<T> Get<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		var objectList = isServer ? serverObjects : clientObjects;
		if ( objectList == null || !objectList.HasObjects( position ) )
		{
			return Enumerable.Empty<T>(); //?
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in objectList.Get(position) )
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
		var objectList = isServer ? serverObjects : clientObjects;
		//This has been checked in the profiler. 0% CPU and 0kb garbage, so should be fine
		foreach ( RegisterTile t in objectList.Get(position) )
		{
			T c = t.GetComponent<T>();
			if (c != null)
			{
				return c;
			}
		}

		return null;
		//Old way that only checked the first RegisterTile on a cell pos:
		//return objects.GetFirst(position)?.GetComponent<T>();
	}

	public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type, bool isServer) where T : MonoBehaviour
	{
		var objectList = isServer ? serverObjects : clientObjects;
		if ( !objectList.HasObjects( position ) )
		{
			return Enumerable.Empty<T>();
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in objectList.Get(position, type) )
		{
			T x = t.GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public bool HasTile( Vector3Int position )
	{
		return metaTileMap.HasTile( position );
	}

	public IEnumerable<ElectricalOIinheritance> GetElectricalConnections(Vector3Int position)
	{
		return serverObjects.Get(position).Select(x => x.GetComponent<ElectricalOIinheritance>()).Where(x => x != null);
	}
}