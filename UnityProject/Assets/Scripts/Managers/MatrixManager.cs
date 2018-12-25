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
	private List<MatrixInfo> activeMatrices = new List<MatrixInfo>();

	/// List of active matrices
	public List<MatrixInfo> Matrices => activeMatrices;

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
		MatrixInfo matrixInfo = getInternal(mat => !mat.Matrix.IsEmptyAt(WorldToLocalInt(worldPos, mat)));
		return Equals(matrixInfo, MatrixInfo.Invalid) ? Instance.activeMatrices[0] : matrixInfo;
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsFloatingAt( WorldToLocalInt( worldPos, mat ) ) );
	}
	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(UnityEngine.Vector3Int)"/>
	public static bool IsNonStickyAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsNonStickyAt( WorldToLocalInt( worldPos, mat ) ) );
	}
	///Cross-matrix edition of <see cref="Matrix.IsNoGravityAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsNoGravityAt(UnityEngine.Vector3Int)"/>
	public static bool IsNoGravityAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsNoGravityAt( WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject context, Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsFloatingAt( new []{context}, WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsFloatingAt(GameObject[],UnityEngine.Vector3Int)"/>
	public static bool IsFloatingAt(GameObject[] context, Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsFloatingAt( context, WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsSpaceAt"/>
	///<inheritdoc cref="Matrix.IsSpaceAt"/>
	public static bool IsSpaceAt(Vector3Int worldPos){
		return isAtInternal( mat => mat.Matrix.IsSpaceAt( WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsEmptyAt"/>
	///<inheritdoc cref="Matrix.IsEmptyAt"/>
	public static bool IsEmptyAt(Vector3Int worldPos) {
		return isAtInternal( mat => mat.Matrix.IsEmptyAt( WorldToLocalInt( worldPos, mat ) ) );
	}

	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int,UnityEngine.Vector3Int,bool,GameObject)"/>
	/// FIXME: not truly cross-matrix. can walk diagonally between matrices
	public static bool IsPassableAt(Vector3Int worldOrigin, Vector3Int worldTarget, bool includingPlayers = true, GameObject context = null) {
		return isAtInternal( mat => mat.Matrix.IsPassableAt( WorldToLocalInt( worldOrigin, mat ),
															 WorldToLocalInt( worldTarget, mat ), includingPlayers, context ) );
	}
	///Cross-matrix edition of <see cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	///<inheritdoc cref="Matrix.IsPassableAt(UnityEngine.Vector3Int)"/>
	public static bool IsPassableAt(Vector3Int worldTarget) {
		return isAtInternal( mat => mat.Matrix.IsPassableAt( WorldToLocalInt( worldTarget, mat ) ) );
	}

	/// <see cref="Matrix.Get{T}(UnityEngine.Vector3Int)"/>
	public static IEnumerable<T> GetAt<T>(Vector3Int worldPos) where T : MonoBehaviour
	{
		return getAtInternal(mat => mat.Matrix.Get<T>(WorldToLocalInt(worldPos, mat)));
	}

	private static IEnumerable<T> getAtInternal<T>( Func<MatrixInfo, IEnumerable<T>> condition ) where T : MonoBehaviour {
		IEnumerable<T> t = new List<T>();
		for (var i = 0; i < Instance.activeMatrices.Count; i++)
		{
			t = t.Concat(condition(Instance.activeMatrices[i]));
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

	/// <Summary>
	/// Cross-matrix edition of GetFirst
	/// Use a Vector3Int of the WorldPosition to use
	/// </Summary>
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
		List<Matrix> findMatrices = FindObjectsOfType<Matrix>().ToList();
		if ( spaceMatrix ) {
			findMatrices.Remove( spaceMatrix );
			findMatrices.Insert( 0, spaceMatrix );
		}
		if (findMatrices.Count < matrixCount)
		{
//			Logger.Log( "Matrix init failure, will try in 0.5" );
			StartCoroutine(WaitForLoad());
			return;
		}

		for (int i = 0; i < findMatrices.Count; i++)
		{
			MatrixInfo matrixInfo = new MatrixInfo
			{
				Id = i,
				Matrix = findMatrices[i],
				GameObject = findMatrices[i].gameObject,
				Objects = findMatrices[i].gameObject.transform.GetComponentInChildren<ObjectLayer>().transform,
				MatrixMove = findMatrices[i].gameObject.GetComponentInParent<MatrixMove>(),
				MetaTileMap = findMatrices[i].gameObject.GetComponent<MetaTileMap>(),
//				NetId is initialized later
				InitialOffset = findMatrices[i].InitialOffset
			};
			if (!activeMatrices.Contains(matrixInfo))
			{
				activeMatrices.Add(matrixInfo);
			}
		}

		//These aren't fully initialized at that moment; init is truly finished when server is up and NetIDs are resolved
//		Logger.Log($"Semi-init {this}");
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
		if (Instance.activeMatrices.Count == 0)
		{
			//			Logger.Log( "MatrixManager list not ready yet, trying to init" );
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
	public static Vector3Int LocalToWorldInt(Vector3 localPos, MatrixInfo matrix , MatrixState state = default( MatrixState ))
	{
		return Vector3Int.RoundToInt(LocalToWorld(localPos, matrix, state));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 LocalToWorld(Vector3 localPos, Matrix matrix)
	{
		return LocalToWorld(localPos, Get(matrix));
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 LocalToWorld(Vector3 localPos, MatrixInfo matrix, MatrixState state = default( MatrixState ))
	{
		//Invalid matrix info provided
		if ( matrix.Equals( MatrixInfo.Invalid) ) {
			return TransformState.HiddenPos;
		}
		if (!matrix.MatrixMove)
		{
			return localPos + matrix.Offset;
		}

		if ( state.Equals( default(MatrixState) ) ) {
			state = matrix.MatrixMove.ClientState;
		}

		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos = state.Orientation.Euler * unpivotedPos; //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted = rotatedPos + matrix.MatrixMove.Pivot + matrix.GetOffset( state ); //adding back localPivot and applying localToWorldOffset
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
		//Invalid matrix info provided
		if (matrix.Equals(MatrixInfo.Invalid))
		{
			return TransformState.HiddenPos;
		}
		if (!matrix.MatrixMove)
		{
			return worldPos - matrix.Offset;
		}

		return matrix.MatrixMove.ClientState.Orientation.EulerInverted * (worldPos - matrix.Offset - matrix.MatrixMove.Pivot) + matrix.MatrixMove.Pivot;
	}
}

/// Struct that helps identify matrices
public struct MatrixInfo
{
	public int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public GameObject GameObject;
	/// <summary>
	/// Transform containing all the physical objects on the map
	public Transform Objects;

	public Vector3Int InitialOffset;

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
		return InitialOffset + (Vector3Int.RoundToInt(state.Position) - MatrixMove.InitialPos);
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
	public static readonly MatrixInfo Invalid = new MatrixInfo { Id = -1 };

	public override string ToString()
	{
		return Equals(Invalid) ?
			"[Invalid matrix]" :
			$"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetId}]";
	}

	public bool Equals( MatrixInfo other ) {
		return Id == other.Id;
	}

	public override bool Equals( object obj ) {
		if ( ReferenceEquals( null, obj ) ) {
			return false;
		}

		return obj is MatrixInfo && Equals( ( MatrixInfo ) obj );
	}

	public override int GetHashCode() {
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