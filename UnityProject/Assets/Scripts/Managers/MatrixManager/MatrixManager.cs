using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Chemistry;
using Doors;
using TileManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Systems.Atmospherics;
using Objects.Construction;

/// <summary>
/// Defines collision type we expect
/// </summary>
public enum CollisionType
{
	None = -1,
	Player = 0,
	Shuttle = 1,
	Airborne = 2
};

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
[ExecuteInEditMode]
public partial class MatrixManager : MonoBehaviour
{
	private static MatrixManager matrixManager;

	public static MatrixManager Instance
	{
		get
		{
			if (matrixManager == null)
			{
				matrixManager = FindObjectOfType<MatrixManager>();
			}

			return matrixManager;
		}
	}

	private static LayerMask tileDmgMask;

	public List<MatrixInfo> ActiveMatrices { get; private set; } = new List<MatrixInfo>();
	public List<MatrixInfo> MovableMatrices { get; private set; } = new List<MatrixInfo>();

	public static bool IsInitialized;

	/// <summary>
	/// Find a wall tilemap via its Tilemap collider
	/// </summary>
	public Dictionary<Collider2D, Tilemap> wallsTileMaps = new Dictionary<Collider2D, Tilemap>();

	public Matrix spaceMatrix { get; private set; }
	public Matrix lavaLandMatrix { get; private set; }
	private Matrix mainStationMatrix = null;

	public static MatrixInfo MainStationMatrix => Get(Instance.mainStationMatrix);

