using System;
using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// Matrix manager keeps a list of matrices that you can access from both client and server.
/// Contains world/local position conversion methods, as well as several cross-matrix adaptations of Matrix methods.
/// Also very common use scenario is Get()'ting matrix info using matrixId from PlayerState
public class MatrixManager : MonoBehaviour {

	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
	private List<MatrixInfo> activeMatrices = new List<MatrixInfo>();

	/// List of active matrices
	public List<MatrixInfo> Matrices => activeMatrices;

	///Used for FoV ray hits and turning walls on and off:
	public Dictionary<Collider2D, Tilemap> wallTileMaps = new Dictionary<Collider2D, Tilemap>();

	/// Finds first matrix that is not empty at given world pos
	public MatrixInfo AtPoint( Vector3Int worldPos ) {
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			if ( !activeMatrices[i].Matrix.IsEmptyAt( WorldToLocalInt(worldPos, activeMatrices[i]) ) ) {
				return activeMatrices[i];
			}
		}
//		Debug.LogWarning( $"Did not find matrix at point {worldPos}!" );
//		return MatrixInfo.Invalid;
		//returning station's matrix for now
		return activeMatrices.Count == 0 ? MatrixInfo.Invalid : activeMatrices[0];
	}

	/// Cross-matrix floating check by world pos
	public bool IsFloatingAt( Vector3Int worldPos ) {
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			if ( !activeMatrices[i].Matrix.IsFloatingAt( WorldToLocalInt(worldPos, activeMatrices[i]) ) ) {
				return false;
			}
		}
		return true;
	}
	/// Cross-matrix passable check by world pos //FIXME: not truly cross-matrix. can walk diagonally between matrices
	public bool IsPassableAt( Vector3Int worldOrigin, Vector3Int worldTarget ) {
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			if ( !activeMatrices[i].Matrix.IsPassableAt( WorldToLocalInt( worldOrigin, activeMatrices[i] ), 
														 WorldToLocalInt( worldTarget, activeMatrices[i] ) ) ) {
				return false;
			}
		}
		return true;
	}
	/// Cross-matrix edition of GetFirst
	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			T first = activeMatrices[i].Matrix.GetFirst<T>( WorldToLocalInt( position, activeMatrices[i] ) );
			if ( first ) {
				return first;
			}
		}
		return null;
	}
	
	void Awake() {
		if(Instance == null){
			Instance = this;
		} else {
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
	private void InitMatrices() {
		Matrix[] findMatrices = FindObjectsOfType<Matrix>();
		if ( findMatrices.Length < 2 ) { 
//			Debug.Log( "Matrix init failure, will try in 0.5" );
			StartCoroutine(WaitForLoad());
			return;
		}
		for ( int i = 0; i < findMatrices.Length; i++ ) {	
			activeMatrices.Add( new MatrixInfo {
				Id = i,
				Matrix = findMatrices[i],
				GameObject = findMatrices[i].gameObject,
				MatrixMove = findMatrices[i].gameObject.GetComponentInParent<MatrixMove>(),
//				NetId is initialized later
				InitialOffset = findMatrices[i].InitialOffset
			} );
		}
		//These aren't fully initialized at that moment; init is truly finished when server is up and NetIDs are resolved
		Debug.Log( $"Semi-init {this}" );
	}

	public override string ToString() {
		return $"MatrixManager: {String.Join( ",\n", activeMatrices )}";
	}

	/// Get MatrixInfo by matrix id
	public MatrixInfo Get( int id ) {
		return getInternal(mat => mat.Id == id);
	}
	/// Get MatrixInfo by gameObject containing Matrix component
	public MatrixInfo Get( GameObject go ) {
		return getInternal(mat => mat.GameObject == go);
	}
	/// Get MatrixInfo by Matrix component
	public MatrixInfo Get( Matrix matrix ) {
		return getInternal(mat => mat.Matrix == matrix);
	}
	
	private MatrixInfo getInternal(Func<MatrixInfo, bool> condition)
	{
		for ( var i = 0; i < activeMatrices.Count; i++ )
		{
			if ( condition(activeMatrices[i]) )
			{
				return activeMatrices[i];
			}
		}
		return MatrixInfo.Invalid;
	}

	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3Int LocalToWorldInt( Vector3 localPos, Matrix matrix ) {
		return LocalToWorldInt( localPos, Get( matrix ) );
	}
	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3Int LocalToWorldInt( Vector3 localPos, MatrixInfo matrix ) {
		return Vector3Int.RoundToInt( LocalToWorld( localPos, matrix ) );
	}
	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 LocalToWorld( Vector3 localPos, Matrix matrix ) {
		return LocalToWorld( localPos, Get( matrix ) );
	}
	/// Convert local matrix coordinates to world position. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 LocalToWorld( Vector3 localPos, MatrixInfo matrix ) {
		if ( !matrix.MatrixMove ) {
			return localPos + matrix.Offset;
		}
		Vector3 unpivotedPos = localPos - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3 rotatedPos = matrix.MatrixMove.ClientState.Orientation.Euler * unpivotedPos;  //unpivotedPos rotated by N degrees
		Vector3 rotatedPivoted = rotatedPos + matrix.MatrixMove.Pivot + matrix.Offset; //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
	}

	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3Int WorldToLocalInt( Vector3 worldPos, Matrix matrix ) {
		return WorldToLocalInt( worldPos, Get( matrix ) );
	}
	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3Int WorldToLocalInt( Vector3 worldPos, MatrixInfo matrix ) {
		return Vector3Int.RoundToInt( WorldToLocal( worldPos, matrix ) );
	}
	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public Vector3 WorldToLocal( Vector3 worldPos, Matrix matrix ) {
		return WorldToLocal( worldPos, Get( matrix ) );
	}
	/// Convert world position to local matrix coordinates. Keeps offsets in mind (+ rotation and pivot if MatrixMove is present)
	public static Vector3 WorldToLocal( Vector3 worldPos, MatrixInfo matrix ) {
		if ( !matrix.MatrixMove ) {
			return worldPos - matrix.Offset;
		}
		Vector3 rotatedClean = worldPos - matrix.Offset - matrix.MatrixMove.Pivot;
		Vector3 unrotatedPos = matrix.MatrixMove.ClientState.Orientation.EulerInverted * rotatedClean;
		return unrotatedPos + matrix.MatrixMove.Pivot;
	}
}
/// Struct that helps identify matrices
public struct MatrixInfo {
	public int Id;
	public Matrix Matrix;
	public GameObject GameObject;

