using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Matrix : MonoBehaviour
{
	private MetaTileMap metaTileMap;
	private TileList objects;
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
			objects = ((ObjectLayer) metaTileMap.Layers[LayerType.Objects]).Objects;
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

	/// Can one pass from `origin` to adjacent `position`?
	/// <param name="origin">Position object is at now</param>
	/// <param name="position">Adjacent position object wants to move to</param>
	/// <param name="includingPlayers">Set this to false to ignore players from check</param>
	/// <param name="context">Is excluded from passable check</param>
	/// <returns></returns>
	public bool IsPassableAt(Vector3Int origin, Vector3Int position, bool includingPlayers = true, GameObject context = null)
	{
		return metaTileMap.IsPassableAt(origin, position, includingPlayers, context);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int position)
	{
		return metaTileMap.IsAtmosPassableAt(origin, position);
	}

	public bool IsSpaceAt(Vector3Int position)
	{
		return metaTileMap.IsSpaceAt(position);
	}

	public bool IsEmptyAt(Vector3Int position)
	{
		return metaTileMap.IsEmptyAt(position);
	}

	/// Is this position and surrounding area completely clear of solid objects?
	public bool IsFloatingAt(Vector3Int position)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!metaTileMap.IsEmptyAt(pos))
			{
				return false;
			}
		}

		return true;
	}

	/// Is current position NOT a station tile? (Objects not taken into consideration)
	public bool IsNoGravityAt(Vector3Int position)
	{
		return metaTileMap.IsNoGravityAt(position);
	}

	/// Should player NOT stick to the station at this position?
	public bool IsNonStickyAt(Vector3Int position)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!metaTileMap.IsNoGravityAt(pos))
			{
				return false;
			}
		}

		return true;
	}

	/// Is this position and surrounding area completely clear of solid objects except for provided one?
	public bool IsFloatingAt(GameObject[] context, Vector3Int position)
	{
		foreach (Vector3Int pos in position.BoundsAround().allPositionsWithin)
		{
			if (!metaTileMap.IsEmptyAt(context, pos))
			{
				return false;
			}
		}

		return true;
	}

	public List<T> Get<T>(Vector3Int position) where T : MonoBehaviour
	{
		if(objects == null)
		{
			//Return an empty list if objects is not initialized yet
			return new List<T>();
		}

		List<RegisterTile> xes = objects.Get(position);
		var filtered = new List<T>();
		for (var i = 0; i < xes.Count; i++)
		{
			T x = xes[i].GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		//This has been checked in the profiler. 0% CPU and 0kb garbage, so should be fine
		var registerTiles = objects.Get(position);
		for (int i = 0; i < registerTiles.Count; i++)
		{
			T c = registerTiles[i].GetComponent<T>();
			if (c != null)
			{
				return c;
			}
		}

		return null;
		//Old way that only checked the first RegisterTile on a cell pos:
		//return objects.GetFirst(position)?.GetComponent<T>();
	}

	public List<T> Get<T>(Vector3Int position, ObjectType type) where T : MonoBehaviour
	{
		List<RegisterTile> xes = objects.Get(position, type);
		var filtered = new List<T>();
		for (var i = 0; i < xes.Count; i++)
		{
			T x = xes[i].GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public bool ContainsAt(Vector3Int position, GameObject gameObject)
	{
		RegisterTile registerTile = gameObject.GetComponent<RegisterTile>();
		if (!registerTile)
		{
			return false;
		}

		// Check if tile contains a player
		if (registerTile.ObjectType == ObjectType.Player)
		{
			var playersAtPosition = objects.Get<RegisterPlayer>(position);

			if (playersAtPosition.Count == 0 || playersAtPosition.Contains(registerTile))
			{
				return false;
			}

			// Check if the player is passable (corpse)
			return playersAtPosition.First().IsBlocking;
		}

		// Otherwise check for blocking objects
		return objects.Get(position).Contains(registerTile);
	}

	public bool HasTile( Vector3Int position )
	{
		return metaTileMap.HasTile( position );
	}

	public IEnumerable<IElectricityIO> GetElectricalConnections(Vector3Int position)
	{
		return objects.Get(position).Select(x => x.GetComponent<IElectricityIO>()).Where(x => x != null);
	}
}