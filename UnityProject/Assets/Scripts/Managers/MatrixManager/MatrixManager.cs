using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Doors;
using Logs;
using Managers;
using TileManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Systems.Atmospherics;
using Messages.Client.NewPlayer;
using Messages.Client.SpriteMessages;
using Shared.Managers;
using Mirror;
using Objects;
using Tiles;

/// <summary>
/// Defines collision type we expect
/// </summary>
public enum CollisionType
{
	None = -1,
	Player = 0,
	Shuttle = 1,
	Airborne = 2,
	Click = 3
};

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
[ExecuteInEditMode]
public partial class MatrixManager : SingletonManager<MatrixManager>
{
	public Dictionary<int, MatrixInfo> ActiveMatrices { get; private set; } = new Dictionary<int, MatrixInfo>();

	public List<MatrixInfo> ActiveMatricesList { get; private set; } = new List<MatrixInfo>();

	public Dictionary<Scene, List<Matrix>> InitializingMatrixes = new Dictionary<Scene, List<Matrix>>();

	public List<MatrixInfo> MovableMatrices { get; private set; } = new List<MatrixInfo>();

	public static bool IsInitialized;

	/// <summary>
	/// Find a wall tilemap via its Tilemap collider
	/// </summary>
	public Dictionary<Collider2D, Tilemap> wallsTileMaps = new Dictionary<Collider2D, Tilemap>();

	public Matrix spaceMatrix { get; private set; }
	public Matrix lavaLandMatrix { get; private set; }
	private Matrix mainStationMatrix = null;

	public static MatrixInfo MainStationMatrix
	{
		get
		{
			if (Instance.mainStationMatrix == null)
			{
				return Instance.ActiveMatricesList[1];
			}
			else
			{
				return Get(Instance.mainStationMatrix);
			}
		}
	}

	private bool ClientWaitingRoutine = false;

