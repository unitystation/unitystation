using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Doors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using Light2D;
using HealthV2;
using Logs;
using Systems.Atmospherics;
using Systems.Electricity;
using Systems.Pipes;
using Tilemaps.Utils;
using Util;
using Tiles;
using PipeLayer = Tilemaps.Behaviours.Layers.PipeLayer;

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


	public int Id { get; set; } = 0;
	public Vector3Int InitialOffset { get; private set; }
	public ReactionManager ReactionManager { get; private set; }
	public MetaDataLayer MetaDataLayer { get; private set; }
	public UnderFloorLayer UnderFloorLayer { get; private set; }
	public ElectricalLayer ElectricalLayer { get; private set; }
	public PipeLayer PipeLayer { get; private set; }
	public DisposalsLayer DisposalsLayer { get; private set; }

	public bool IsSpaceMatrix;
	public bool IsMainStation;
	public bool IsLavaLand;

	private CheckedComponent<MatrixMove> checkedMatrixMove;
	public bool IsMovable => checkedMatrixMove?.HasComponent ?? false;

	public MatrixMove MatrixMove => checkedMatrixMove.Component;

	private TileChangeManager tileChangeManager;
	public TileChangeManager TileChangeManager => tileChangeManager;

	public Color Color => colors.Wrap(Id).WithAlpha(0.7f);

	/// <summary>
	/// Does this have a matrix move and is that matrix move moving?
	/// </summary>
	public bool IsMovingServer => checkedMatrixMove.HasComponent && MatrixMove.IsMovingServer;

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

	public List<RegisterPlayer> PresentPlayers = new List<RegisterPlayer>();

	public int UpdatedPlayerFrame = 0;

	//Pretty self-explanatory, TODO gravity generator
	public bool HasGravity = true;

	private void Awake()
	{
		metaTileMap = GetComponent<MetaTileMap>();
		if (metaTileMap == null)
		{
			Loggy.LogError($"MetaTileMap was null on {gameObject.name}");
		}

		networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();
		InitialOffset = Vector3Int.CeilToInt(gameObject.transform.position);
		ReactionManager = GetComponent<ReactionManager>();
		MetaDataLayer = GetComponent<MetaDataLayer>();
		checkedMatrixMove = new CheckedComponent<MatrixMove>(GetComponentInParent<MatrixMove>());
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		UnderFloorLayer = GetComponentInChildren<UnderFloorLayer>();
		ElectricalLayer = GetComponentInChildren<ElectricalLayer>();
		PipeLayer = GetComponentInChildren<PipeLayer>();
		DisposalsLayer = GetComponentInChildren<DisposalsLayer>();
		tilemapsDamage = GetComponentsInChildren<TilemapDamage>().ToList();

		if (MatrixManager.Instance.InitializingMatrixes.ContainsKey(gameObject.scene) == false)
		{
			MatrixManager.Instance.InitializingMatrixes.Add(gameObject.scene, new List<Matrix>());
		}
		MatrixManager.Instance.InitializingMatrixes[gameObject.scene].Add(this);


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

					player.RegisterPlayer.ServerSlip(true);
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
		var firelock = GetFirst<FireLock>(position, isServer);
		if (firelock != null && firelock.fireAlarm.activated) return true;
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

	public bool IsFloorAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.HasTile(position, LayerType.Floors);
	}

	public bool IsEmptyAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsEmptyAt(position, isServer);
	}

	/// Is current position NOT a station tile? (Objects not taken into consideration)
	public bool IsNoGravityAt(Vector3Int position, bool isServer)
	{
		return MetaTileMap.IsNoGravityAt(position, isServer);
	}

	public List<RegisterTile> GetRegisterTile(Vector3Int localPosition, bool isServer)
	{
		return (isServer ? ServerObjects : ClientObjects).Get(localPosition);
	}

	//Has to inherit from register tile
	public IEnumerable<T> GetAs<T>(Vector3Int localPosition, bool isServer) where T : RegisterTile
	{

		var objects = (isServer ? ServerObjects : ClientObjects).Get(localPosition);
		if (objects.Count == 0)
		{
			return Enumerable.Empty<T>(); //Enumerable.Empty<T>() Does not GC while new List<T> does
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in objects)
		{
			if (t is T x)
			{
				filtered.Add(x);
			}
		}

		return filtered;
	}


	public IEnumerable<RegisterTile> Get(Vector3Int localPosition, bool isServer)
	{
		var objects = (isServer ? ServerObjects : ClientObjects).Get(localPosition);

		if (objects.Count == 0)
		{
			return Enumerable.Empty<RegisterTile>(); //Enumerable.Empty<T>() Does not GC while new List<T> does
		}

		var filtered = new List<RegisterTile>();
		filtered.AddRange(objects);
		return filtered;
	}


	public IEnumerable<T> Get<T>(Vector3Int localPosition, bool isServer)
	{
		var objects = (isServer ? ServerObjects : ClientObjects).Get(localPosition);
		if (objects.Count == 0)
		{
			return Enumerable.Empty<T>(); //Enumerable.Empty<T>() Does not GC while new List<T> does
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in objects)
		{
			if (t == null || t.TryGetComponent<T>(out var x) == false) continue;
			filtered.Add(x);
		}

		return filtered;
	}

	public T GetFirst<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		//This has been checked in the profiler. 0% CPU and 0kb garbage, so should be fine
		foreach (RegisterTile t in (isServer ? ServerObjects : ClientObjects).Get(position))
		{
			//Note GetComponent GC's in editor but not in build
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
		var objects = (isServer ? ServerObjects : ClientObjects).Get(localPosition, type);

		if (objects.Count == 0)
		{
			return Enumerable.Empty<T>(); //Enumerable.Empty<T>() Does not GC while new List<T> does
		}

		var filtered = new List<T>();
		foreach (RegisterTile t in objects)
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
		var tile = MetaTileMap.GetTile(position, LayerType.Floors);
		var basicTile = tile as BasicTile;
		if (tile != null && (basicTile == null || basicTile.BlocksTileInteractionsUnder))
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
		return metaTileMap.GetAllTilesByType<Objects.Disposals.DisposalPipe>(position, LayerType.Disposals);
	}

	public ElectricalPool.IntrinsicElectronicDataList GetElectricalConnections(Vector3Int localPosition)
	{
		var list = ElectricalPool.GetFPCList();
		if (ServerObjects != null)
		{
			var collection = ServerObjects.Get(localPosition);
			for (int i = collection.Count - 1; i >= 0; i--)
			{
				if (i < collection.Count && collection[i] != null
				                         && collection[i].ElectricalData != null &&
				                         collection[i].ElectricalData.InData != null)
				{
					list.List.Add(collection[i].ElectricalData.InData);
				}
			}
		}

		if (MetaDataLayer.Get(localPosition)?.ElectricalData != null)
		{
			foreach (var electricalMetaData in MetaDataLayer.Get(localPosition).ElectricalData)
			{
				list.List.Add(electricalMetaData.InData);
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

		var pipes =  MetaDataLayer.Get(position).PipeData;
		foreach (var PipeNode in pipes)
		{
			list.Add(PipeNode.pipeData);
		}


		return (list);
	}

	public IEnumerator MatrixInitialization()
	{
		var subsystemManager = this.GetComponentInParent<MatrixSystemManager>();
		yield return subsystemManager.Initialize();

		if (CustomNetworkManager.IsServer)
		{
			var iServerSpawnList = this.GetComponentsInChildren<IServerSpawn>();
			GameManager.Instance.MappedOnSpawnServer(iServerSpawnList);
		}
	}

	public void AddElectricalNode(Vector3Int position, WireConnect wireConnect)
	{
		var metaData = MetaDataLayer.Get(position, true);
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
		var metaData = MetaDataLayer.Get(checkPos, true);
		if (AddTile)
		{
			if (electricalCableTile != null)
			{
				position = TileChangeManager.MetaTileMap.SetTile(position, electricalCableTile);
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
			TileChangeManager.MetaTileMap.SetTile(position, Tile, Matrix4x4.identity, Color.white);
		}
	}

	public MetaDataNode GetMetaDataNode(Vector3Int localPosition, bool createIfNotExists = true)
	{
		return (MetaDataLayer.Get(localPosition, createIfNotExists));
	}

	public MetaDataNode GetMetaDataNode(Vector2Int localPosition, bool createIfNotExists = true)
	{
		return (MetaDataLayer.Get(new Vector3Int(localPosition.x, localPosition.y, 0), createIfNotExists));
	}

	public float GetRadiationLevel(Vector3Int localPosition)
	{
		var Node = MetaDataLayer.Get(localPosition);
		if (Node != null)
		{
			return (Node.RadiationNode.RadiationLevel);
		}
		return (0);
	}

	/// <summary>
	/// Retrieves the world position from an object's highest root.
	/// Helpful when checking for positions in a moving container.
	/// </summary>
	/// <param name="physics">the object's physics.</param>
	/// <returns>The local position the object is on. Will return Vector3Int.ZERO if there's no RegisterTile assigned.</returns>
	public static Vector3Int GetWorldPositionFromRootObject(UniversalObjectPhysics physics)
	{
		if (physics.GetRootObject.RegisterTile() != null) return physics.GetRootObject.RegisterTile().WorldPosition;
		Loggy.LogError("[Matrix/GetLocalPosFromWorldPos] - Could not find RegisterTile.");
		return Vector3Int.zero;
	}

	/// <summary>
	/// Retrieves the local position from an object's highest root.
	/// Helpful when checking for positions in a moving container.
	/// </summary>
	/// <param name="physics">the object's physics.</param>
	/// <returns>The local position the object is on. Will return Vector3Int.ZERO if there's no RegisterTile assigned.</returns>
	public static Vector3Int GetLocalPositionFromRootObject(UniversalObjectPhysics physics)
	{
		if (physics.GetRootObject.RegisterTile() != null) return physics.GetRootObject.RegisterTile().LocalPosition;
		Loggy.LogError("[Matrix/GetLocalPosFromWorldPos] - Could not find RegisterTile.");
		return Vector3Int.zero;
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

		var bounds = MetaTileMap.GetWorldBounds();
		DebugGizmoUtils.DrawText(gameObject.name, bounds.max, 11, 5);
		DebugGizmoUtils.DrawRect(bounds.Minimum, bounds.Maximum);
	}
#endif
}

public class EarthquakeEvent : UnityEvent<Vector3Int, byte> { }
