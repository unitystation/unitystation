using System;
using System.Collections.Generic;
using Tilemaps;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using UnityEngine.XR.WSA.Persistence;

/// <summary>
/// Matrix manager handles the netcomms of the position and movement of the matrices
/// </summary>
public class MatrixManager : MonoBehaviour {

	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
	public List<MatrixInfo> activeMatrices = new List<MatrixInfo>();
	//Dictionary<GameObject, Matrix> activeMatrices = new Dictionary<GameObject, Matrix>();

	//At the moment this used for FoV ray hits and turning walls on and off:
	public Dictionary<Collider2D, Tilemap> wallTileMaps = new Dictionary<Collider2D, Tilemap>();

	/// Finds first matrix that is not empty at given world pos
	public MatrixInfo AtPoint( Vector3Int worldPos ) {
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			if ( !activeMatrices[i].Matrix.IsEmptyAt( WorldToLocal(worldPos, activeMatrices[i]) ) ) {
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
			if ( !activeMatrices[i].Matrix.IsFloatingAt( WorldToLocal(worldPos, activeMatrices[i]) ) ) {
				return false;
			}
		}
		return true;
	}
	/// Cross-matrix passable check by world pos
	public bool IsPassableAt( Vector3Int worldOrigin, Vector3Int worldPos ) {
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			if ( !activeMatrices[i].Matrix.IsPassableAt( WorldToLocal( worldOrigin, activeMatrices[i] ), 
														 WorldToLocal( worldPos, activeMatrices[i] ) ) ) {
				return false;
			}
		}
		return true;
	}
	/// poorly ported cross-matrix edition of GetFirst
	public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
	{
		for ( var i = 0; i < activeMatrices.Count; i++ ) {
			T first = activeMatrices[i].Matrix.GetFirst<T>( WorldToLocal( position, activeMatrices[i] ) );
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
		InitMatrices();
	}
	///Find all matrices
	private void InitMatrices() {
		Matrix[] findMatrices = FindObjectsOfType<Matrix>();
		for ( int i = 0; i < findMatrices.Length; i++ ) {	
			activeMatrices.Add( new MatrixInfo {
				Id = i,
				Matrix = findMatrices[i],
				GameObject = findMatrices[i].gameObject,
				MatrixMove = findMatrices[i].gameObject.GetComponentInParent<MatrixMove>(),
//				NetId =  getNetId(findMatrices[i]),
				InitialOffset = findMatrices[i].InitialOffset //Vector3Int.CeilToInt( findMatrices[i].gameObject.transform.position )
			} );
		}
		//These aren't fully initialized at that moment; init is truly finished when server is up and NetIDs are resolved
//		Debug.Log( $"Init matrices: {string.Join( ",\n", activeMatrices )}" );
	}

	public override string ToString() {
		return $"MatrixManager: {String.Join( ",\n", activeMatrices )}";
	}


	public MatrixInfo Get( int id ) {
		return getInternal(mat => mat.Id == id);
	}
	public MatrixInfo Get( GameObject go ) {
		return getInternal(mat => mat.GameObject == go);
	}
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

	public Vector3Int LocalToWorld( Vector3 localPos, Matrix matrix ) {
		return LocalToWorld( localPos, Get( matrix ) );
	} 
	public static Vector3Int LocalToWorld( Vector3 localPos, MatrixInfo matrix ) {
		if ( !matrix.MatrixMove ) {
			return Vector3Int.RoundToInt( localPos ) + matrix.Offset;
		}
		Vector3Int unpivotedPos = Vector3Int.RoundToInt( localPos ) - matrix.MatrixMove.Pivot; //localPos - localPivot
		Vector3Int rotatedPos = Vector3Int.RoundToInt(matrix.MatrixMove.ClientState.Orientation.Euler * unpivotedPos);  //unpivotedPos rotated by N degrees
		Vector3Int rotatedPivoted = rotatedPos + matrix.MatrixMove.Pivot + matrix.Offset; //adding back localPivot and applying localToWorldOffset
		return rotatedPivoted;
	}

	public Vector3Int WorldToLocal( Vector3Int worldPos, Matrix matrix ) {
		return WorldToLocal( worldPos, Get( matrix ) );
	} 
	public static Vector3Int WorldToLocal( Vector3Int worldPos, MatrixInfo matrix ) {
		if ( !matrix.MatrixMove ) {
			return worldPos - matrix.Offset;
		}
		Vector3Int rotatedClean = worldPos - matrix.Offset - matrix.MatrixMove.Pivot;
		Vector3Int unrotatedPos = Vector3Int.RoundToInt(matrix.MatrixMove.ClientState.Orientation.EulerInverted * rotatedClean);
		return unrotatedPos + matrix.MatrixMove.Pivot;
	}
}
/// Tiny struct that helps identify matrices
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
//			int degree = MatrixOrientation.DegreeBetween( MatrixOrientation.Up, MatrixMove.ClientState.Orientation );
//			Vector3Int point =  InitialOffset + Vector3Int.RoundToInt(  MatrixMove.ClientState.Position) - MatrixMove.InitialPos;
//			return Vector3Int.RoundToInt(Quaternion.Euler( 0, 0, degree ) * point);
			//TODO: figure out rotations
			return InitialOffset + (Vector3Int.RoundToInt(MatrixMove.ClientState.Position) - MatrixMove.InitialPos);
		}
	}

	public MatrixMove MatrixMove;

	private NetworkInstanceId netId;

	public NetworkInstanceId NetId {
		get {
			if ( netId.IsEmpty() || netId == NetworkInstanceId.Invalid ) {
				netId = getNetId( Matrix );
			}
			return netId;
		}
	}

	/// Null object
	public static readonly MatrixInfo Invalid = new MatrixInfo {Id = -1};

	public override string ToString() {
		return Equals(Invalid) ? "[Invalid matrix]" : $"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetId}]";
	}
	///Figuring out netId. NetworkIdentity is located on the pivot (parent) gameObject for MatrixMove-equipped matrices 
	private static NetworkInstanceId getNetId( Matrix matrix ) {
		NetworkIdentity component = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		NetworkIdentity componentInParent = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		var netId = NetworkInstanceId.Invalid;
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