	public override void Start()
	{
		base.Start();
		if (Application.isPlaying)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.ReadyToInitialiseMatrices, OnScenesLoaded);
	}

	private void OnDisable()
	{
		ClientWaitingRoutine = false;
		SceneManager.activeSceneChanged -= OnSceneChange;
		EventManager.RemoveHandler(Event.ReadyToInitialiseMatrices, OnScenesLoaded);
		if (Application.isPlaying)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		ResetMatrixManager();
		IsInitialized = false;
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		ResetMatrixManager();
		if (newScene.name.Equals("Lobby") == false)
		{
			IsInitialized = false;
		}
	}

	public void PostRoundStartCleanup()
	{
		foreach (var a in InitializingMatrixes)
		{
			Debug.Log("removed " + CleanupUtil.RidListOfDeadElements(a.Value) + " dead matrices from MatrixManager.InitializingMatrixes");
		}
	}
	public void ResetMatrixManager()
	{
		if (Instance != null)
		{
			Instance.spaceMatrix = null;
			Instance.mainStationMatrix = null;
		}

		MovableMatrices.Clear();
		ActiveMatrices.Clear();
		ActiveMatricesList.Clear();
		movingMatrices.Clear();
		wallsTileMaps.Clear();
		trackedIntersections.Clear();
		InitializingMatrixes.Clear();
	}

	public IEnumerator RegisterWhenReady(Matrix matrix)
	{
		while (matrix.NetworkedMatrix.Initialized == false ||
		       (matrix.MatrixMove && matrix.MatrixMove.Initialized == false))
		{
			yield return null;
		}

		RegisterMatrix(matrix);
		matrix.Initialized = true;

		var registerTileList = matrix.GetComponentsInChildren<RegisterTile>();
		foreach (var registerTile in registerTileList)
		{
			registerTile.Initialize(matrix);
		}

		SubsystemMatrixQueueInit.Queue(matrix);


		if (CustomNetworkManager.IsServer == false)
		{
			matrix.MetaTileMap.InitialiseUnderFloorUtilities(CustomNetworkManager.IsServer);
			TileChangeNewPlayer.Send(matrix.MatrixInfo.NetID);

			if (AreAllMatrixReady())
			{
				IsInitialized = true;
				ClientMatrixInitialization(matrix);
			}
			else
			{
				if (ClientWaitingRoutine == false)
				{
					ClientWaitingRoutine = true;
					StartCoroutine(ClientWaitForAllMatrices());
				}
			}
		}
	}

	[Client]
	private void ClientMatrixInitialization(Matrix matrix)
	{
		var subsystemManager = matrix.GetComponentInParent<MatrixSystemManager>();
		subsystemManager.SelfInitialize();
	}
	[Client]

	private IEnumerator ClientWaitForAllMatrices()
	{
		while (AreAllMatrixReady() == false)
		{
			yield return null;
		}
		InitializingMatrixes.Clear();
		ClientWaitingRoutine = false;
		foreach (var matrixInfo in ActiveMatricesList)
		{
			ClientMatrixInitialization(matrixInfo.Matrix);
		}
		SpriteRequestCurrentStateMessage.Send(SpriteHandlerManager.Instance.GetComponent<NetworkIdentity>().netId);
		ClientWaitingRoutine = false;
	}
	private bool AreAllMatrixReady()
	{
		int Count = 0;

		Count = SubSceneManager.Instance.loadedScenesList.Count;

		if (Count != InitializingMatrixes.Count || SubSceneManager.Instance.clientIsLoadingSubscene || Count == 0 || SubSceneManager.Instance.SubSceneManagerNetworked.ScenesInitialLoadingComplete == false)
		{
			return false;
		}

		foreach (var matrixList in InitializingMatrixes.Values)
		{
			foreach (var matrix in matrixList)
			{
				if (matrix.Initialized == false)
				{
					return false;
				}
			}
		}

		return true;
	}

	private void OnScenesLoaded()
	{
		StartCoroutine(WaitForAllMatrices());
	}

	private IEnumerator WaitForAllMatrices()
	{
		while (AreAllMatrixReady() == false)
		{
			yield return null;
		}
		InitializingMatrixes.Clear();
		IsInitialized = true;

		EventManager.Broadcast(Event.MatrixManagerInit);
	}

	private void RegisterMatrix(Matrix matrix)
	{
		var matrixInfo = CreateMatrixInfoFromMatrix(matrix, matrix.NetworkedMatrix.MatrixSync.matrixID);

		ActiveMatrices.Add(matrixInfo.Id, matrixInfo);
		ActiveMatricesList.Add(matrixInfo);
		if (matrixInfo.IsMovable)
		{
			MovableMatrices.Add(matrixInfo);
		}

		matrix.Id = matrixInfo.Id;

		if (matrix.IsSpaceMatrix)
		{
			if (spaceMatrix == null)
			{
				spaceMatrix = matrix;
			}
			else
			{
				Loggy.Log("There is already a space matrix registered", Category.Matrix);
			}
		}

		if (matrix.IsMainStation)
		{
			if (mainStationMatrix == null)
			{
				mainStationMatrix = matrix;
			}
			else
			{
				Loggy.Log("There is already a main station matrix registered", Category.Matrix);
			}
		}

		if (matrix.IsLavaLand)
		{
			if (lavaLandMatrix == null)
			{
				lavaLandMatrix = matrix;
			}
			else
			{
				Loggy.Log("There is already a lava land matrix registered", Category.Matrix);
			}
		}

		matrix.ConfigureMatrixInfo(matrixInfo);
		InitCollisions(matrixInfo);
	}

	private static MatrixInfo CreateMatrixInfoFromMatrix(Matrix matrix, int id)
	{
		var gameObj = matrix.gameObject;
		return new MatrixInfo(id)
		{
			Matrix = matrix,
			GameObject = gameObj,
			Objects = gameObj.transform.GetComponentInChildren<ObjectLayer>().transform,
			MetaTileMap = gameObj.GetComponent<MetaTileMap>(),
			MetaDataLayer = gameObj.GetComponent<MetaDataLayer>(),
			SubsystemManager = gameObj.GetComponentInParent<MatrixSystemManager>(),
			ReactionManager = gameObj.GetComponentInParent<ReactionManager>(),
			TileChangeManager = gameObj.GetComponentInParent<TileChangeManager>(),
			InitialOffset = matrix.InitialOffset
		};
	}

	/// Finds first matrix that is not empty at given world pos
	public static MatrixInfo AtPoint(Vector3 worldPos, bool isServer, MatrixInfo possibleMatrix = null)
	{
		//for performance, this is just a suggestion on which matrix we believe this point could be in, not always correct
		if (possibleMatrix != null && IsInMatrix(worldPos, isServer, possibleMatrix))
		{
			return possibleMatrix;
		}

		for (int i = 0; i < Instance.ActiveMatricesList.Count; i++)
		{
			var matrixInfo = Instance.ActiveMatricesList[i];
			if (IsInMatrix(worldPos, isServer, matrixInfo))
			{
				return matrixInfo;
			}
		}

		return Instance.spaceMatrix.MatrixInfo;
	}

	public static MatrixInfo GetByName_DEBUG_ONLY(string Name)
	{
		for (int i = 0; i < Instance.ActiveMatricesList.Count; i++)
		{
			if (Instance.ActiveMatricesList[i].Name == Name)
			{
				return Instance.ActiveMatricesList[i];
			}
		}

		return null;
	}

	private static bool IsInMatrix(Vector3 worldPos, bool isServer, MatrixInfo matrixInfo)
	{
		if (matrixInfo.WorldBounds.Contains(worldPos) == false)
			return false;

		if (matrixInfo.Matrix == Instance.spaceMatrix)
			return false;

		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		if (matrixInfo.Matrix.IsEmptyAt(localPos, isServer) == false)
			return true;

		return false;
	}


	public static CustomPhysicsHit Linecast(Vector3 Worldorigin, LayerTypeSelection layerMask, LayerMask? Layermask2D,
		Vector3 WorldTo, bool DEBUG = false)
	{
		return RayCast(Worldorigin, Vector2.zero, 0, layerMask, Layermask2D, WorldTo, DEBUG: DEBUG);
	}

	public static CustomPhysicsHit RayCast(Vector3 Worldorigin,
		Vector2 direction,
		float distance,
		LayerTypeSelection layerMask, LayerMask? Layermask2D = null, Vector3? WorldTo = null,
		LayerTile[] tileNamesToIgnore = null, bool DEBUG = false)
	{
		Worldorigin.z = 0;

		//TODO RRT
		//get to  from vector
		CustomPhysicsHit? ClosestHit = null;
		CustomPhysicsHit? Checkhit = null;
		// Vector3 Localdorigin = Vector3.zero;
		// Vector3 LocalTo = Vector3.zero;

		if (WorldTo == null)
		{
			WorldTo = Worldorigin + (Vector3) (direction.normalized * distance);
		}
		else
		{
			distance = (WorldTo - Worldorigin).Value.magnitude;
		}

		if (distance > 30)
		{
			Loggy.LogError(
				$" Limit exceeded on raycast, Look at stack trace for What caused it at {distance}"); //Meant to catch up stuff that's been naughty and doing stuff like 900 tile Ray casts
			return new CustomPhysicsHit();
		}


		if (direction.x == 0 && direction.y == 0)
		{
			direction = (WorldTo.Value - Worldorigin).normalized;
		}

		if (layerMask != LayerTypeSelection.None)
		{
			//TODO do we really need to go through all matrixes? Can we break out at some point?
			foreach (var matrixInfo in Instance.ActiveMatricesList)
			{
				if (LineIntersectsRect(Worldorigin, WorldTo.Value, matrixInfo.WorldBounds))
				{
					var localOrigin = WorldToLocal(Worldorigin, matrixInfo).To2();
					var localTo = WorldToLocal(WorldTo.Value, matrixInfo).To2();
					Checkhit = matrixInfo.MetaTileMap.Raycast(localOrigin, Vector2.zero,
						distance,
						layerMask,
						localTo, tileNamesToIgnore, DEBUG: DEBUG);

					if (Checkhit != null)
					{
						if (ClosestHit != null)
						{
							if (ClosestHit.Value.Distance > Checkhit.Value.Distance)
							{
								ClosestHit = Checkhit;
							}
						}
						else
						{
							ClosestHit = Checkhit;
						}
					}
				}
			}
		}

		if (Layermask2D != null)
		{
			var Hit2D = Physics2D.Raycast(Worldorigin, direction.normalized, distance, Layermask2D.Value);
			if (ClosestHit != null)
			{
				if (Hit2D.distance != 0 && ClosestHit.Value.Distance > Hit2D.distance)
				{
					ClosestHit = new CustomPhysicsHit(Hit2D);
				}
			}
			else
			{
				ClosestHit = new CustomPhysicsHit(Hit2D);
			}
		}

		if (ClosestHit == null)
		{
			ClosestHit = new CustomPhysicsHit();
		}

		return ClosestHit.Value;
	}


	public struct CustomPhysicsHit
	{
		public Vector3 TileHitWorld;
		public Vector3 HitWorld;
		public Vector2 Normal;
		public float Distance;
		public CollisionHit CollisionHit;
		public bool ItHit;

		public CustomPhysicsHit(bool unneededbool = false)
		{
			TileHitWorld = Vector3.zero;
			HitWorld = Vector3.zero;
			Normal = Vector2.zero;
			Distance = 9999999999f;
			CollisionHit = new CollisionHit(null, null);
			ItHit = false;
		}

		public CustomPhysicsHit(Vector3 _TileHitWorld, Vector3 _HitWorld, Vector2 _Normal, float _RelativeHit,
			TileLocation TileHit)
		{
			TileHitWorld = _TileHitWorld;
			HitWorld = _HitWorld;
			Normal = _Normal;
			Distance = _RelativeHit;
			CollisionHit = new CollisionHit(null, TileHit);
			ItHit = TileHit != null;
		}

		public CustomPhysicsHit(RaycastHit2D raycastHit2D)
		{
			TileHitWorld = raycastHit2D.point;
			HitWorld = raycastHit2D.point;
			Normal = raycastHit2D.normal;
			Distance = raycastHit2D.distance;
			CollisionHit = new CollisionHit(raycastHit2D.collider?.gameObject, null);
			ItHit = raycastHit2D.collider?.gameObject != null;
		}
	}

	public struct CollisionHit
	{
		public CollisionHit(GameObject _GameObject, TileLocation _TileLocation)
		{
			GameObject = _GameObject;
			TileLocation = _TileLocation;
		}

		public GameObject GameObject;
		public TileLocation TileLocation;
	}

	public static bool LineIntersectsRect(Vector2 p1, Vector2 p2, BetterBounds r)
	{
		var xMin = r.xMin;
		var yMin = r.yMin;
		var xMax = r.xMax;
		var yMax = r.yMax;

		return LineIntersectsLine(p1, p2, new Vector2(xMin, yMin), new Vector2(xMin, yMax)) ||
		       LineIntersectsLine(p1, p2, new Vector2(xMin, yMin), new Vector2(xMax, yMin)) ||
		       LineIntersectsLine(p1, p2, new Vector2(xMax, yMax), new Vector2(xMin, yMax)) ||
		       LineIntersectsLine(p1, p2, new Vector2(xMax, yMax), new Vector2(xMax, yMin)) ||
		       (FindPoint(r.min, r.max, p1) && FindPoint(r.min, r.max, p2));
	}

	static bool FindPoint(Vector3 min, Vector3 max, Vector2 Point)
	{
		if (Point.x > min.x && Point.x < max.x &&
		    Point.y > min.y && Point.y < max.y)
			return true;

		return false;
	}

	private static bool LineIntersectsLine(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2)
	{
		float q = (l1p1.y - l2p1.y) * (l2p2.x - l2p1.x) - (l1p1.x - l2p1.x) * (l2p2.y - l2p1.y);
		float d = (l1p2.x - l1p1.x) * (l2p2.y - l2p1.y) - (l1p2.y - l1p1.y) * (l2p2.x - l2p1.x);

		if (d == 0)
		{
			return false;
		}

		float r = q / d;

		q = (l1p1.y - l2p1.y) * (l1p2.x - l1p1.x) - (l1p1.x - l2p1.x) * (l1p2.y - l1p1.y);
		float s = q / d;

		if (r < 0 || r > 1 || s < 0 || s > 1)
		{
			return false;
		}

		return true;
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	///<inheritdoc cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix = null)
	{
		var matrixInfo = AtPoint(worldPos, isServer, possibleMatrix);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsSpaceAt(localPos, isServer);
		return value;
	}

	public static bool IsConstructable(Vector3Int worldPos, MatrixInfo matrixInfo)
	{
		if (matrixInfo.Matrix.MetaTileMap.IsConstructable(WorldToLocalInt(worldPos, matrixInfo)))
		{
			return true;
		}

		return false;
	}

	///Cross-matrix edition of <see cref="Matrix.IsEmptyAt"/>
	///<inheritdoc cref="Matrix.IsEmptyAt"/>
	public static bool IsEmptyAt(Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix)
	{
		var matrixInfo = AtPoint(worldPos, isServer, possibleMatrix);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsEmptyAt(localPos, isServer);
		return value;
	}

	/// <summary>
	/// Server only.
	/// Finds all TilemapDamage at given world pos, so you could apply damage to given tiles via DoMeleeDamage etc.
	/// </summary>
	/// <param name="worldTarget"></param>
	/// <returns></returns>
	public static IReadOnlyList<TilemapDamage> GetDamageableTilemapsAt(Vector3Int worldTarget)
	{
		var Matrix = MatrixManager.AtPoint(worldTarget, CustomNetworkManager.Instance._isServer);
		return Matrix.Matrix.TilemapsDamage;
	}

	/// <summary>
	/// Gets the closest door to worldOrigin on the path from worldOrigin to targetPos (if there is one)
	/// Assumes that
	/// </summary>
	/// <param name="worldOrigin">Origin World position to check from. This is required because e.g. Windoors may not appear closed from certain positions.</param>
	/// <param name="targetPos">target world position to check</param>
	/// <returns>The DoorTrigger of the closed door object specified in the summary, null if no such object
	/// exists at that location</returns>
	public static InteractableDoor GetClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos, bool isServer,
		MatrixInfo MatrixAtOrigin = null, MatrixInfo MatrixAtTarget = null)
	{
		if (MatrixAtOrigin == null)
		{
			MatrixAtOrigin = AtPoint(worldOrigin, isServer);
		}

		if (MatrixAtTarget == null)
		{
			MatrixAtTarget = AtPoint(targetPos, isServer);
		}

		// Check door on the local tile first
		Vector3Int localTarget = WorldToLocalInt(targetPos, MatrixAtTarget.Matrix);
		var originDoorList = GetAs<RegisterDoor>(worldOrigin, isServer, MatrixAtOrigin);
		foreach (var originDoor in originDoorList)
		{
			if (originDoor && originDoor.IsPassableFromInside(localTarget, isServer) ==
			    false)
				return originDoor.InteractableDoor;
		}

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = WorldToLocalInt(worldOrigin, MatrixAtOrigin.Matrix);
		var targetDoorList = GetAs<RegisterDoor>(targetPos, isServer, MatrixAtTarget);
		foreach (var targetDoor in targetDoorList)
		{
			if (targetDoor && targetDoor.IsPassableFromOutside(localOrigin, isServer) ==
			    false)
				return targetDoor.InteractableDoor;
		}

		// No closed doors on either tile
		return null;
	}


	public static DoorMasterController GetNewClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos, bool isServer)
	{
		// Check door on the local tile first
		Vector3Int localTarget = WorldToLocalInt(targetPos, AtPoint(targetPos, isServer).Matrix);
		var originDoorList = GetAt<DoorMasterController>(worldOrigin, isServer);
		foreach (DoorMasterController originDoor in originDoorList)
		{
			if (originDoor && originDoor.GetComponent<RegisterDoor>().IsPassableFromInside(localTarget, isServer) ==
			    false)
				return originDoor;
		}

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = WorldToLocalInt(worldOrigin, AtPoint(worldOrigin, isServer).Matrix);
		var targetDoorList = GetAt<DoorMasterController>(targetPos, isServer);
		foreach (DoorMasterController targetDoor in targetDoorList)
		{
			if (targetDoor && targetDoor.GetComponent<RegisterDoor>().IsPassableFromOutside(localOrigin, isServer) ==
			    false)
				return targetDoor;
		}

		// No closed doors on either tile
		return null;
	}

	/// <summary>
	/// Gets the MetaDataNode from the Matrix at the given position.
	/// </summary>
	/// <param name="worldPosition">Position at which the node is looked for.</param>
	/// <returns>MetaDataNode at the position. If no Node that isn't space is found, MetaDataNode.Node will be returned.</returns>
	public static MetaDataNode GetMetaDataAt(Vector3Int worldPosition)
	{
		var mat = AtPoint(worldPosition, CustomNetworkManager.Instance._isServer, MainStationMatrix);
		Vector3Int position = WorldToLocalInt(worldPosition, mat);
		MetaDataNode node = mat.MetaDataLayer.Get(position, false);

		if (node.Exists && node.IsSpace == false)
		{
			return node;
		}

		return MetaDataNode.None;
	}

	/// <summary>
	/// Server only:
	/// Picks best matching matrix at provided coords and releases reagents to that tile.
	/// <inheritdoc cref="MetaDataLayer.ReagentReact"/>
	/// </summary>
	public static void ReagentReact(ReagentMix reagents, Vector3Int worldPos, MatrixInfo matrixInfo = null,bool spawnPrefabEffect = true, OrientationEnum direction = OrientationEnum.Up_By0)
	{
		if (CustomNetworkManager.IsServer == false)
		{
			return;
		}

		if (matrixInfo is null)
		{
			matrixInfo = AtPoint(worldPos, true);
		}

		Vector3Int localPos = WorldToLocalInt(worldPos, matrixInfo);
		matrixInfo.MetaDataLayer.ReagentReact(reagents, worldPos, localPos, spawnPrefabEffect, direction);
	}

	/// <summary>
	/// Triggers an Subsystem Update at the given position. Only triggers, when a MetaDataNode already exists.
	/// </summary>
	/// <param name="worldPosition">Position where the update is triggered.</param>
	public static void TriggerSubsystemUpdateAt(Vector3Int worldPosition)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatricesList)
		{
			Vector3Int position = WorldToLocalInt(worldPosition, mat);

			if (mat.MetaDataLayer.ExistsAt(position))
			{
				mat.SubsystemManager.UpdateAt(position);
			}
		}
	}


	public static bool IsTotallyImpassable(Vector3Int worldTarget, bool isServer)
	{
		return IsPassableAtAllMatricesOneTile(worldTarget, isServer) == false &&
		       IsAtmosPassableAt(worldTarget, isServer) == false;
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int,bool)"/>
	public static IEnumerable<T> GetAt<T>(Vector3Int worldPos, bool isServer, MatrixInfo MatrixAt = null)
	{
		if (MatrixAt == null)
		{
			MatrixAt = AtPoint(worldPos, isServer);
		}

		var position = WorldToLocalInt(worldPos, MatrixAt);
		return MatrixAt.Matrix.Get<T>(position, isServer);
	}


	public static IEnumerable<T> GetAs<T>(Vector3Int worldPos, bool isServer, MatrixInfo matrixAt = null)
		where T : RegisterTile
	{
		if (matrixAt == null)
		{
			matrixAt = AtPoint(worldPos, isServer);
		}

		var position = WorldToLocalInt(worldPos, matrixAt);
		return matrixAt.Matrix.GetAs<T>(position, isServer);
	}

	public static IEnumerable<RegisterTile> GetRegisterTiles(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var position = WorldToLocalInt(worldPos, matrixInfo);
		return matrixInfo.Matrix.Get(position, isServer);
	}

	public static IEnumerable<T> GetReachableAt<T>(Vector3Int fromPos, Vector3Int toPos, bool isServer)
		where T : MonoBehaviour
	{
		if (Validations.IsReachableByPositions(fromPos, toPos, isServer))
		{
			return GetAt<T>(toPos, isServer);
		}

		return Enumerable.Empty<T>();
	}

	/// <summary>
	/// checks all tiles adjacent to the indicated world position for objects with the indicated component.
	/// Probably pretty expensive.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <param name="isServer"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static List<T> GetAdjacent<T>(Vector3Int worldPos, bool isServer) where T : MonoBehaviour
	{
		List<T> result = new List<T>();

		result.AddRange(GetAt<T>(worldPos + Vector3Int.right, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.right + Vector3Int.up, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.up, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.up + Vector3Int.left, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.left, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.left + Vector3Int.down, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.down, isServer));
		result.AddRange(GetAt<T>(worldPos + Vector3Int.down + Vector3Int.right, isServer));

		return result;
	}

	/// <summary>
	/// checks all reachable tiles adjacent to the indicated world position for objects with the indicated component.
	/// Probably pretty expensive.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <param name="isServer"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static List<T> GetReachableAdjacent<T>(Vector3Int worldPos, bool isServer) where T : MonoBehaviour
	{
		List<T> result = new List<T>();

		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.right, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.right + Vector3Int.up, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.up, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.up + Vector3Int.left, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.left, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.left + Vector3Int.down, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.down, isServer));
		result.AddRange(GetReachableAt<T>(worldPos, worldPos + Vector3Int.down + Vector3Int.right, isServer));

		return result;
	}

	//shorthand for calling GetAt at the targeted object's position
	public static IEnumerable<T> GetAt<T>(GameObject targetObject, NetworkSide side, MatrixInfo MatrixAt = null)
		where T : MonoBehaviour
	{
		return GetAt<T>((Vector3Int) targetObject.TileWorldPosition(), side == NetworkSide.Server, MatrixAt);
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Vector3Int"/>
	public static bool IsPassableAtAllMatricesOneTile(Vector3Int worldPos, bool isServer,
		bool includingPlayers = true,
		List<LayerType> excludeLayers = null, List<TileType> excludeTiles = null, GameObject context = null,
		bool ignoreObjects = false,
		bool onlyExcludeLayerOnDestination = false)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsPassableAtOneMatrixOneTile(localPos, isServer, includingPlayers,
			excludeLayers, excludeTiles, context, ignoreObjects, onlyExcludeLayerOnDestination);
		return value;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	public static bool IsPassableAtAllMatrices(Vector3Int worldOrigin, Vector3Int worldTarget, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null,
		MatrixInfo excludeMatrix = null, List<LayerType> excludeLayers = null, bool isReach = false,
		bool onlyExcludeLayerOnDestination = false, MatrixInfo matrixOrigin = null, MatrixInfo matrixTarget = null)
	{
		MatrixInfo possibleMatrix = context ? context.GetComponent<RegisterTile>()?.Matrix.MatrixInfo : null;

		if (matrixOrigin == null)
		{
			matrixOrigin = AtPoint(worldOrigin, isServer, possibleMatrix);
		}

		if (matrixTarget == null)
		{
			matrixTarget = AtPoint(worldTarget, isServer, possibleMatrix);
		}


		if (excludeMatrix != null)
		{
			if (excludeMatrix == matrixOrigin)
			{
				matrixOrigin = Instance.spaceMatrix.MatrixInfo;
			}

			if (excludeMatrix == matrixTarget)
			{
				matrixTarget = Instance.spaceMatrix.MatrixInfo;
			}
		}

		var localPosOrigin = WorldToLocalInt(worldOrigin, matrixOrigin);
		var localPosTarget = WorldToLocalInt(worldTarget, matrixOrigin);

		var value = matrixOrigin.Matrix.IsPassableAtOneMatrix(localPosOrigin, localPosTarget, isServer,
			collisionType, includingPlayers, context, excludeLayers, isReach: isReach,
			onlyExcludeLayerOnDestination: onlyExcludeLayerOnDestination);
		if (value == false)
		{
			return false;
		}

		if (matrixOrigin != matrixTarget)
		{
			localPosOrigin = WorldToLocalInt(worldOrigin, matrixTarget);
			localPosTarget = WorldToLocalInt(worldTarget, matrixTarget);

			value = matrixTarget.Matrix.IsPassableAtOneMatrix(localPosOrigin, localPosTarget, isServer,
				collisionType, includingPlayers, context, excludeLayers, isReach: isReach,
				onlyExcludeLayerOnDestination: onlyExcludeLayerOnDestination);
			if (value == false)
			{
				return false;
			}
		}

		return true;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	public static bool IsPassableAtAllMatricesV2(Vector3 worldOrigin, Vector3 worldTarget,
		MatrixCash MatrixCash, UniversalObjectPhysics Context, List<UniversalObjectPhysics> PushIng,
		List<IBumpableObject> Bumps, List<UniversalObjectPhysics> Hits = null)
	{
		var MatrixOrigin = MatrixCash.GetforDirection(Vector3Int.zero);

		var localPosOrigin = WorldToLocal(worldOrigin, MatrixOrigin);
		var localPosTarget = WorldToLocal(worldTarget, MatrixOrigin);

		bool IsPassable = MatrixOrigin.Matrix.MetaTileMap.IsPassableAtOneObjectsV2(localPosOrigin.RoundToInt(),
			localPosTarget.RoundToInt(), Context, PushIng, Bumps, Hits);

		if (IsPassable == false)
		{
			return false;
		}


		// PushIng.Clear();
		if (Hits != null) Hits.Clear();

		var matrixTarget = MatrixCash.GetforDirection((worldTarget - worldOrigin).RoundToInt());


		localPosOrigin = WorldToLocal(worldOrigin, matrixTarget);
		localPosTarget = WorldToLocal(worldTarget, matrixTarget);

		var intlocalPosOrigin = localPosOrigin.RoundToInt();
		var intlocalPosTarget = localPosTarget.RoundToInt();
		bool isSpace = matrixTarget.Matrix == MatrixManager.Instance.spaceMatrix;
		var CollisionType = global::CollisionType.Player;
		if (Context.airTime > 0)
		{
			CollisionType = CollisionType.Airborne;
		}
		else if (Context.pickupable.HasComponent)
		{
			CollisionType = CollisionType.Airborne;
		}

		if (intlocalPosOrigin.x == intlocalPosTarget.x || intlocalPosOrigin.y == intlocalPosTarget.y)
		{
			if (isSpace || matrixTarget.Matrix.MetaTileMap.IsPassableAtOneTileMapV2(intlocalPosOrigin, intlocalPosTarget,
				    CollisionType))
			{
				if (matrixTarget.Matrix.MetaTileMap.IsPassableAtOneObjectsV2(intlocalPosOrigin, intlocalPosTarget,
					    Context,
					    PushIng, Bumps, Hits))
				{
					return true;
				}
			}
		}
		else
		{
			if (isSpace || matrixTarget.Matrix.MetaTileMap.IsPassableTileMapHorizontal(intlocalPosOrigin, intlocalPosTarget,
				    CollisionType))
			{
				if (matrixTarget.Matrix.MetaTileMap.IsPassableObjectsHorizontal(intlocalPosOrigin,
					    intlocalPosTarget, Context,
					    PushIng, Bumps, Hits))
				{
					return true;
				}
			}

			if (Hits != null) Hits.Clear();
			Bumps.Clear();
			PushIng.Clear();

			if (isSpace || matrixTarget.Matrix.MetaTileMap.IsPassableTileMapVertical(intlocalPosOrigin, intlocalPosTarget,
				    CollisionType))
			{
				if (matrixTarget.Matrix.MetaTileMap.IsPassableObjectsVertical(intlocalPosOrigin, intlocalPosTarget,
					    Context,
					    PushIng, Bumps, Hits))
				{
					return true;
				}
			}
		}


		return false;
	}

	public static bool IsTableAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsTableAt(localPos, isServer);
		return value;
	}

	public static bool IsWallAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsWallAt(localPos, isServer);
		return value;
	}

	public static bool IsWindowAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsWindowAt(localPos, isServer);
		return value;
	}

	public static bool IsGrillAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsGrillAt(localPos, isServer);
		return value;
	}

	public static bool IsFloorAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsFloorAt(localPos, isServer);
		return value;
	}

	/// <summary>
	/// Serverside only: checks if tile is slippery
	/// </summary>
	[Server]
	public static bool IsSlipperyAt(Vector3Int worldPos, MatrixInfo possibleMatrix)
	{
		var matrixInfo = AtPoint(worldPos, true, possibleMatrix);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.MetaDataLayer.IsSlipperyAt(localPos);
		return value;
	}

	/// <inheritdoc cref="ObjectLayer.HasAnyDepartureBlockedByRegisterTile(Vector3Int, bool, RegisterTile)"/>
	public static bool HasAnyDepartureBlockedAt(Vector3Int worldPos, bool isServer, RegisterTile context)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.HasAnyDepartureBlockedOneMatrix(localPos, isServer, context);
		return value;
	}

	public static bool IsNonStickyAt(Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix = null)
	{
		foreach (Vector3Int pos in worldPos.BoundsAround().allPositionsWithin)
		{
			var matrixInfo = AtPoint(pos, isServer, possibleMatrix);
			var localPos = WorldToLocalInt(pos, matrixInfo);
			if (matrixInfo.MetaTileMap.IsNoGravityAt(localPos, isServer) == false)
			{
				return false;
			}
		}

		return true;
	}

	///Cross-matrix edition of <see cref="Matrix.IsNoGravityAt"/>
	///<inheritdoc cref="Matrix.IsNoGravityAt"/>
	public static bool IsNoGravityAt(Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix)
	{
		var matrixInfo = AtPoint(worldPos, isServer, possibleMatrix);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsNoGravityAt(localPos, isServer);
		return value;
	}

	/// <summary>
	/// Server only.
	/// Returns true if it's slippery or no gravity at provided position.
	/// </summary>
	[Server]
	public static bool IsSlipperyOrNoGravityAt(Vector3Int worldPos, MatrixInfo possibleMatrix)
	{
		return IsSlipperyAt(worldPos, possibleMatrix) || IsNoGravityAt(worldPos, true, possibleMatrix);
	}

	public static bool IsFloatingAt(GameObject context, Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix)
	{
		return IsFloatingAt(new[] {context}, worldPos, isServer, possibleMatrix);
	}

	public static bool IsFloatingAt(GameObject[] context, Vector3Int worldPos, bool isServer, MatrixInfo possibleMatrix)
	{
		foreach (Vector3Int pos in worldPos.BoundsAround().allPositionsWithin)
		{
			var matrixInfo = AtPoint(pos, isServer, possibleMatrix);
			var localPos = WorldToLocalInt(pos, matrixInfo);
			if (matrixInfo.MetaTileMap.IsEmptyAt(context, localPos, isServer) == false)
			{
				return false;
			}
		}

		return true;
	}

	public static bool IsFloatingAtV2Tile(Vector3 worldPos, bool isServer,
		MatrixCash MatrixCash, bool DEBUG = false) //Assuming MatrixCash is Initialised
	{
		for (int i = 0; i < MatrixCash.DIRs.Length; i++)
		{
			var DIR = MatrixCash.DIRs[i];
			var matrixInfo = MatrixCash.GetforDirection(DIR);
			var localPos = WorldToLocalInt(worldPos + DIR, matrixInfo);
			if (matrixInfo.Matrix.HasGravity)
			{
				if (matrixInfo.MetaTileMap.IsEmptyTileMap(localPos) == false)
				{
					return false;
				}
			}
			else
			{
				if (matrixInfo.MetaTileMap.HasGrabbleTileMap(localPos, isServer))
				{
					return false;
				}
			}
		}

		return true;
	}

	public static bool IsNotFloatingAtV2Objects(MovementSynchronisation.MoveData moveAction, GameObject[] context,
		Vector3Int worldPos, bool isServer,
		MatrixCash MatrixCash, out RegisterTile CanPushOff) //Assuming MatrixCash is Initialised
	{
		bool SomethingToHold = false;
		var Direction = moveAction.GlobalMoveDirection.ToVector().To3Int();
		for (int i = 0; i < MatrixCash.DIRs.Length; i++)
		{
			var DIR = MatrixCash.DIRs[i];
			var matrixInfo = MatrixCash.GetforDirection(DIR);

			var localPos = WorldToLocalInt(worldPos + DIR, matrixInfo);
			if (matrixInfo.MetaTileMap.IsObjectPresent(context, localPos, isServer, out var registerTile))
			{
				if (Direction != DIR)
				{
					SomethingToHold = true;
					CanPushOff = registerTile;
					return true;
				}
			}
		}

		CanPushOff = null;
		return SomethingToHold;
	}


	public static bool IsFloatingAtV2Objects(GameObject[] context, Vector3Int worldPos, bool isServer,
		MatrixCash MatrixCash) //Assuming MatrixCash is Initialised
	{
		for (int i = 0; i < MatrixCash.DIRs.Length; i++)
		{
			var DIR = MatrixCash.DIRs[i];
			var matrixInfo = MatrixCash.GetforDirection(DIR);

			var localPos = WorldToLocalInt(worldPos + DIR, matrixInfo);
			if (matrixInfo.MetaTileMap.IsObjectPresent(context, localPos, isServer, out var _))
			{
				return false;
			}
		}

		return true;
	}


	///Cross-matrix edition of <see cref="Matrix.IsAtmosPassableAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.IsAtmosPassableAt(UnityEngine.Vector3Int,bool)"/>
	public static bool IsAtmosPassableAt(Vector3Int worldPos, bool isServer)
	{
		var matrixInfo = AtPoint(worldPos, isServer);
		var localPos = WorldToLocalInt(worldPos, matrixInfo);
		var value = matrixInfo.Matrix.IsAtmosPassableAt(localPos, isServer);
		return value;
	}

	/// <Summary>
	/// Cross-matrix edition of GetFirst
	/// Use a Vector3Int of the WorldPosition to use
	/// </Summary>
	public T GetFirst<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		foreach (var matrixInfo in ActiveMatricesList)
		{
			T first = matrixInfo.Matrix.GetFirst<T>(WorldToLocalInt(position, matrixInfo), isServer);
			if (first)
			{
				return first;
			}
		}

		return null;
	}

	public override string ToString()
	{
		return $"MatrixManager: {string.Join(",\n", ActiveMatrices)}";
	}

	/// Get MatrixInfo by matrix id
	public static MatrixInfo Get(int id)
	{
		//Sometimes Get is still being called on the old matrixmanager instance on a
		//round restart
		if (Instance.ActiveMatrices.ContainsKey(id) == false)
			return MatrixInfo.Invalid;

		return Instance.ActiveMatrices[id];
	}

	/// Get MatrixInfo by gameObject containing Matrix component
	public static MatrixInfo Get(GameObject go)
	{
		return getInternal(mat => mat != null && mat.GameObject == go);
	}

	/// Get MatrixInfo by Objects layer transform
	public static MatrixInfo Get(Transform objectParent)
	{
		return getInternal(mat => mat != null && mat.ObjectParent == objectParent);
	}

	/// Get MatrixInfo by Matrix component
	public static MatrixInfo Get(Matrix matrix)
	{
		return matrix == null ? MatrixInfo.Invalid : Get(matrix.Id);
	}

	private static MatrixInfo getInternal(Func<MatrixInfo, bool> condition)
	{
		foreach (var matrixInfo in Instance.ActiveMatricesList)
		{
			if (condition(matrixInfo))
			{
				return matrixInfo;
			}
		}

		return MatrixInfo.Invalid;
	}

	/// <summary>
	/// <inheritdoc cref="LocalToWorld(Vector3, Matrix)"/>
	/// <para>Rounds to <c>Vector3Int</c>.</para>
	/// </summary>
	public static Vector3Int LocalToWorldInt(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorldInt(localPos, matrix?.MatrixInfo);
	}

	/// <inheritdoc cref="LocalToWorldInt(Vector3, Matrix)"/>
	public static Vector3Int LocalToWorldInt(Vector3 localPos, MatrixInfo matrix,
		MatrixState? state = null)
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix, state));
	}

	/// <inheritdoc cref="LocalToWorldInt(Vector3, Matrix)"/>
	public static Vector3Int LocalToWorldInt(Vector3Int localPos, MatrixInfo matrix,
		MatrixState? state = null)
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix, state));
	}

	/// <summary>
	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	/// </summary>
	public static Vector3 LocalToWorld(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorld(localPos, matrix?.MatrixInfo);
	}

	/// <inheritdoc cref="LocalToWorld(Vector3, Matrix)"/>
	public static Vector3 LocalToWorld(Vector3 localPos, MatrixInfo matrix, MatrixState? state = null) //Only used if you want a different rotation from the current rotation
	{
		//Invalid matrix info provided
		if (matrix == null || matrix.Equals(MatrixInfo.Invalid) || localPos == TransformState.HiddenPos)
		{
			return TransformState.HiddenPos;
		}

//		return matrix.MetaTileMap.LocalToWorld( localPos );

		if (matrix.IsMovable == false)
		{
			return localPos + matrix.Offset;
		}

		if (state == null && matrix.MetaTileMap.localToWorldMatrix != null)
		{
			return matrix.MetaTileMap.localToWorldMatrix.Value.MultiplyPoint(localPos);
		}

		var Setstate = default(MatrixState);
		if (state != null)
		{
			Setstate = state.Value;
		}

		if (Setstate.Equals(default(MatrixState)))
		{
			Setstate = matrix.MatrixMove.ClientState;
		}

		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos =
			Setstate.FacingOffsetFromInitial(matrix.MatrixMove).Quaternion *
			unpivotedPos; //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted =
			rotatedPos + matrix.MatrixMove.Pivot +
			matrix.GetOffset(Setstate); //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
	}

	/// <inheritdoc cref="WorldToLocal(Vector3, Matrix)"/>
	public static Vector3 WorldToLocal(Vector3 worldPos, MatrixInfo matrix)
	{
		// Invalid matrix info provided
		if (matrix is null || matrix.Equals(MatrixInfo.Invalid) || worldPos == TransformState.HiddenPos)
		{
			return TransformState.HiddenPos;
		}

		if (matrix.IsMovable == false)
		{
			return worldPos - matrix.Offset;
		}

		if (matrix.MetaTileMap.worldToLocalMatrix !=
		    null) //TODO After fixing Shuttle offset and Change movement to local
		{
			return matrix.MetaTileMap.worldToLocalMatrix.Value.MultiplyPoint(worldPos);
		}


		var state = matrix.MatrixMove.ClientState;
		var pivot = matrix.MatrixMove.Pivot.To3();

		return (state.FacingOffsetFromInitial(matrix.MatrixMove).QuaternionInverted *
		        (worldPos - pivot - matrix.GetOffset(state))) + pivot;
	}

	/// <summary>
	/// <inheritdoc cref="WorldToLocal(Vector3, Matrix)"/>
	/// <para>Rounds to <c>Vector3Int</c>.</para>
	/// </summary>
	public static Vector3Int WorldToLocalInt(Vector3 worldPos, int id)
	{
		return WorldToLocalInt(worldPos, Get(id));
	}

	/// <inheritdoc cref="WorldToLocalInt(Vector3, int)"/>
	public static Vector3Int WorldToLocalInt(Vector3 worldPos, Matrix matrix)
	{
		return WorldToLocalInt(worldPos, Get(matrix));
	}

	/// <inheritdoc cref="WorldToLocalInt(Vector3, int)"/>
	public static Vector3Int WorldToLocalInt(Vector3 worldPos, MatrixInfo matrix)
	{
		return Vector3Int.RoundToInt(WorldToLocal(worldPos, matrix));
	}

	/// <summary>
	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	/// </summary>
	public static Vector3 WorldToLocal(Vector3 worldPos, Matrix matrix)
	{
		return WorldToLocal(worldPos, Get(matrix));
	}

	/// <summary>
	/// Returns a list of turfs that are inbetween two points
	/// </summary>
	public static List<Vector3Int> GetTiles(Vector3 startPos, Vector2 targetPos, int travelDistance)
	{
		List<Vector3Int> positionList = new List<Vector3Int>();
		for (int i = 0; i < travelDistance; i++)
		{
			startPos = Vector2.MoveTowards(startPos, targetPos, 1);
			positionList.Add((startPos).RoundToInt());
		}

		return positionList;
	}

	public static Transform GetDefaultParent(Vector3? position, bool isServer)
	{
		if (position.HasValue == false)
		{
			return MainStationMatrix.ObjectParent;
		}

		return AtPoint(position.Value.RoundToInt(), isServer).ObjectParent;
	}

	/// <summary>
	/// Do something to matrix using its local position if you only know world position
	/// ..and don't need the reference afterwards
	/// </summary>
	/// <param name="worldPos"></param>
	/// <param name="isServer"></param>
	/// <param name="matrixAction"></param>
	public static void ForMatrixAt(Vector3Int worldPos, bool isServer, Action<MatrixInfo, Vector3Int> matrixAction)
	{
		var matrixAtPoint = AtPoint(worldPos, isServer);
		matrixAction.Invoke(matrixAtPoint, WorldToLocalInt(worldPos, matrixAtPoint));
	}
}

/// <summary>
/// Types of bumps which can occur when bumping into something
/// </summary>
public enum BumpType
{
	/// Not bumping into anything - movement not prevented
	None,

	/// A closed door
	ClosedDoor,

	/// something which can be pushed from the current direction of movement
	Push,

	/// Bump which blocks movement and causes nothing else to happen
	Blocked,

	// Something we can swap places with
	Swappable
}