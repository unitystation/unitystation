using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Matrix : MonoBehaviour
{
	private MetaTileMap metaTileMap;
	private TileList objects;
	private TileList players;
	private Vector3Int initialOffset;
	public Vector3Int InitialOffset => initialOffset;

	private MetaDataLayer metaDataLayer;

	private void Start()
	{
		metaDataLayer = GetComponentInChildren<MetaDataLayer>(true);
		metaTileMap = GetComponent<MetaTileMap>();

		try
		{
			objects = ((ObjectLayer)metaTileMap.Layers[LayerType.Objects]).Objects;
		}
		catch
		{
			Logger.LogError("CAST ERROR: Make sure everything is in its proper layer type.", Category.Matrix);
		}
	}

	private void Awake()
	{
		initialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
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
	public bool IsPassableAt( Vector3Int origin, Vector3Int position, bool includingPlayers = true, GameObject context = null )
	{
		return metaTileMap.IsPassableAt(origin, position, includingPlayers, context);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int position)
	{
		return metaTileMap.IsAtmosPassableAt(origin, position);
	}

	public bool IsSpaceAt(Vector3Int position)
	{
		return metaDataLayer.IsSpaceAt(position);
	}

	/// Is this position completely clear of solid objects?
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
	public bool IsNoGravityAt( Vector3Int position ) {
		return metaTileMap.IsNoGravityAt( position );
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

	public IEnumerable<T> Get<T>(Vector3Int position) where T : MonoBehaviour
	{
		return objects.Get(position).Select(x => x.GetComponent<T>()).Where(x => x != null);
	}

	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		return objects.GetFirst(position)?.GetComponent<T>();
	}

	public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type) where T : MonoBehaviour
	{
		return objects.Get(position, type).Select(x => x.GetComponent<T>()).Where(x => x != null);
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
		return objects.Get<RegisterTile>(position).Contains(registerTile);
	}

	public IEnumerable<IElectricityIO> GetElectricalConnections(Vector3Int position)
	{
		return objects.Get(position).Select(x => x.GetComponent<IElectricityIO>()).Where(x => x != null);
	}
}
