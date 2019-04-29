using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// <summary>
/// Defines collision type we expect
/// </summary>
public enum CollisionType
{
	None = -1,
	Player = 0,
	Shuttle = 1
};

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
public class MatrixManager : MonoBehaviour
{
	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
	private static LayerMask tileDmgMask;

	private MatrixInfo[] ActiveMatrices = new MatrixInfo[0];

	public static bool IsInitialized;

	/// <summary>
	/// Find a wall tilemap via its Tilemap collider
	/// </summary>
	public Dictionary<Collider2D, Tilemap> wallsTileMaps = new Dictionary<Collider2D, Tilemap>();

	[Header("Set the amount of matrices in the scene here")]
	public int matrixCount;

	[Header("Cache the matrix that also handles space here")]
	public Matrix spaceMatrix;

	/// Finds first matrix that is not empty at given world pos
	public static MatrixInfo AtPoint(Vector3Int worldPos, bool isServer)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			if (mat.Matrix.HasTile(WorldToLocalInt(worldPos, mat), isServer))
			{
				return mat;
			}
		}
		return Instance.ActiveMatrices[0];
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(Vector3Int worldPos, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsNonStickyAt(Vector3Int worldPos, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsNonStickyAt(WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsNoGravityAt"/>
	///<inheritdoc cref="Matrix.IsNoGravityAt"/>
	public static bool IsNoGravityAt(Vector3Int worldPos, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsNoGravityAt(WorldToLocalInt(worldPos, mat), isServer ));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject context, Vector3Int worldPos, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(new[] {context}, WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject[] context, Vector3Int worldPos, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(context, WorldToLocalInt(worldPos, mat), isServer));
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	///<inheritdoc cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos, bool isServer)
	{
		foreach (MatrixInfo mat in Instance.ActiveMatrices)
		{
			if (!mat.Matrix.IsSpaceAt(WorldToLocalInt(worldPos, mat), isServer))
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
			if (!mat.Matrix.IsEmptyAt(WorldToLocalInt(worldPos, mat), isServer))
			{
				return false;
			}
		}

		return true;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	/// FIXME: not truly cross-matrix. can walk diagonally between matrices
	public static bool IsPassableAt(Vector3Int worldOrigin, Vector3Int worldTarget, bool isServer, CollisionType collisionType = CollisionType.Player, bool includingPlayers = true, GameObject context = null, int[] excludeList = null)
	{
		// Gets the list of Matrixes to actually check
		MatrixInfo[] includeList = excludeList != null ? ExcludeFromAllMatrixes(GetList(excludeList)) : Instance.ActiveMatrices;
		return isAtInternal(mat =>
			mat.Matrix.IsPassableAt(WorldToLocalInt(worldOrigin, mat),WorldToLocalInt(worldTarget, mat), isServer,
				collisionType: collisionType, includingPlayers: includingPlayers, context: context), includeList);
	}

	/// <summary>
	/// Server only.
	/// Finds all TilemapDamage at given world pos, so you could apply damage to given tiles via DoMeleeDamage etc.
	/// </summary>
	/// <param name="worldTarget"></param>
	/// <returns></returns>
	public static List<TilemapDamage> GetDamagetableTilemapsAt( Vector3Int worldTarget )
	{
		var hit2D = Physics2D.RaycastAll(worldTarget.To2Int(), Vector2.zero, 10, tileDmgMask);
		var hitTilemaps = new List<TilemapDamage>();
		for (int i = 0; i < hit2D.Length; i++) {
			var tileDmg = hit2D[i].collider.gameObject.GetComponent<TilemapDamage>();
			if ( tileDmg != null ) {
				hitTilemaps.Add( tileDmg );
			}
		}

		return hitTilemaps;
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
		if (bumper.IsHelpIntent)
		{
			//check for other players with help intent
			PlayerMove other = GetHelpIntentAt(targetPos, bumper.gameObject, isServer);
			if (other != null)
			{
				//if we are pulling something, we can only swap with that thing
				if (bumper.PlayerScript.pushPull.IsPullingSomething && bumper.PlayerScript.pushPull.PulledObject == other.PlayerScript.pushPull)
				{
					return BumpType.HelpIntent;
				}
				else if (!bumper.PlayerScript.pushPull.IsPullingSomething)
				{
					return BumpType.HelpIntent;
				}
			}
		}
		if (GetPushableAt(worldOrigin, dir, bumper.gameObject, isServer).Count > 0)
		{
			return BumpType.Push;
		}

		if (GetClosedDoorAt(worldOrigin, targetPos, isServer) != null)
		{
			return BumpType.ClosedDoor;
		}

		if (!IsPassableAt(worldOrigin, targetPos, isServer, includingPlayers: true, context: bumper.gameObject))
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
	public static BumpType GetBumpTypeAt(PlayerState playerState, PlayerAction playerAction, PlayerMove bumper, bool isServer)
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
	public static DoorTrigger GetClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos, bool isServer)
	{
		// Check door on the local tile first
		Vector3Int localTarget = Instance.WorldToLocalInt(targetPos, AtPoint(targetPos, isServer).Matrix);
		DoorTrigger originDoor = Instance.GetFirst<DoorTrigger>(worldOrigin, isServer);
		if (originDoor && !originDoor.GetComponent<RegisterDoor>().IsPassableTo(localTarget, isServer))
			return originDoor;

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = Instance.WorldToLocalInt(worldOrigin, AtPoint(worldOrigin, isServer).Matrix);
		DoorTrigger targetDoor = Instance.GetFirst<DoorTrigger>(targetPos, isServer);
		if (targetDoor && !targetDoor.GetComponent<RegisterDoor>().IsPassable(localOrigin, isServer))
			return targetDoor;

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

			if (node.Exists && !node.IsSpace)
			{
				return node;
			}
		}

		return MetaDataNode.None;
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

	/// <summary>
	/// Checks if there are any pushables at the specified target which can be pushed from the current position.
	/// </summary>
	/// <param name="worldOrigin">position pushing from</param>
	/// <param name="dir">direction to push</param>
	/// <param name="pusher">gameobject of the thing attempting the push, only used to prevent itself from being able to push itself</param>
	/// <returns>each pushable other than pusher at worldTarget for which it is possible to actually move it
	/// when pushing from worldOrigin (i.e. if it's against a wall and you try to push against the wall, that pushable would be excluded).
	/// Empty list if no pushables.</returns>
	public static List<PushPull> GetPushableAt(Vector3Int worldOrigin, Vector2Int dir, GameObject pusher, bool isServer)
	{
		Vector3Int worldTarget = worldOrigin + dir.To3Int();
		List<PushPull> result = new List<PushPull>();
		foreach ( PushPull pushPull in GetAt<PushPull>(worldTarget, isServer) )
		{
			if ( pushPull && pushPull.gameObject != pusher && (isServer ? pushPull.IsSolidServer : pushPull.IsSolidClient) )
			{
				if ( isServer ?
					pushPull.CanPushServer( worldTarget, Vector2Int.RoundToInt( dir ) )
					: pushPull.CanPushClient( worldTarget, Vector2Int.RoundToInt( dir ) )
				)
				{
					result.Add( pushPull );
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Return the playermove if there is a non-passable player with help intent at the specified position who is
	/// not pulling something and not restrained (it is not possible to swap with someone who is pulling something)
	/// </summary>
	/// <param name="targetWorldPos">Position to check</param>
	/// <param name="mover">gameobject of the thing attempting the move, only used to prevent itself from being checked</param>
	/// <returns>the player move if they have help intent and are not passable, otherwise null</returns>
	public static PlayerMove GetHelpIntentAt(Vector3Int targetWorldPos, GameObject mover, bool isServer)
	{
		var playerMoves = GetAt<PlayerMove>(targetWorldPos, isServer);
		foreach (PlayerMove playerMove in playerMoves)
		{
			if (playerMove.IsHelpIntent && !playerMove.PlayerScript.registerTile.IsPassable(isServer) && playerMove.gameObject != mover
			    && !playerMove.PlayerScript.pushPull.IsPullingSomething && !playerMove.IsRestrained)
			{
				return playerMove;
			}
		}

		return null;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	public static bool IsPassableAt(Vector3Int worldTarget, bool isServer)
	{
		return isAtInternal(mat => mat.Matrix.IsPassableAt(WorldToLocalInt(worldTarget, mat), isServer));
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int)"/>
	public static List<T> GetAt<T>(Vector3Int worldPos, bool isServer) where T : MonoBehaviour
	{
		List<T> t = new List<T>();
		for (var i = 0; i < Instance.ActiveMatrices.Length; i++)
		{
			t.AddRange( Get(i).Matrix.Get<T>( WorldToLocalInt(worldPos,i), isServer ) );
		}

		return t;
	}

	private static bool isAtInternal(Func<MatrixInfo, bool> condition, MatrixInfo[] matrixInfos)
	{
		for (var i = 0; i < matrixInfos.Length; i++)
		{
			if (!condition(matrixInfos[i]))
			{
				return false;
			}
		}

		return true;
	}

	// By default will just check every Matrix
	private static bool isAtInternal(Func<MatrixInfo, bool> condition)
	{
		return isAtInternal(condition, Instance.ActiveMatrices);
	}

	/// <Summary>
	/// Cross-matrix edition of GetFirst
	/// Use a Vector3Int of the WorldPosition to use
	/// </Summary>
	public T GetFirst<T>(Vector3Int position, bool isServer) where T : MonoBehaviour
	{
		for (var i = 0; i < ActiveMatrices.Length; i++)
		{
			T first = ActiveMatrices[i].Matrix.GetFirst<T>(WorldToLocalInt(position, ActiveMatrices[i]), isServer);
			if (first)
			{
				return first;
			}
		}

		return null;
	}

	private void OnEnable()
	{
		IsInitialized = false;
		tileDmgMask = LayerMask.GetMask( "Windows", "Walls" );
	}

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}

		IsInitialized = false;
		StartCoroutine(WaitForLoad());
	}

	/// Waiting for scene to load before finding matrices
	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(0.5f);
		InitMatrices();
	}

	///Finding all matrices
	private void InitMatrices()
	{
		List<Matrix> findMatrices = FindObjectsOfType<Matrix>().ToList();
		if (spaceMatrix)
		{
			findMatrices.Remove(spaceMatrix);
			findMatrices.Insert(0, spaceMatrix);
		}

		if (findMatrices.Count < matrixCount)
		{
//			Logger.Log( "Matrix init failure, will try in 0.5" );
			StartCoroutine(WaitForLoad());
			return;
		}

		var activeMatrices = new List<MatrixInfo>(/*ActiveMatrices*/);

		for (int i = 0; i < findMatrices.Count; i++)
		{
			Matrix matrix = findMatrices[i];
			GameObject gameObj = matrix.gameObject;
			MatrixInfo matrixInfo = new MatrixInfo
			{
				Id = i,
				Matrix = matrix,
				GameObject = gameObj,
				Objects = gameObj.transform.GetComponentInChildren<ObjectLayer>().transform,
				MatrixMove = gameObj.GetComponentInParent<MatrixMove>(),
				MetaTileMap = gameObj.GetComponent<MetaTileMap>(),
				MetaDataLayer = gameObj.GetComponent<MetaDataLayer>(),
				SubsystemManager = gameObj.GetComponentInParent<SubsystemManager>(),
//				NetId is initialized later
				InitialOffset = matrix.InitialOffset
			};
			if (!activeMatrices.Contains(matrixInfo))
			{
				activeMatrices.Add(matrixInfo);
			}
			matrix.Id = i;
		}

		ActiveMatrices = activeMatrices.ToArray();

		IsInitialized = true;

		//These aren't fully initialized at that moment; init is truly finished when server is up and NetIDs are resolved
//		Logger.Log($"Semi-init {this}");
	}

	public override string ToString()
	{
		return $"MatrixManager: {string.Join(",\n", ActiveMatrices)}";
	}

	/// Get MatrixInfo by matrix id
	public static MatrixInfo Get(int id)
	{
		if ( !IsInitialized )
		{
			Instance.InitMatrices();
		}
		return Instance.ActiveMatrices[id];
	}

	/// Get MatrixInfo by gameObject containing Matrix component
	public static MatrixInfo Get(GameObject go)
	{
		return getInternal(mat => mat.GameObject == go);
	}

	/// Get MatrixInfo by Matrix component
	public static MatrixInfo Get(Matrix matrix)
	{
		return matrix == null ? MatrixInfo.Invalid : Get( matrix.Id );
	}

	private static MatrixInfo getInternal(Func<MatrixInfo, bool> condition)
	{
		if (!IsInitialized)
		{
			//			Logger.Log( "MatrixManager list not ready yet, trying to init" );
			Instance.InitMatrices();
		}

		for (var i = 0; i < Instance.ActiveMatrices.Length; i++)
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
		MatrixInfo[] matrixList = new MatrixInfo[Instance.ActiveMatrices.Length];

		if (excludeList == null)
		{
			Array.Copy(Instance.ActiveMatrices, matrixList, matrixList.Length);
			return matrixList;
		}

		int currentIndex = 0;
		bool toAdd = true;
		for (int i = 0; i < Instance.ActiveMatrices.Length; i++)
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
	public static Vector3Int LocalToWorldInt(Vector3 localPos, MatrixInfo matrix, MatrixState state = default(MatrixState))
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix, state));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 LocalToWorld(Vector3 localPos, Matrix matrix)
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

		if (!matrix.MatrixMove)
		{
			return localPos + matrix.Offset;
		}

		if (state.Equals(default(MatrixState)))
		{
			state = matrix.MatrixMove.ClientState;
		}

		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos = state.RotationOffset.Quaternion * unpivotedPos; //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted = rotatedPos + matrix.MatrixMove.Pivot + matrix.GetOffset( state ); //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 WorldToLocal( Vector3 worldPos, MatrixInfo matrix )
	{
		//Invalid matrix info provided
		if (matrix.Equals(MatrixInfo.Invalid) || worldPos == TransformState.HiddenPos)
		{
			return TransformState.HiddenPos;
		}

		if (!matrix.MatrixMove)
		{
			return worldPos - matrix.Offset;
		}

		return (matrix.MatrixMove.ClientState.RotationOffset.QuaternionInverted * (worldPos - matrix.Offset - matrix.MatrixMove.Pivot)) +
		       matrix.MatrixMove.Pivot;
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

}

/// Struct that helps identify matrices
public struct MatrixInfo
{
	public int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public MetaDataLayer MetaDataLayer;
	public SubsystemManager SubsystemManager;
	public GameObject GameObject;
	public Vector3Int CachedOffset;
	// position we were at when offset was last cached, to check if it is invalid
	private Vector3 cachedPosition;

	/// <summary>
	/// Transform containing all the physical objects on the map
	public Transform Objects;

	private Vector3Int initialOffset;

	public Vector3Int InitialOffset
	{
		get { return initialOffset; }
		set
		{
			initialOffset = value;
		}
	}

	public Vector3Int Offset => GetOffset();

	public Vector3Int GetOffset(MatrixState state = default(MatrixState))
	{
		if (!MatrixMove)
		{
			return InitialOffset;
		}

		if (state.Equals(default(MatrixState)))
		{
			state = MatrixMove.ClientState;
		}

		if (cachedPosition != state.Position)
		{
			//if we moved, update cached offset
			cachedPosition = state.Position;
			CachedOffset = initialOffset + (state.Position.RoundToInt() - MatrixMove.InitialPos);
		}

		return CachedOffset;
	}

	public MatrixMove MatrixMove;

	private NetworkInstanceId netId;

	public NetworkInstanceId NetId
	{
		get
		{
			//late init, because server is not yet up during InitMatrices()
			if (netId.IsEmpty() || netId == NetworkInstanceId.Invalid)
			{
				netId = getNetId(Matrix);
			}

			return netId;
		}
	}

	/// Null object
	public static readonly MatrixInfo Invalid = new MatrixInfo {Id = -1};

	public override string ToString()
	{
		return Equals(Invalid)
			? "[Invalid matrix]"
			: $"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetId}]";
	}

	public bool Equals(MatrixInfo other)
	{
		return Id == other.Id;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}

		return obj is MatrixInfo && Equals((MatrixInfo) obj);
	}

	public override int GetHashCode()
	{
		return Id;
	}

	///Figuring out netId. NetworkIdentity is located on the pivot (parent) gameObject for MatrixMove-equipped matrices
	private static NetworkInstanceId getNetId(Matrix matrix)
	{
		var netId = NetworkInstanceId.Invalid;
		if (!matrix)
		{
			return netId;
		}

		NetworkIdentity component = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		NetworkIdentity componentInParent = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		if (component && component.netId != NetworkInstanceId.Invalid && !component.netId.IsEmpty())
		{
			netId = component.netId;
		}

		if (componentInParent && componentInParent.netId != NetworkInstanceId.Invalid && !componentInParent.netId.IsEmpty())
		{
			netId = componentInParent.netId;
		}

		if (netId == NetworkInstanceId.Invalid)
		{
			Logger.LogWarning($"Invalid NetID for matrix {matrix.gameObject.name}!", Category.Matrix);
		}

		return netId;
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

	// A help intent player
	HelpIntent
}