	public Vector3Int InitialOffset;
	public Vector3Int Offset {
		get {
			if ( !MatrixMove ) {
				return InitialOffset;
			}
			return InitialOffset + (Vector3Int.RoundToInt(MatrixMove.ClientState.Position) - MatrixMove.InitialPos);
		}
	}

	public MatrixMove MatrixMove;

	private NetworkInstanceId netId;

	public NetworkInstanceId NetId {
		get { //late init, because server is not yet up during InitMatrices()
			if ( netId.IsEmpty() || netId == NetworkInstanceId.Invalid ) {
				netId = getNetId( Matrix );
			}
			return netId;
		}
	}

	/// Null object
	public static readonly MatrixInfo Invalid = new MatrixInfo {Id = -1};

	public override string ToString() {
		return Equals(Invalid) ? "[Invalid matrix]" 
		: $"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetId}]";
	}
	///Figuring out netId. NetworkIdentity is located on the pivot (parent) gameObject for MatrixMove-equipped matrices 
	private static NetworkInstanceId getNetId( Matrix matrix ) {
		var netId = NetworkInstanceId.Invalid;
		if ( !matrix ) {
			return netId;
		}
		NetworkIdentity component = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		NetworkIdentity componentInParent = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		if ( component && component.netId != NetworkInstanceId.Invalid && !component.netId.IsEmpty() ) {
			netId = component.netId;
		}
		if ( componentInParent && componentInParent.netId != NetworkInstanceId.Invalid && !componentInParent.netId.IsEmpty() ) {
			netId = componentInParent.netId;
		}
		if ( netId == NetworkInstanceId.Invalid ) {
			Debug.LogWarning( $"Invalid NetID for matrix {matrix.gameObject.name}!" );
		}
		return netId;
	}
}
