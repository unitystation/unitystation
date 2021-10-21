using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using Light2D;
using HealthV2;
using Systems.Atmospherics;
using Systems.Electricity;
using Systems.Pipes;


/// <summary>
/// Behavior which indicates a matrix - a contiguous grid of tiles.
///
/// If a matrix can move / rotate, the parent gameobject will have a MatrixMove component. Not this gameobject.
/// </summary>
public class Matrix : MonoBehaviour
{
	private List<TilemapDamage> tilemapsDamage = new List<TilemapDamage>();

	public List<TilemapDamage> TilemapsDamage => tilemapsDamage;

	private MetaTileMap metaTileMap;
	public MetaTileMap MetaTileMap => metaTileMap;

	private TileList serverObjects;

	public TileList ServerObjects => serverObjects ??
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

	private NetworkedMatrix networkedMatrix;
	public NetworkedMatrix NetworkedMatrix => networkedMatrix;

	[NonSerialized] public bool Initialized;

	private void Awake()
	{
		metaTileMap = GetComponent<MetaTileMap>();
		if (metaTileMap == null)
		{
			Logger.LogError($"MetaTileMap was null on {gameObject.name}");
		}

		networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();
		initialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
		reactionManager = GetComponent<ReactionManager>();
		metaDataLayer = GetComponent<MetaDataLayer>();
		MatrixMove = GetComponentInParent<MatrixMove>();
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		underFloorLayer = GetComponentInChildren<UnderFloorLayer>();
		tilemapsDamage = GetComponentsInChildren<TilemapDamage>().ToList();
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
		StartCoroutine(MatrixManager.Instance.RegisterWhenReady(this));
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
		StartCoroutine(WaitForNetId());
	}

	private IEnumerator WaitForNetId()
	{
		while (networkedMatrix.MatrixSync != null && networkedMatrix.MatrixSync.netId == NetId.Empty)
		{
			yield return WaitFor.EndOfFrame;
		}

		MatrixInfoConfigured = true;
		OnConfigLoaded?.Invoke(MatrixInfo);
	}

	/// <inheritdoc cref="IsPassableAtOneMatrix(Vector3Int, Vector3Int, bool, CollisionType, bool, GameObject, List{LayerType}, List{TileType}, bool, bool, bool)"/>
	public bool IsPassableAtOneMatrixOneTile(Vector3Int position, bool isServer, bool includingPlayers = true,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, GameObject context = null, bool ignoreObjects = false,
		bool onlyExcludeLayerOnDestination = false
		)
	{
		return IsPassableAtOneMatrix(position, position, isServer, context: context, includingPlayers: includingPlayers,
			excludeLayers: excludeLayers, excludeTiles: excludeTiles, ignoreObjects: ignoreObjects,
			onlyExcludeLayerOnDestination: onlyExcludeLayerOnDestination);
	}

	/// <summary>
	/// Checks if door can be closed at this tile
	/// – isn't occupied by solid objects and has no living beings
	/// </summary>
	public bool CanCloseDoorAt(Vector3Int position, bool isServer)
	{
		return IsPassableAtOneMatrix(position, position, isServer) &&
		       GetFirst<LivingHealthMasterBase>(position, isServer) == null;
	}

	/// Can one pass from `origin` to adjacent `position`?
	/// <param name="origin">Position object is at now</param>
	/// <param name="position">Adjacent position object wants to move to</param>
	/// <param name="includingPlayers">Set this to false to ignore players from check</param>
	/// <param name="context">Is excluded from passable check</param>
	/// <param name="isReach">True if we're seeing if an object can be reached through</param>
	/// <param name="onlyExcludeLayerOnDestination">false if every involved tile should have the layers excluded, true if only the destination tile</param>
	/// <returns></returns>
	public bool IsPassableAtOneMatrix(Vector3Int origin, Vector3Int position, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null,
			List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, bool ignoreObjects = false,
			bool isReach = false, bool onlyExcludeLayerOnDestination = false)
	{
		return MetaTileMap.IsPassableAtOneTileMap(origin, position, isServer, collisionType: collisionType,
			inclPlayers: includingPlayers, context: context, excludeLayers: excludeLayers,
			excludeTiles: excludeTiles, ignoreObjects: ignoreObjects, isReach: isReach,
			onlyExcludeLayerOnDestination: onlyExcludeLayerOnDestination);
	}


