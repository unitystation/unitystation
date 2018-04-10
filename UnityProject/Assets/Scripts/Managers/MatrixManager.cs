using System;
using System.Collections.Generic;
using Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Matrix manager handles the netcomms of the position and movement of the matricies
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
		Debug.LogWarning( $"Did not find matrix at point {worldPos}!" );
		return MatrixInfo.Invalid;
	}

	void Awake(){
		if(Instance == null){
			Instance = this;
		} else {
			Destroy(gameObject);
		}
		//find all matrices
		Matrix[] findMatrices = FindObjectsOfType<Matrix>();
		for ( int i = 0; i < findMatrices.Length; i++ ) {
			activeMatrices.Add( new MatrixInfo {
				Id = i,
				Matrix = findMatrices[i],
				GameObject = findMatrices[i].gameObject,
				Offset = findMatrices[i].Offset//Vector3Int.CeilToInt( findMatrices[i].gameObject.transform.position )
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
	///?
	public Vector3Int Offset;
	/// Null object
	public static readonly MatrixInfo Invalid = new MatrixInfo {Id = -1};

	public override string ToString() {
		return Equals(Invalid) ? "[Invalid matrix]" : $"[({Id}){GameObject.name},offset={Offset}]";
	}
}