	private IEnumerator WaitForLoad()
	{
		while (Instance.spaceMatrix == null || Instance.mainStationMatrix == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		//Wait half a second to capture the majority of other matrices loading in
		yield return WaitFor.Seconds(0.1f);
		IsInitialized = true;

		EventManager.Broadcast(EVENT.MatrixManagerInit);
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		ResetMatrixManager();
		if (newScene.name.Equals("Lobby") == false)
		{
			IsInitialized = false;
			StartCoroutine(WaitForLoad());
		}
	}

	void ResetMatrixManager()
	{
		tileDmgMask = LayerMask.GetMask("Windows", "Walls");
		Instance.spaceMatrix = null;
		Instance.mainStationMatrix = null;
		MovableMatrices.Clear();
		ActiveMatrices.Clear();
		movingMatrices.Clear();
		wallsTileMaps.Clear();
		trackedIntersections.Clear();
	}

	public static void RegisterMatrix(Matrix matrixToRegister, bool isSpaceMatrix = false, bool isMainStation = false,
		bool isLavaLand = false)
	{
		foreach (var curInfo in Instance.ActiveMatrices)
		{
			if (curInfo.Matrix == matrixToRegister) return;
		}

		var matrixInfo = CreateMatrixInfoFromMatrix(matrixToRegister, Instance.ActiveMatrices.Count);

		if (Instance.ActiveMatrices.Contains(matrixInfo) == false)
		{
			Instance.ActiveMatrices.Add(matrixInfo);
		}

		if (Instance.MovableMatrices.Contains(matrixInfo) == false && matrixInfo.MatrixMove != null)
		{
			Instance.MovableMatrices.Add(matrixInfo);
		}

		matrixToRegister.Id = matrixInfo.Id;

		if (isSpaceMatrix)
		{
			if (Instance.spaceMatrix == null)
			{
				Instance.spaceMatrix = matrixToRegister;
			}
			else
			{
				Logger.Log("There is already a space matrix registered");
			}
		}

		if (isMainStation)
		{
			if (Instance.mainStationMatrix == null)
			{
				Instance.mainStationMatrix = matrixToRegister;
			}
			else
			{
				Logger.Log("There is already a main station matrix registered");
			}
		}

		if (isLavaLand)
		{
			if (Instance.lavaLandMatrix == null)
			{
				Instance.lavaLandMatrix = matrixToRegister;
			}
			else
			{
				Logger.Log("There is already a lava land matrix registered");
			}
		}

		matrixToRegister.ConfigureMatrixInfo(matrixInfo);
		Instance.InitCollisions(matrixInfo);
	}

	private static MatrixInfo CreateMatrixInfoFromMatrix(Matrix matrix, int id)
	{
		var gameObj = matrix.gameObject;
		return new MatrixInfo
		{
			Id = id,
			Matrix = matrix,
			GameObject = gameObj,
			Objects = gameObj.transform.GetComponentInChildren<ObjectLayer>().transform,
			MatrixMove = gameObj.GetComponentInParent<MatrixMove>(),
			MetaTileMap = gameObj.GetComponent<MetaTileMap>(),
			MetaDataLayer = gameObj.GetComponent<MetaDataLayer>(),
			SubsystemManager = gameObj.GetComponentInParent<SubsystemManager>(),
			ReactionManager = gameObj.GetComponentInParent<ReactionManager>(),
			TileChangeManager = gameObj.GetComponentInParent<TileChangeManager>(),
			InitialOffset = matrix.InitialOffset
		};
	}

	/// Finds first matrix that is not empty at given world pos
	public static MatrixInfo AtPoint(Vector3Int worldPos, bool isServer)
	{
		for (var i = Instance.ActiveMatrices.Count - 1; i >= 0; i--)
		{
			MatrixInfo mat = Instance.ActiveMatrices[i];
			if (mat.Matrix == Instance.spaceMatrix) continue;
			if (mat.Matrix.IsEmptyAt(WorldToLocalInt(worldPos, mat), isServer) == false)
			{
				return mat;
			}
		}

		return Instance.ActiveMatrices[Instance.spaceMatrix.Id];
	}

	public static CustomPhysicsHit Linecast(Vector3 Worldorigin, LayerTypeSelection layerMask, LayerMask? Layermask2D,
		Vector3 WorldTo)
	{
		return RayCast(Worldorigin, Vector2.zero, 0, layerMask, Layermask2D, WorldTo);
	}



	public static CustomPhysicsHit RayCast(Vector3 Worldorigin,
		Vector2 direction,
		float distance,
		LayerTypeSelection layerMask, LayerMask? Layermask2D = null, Vector3? WorldTo = null)
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

		if (direction.x == 0 && direction.y == 0)
		{
			direction = (WorldTo.Value - Worldorigin).normalized;
		}

		if (layerMask != LayerTypeSelection.None)
		{
			for (var i = Instance.ActiveMatrices.Count - 1; i >= 0; i--)
			{
				MatrixInfo mat = Instance.ActiveMatrices[i];
				//if (mat.Matrix == Instance.spaceMatrix) continue;
				if (LineIntersectsRect(Worldorigin, (Vector2) WorldTo, mat.WorldBounds))
				{
					Checkhit = mat.MetaTileMap.Raycast(Worldorigin.ToLocal(mat.Matrix), Vector2.zero, distance,
						layerMask,
						WorldTo.Value.ToLocal(mat.Matrix));


					if (Checkhit != null)
					{
						if (ClosestHit != null)
						{
							if (ClosestHit.Value.Distance < Checkhit.Value.Distance)
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

	public static bool LineIntersectsRect(Vector2 p1, Vector2 p2, BoundsInt r)
	{
		//return true;
		return LineIntersectsLine(p1, p2, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax)) ||
		       LineIntersectsLine(p1, p2, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin)) ||
		       LineIntersectsLine(p1, p2, new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax)) ||
		       LineIntersectsLine(p1, p2, new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin)) ||
		       (FindPoint(r.min, r.max, p1) && FindPoint(r.min, r.max, p2));
	}

	static bool FindPoint(Vector3Int min, Vector3Int max, Vector2 Point)
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

	public static void ListAllMatrices()
	{
		for (var i = Instance.ActiveMatrices.Count - 1; i >= 0; i--)
		{
			Debug.Log("MATRIX: " + Instance.ActiveMatrices[i].Name);
		}
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int,bool)"/>
	public static bool IsFloatingAt(Vector3Int worldPos, bool isServer)
	{
		return AllMatchInternal(mat => mat.Matrix.IsFloatingAt(WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int,bool)"/>
	public static bool IsNonStickyAt(Vector3Int worldPos, bool isServer)
	{
		return AllMatchInternal(mat => mat.Matrix.IsNonStickyAt(WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsNoGravityAt"/>
	///<inheritdoc cref="Matrix.IsNoGravityAt"/>
	public static bool IsNoGravityAt(Vector3Int worldPos, bool isServer)
	{
		return AllMatchInternal(mat => mat.Matrix.IsNoGravityAt(WorldToLocalInt(worldPos, mat), isServer));
	}

	/// <summary>
	/// Server only.
	/// Returns true if it's slippery or no gravity at provided position.
	/// </summary>
	public static bool IsSlipperyOrNoGravityAt(Vector3Int worldPos)
	{
		return IsSlipperyAt(worldPos) || IsNoGravityAt(worldPos, isServer: true);
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int,bool)"/>
	public static bool IsFloatingAt(GameObject context, Vector3Int worldPos, bool isServer)
	{
		return AllMatchInternal(mat =>
			mat.Matrix.IsFloatingAt(new[] {context}, WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject[] context, Vector3Int worldPos, bool isServer)
	{
		return AllMatchInternal(mat => mat.Matrix.IsFloatingAt(context, WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	///<inheritdoc cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos, bool isServer)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			if (mat.Matrix.IsSpaceAt(WorldToLocalInt(worldPos, mat), isServer) == false)
			{
				return false;
			}
		}

		return true;
	}

	///Cross-matrix edition of <see cref="Matrix.IsEmptyAt"/>
	///<inheritdoc cref="Matrix.IsEmptyAt"/>
	public static bool IsEmptyAt(Vector3Int worldPos, bool isServer)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			if (mat.Matrix.IsEmptyAt(WorldToLocalInt(worldPos, mat), isServer) == false)
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc cref="ObjectLayer.HasAnyDepartureBlockedByRegisterTile(Vector3Int, bool, RegisterTile)"/>
	public static bool HasAnyDepartureBlockedAnyMatrix(Vector3Int to, bool isServer, RegisterTile context)
	{
		return AnyMatchInternal(mat => mat.Matrix.HasAnyDepartureBlockedOneMatrix(WorldToLocalInt(to, mat), isServer, context));
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	public static bool IsPassableAtAllMatrices(Vector3Int worldOrigin, Vector3Int worldTarget, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null,
		int[] excludeList = null, List<LayerType> excludeLayers = null, bool isReach = false, bool onlyExcludeLayerOnDestination = false)
	{
		// Gets the list of Matrixes to actually check
		MatrixInfo[] includeList = excludeList != null
			? ExcludeFromAllMatrixes(GetList(excludeList)).ToArray()
			: Instance.ActiveMatrices.ToArray();

		return AllMatchInternal(mat =>
			mat.Matrix.IsPassableAtOneMatrix(WorldToLocalInt(worldOrigin, mat), WorldToLocalInt(worldTarget, mat), isServer,
				collisionType: collisionType, includingPlayers: includingPlayers, context: context,
				excludeLayers: excludeLayers, isReach: isReach, onlyExcludeLayerOnDestination: onlyExcludeLayerOnDestination),
				includeList);
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
	/// Checks what type of bump occurs at the specified destination.
	/// </summary>
	/// <param name="worldOrigin">current position in world coordinates</param>
	/// <param name="dir">direction of movement</param>
	/// <param name="bumper">PlayerMove trying to bump / move, used to check if we can swap with another player with help intent.</param>
	/// <returns>the bump type which occurs at the specified point (BumpInteraction.None if it's open space)</returns>
	public static BumpType GetBumpTypeAt(Vector3Int worldOrigin, Vector2Int dir, PlayerMove bumper, bool isServer)
	{
		Vector3Int targetPos = worldOrigin + dir.To3Int();


		bool hasHelpIntent = false;
		if (bumper.gameObject == PlayerManager.LocalPlayer && isServer == false)
		{
			//locally predict based on our set intent.
			hasHelpIntent = UIManager.CurrentIntent == Intent.Help;
		}

		if (isServer)
		{
			//use value known to server
			hasHelpIntent = bumper.IsHelpIntentServer;
		}

		if (hasHelpIntent)
		{
			//check for other players we can swap with
			PlayerMove other = GetSwappableAt(targetPos, bumper.gameObject, isServer);
			if (other != null)
			{
				//if we are pulling something, we can only swap with that thing
				if (bumper.PlayerScript.pushPull.IsPullingSomething &&
				    bumper.PlayerScript.pushPull.PulledObject == other.PlayerScript.pushPull)
				{
					return BumpType.Swappable;
				}
				else if (bumper.PlayerScript.pushPull.IsPullingSomething == false)
				{
					return BumpType.Swappable;
				}
			}
		}

		bool isPassable = IsPassableAtAllMatrices(worldOrigin, targetPos, isServer, includingPlayers: true, context: bumper.gameObject);
		// Only push if not passable, e.g. for directional windows being pushed from parallel
		if (isPassable == false && GetPushableAt(worldOrigin, dir, bumper.gameObject, isServer, true).Count > 0)
		{
			return BumpType.Push;
		}

		if (GetClosedDoorAt(worldOrigin, targetPos, isServer) != null)
		{
			return BumpType.ClosedDoor;
		}

		if (isPassable == false)
		{
			return BumpType.Blocked;
		}

		return BumpType.None;
	}


	/// <summary>
	/// Checks what type of bump occurs at the specified destination.
	/// </summary>
	/// <param name="playerState">player state, used to get current world position</param>
	/// <param name="playerAction">action indicating the direction player is trying to move</param>
	/// <param name="bumper">PlayerMove trying to bump / move, used to check if we can swap with another player with help intent.</param>
	/// <returns>the bump type which occurs at the specified point (BumpInteraction.None if it's open space)</returns>
	public static BumpType GetBumpTypeAt(PlayerState playerState, PlayerAction playerAction, PlayerMove bumper,
		bool isServer)
	{
		return GetBumpTypeAt(playerState.WorldPosition.RoundToInt(), playerAction.Direction(), bumper, isServer);
	}

	/// <summary>
	/// Gets the closest door to worldOrigin on the path from worldOrigin to targetPos (if there is one)
	/// Assumes that
	/// </summary>
	/// <param name="worldOrigin">Origin World position to check from. This is required because e.g. Windoors may not appear closed from certain positions.</param>
	/// <param name="targetPos">target world position to check</param>
	/// <returns>The DoorTrigger of the closed door object specified in the summary, null if no such object
	/// exists at that location</returns>
	public static InteractableDoor GetClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos, bool isServer)
	{
		// Check door on the local tile first
		Vector3Int localTarget = Instance.WorldToLocalInt(targetPos, AtPoint(targetPos, isServer).Matrix);
		var originDoorList = GetAt<InteractableDoor>(worldOrigin, isServer);
		foreach (InteractableDoor originDoor in originDoorList)
		{
			if (originDoor && originDoor.GetComponent<RegisterDoor>().IsPassableFromInside(localTarget, isServer) == false)
				return originDoor;
		}

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = Instance.WorldToLocalInt(worldOrigin, AtPoint(worldOrigin, isServer).Matrix);
		var targetDoorList = GetAt<InteractableDoor>(targetPos, isServer);
		foreach (InteractableDoor targetDoor in targetDoorList)
		{
			if (targetDoor && targetDoor.GetComponent<RegisterDoor>().IsPassableFromOutside(localOrigin, isServer) == false)
				return targetDoor;
		}

		// No closed doors on either tile
		return null;
	}


	public static DoorMasterController GetNewClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos, bool isServer)
	{
		// Check door on the local tile first
		Vector3Int localTarget = Instance.WorldToLocalInt(targetPos, AtPoint(targetPos, isServer).Matrix);
		var originDoorList = GetAt<DoorMasterController>(worldOrigin, isServer);
		foreach (DoorMasterController originDoor in originDoorList)
		{
			if (originDoor && originDoor.GetComponent<RegisterDoor>().IsPassableFromInside(localTarget, isServer) == false)
				return originDoor;
		}

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = Instance.WorldToLocalInt(worldOrigin, AtPoint(worldOrigin, isServer).Matrix);
		var targetDoorList = GetAt<DoorMasterController>(targetPos, isServer);
		foreach (DoorMasterController targetDoor in targetDoorList)
		{
			if (targetDoor && targetDoor.GetComponent<RegisterDoor>().IsPassableFromOutside(localOrigin, isServer) == false)
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
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			Vector3Int position = WorldToLocalInt(worldPosition, mat);
			MetaDataNode node = mat.MetaDataLayer.Get(position, false);

			if (node.Exists && node.IsSpace == false)
			{
				return node;
			}
		}

		return MetaDataNode.None;
	}

	/// <summary>
	/// Server only:
	/// Picks best matching matrix at provided coords and releases reagents to that tile.
	/// <inheritdoc cref="MetaDataLayer.ReagentReact"/>
	/// </summary>
	public static void ReagentReact(ReagentMix reagents, Vector3Int worldPos)
	{
		if (CustomNetworkManager.IsServer == false) 
		{
			return;
		}

		var matrixInfo = AtPoint(worldPos, true);
		Vector3Int localPos = WorldToLocalInt(worldPos, matrixInfo);
		matrixInfo.MetaDataLayer.ReagentReact(reagents, worldPos, localPos);
	}

	/// <summary>
	/// Triggers an Subsystem Update at the given position. Only triggers, when a MetaDataNode already exists.
	/// </summary>
	/// <param name="worldPosition">Position where the update is triggered.</param>
	public static void TriggerSubsystemUpdateAt(Vector3Int worldPosition)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			Vector3Int position = WorldToLocalInt(worldPosition, mat);

			if (mat.MetaDataLayer.ExistsAt(position))
			{
				mat.SubsystemManager.UpdateAt(position);
			}
		}
	}

	/// Gets pushables residing on one tile
	/// <see cref="MatrixManager.GetPushableAt(Vector3Int, Vector2Int, GameObject, bool)"/>
	private static void GetPushablesOneTile(ref List<PushPull> pushableList, Vector3Int pushableLocation, Vector2Int dir, GameObject pusher, bool isServer,
		bool ignoreNonBlockable, bool isLeaving)
	{
		Vector3Int localPushableLocation = Instance.WorldToLocalInt(pushableLocation, AtPoint(pushableLocation, isServer).Matrix);
		foreach (PushPull pushPull in GetAt<PushPull>(pushableLocation, isServer))
		{
			if (pushPull == null || pushPull.gameObject == pusher) continue;

			if (ignoreNonBlockable)
			{
				if (isLeaving)
				{
					// ignore nonblocking pushables on tile we're leaving
					if (pushPull.registerTile.IsPassableFromInside(localPushableLocation + (Vector3Int)dir, isServer: isServer))
					{
						continue;
					}
				}
				else
				{
					// ignore nonblocking pushables on tile we're entering
					if (pushPull.registerTile.IsPassableFromOutside(pushableLocation - (Vector3Int)dir,isServer: isServer))
					{
						continue;
					}
				}
			}

			// If the object being Push/Pulled is a player, and that player is buckled, we should use the pushPull object that the player is buckled to.
			// By design, chairs are not "solid" so, the condition above will filter chairs but won't filter players
			PushPull pushable = pushPull;
			PlayerMove playerMove = pushPull.GetComponent<PlayerMove>();
			if (playerMove && playerMove.IsBuckled)
			{
				PushPull buckledPushPull = playerMove.BuckledObject.GetComponent<PushPull>();

				if (buckledPushPull)
					pushable = buckledPushPull;
			}

			pushableList.Add(pushable);
		}
	}

	/// <summary>
	/// Checks if there are any pushables int the specified direction which can be pushed from the current position.
	/// </summary>
	/// <param name="worldOrigin">position pushing from</param>
	/// <param name="dir">direction to push</param>
	/// <param name="pusher">gameobject of the thing attempting the push, only used to prevent itself from being able to push itself</param>
	/// <param name="ignoreNonBlockable">true if only objects that block the indicated movement should be included</param>
	/// <returns>each pushable other than pusher at worldTarget for which it is possible to actually move it
	/// when pushing from worldOrigin (i.e. if it's against a wall and you try to push against the wall, that pushable would be excluded).
	/// Empty list if no pushables.</returns>
	public static List<PushPull> GetPushableAt(Vector3Int worldOrigin, Vector2Int dir, GameObject pusher, bool isServer, bool ignoreNonBlockable)
	{
		List<PushPull> result = new List<PushPull>();

		// Get pushables the pusher is pushing "inside" from
		GetPushablesOneTile(ref result, worldOrigin, dir, pusher, isServer, ignoreNonBlockable, true);

		// Get pushables the pusher is pushing into in the destination space
		Vector3Int worldTarget = worldOrigin + dir.To3Int();
		GetPushablesOneTile(ref result, worldTarget, dir, pusher, isServer, ignoreNonBlockable, false);

		return result;
	}

	/// <summary>
	/// Gets the player move at the position which is currently able to be swapped with (if one exists).
	/// </summary>
	/// <param name="targetWorldPos">Position to check</param>
	/// <param name="mover">gameobject of the thing attempting the move, only used to prevent itself from being checked</param>
	/// <returns>the player move that is swappable, otherwise null</returns>
	public static PlayerMove GetSwappableAt(Vector3Int targetWorldPos, GameObject mover, bool isServer)
	{
		var playerMoves = GetAt<PlayerMove>(targetWorldPos, isServer);
		foreach (PlayerMove playerMove in playerMoves)
		{
			if (playerMove && playerMove.IsSwappable && playerMove.gameObject != mover)
			{
				return playerMove;
			}
		}

		return null;
	}

	public static bool IsTotallyImpassable(Vector3Int worldTarget, bool isServer)
	{
		return IsPassableAtAllMatricesOneTile(worldTarget, isServer) == false && IsAtmosPassableAt(worldTarget, isServer) == false;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.(UnityEngine.Vector3Int,bool)"/>
	public static bool IsPassableAtAllMatricesOneTile(Vector3Int worldTarget, bool isServer, bool includingPlayers = true)
	{
		return AllMatchInternal(mat =>
			mat.Matrix.IsPassableAtOneMatrixOneTile(WorldToLocalInt(worldTarget, mat), isServer, includingPlayers: includingPlayers));
	}

	/// <summary>
	/// Serverside only: checks if tile is slippery
	/// </summary>
	public static bool IsSlipperyAt(Vector3Int worldPos)
	{
		return AnyMatchInternal(mat => mat.MetaDataLayer.IsSlipperyAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsAtmosPassableAt(UnityEngine.Vector3Int,bool)"/>
	///<inheritdoc cref="Matrix.IsAtmosPassableAt(UnityEngine.Vector3Int,bool)"/>
	public static bool IsAtmosPassableAt(Vector3Int worldTarget, bool isServer)
	{
		return AllMatchInternal(mat => mat.Matrix.IsAtmosPassableAt(WorldToLocalInt(worldTarget, mat), isServer));
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int,bool)"/>
	public static List<T> GetAt<T>(Vector3Int worldPos, bool isServer) where T : MonoBehaviour
	{
		List<T> t = new List<T>();
		for (var i = 0; i < Instance.ActiveMatrices.Count; i++)
		{
			t.AddRange(Get(i).Matrix.Get<T>(WorldToLocalInt(worldPos, i), isServer));
		}

		return t;
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

	//shorthand for calling GetAt at the targeted object's position
	public static List<T> GetAt<T>(GameObject targetObject, NetworkSide side) where T : MonoBehaviour
	{
		return GetAt<T>((Vector3Int) targetObject.TileWorldPosition(), side == NetworkSide.Server);
	}

	private static bool AllMatchInternal(Func<MatrixInfo, bool> condition, MatrixInfo[] matrixInfos)
	{
		for (var i = 0; i < matrixInfos.Length; i++)
		{
			if (condition(matrixInfos[i]) == false)
			{
				return false;
			}
		}

		return true;
	}

	// By default will just check every Matrix
	private static bool AllMatchInternal(Func<MatrixInfo, bool> condition)
	{
		return AllMatchInternal(condition, Instance.ActiveMatrices.ToArray());
	}

	private static bool AnyMatchInternal(Func<MatrixInfo, bool> condition, MatrixInfo[] matrixInfos)
	{
		for (var i = 0; i < matrixInfos.Length; i++)
		{
			if (condition(matrixInfos[i]))
			{
				return true;
			}
		}

		return false;
	}

	private static bool AnyMatchInternal(Func<MatrixInfo, bool> condition)
	{
		return AnyMatchInternal(condition, Instance.ActiveMatrices.ToArray());
	}

	public static bool IsTableAtAnyMatrix(Vector3Int worldTarget, bool isServer)
	{
		return AnyMatchInternal(mat => mat.Matrix.IsTableAt(WorldToLocalInt(worldTarget, mat), isServer));
	}

	public static bool IsWallAtAnyMatrix(Vector3Int worldTarget, bool isServer)
	{
		return AnyMatchInternal(mat => mat.Matrix.IsWallAt(WorldToLocalInt(worldTarget, mat), isServer));
	}

	public static bool IsWindowAtAnyMatrix(Vector3Int worldTarget, bool isServer)
	{
		return AnyMatchInternal(mat => mat.Matrix.IsWindowAt(WorldToLocalInt(worldTarget, mat), isServer));
	}

	/// <Summary>
	/// Cross-matrix edition of GetFirst
	/// Use a Vector3Int of the WorldPosition to use
	/// </Summary>
	public T GetFirst<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		//reverse loop so that station matrix comes last
		for (var i = ActiveMatrices.Count - 1; i >= 0; i--)
		{
			T first = ActiveMatrices[i].Matrix.GetFirst<T>(WorldToLocalInt(position, ActiveMatrices[i]), isServer);
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
		if (id >= Instance.ActiveMatrices.Count) return MatrixInfo.Invalid;

		return Instance.ActiveMatrices[id];
	}

	/// Get MatrixInfo by gameObject containing Matrix component
	public static MatrixInfo Get(GameObject go)
	{
		return getInternal(mat => mat.GameObject == go);
	}

	/// Get MatrixInfo by Objects layer transform
	public static MatrixInfo Get(Transform objectParent)
	{
		return getInternal(mat => mat.ObjectParent == objectParent);
	}

	/// Get MatrixInfo by Matrix component
	public static MatrixInfo Get(Matrix matrix)
	{
		return matrix == null ? MatrixInfo.Invalid : Get(matrix.Id);
	}

	private static MatrixInfo getInternal(Func<MatrixInfo, bool> condition)
	{
		//reverse loop so that station comes up last
		for (var i = Instance.ActiveMatrices.Count - 1; i >= 0; i--)
		{
			if (condition(Instance.ActiveMatrices[i]))
			{
				return Instance.ActiveMatrices[i];
			}
		}

		return MatrixInfo.Invalid;
	}

	/// <summary>
	/// Gets the MatrixInfo for multiple matrix ids
	/// </summary>
	/// <param name="idList"></param>
	/// <returns>List of MatrixInfos</returns>
	public static MatrixInfo[] GetList(int[] idList)
	{
		if (idList == null)
		{
			return null;
		}

		MatrixInfo[] matrixInfoList = new MatrixInfo[idList.Length];
		for (int i = 0; i < idList.Length; i++)
		{
			matrixInfoList[i] = Get(idList[i]);
		}

		return matrixInfoList;
	}

	/// <summary>
	/// Gets the MatrixInfo for multiple matrixs
	/// </summary>
	/// <param name="matrixList"></param>
	/// <returns>List of MatrixInfos</returns>
	public static MatrixInfo[] GetList(Matrix[] matrixList)
	{
		if (matrixList == null)
		{
			return null;
		}

		int[] intList = new int[matrixList.Length];
		for (int i = 0; i < matrixList.Length; i++)
		{
			intList[i] = matrixList[i].Id;
		}

		return GetList(intList);
	}

	public static MatrixInfo[] ExcludeFromAllMatrixes(MatrixInfo[] excludeList)
	{
		MatrixInfo[] matrixList = new MatrixInfo[Instance.ActiveMatrices.Count];

		if (excludeList == null)
		{
			Array.Copy(Instance.ActiveMatrices.ToArray(), matrixList, matrixList.Length);
			return matrixList;
		}

		int currentIndex = 0;
		bool toAdd = true;
		for (int i = 0; i < Instance.ActiveMatrices.Count; i++)
		{
			toAdd = true;
			for (int j = 0; j < excludeList.Length; j++)
			{
				if (Instance.ActiveMatrices[i].Id == excludeList[j].Id)
				{
					toAdd = false;
					break;
				}
			}

			if (toAdd)
			{
				matrixList[currentIndex++] = Instance.ActiveMatrices[i];
			}
		}

		// Only return the number of matrix's actually included
		MatrixInfo[] returnList = new MatrixInfo[currentIndex];
		Array.Copy(matrixList, returnList, currentIndex);

		return returnList;
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3Int LocalToWorldInt(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorldInt(localPos, Get(matrix));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3Int LocalToWorldInt(Vector3 localPos, MatrixInfo matrix,
		MatrixState state = default(MatrixState))
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix, state));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 LocalToWorld(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorld(localPos, Get(matrix));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 LocalToWorld(Vector3 localPos, MatrixInfo matrix, MatrixState state = default(MatrixState))
	{
		//Invalid matrix info provided
		if (matrix.Equals(MatrixInfo.Invalid) || localPos == TransformState.HiddenPos)
		{
			return TransformState.HiddenPos;
		}

//		return matrix.MetaTileMap.LocalToWorld( localPos );

		if (matrix.MatrixMove == null)
		{
			return localPos + matrix.Offset;
		}

		if (state.Equals(default(MatrixState)))
		{
			state = matrix.MatrixMove.ClientState;
		}

		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos =
			state.FacingOffsetFromInitial(matrix.MatrixMove).Quaternion *
			unpivotedPos; //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted =
			rotatedPos + matrix.MatrixMove.Pivot +
			matrix.GetOffset(state); //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 WorldToLocal(Vector3 worldPos, MatrixInfo matrix)
	{
		//Invalid matrix info provided
		if (matrix.Equals(MatrixInfo.Invalid) || worldPos == TransformState.HiddenPos)
		{
			return TransformState.HiddenPos;
		}


		if (matrix.MatrixMove == null)
		{
			return worldPos - matrix.Offset;
		}

		var state = matrix.MatrixMove.ClientState;

		return (state.FacingOffsetFromInitial(matrix.MatrixMove).QuaternionInverted * (worldPos -
			matrix.MatrixMove.Pivot -
			matrix.GetOffset(state))) + matrix.MatrixMove.Pivot;


		return (matrix.MatrixMove.FacingOffsetFromInitial.QuaternionInverted *
		        (worldPos - matrix.Offset - matrix.MatrixMove.Pivot)) +
		       matrix.MatrixMove.Pivot;


		//return

		// return (matrix.MatrixMove.FacingOffsetFromInitial.QuaternionInverted *
		// (worldPos - matrix.Offset - matrix.MatrixMove.Pivot)) +
		// matrix.MatrixMove.Pivot;
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3Int WorldToLocalInt(Vector3 worldPos, int id)
	{
		return WorldToLocalInt(worldPos, Get(id));
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3Int WorldToLocalInt(Vector3 worldPos, Matrix matrix)
	{
		return WorldToLocalInt(worldPos, Get(matrix));
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3Int WorldToLocalInt(Vector3 worldPos, MatrixInfo matrix)
	{
		return Vector3Int.RoundToInt(WorldToLocal(worldPos, matrix));
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 WorldToLocal(Vector3 worldPos, Matrix matrix)
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
			positionList.Add((startPos).CutToInt());
		}

		return positionList;
	}

	public void CleanTile(Vector3 worldPos, bool makeSlippery)
	{
		var worldPosInt = worldPos.CutToInt();
		var matrix = AtPoint(worldPosInt, true);
		var localPosInt = WorldToLocalInt(worldPosInt, matrix);
		var floorDecals = GetAt<FloorDecal>(worldPosInt, isServer: true);

		for (var i = 0; i < floorDecals.Count; i++)
		{
			floorDecals[i].TryClean();
		}

		if (IsSpaceAt(worldPosInt, true) == false && makeSlippery)
		{
			// Create a WaterSplat Decal (visible slippery tile)
			EffectsFactory.WaterSplat(worldPosInt);

			// Sets a tile to slippery
			matrix.MetaDataLayer.MakeSlipperyAt(localPosInt);
		}
	}

	public static Transform GetDefaultParent( Vector3? position, bool isServer )
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
