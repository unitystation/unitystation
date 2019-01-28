using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
public class MatrixManager : MonoBehaviour
{
	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
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
	public static MatrixInfo AtPoint(Vector3Int worldPos)
	{
		MatrixInfo matrixInfo = getInternal(mat => mat.Matrix.HasTile( WorldToLocalInt(worldPos, mat) ));
		return Equals(matrixInfo, MatrixInfo.Invalid) ? Instance.ActiveMatrices[0] : matrixInfo;
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsNonStickyAt(Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsNonStickyAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsNoGravityAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsNoGravityAt(UnityEngine.Vector3Int)"/>
	public static bool IsNoGravityAt(Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsNoGravityAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject context, Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(new[] {context}, WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject[] context, Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsFloatingAt(context, WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	///<inheritdoc cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsSpaceAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsEmptyAt"/>
	///<inheritdoc cref="Matrix.IsEmptyAt"/>
	public static bool IsEmptyAt(Vector3Int worldPos)
	{
		return isAtInternal(mat => mat.Matrix.IsEmptyAt(WorldToLocalInt(worldPos, mat)));
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	/// FIXME: not truly cross-matrix. can walk diagonally between matrices
	public static bool IsPassableAt(Vector3Int worldOrigin, Vector3Int worldTarget, bool includingPlayers = true, GameObject context = null)
	{
		return isAtInternal(mat => mat.Matrix.IsPassableAt(WorldToLocalInt(worldOrigin, mat),
			WorldToLocalInt(worldTarget, mat), includingPlayers, context));
	}

	/// <summary>
	/// Checks what type of bump occurs at the specified destination.
	/// </summary>
	/// <param name="worldOrigin">current position in world coordinates</param>
	/// <param name="dir">direction of movement</param>
	/// <param name="bumper">GameObject trying to bump / move, used so we don't bump against ourselves</param>
	/// <returns>the bump type which occurs at the specified point (BumpInteraction.None if it's open space)</returns>
	public static BumpType GetBumpTypeAt(Vector3Int worldOrigin, Vector2Int dir, GameObject bumper)
	{
		Vector3Int targetPos = worldOrigin + dir.To3Int();
		if (GetPushableAt(worldOrigin, dir, bumper).Count > 0)
		{
			return BumpType.Push;
		}
		else if (GetClosedDoorAt(worldOrigin, targetPos) != null)
		{
			return BumpType.ClosedDoor;
		}
		else if (!IsPassableAt(worldOrigin, targetPos, true, bumper))
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
	/// <param name="bumper">GameObject trying to bump / move, used so we don't bump against ourselves</param>
	/// <returns>the bump type which occurs at the specified point (BumpInteraction.None if it's open space)</returns>
	public static BumpType GetBumpTypeAt(PlayerState playerState, PlayerAction playerAction, GameObject bumper)
	{
		return GetBumpTypeAt(playerState.WorldPosition.RoundToInt(), playerAction.Direction(), bumper);
	}

	/// <summary>
	/// Gets the closest door to worldOrigin on the path from worldOrigin to targetPos (if there is one)
	/// Assumes that
	/// </summary>
	/// <param name="worldOrigin">Origin World position to check from. This is required because e.g. Windoors may not appear closed from certain positions.</param>
	/// <param name="targetPos">target world position to check</param>
	/// <returns>The DoorTrigger of the closed door object specified in the summary, null if no such object
	/// exists at that location</returns>
	public static DoorTrigger GetClosedDoorAt(Vector3Int worldOrigin, Vector3Int targetPos)
	{
		// Check door on the local tile first
		Vector3Int localTarget = Instance.WorldToLocalInt(targetPos, AtPoint(targetPos).Matrix);
		DoorTrigger originDoor = Instance.GetFirst<DoorTrigger>(worldOrigin);
		if (originDoor && !originDoor.GetComponent<RegisterDoor>().IsPassableTo(localTarget))
			return originDoor;

		// No closed door on local tile, check target tile
		Vector3Int localOrigin = Instance.WorldToLocalInt(worldOrigin, AtPoint(worldOrigin).Matrix);
		DoorTrigger targetDoor = Instance.GetFirst<DoorTrigger>(targetPos);
		if (targetDoor && !targetDoor.GetComponent<RegisterDoor>().IsPassable(localOrigin))
			return targetDoor;

		// No closed doors on either tile
		return null;
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
	public static List<PushPull> GetPushableAt(Vector3Int worldOrigin, Vector2Int dir, GameObject pusher)
	{
		Vector3Int worldTarget = worldOrigin + dir.To3Int();
		var pushPulls = MatrixManager.GetAt<PushPull>(worldTarget);
		List<PushPull> result = new List<PushPull>();
		for ( var i = 0; i < pushPulls.Count; i++ )
		{
			PushPull pushPull = pushPulls[i];
			if ( pushPull && pushPull.gameObject != pusher && pushPull.IsSolid )
			{
				if ( pushPull.CanPush( worldTarget, Vector2Int.RoundToInt( dir ) ) )
				{
					result.Add( pushPull );
				}
			}
		}

		return result;
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	public static bool IsPassableAt(Vector3Int worldTarget)
	{
		return isAtInternal(mat => mat.Matrix.IsPassableAt(WorldToLocalInt(worldTarget, mat)));
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int)"/>
	public static List<T> GetAt<T>(Vector3Int worldPos) where T : MonoBehaviour
	{
		List<T> t = new List<T>();
		for (var i = 0; i < Instance.ActiveMatrices.Length; i++)
		{
			t.AddRange( Get(i).Matrix.Get<T>( WorldToLocalInt(worldPos,i) ) );
		}

		return t;
	}

//	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int)"/>
//	public static IEnumerable<T> GetAt<T>(Vector3Int worldPos) where T : MonoBehaviour
//	{
//		return getAtInternal(mat => mat.Matrix.Get<T>(WorldToLocalInt(worldPos, mat)));
//	}
//
//	private static IEnumerable<T> getAtInternal<T>(Func<MatrixInfo, IEnumerable<T>> condition) where T : MonoBehaviour
//	{
//		IEnumerable<T> t = new List<T>();
//		for (var i = 0; i < Instance.activeMatrices.Count; i++)
//		{
//			t = t.Concat(condition(Instance.activeMatrices[i]));
//		}
//
//		return t;
//	}

	private static bool isAtInternal(Func<MatrixInfo, bool> condition)
	{
		for (var i = 0; i < Instance.ActiveMatrices.Length; i++)
		{
			if (!condition(Instance.ActiveMatrices[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <Summary>
	/// Cross-matrix edition of GetFirst
	/// Use a Vector3Int of the WorldPosition to use
	/// </Summary>
	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		for (var i = 0; i < ActiveMatrices.Length; i++)
		{
			T first = ActiveMatrices[i].Matrix.GetFirst<T>(WorldToLocalInt(position, ActiveMatrices[i]));
			if (first)
			{
				return first;
			}
		}

		return null;
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
		Logger.Log($"Semi-init {this}");
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
		return Get( matrix == null ? 0 : matrix.Id );
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
		if (matrix.Equals(MatrixInfo.Invalid))
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
		if (matrix.Equals(MatrixInfo.Invalid))
		{
			return TransformState.HiddenPos;
		}

		if (!matrix.MatrixMove)
		{
			return worldPos - matrix.Offset;
		}

		return (matrix.MatrixMove.ClientState.RotationOffset.EulerInverted * (worldPos - matrix.Offset - matrix.MatrixMove.Pivot)) +
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
	public GameObject GameObject;
	public Vector3Int CachedOffset;

	/// <summary>
	/// Transform containing all the physical objects on the map
	public Transform Objects;

	private Vector3Int initialOffset;
	private Vector3Int initialOffset2;

	public Vector3Int InitialOffset
	{
		get { return initialOffset; }
		set
		{
			initialOffset = value;
			CachedOffset = value;
		}
	}

	public Vector3Int Offset => GetOffset();

	public Vector3Int GetOffset(MatrixState state = default(MatrixState))
	{
		if (!MatrixMove || !MatrixMove.IsMoving)
		{
			return CachedOffset;
		}

		if (state.Equals(default(MatrixState)))
		{
			state = MatrixMove.ClientState;
		}

		CachedOffset = initialOffset + (state.Position.RoundToInt() - MatrixMove.InitialPos);
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
	Blocked
}