using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tilemaps;
using UnityEngine.Tilemaps;

/// <summary>
/// Matrix manager handles the netcomms of the position and movement of the matricies
/// </summary>
public class MatrixManager : MonoBehaviour {

	//Declare in awake as MatrixManager needs to be destroyed on each scene change
	public static MatrixManager Instance;
	public Dictionary<GameObject, Matrix> activeMatricies = new Dictionary<GameObject, Matrix>();

	//At the moment this used for FoV ray hits and turning walls on and off:
	public Dictionary<Collider2D, Tilemap> wallTileMaps = new Dictionary<Collider2D, Tilemap>();

	void Awake(){
		if(Instance == null){
			Instance = this;
		} else {
			Destroy(gameObject);
		}
		//find all matricies
		Matrix[] findMatricies = FindObjectsOfType<Matrix>();
		for (int i = 0; i < findMatricies.Length; i++){
			activeMatricies.Add(findMatricies[i].gameObject, findMatricies[i]);
		}
	}
}
