using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tilemaps;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Tiles;

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
public class MatrixManager : MonoBehaviour
{
	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
	private List<MatrixInfo> activeMatrices = new List<MatrixInfo>();

	/// List of active matrices
	public List<MatrixInfo> Matrices => activeMatrices;

	/// <summary>
	/// using the Collider2D of a wall tile you can find the tilemap of the topLayerFX in that matrix.
	/// Used for FOV and any effects that can be shown over the top of walls
	/// </summary>
	public Dictionary<Collider2D, Tilemap> wallsToTopLayerFX = new Dictionary<Collider2D, Tilemap>();

	/// <summary>
	/// Find a wall tilemap via its Tilemap collider
	/// </summary>
	public Dictionary<Collider2D, Tilemap> wallsTileMaps = new Dictionary<Collider2D, Tilemap>();

	[Header("Set the amount of matricies in the scene here")]
	public int matrixCount;

	/// Finds first matrix that is not empty at given world pos
	public static MatrixInfo AtPoint(Vector3Int worldPos) {
		MatrixInfo matrixInfo = getInternal( mat => !mat.Matrix.IsEmptyAt( WorldToLocalInt( worldPos, mat ) ) );
		return Equals( matrixInfo, MatrixInfo.Invalid ) ? Instance.activeMatrices[0] : matrixInfo;
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt"/>
	public static bool IsFloatingAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsFloatingAt( WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsSpaceAt( WorldToLocalInt( worldPos, mat ) ) );
	}
	
	///Cross-matrix edition of <see cref="Matrix.IsEmptyAt"/>
	public static bool IsEmptyAt(Vector3Int worldPos) {
		return isAtInternal( mat => mat.Matrix.IsEmptyAt( WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int)"/>
	/// FIXME: not truly cross-matrix. can walk diagonally between matrices
	public static bool IsPassableAt(Vector3Int worldOrigin, Vector3Int worldTarget) {
		return isAtInternal( mat => mat.Matrix.IsPassableAt( WorldToLocalInt( worldOrigin, mat ),
															 WorldToLocalInt( worldTarget, mat ) ) );
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int)"/>
	public static IEnumerable<T> GetAt<T>( Vector3Int worldPos ) where T : MonoBehaviour {
		return getAtInternal( mat => mat.Matrix.Get<T>( WorldToLocalInt( worldPos, mat ) ));
	}

	private static IEnumerable<T> getAtInternal<T>( Func<MatrixInfo, IEnumerable<T>> condition ) where T : MonoBehaviour {
		IEnumerable<T> t = new List<T>();
		for (var i = 0; i < Instance.activeMatrices.Count; i++) {
			t = t.Concat( condition( Instance.activeMatrices[i] ) );
		}

		return t;
	}

	private static bool isAtInternal(Func<MatrixInfo, bool> condition)
	{
		for (var i = 0; i < Instance.activeMatrices.Count; i++)
		{
			if (!condition(Instance.activeMatrices[i]))
			{
				return false;
			}
		}
		return true;
	}

	/// Cross-matrix edition of GetFirst
	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		for (var i = 0; i < activeMatrices.Count; i++)
		{
			T first = activeMatrices[i].Matrix.GetFirst<T>(WorldToLocalInt(position, activeMatrices[i]));
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
		Matrix[] findMatrices = FindObjectsOfType<Matrix>();
		if (findMatrices.Length < matrixCount)
		{
//			Debug.Log( "Matrix init failure, will try in 0.5" );
			StartCoroutine(WaitForLoad());
			return;
		}

		for (int i = 0; i < findMatrices.Length; i++) {
			MatrixInfo matrixInfo = new MatrixInfo
			{
				Id = i,
				Matrix = findMatrices[i],
				GameObject = findMatrices[i].gameObject,
				MatrixMove = findMatrices[i].gameObject.GetComponentInParent<MatrixMove>(),
				MetaTileMap = findMatrices[i].gameObject.GetComponent<MetaTileMap>(),
//				NetId is initialized later
				InitialOffset = findMatrices[i].InitialOffset
			};
			if ( !activeMatrices.Contains( matrixInfo ) ) {
				activeMatrices.Add( matrixInfo );
			}
		}

		//These aren't fully initialized at that moment; init is truly finished when server is up and NetIDs are resolved
//		Debug.Log($"Semi-init {this}");
	}

	public override string ToString()
	{
		return $"MatrixManager: {String.Join(",\n", activeMatrices)}";
	}

	/// Get MatrixInfo by matrix id
	public static MatrixInfo Get(int id)
	{
		return getInternal(mat => mat.Id == id);
	}

	/// Get MatrixInfo by gameObject containing Matrix component
	public static MatrixInfo Get(GameObject go)
	{
		return getInternal(mat => mat.GameObject == go);
	}

	/// Get MatrixInfo by Matrix component
	public static MatrixInfo Get(Matrix matrix)
	{
		return getInternal(mat => mat.Matrix == matrix);
	}

	private static MatrixInfo getInternal(Func<MatrixInfo, bool> condition)
	{
		if ( Instance.activeMatrices.Count == 0 ) {
//			Debug.Log( "MatrixManager list not ready yet, trying to init" );
			Instance.InitMatrices();
		}
		for (var i = 0; i < Instance.activeMatrices.Count; i++)
		{
			if (condition(Instance.activeMatrices[i]))
			{
				return Instance.activeMatrices[i];
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
	public static Vector3Int LocalToWorldInt(Vector3 localPos, MatrixInfo matrix)
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 LocalToWorld(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorld(localPos, Get(matrix));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 LocalToWorld(Vector3 localPos, MatrixInfo matrix)
	{
		if (!matrix.MatrixMove)
		{
			return localPos + matrix.Offset;
		}

		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos = matrix.MatrixMove.ClientState.Orientation.Euler * unpivotedPos; //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted = rotatedPos + matrix.MatrixMove.Pivot + matrix.Offset; //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
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

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 WorldToLocal(Vector3 worldPos, MatrixInfo matrix)
	{
		if (!matrix.MatrixMove)
		{
			return worldPos - matrix.Offset;
		}

		Vector3 rotatedClean = worldPos - matrix.Offset - matrix.MatrixMove.Pivot;
		Vector3 unrotatedPos = matrix.MatrixMove.ClientState.Orientation.EulerInverted * rotatedClean;
		return unrotatedPos + matrix.MatrixMove.Pivot;
	}
}

/// Struct that helps identify matrices
public struct MatrixInfo
{
	public int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public GameObject GameObject;

	public Vector3Int InitialOffset;

	public Vector3Int Offset
	{
		get
		{
			if (!MatrixMove)
			{
				return InitialOffset;
			}

			return InitialOffset + (Vector3Int.RoundToInt(MatrixMove.ClientState.Position) - MatrixMove.InitialPos);
		}
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
			Debug.LogWarning($"Invalid NetID for matrix {matrix.gameObject.name}!");
		}

		return netId;
	}
}