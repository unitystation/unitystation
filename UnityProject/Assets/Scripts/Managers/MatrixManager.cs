using System;
using System.Collections.Generic;
using Tilemaps;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

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
			if ( !activeMatrices[i].Matrix.IsEmptyAt( worldPos - activeMatrices[i].Offset ) ) {
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
			if ( !activeMatrices[i].Matrix.IsFloatingAt( worldPos - activeMatrices[i].Offset ) ) {
				return false;
			}
		}
		return true;
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
		Debug.Log( $"Init matrices: {string.Join( ",\n", activeMatrices )}" );
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
	
	private MatrixInfo getInternal(Func<MatrixInfo,bool> condition)
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
		return Equals(Invalid) ? "[Invalid matrix]" : $"[({Id}){GameObject.name},offset={Offset},state={MatrixMove?.State},netId={NetId}]";
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
