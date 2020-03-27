using System;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

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
	private ReactionManager reactionManager;
	public ReactionManager ReactionManager => reactionManager;
	public int Id { get; set; } = 0;
	public MetaDataLayer MetaDataLayer => metaDataLayer;
	private MetaDataLayer metaDataLayer;

	public MatrixMove MatrixMove { get; private set; }

	private TileChangeManager tileChangeManager;
	public TileChangeManager TileChangeManager => tileChangeManager;

	public Color Color => colors.Wrap( Id ).WithAlpha( 0.7f );

	/// <summary>
	/// Does this have a matrix move and is that matrix move moving?
	/// </summary>
	public bool IsMovingServer => MatrixMove != null && MatrixMove.IsMovingServer;

	/// <summary>
	/// Invoked when some serious collision/explosion happens.
	/// Should make people fall and shake items a bit
	/// </summary>
	public EarthquakeEvent OnEarthquake = new EarthquakeEvent();

	private void Awake()
	{
		initialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
		reactionManager = GetComponent<ReactionManager>();
		metaDataLayer = GetComponent<MetaDataLayer>();
		MatrixMove = GetComponentInParent<MatrixMove>();
		tileChangeManager = GetComponentInParent<TileChangeManager>();


		OnEarthquake.AddListener( ( worldPos, magnitude ) =>
		{
			var cellPos = metaTileMap.WorldToCell( worldPos );

			var bounds =
				new BoundsInt(cellPos - new Vector3Int(magnitude, magnitude, 0), new Vector3Int(magnitude*2, magnitude*2, 1));

			foreach ( var pos in bounds.allPositionsWithin )
			{
				foreach ( var player in Get<PlayerScript>(pos, true) )
				{
					if ( player.IsGhost )
					{
						continue;
					}
					player.registerTile.ServerSlip(true);
				}
				//maybe shake items somehow, too
			}
		} );
	}

	public void CompressAllBounds()
	{
		foreach ( var tilemap in GetComponentsInChildren<Tilemap>() )
		{
			tilemap.CompressBounds();
		}

		foreach ( var layer in MetaTileMap.LayersValues )
		{
			layer.RecalculateBounds();
		}
	}

	public bool IsPassableAt(Vector3Int position, bool isServer, bool includingPlayers = true)
	{
		return IsPassableAt(position, position, isServer, includingPlayers: includingPlayers);
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

	public bool IsAtmosPassableAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsAtmosPassableAt(position, isServer);
	}

	public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsAtmosPassableAt(origin, position, isServer);
	}

	public bool IsSpaceAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsSpaceAt(position, isServer);
	}

	public bool IsTableAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsTileTypeAt(position, isServer, TileType.Table);
	}

	public bool IsWallAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, LayerType.Walls, isServer);
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

	/// <summary>
	/// Efficient way of iterating through the register tiles at a particular position which
	/// also is safe against modifications made to the list of tiles while the action is running.
	/// The limitation compared to Get<> is it can only get RegisterTiles, but the benefit is it avoids
	/// GetComponent so there's no GC. The OTHER benefit is that normally iterating through these
	/// would throw an exception if the RegisterTiles at this position were modified, such as
	/// being destroyed are created. This method uses a locking mechanism to avoid
	/// such issues.
	/// </summary>
	/// <param name="localPosition"></param>
	/// <returns></returns>
	public void ForEachRegisterTileSafe(IRegisterTileAction action, Vector3Int localPosition, bool isServer)
	{
		(isServer ? ServerObjects : ClientObjects).ForEachSafe(action, localPosition);
	}

	public IEnumerable<T> Get<T>(Vector3Int localPosition, bool isServer)
	{
		if ( !(isServer ? ServerObjects : ClientObjects).HasObjects( localPosition ) )
		{
			return Enumerable.Empty<T>(); //?
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(localPosition) )
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

	public IEnumerable<T> Get<T>(Vector3Int localPosition, ObjectType type, bool isServer) where T : MonoBehaviour
	{
		if ( !(isServer ? ServerObjects : ClientObjects).HasObjects( localPosition ) )
		{
			return Enumerable.Empty<T>();
		}

		var filtered = new List<T>();
		foreach ( RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(localPosition, type) )
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
			return ServerObjects.Get(position)
				.Select(x => x != null ? x.GetComponent<ElectricalOIinheritance>() : null)
				.Where(x => x != null)
				.Where(y => y.enabled);
		}
		else
		{
			return null;
		}
	}

	//Visual debug
	private static Color[] colors = new []{
		DebugTools.HexToColor( "a6caf0" ), //winterblue
		DebugTools.HexToColor( "e3949e" ), //brick
		DebugTools.HexToColor( "a8e4a0" ), //cyanish
		DebugTools.HexToColor( "ffff99" ), //canary yellow
		DebugTools.HexToColor( "cbbac5" ), //purplish
		DebugTools.HexToColor( "ffcfab" ), //peach
		DebugTools.HexToColor( "ccccff" ), //bluish
		DebugTools.HexToColor( "caf28d" ), //avocado
		DebugTools.HexToColor( "ffb28b" ), //pinkorange
		DebugTools.HexToColor( "98ff98" ), //mintygreen
		DebugTools.HexToColor( "fcdd76" ), //sand
		DebugTools.HexToColor( "afc797" ), //swamp green
		DebugTools.HexToColor( "ffca86" ), //orange
		DebugTools.HexToColor( "b0e0e6" ), //blue-cyanish
		DebugTools.HexToColor( "d1ba73" ), //khaki
		DebugTools.HexToColor( "c7fcec" ), //also greenish
		DebugTools.HexToColor( "cdb891" ), //brownish
	};
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color;
		BoundsInt bounds = MetaTileMap.GetWorldBounds();
		DebugGizmoUtils.DrawText( gameObject.name, bounds.max, 11, 5 );
		DebugGizmoUtils.DrawRect( bounds );
	}
#endif
}
public class EarthquakeEvent : UnityEvent<Vector3Int, byte> { }