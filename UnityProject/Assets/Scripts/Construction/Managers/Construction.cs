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
		//TODO we need to get the tile that was removed from MetaData and add its name to the unifloortile script
	}
}