	/// <inheritdoc cref="ObjectLayer.HasAnyDepartureBlockedByRegisterTile(Vector3Int, bool, RegisterTile)"/>
	public bool HasAnyDepartureBlockedOneMatrix(Vector3Int to, bool isServer, RegisterTile context)
	{
		return MetaTileMap.ObjectLayer.HasAnyDepartureBlockedByRegisterTile(to, isServer, context);
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
		return MetaTileMap.IsTableAt(position);
	}

	public bool IsWallAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, LayerType.Walls);
	}

	public bool IsWindowAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, LayerType.Windows);
	}

	public bool IsGrillAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, LayerType.Grills);
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
			if (MetaTileMap.IsNoGravityAt(pos, isServer) == false)
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
			if (MetaTileMap.IsEmptyAt(context, pos, isServer) == false)
			{
				return false;
			}
		}

		return true;
	}

	public IEnumerable<RegisterTile> GetRegisterTile(Vector3Int localPosition, bool isServer)
	{
		return (isServer ? ServerObjects : ClientObjects).Get(localPosition);
	}

	//Has to inherit from register tile
	public IEnumerable<T> GetAs<T>(Vector3Int localPosition, bool isServer) where T : class
	{
		if (!(isServer ? ServerObjects : ClientObjects).HasObjects(localPosition))
		{
			return Enumerable.Empty<T>(); //?
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(localPosition))
		{
			T x = t as T;
			if (x != null)
			{
				filtered.Add(x);
			}
		}

		return filtered;
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
		return MetaTileMap.HasTile(position);
	}

	public bool IsClearUnderfloorConstruction(Vector3Int position, bool isServer)
	{
		if (MetaTileMap.HasTile(position, LayerType.Floors))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Walls))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Windows))
		{
			return (false);
		}
		else if (MetaTileMap.HasTile(position, LayerType.Grills))
		{
			return (false);
		}

		return (true);
	}

	public IEnumerable<Objects.Disposals.DisposalPipe> GetDisposalPipesAt(Vector3Int position)
	{
		// Return a list, because we may allow disposal pipes to overlap each other - NS with EW e.g.
		return metaTileMap.GetAllTilesByType<Objects.Disposals.DisposalPipe>(position, LayerType.Underfloor);
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

	public List<PipeData> GetPipeConnections(Vector3Int position)
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

		var pipes =  metaDataLayer.Get(position).PipeData;
		foreach (var PipeNode in pipes)
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
		if (AddTile)
		{
			if (electricalCableTile != null)
			{
				position = TileChangeManager.UpdateTile(position, electricalCableTile);
			}
		}

		var newdata = new ElectricalMetaData();
		newdata.Initialise(electricalCableTile, metaData, position, this);
		metaData.ElectricalData.Add(newdata);

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
			TileChangeManager.UpdateTile(position, Tile, Matrix4x4.identity, Color.white);
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


	// Visual debug
	private static Color[] colors = new[]
	{
		DebugTools.HexToColor("a6caf0"), // winterblue
		DebugTools.HexToColor("e3949e"), // brick
		DebugTools.HexToColor("a8e4a0"), // cyanish
		DebugTools.HexToColor("ffff99"), // canary yellow
		DebugTools.HexToColor("cbbac5"), // purplish
		DebugTools.HexToColor("ffcfab"), // peach
		DebugTools.HexToColor("ccccff"), // bluish
		DebugTools.HexToColor("caf28d"), // avocado
		DebugTools.HexToColor("ffb28b"), // pinkorange
		DebugTools.HexToColor("98ff98"), // mintygreen
		DebugTools.HexToColor("fcdd76"), // sand
		DebugTools.HexToColor("afc797"), // swamp green
		DebugTools.HexToColor("ffca86"), // orange
		DebugTools.HexToColor("b0e0e6"), // blue-cyanish
		DebugTools.HexToColor("d1ba73"), // khaki
		DebugTools.HexToColor("c7fcec"), // also greenish
		DebugTools.HexToColor("cdb891"), // brownish
	};
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color;

		if (metaTileMap == null)
		{
			metaTileMap = GetComponent<MetaTileMap>();
		}

		BoundsInt bounds = MetaTileMap.GetWorldBounds();
		DebugGizmoUtils.DrawText(gameObject.name, bounds.max, 11, 5);
		DebugGizmoUtils.DrawRect(bounds);
	}
#endif
}

public class EarthquakeEvent : UnityEvent<Vector3Int, byte> { }
