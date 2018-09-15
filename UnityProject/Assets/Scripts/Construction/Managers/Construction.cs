using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Construction : MonoBehaviour
{
	//Prefab fields:
	public GameObject uniFloorTilePrefab;

	//This only works serverside:
	public void SpawnFloorTile(Vector3 pos, Transform parent) //TODO: Floor Tile Type!
	{
		var floorTile = PoolManager.Instance.PoolNetworkInstantiate(uniFloorTilePrefab, pos, Quaternion.identity, parent);
		//TODO SET UP THE FLOOR TILE HERE 
	}
}