using System;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using Pipes;
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
	public MetaTileMap MetaTileMap => metaTileMap ? metaTileMap : metaTileMap = GetComponent<MetaTileMap>();

	private TileList serverObjects;

	private TileList ServerObjects => serverObjects ??
	                                  (serverObjects = ((ObjectLayer) MetaTileMap.Layers[LayerType.Objects])
		                                  .ServerObjects);

	private TileList clientObjects;

	private TileList ClientObjects => clientObjects ??
	                                  (clientObjects = ((ObjectLayer) MetaTileMap.Layers[LayerType.Objects])
		                                  .ClientObjects);

	private Vector3Int initialOffset;
	public Vector3Int InitialOffset => initialOffset;
	private ReactionManager reactionManager;
	public ReactionManager ReactionManager => reactionManager;
	public int Id { get; set; } = 0;
	public MetaDataLayer MetaDataLayer => metaDataLayer;
	private MetaDataLayer metaDataLayer;

	public UnderFloorLayer UnderFloorLayer => underFloorLayer;
	private UnderFloorLayer underFloorLayer;

	public bool IsSpaceMatrix;
	public bool IsMainStation;
	public bool IsLavaLand;

	public MatrixMove MatrixMove { get; private set; }

	private TileChangeManager tileChangeManager;
	public TileChangeManager TileChangeManager => tileChangeManager;

	public Color Color => colors.Wrap(Id).WithAlpha(0.7f);

	/// <summary>
	/// Does this have a matrix move and is that matrix move moving?
	/// </summary>
	public bool IsMovingServer => MatrixMove != null && MatrixMove.IsMovingServer;

	/// <summary>
	/// Matrix info that is provided via MatrixManager
	/// </summary>
	public MatrixInfo MatrixInfo { get; private set; }

	/// <summary>
	/// Is the matrix info correctly configured
	/// </summary>
	public bool MatrixInfoConfigured { get; private set; }

	/// <summary>
	/// Register your action here if the MatrixInfo has not been configured correctly yet.
	/// The action will be called when it has been set up correctly and return the
	/// correct MatrixInfo
	/// </summary>
	public UnityAction<MatrixInfo> OnConfigLoaded;

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
		underFloorLayer = GetComponentInChildren<UnderFloorLayer>();

		OnEarthquake.AddListener((worldPos, magnitude) =>
		{
			var cellPos = metaTileMap.WorldToCell(worldPos);

			var bounds =
				new BoundsInt(cellPos - new Vector3Int(magnitude, magnitude, 0),
					new Vector3Int(magnitude * 2, magnitude * 2, 1));

			foreach (var pos in bounds.allPositionsWithin)
			{
				foreach (var player in Get<PlayerScript>(pos, true))
				{
					if (player.IsGhost)
					{
						continue;
					}

					player.registerTile.ServerSlip(true);
				}

				//maybe shake items somehow, too
			}
		});
	}

	void Start()
	{
		MatrixManager.RegisterMatrix(this, IsSpaceMatrix, IsMainStation, IsLavaLand);
	}

	public void CompressAllBounds()
	{
		foreach (var tilemap in GetComponentsInChildren<Tilemap>())
		{
			tilemap.CompressBounds();
		}

		foreach (var layer in MetaTileMap.LayersValues)
		{
			layer.RecalculateBounds();
		}
	}

	public void ConfigureMatrixInfo(MatrixInfo matrixInfo)
	{
		MatrixInfo = matrixInfo;
		MatrixInfoConfigured = true;
		OnConfigLoaded?.Invoke(matrixInfo);
	}

	public bool IsPassableAt(Vector3Int position, bool isServer, bool includingPlayers = true,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null)
	{
		return IsPassableAt(position, position, isServer, includingPlayers: includingPlayers,
			excludeLayers: excludeLayers, excludeTiles: excludeTiles);
	}

	/// <summary>
	/// Checks if door can be closed at this tile
	/// – isn't occupied by solid objects and has no living beings
	/// </summary>
	public bool CanCloseDoorAt(Vector3Int position, bool isServer)
	{
		return IsPassableAt(position, position, isServer) &&
		       GetFirst<LivingHealthBehaviour>(position, isServer) == null;
	}

	/// Can one pass from `origin` to adjacent `position`?
	/// <param name="origin">Position object is at now</param>
	/// <param name="position">Adjacent position object wants to move to</param>
	/// <param name="includingPlayers">Set this to false to ignore players from check</param>
	/// <param name="context">Is excluded from passable check</param>
	/// <returns></returns>
	public bool IsPassableAt(Vector3Int origin, Vector3Int position, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null)
	{
		return MetaTileMap.IsPassableAt(origin, position, isServer, collisionType: collisionType,
			inclPlayers: includingPlayers, context: context, excludeLayers: excludeLayers, excludeTiles: excludeTiles);
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
	public bool IsNoGravityAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsNoGravityAt(position, isServer);
	}

	/// Should player NOT stick to the station at this position?
	public bool IsNonStickyAt(Vector3Int position, bool isServer)
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
		if (!(isServer ? ServerObjects : ClientObjects).HasObjects(localPosition))
		{
			return Enumerable.Empty<T>(); //?
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(localPosition))
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
		foreach (RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(position))
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
		if (!(isServer ? ServerObjects : ClientObjects).HasObjects(localPosition))
		{
			return Enumerable.Empty<T>();
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(localPosition, type))
		{
			T x = t.GetComponent<T>();
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}

	public bool HasTile(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, isServer);
	}

	public bool IsClearUnderfloorConstruction(Vector3Int position, bool isServer)
	{
		if (MetaTileMap.HasTile(position, LayerType.Floors, isServer))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Walls, isServer))
		{
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

	public IEnumerable<Disposals.DisposalPipe> GetDisposalPipesAt(Vector3Int position)
	{
		// Return a list, because we may allow disposal pipes to overlap each other - NS with EW e.g.
		return UnderFloorLayer.GetAllTilesByType<Disposals.DisposalPipe>(position);
	}

	public List<IntrinsicElectronicData> GetElectricalConnections(Vector3Int position)
	{
		var list = ElectricalPool.GetFPCList();
		if (ServerObjects != null)
		{
			var collection = ServerObjects.Get(position);
			for (int i = collection.Count - 1; i >= 0; i--)
			{
				if (i < collection.Count && collection[i] != null
				                         && collection[i].ElectricalData != null &&
				                         collection[i].ElectricalData.InData != null)
				{
					list.Add(collection[i].ElectricalData.InData);
				}
			}
		}

		if (metaDataLayer.Get(position)?.ElectricalData != null)
		{
			foreach (var electricalMetaData in metaDataLayer.Get(position).ElectricalData)
			{
				list.Add(electricalMetaData.InData);
			}
		}

		return (list);
	}

	public List<Pipes.PipeData> GetPipeConnections(Vector3Int position)
	{
		var list = new List<PipeData>();

		var collection = ServerObjects.Get(position);
		//Logger.Log(collection.Count.ToString());
		foreach (var t in collection)
		{
			if (t.PipeData != null)
			{
				list.Add(t.PipeData);
			}
		}


		foreach (var PipeNode in metaDataLayer.Get(position).PipeData)
		{
			list.Add(PipeNode.pipeData);
		}


		return (list);
	}

	public void AddElectricalNode(Vector3Int position, WireConnect wireConnect)
	{
		var metaData = metaDataLayer.Get(position, true);
		var newdata = new ElectricalMetaData();
		newdata.Initialise(wireConnect, metaData, position, this);
		metaData.ElectricalData.Add(newdata);

		UnderFloorElectricalSetTile(wireConnect.InData.WireEndA, wireConnect.InData.WireEndB,
			wireConnect.InData.Categorytype, position, newdata);
	}

	public void AddElectricalNode(Vector3Int position, ElectricalCableTile electricalCableTile, bool AddTile = false)
	{
		var checkPos = position;
		checkPos.z = 0;
		var metaData = metaDataLayer.Get(checkPos, true);
		var newdata = new ElectricalMetaData();
		newdata.Initialise(electricalCableTile, metaData, position, this);
		metaData.ElectricalData.Add(newdata);
		if (AddTile)
		{
			if (electricalCableTile != null)
			{
				AddUnderFloorTile(position, electricalCableTile, Matrix4x4.identity, Color.white);
			}
		}
	}

	public void EditorAddElectricalNode(Vector3Int position, WireConnect wireConnect)
	{
		UnderFloorElectricalSetTile(wireConnect.InData.WireEndA, wireConnect.InData.WireEndB,
			wireConnect.InData.Categorytype, position);
	}

	private void UnderFloorElectricalSetTile(Connection WireEndA, Connection WireEndB,
		PowerTypeCategory powerTypeCategory, Vector3Int position, ElectricalMetaData newdata = null)
	{
		ElectricalCableTile Tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB, powerTypeCategory);
		if (newdata != null)
		{
			newdata.RelatedTile = Tile;
		}

		if (Tile != null)
		{
			AddUnderFloorTile(position, Tile, Matrix4x4.identity, Color.white);
		}
	}

	public MetaDataNode GetMetaDataNode(Vector3Int localPosition, bool createIfNotExists = true)
	{
		return (metaDataLayer.Get(localPosition, createIfNotExists));
	}

	public MetaDataNode GetMetaDataNode(Vector2Int localPosition, bool createIfNotExists = true)
	{
		return (metaDataLayer.Get(new Vector3Int(localPosition.x, localPosition.y, 0), createIfNotExists));
	}


	public float GetRadiationLevel(Vector3Int localPosition)
	{
		var Node = metaDataLayer.Get(localPosition);
		if (Node != null)
		{
			return (Node.RadiationNode.RadiationLevel);
		}
		return (0);
	}

	public float GetRadiationLevel(Vector2Int localPosition)
	{
		return (GetRadiationLevel(new Vector3Int(localPosition.x, localPosition.y, 0)));
	}


	public void AddUnderFloorTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix, Color color)
	{
		if (UnderFloorLayer == null)
		{
			underFloorLayer = GetComponentInChildren<UnderFloorLayer>();
		}

		UnderFloorLayer.SetTile(position, tile, transformMatrix, color);
	}


	public void RemoveUnderFloorTile(Vector3Int position, LayerTile tile)
	{
		if (UnderFloorLayer == null)
		{
			underFloorLayer = GetComponentInChildren<UnderFloorLayer>();
		}

		UnderFloorLayer.RemoveSpecifiedTile(position, tile);
	}

	//Visual debug
	private static Color[] colors = new[]
	{
		DebugTools.HexToColor("a6caf0"), //winterblue
		DebugTools.HexToColor("e3949e"), //brick
		DebugTools.HexToColor("a8e4a0"), //cyanish
		DebugTools.HexToColor("ffff99"), //canary yellow
		DebugTools.HexToColor("cbbac5"), //purplish
		DebugTools.HexToColor("ffcfab"), //peach
		DebugTools.HexToColor("ccccff"), //bluish
		DebugTools.HexToColor("caf28d"), //avocado
		DebugTools.HexToColor("ffb28b"), //pinkorange
		DebugTools.HexToColor("98ff98"), //mintygreen
		DebugTools.HexToColor("fcdd76"), //sand
		DebugTools.HexToColor("afc797"), //swamp green
		DebugTools.HexToColor("ffca86"), //orange
		DebugTools.HexToColor("b0e0e6"), //blue-cyanish
		DebugTools.HexToColor("d1ba73"), //khaki
		DebugTools.HexToColor("c7fcec"), //also greenish
		DebugTools.HexToColor("cdb891"), //brownish
	};
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color;
		BoundsInt bounds = MetaTileMap.GetWorldBounds();
		DebugGizmoUtils.DrawText(gameObject.name, bounds.max, 11, 5);
		DebugGizmoUtils.DrawRect(bounds);
	}
#endif
}

public class EarthquakeEvent : UnityEvent<Vector3Int, byte>
{
}
