using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deconstruction : MonoBehaviour
{
	public GameObject wallGirderPrefab;

	//Server only:
	public void TryTileDeconstruct(TileChangeManager tileChangeManager, TileType tileType, Vector3 cellPos)
	{
		switch (tileType)
		{
			case TileType.Wall:
				DoWallDeconstruction(Vector3Int.RoundToInt(cellPos), tileChangeManager);
				break;
		}
	}

	private void DoWallDeconstruction(Vector3Int cellPos, TileChangeManager tcm)
	{
		tcm.RemoveTile(cellPos, TileChangeLayer.Wall);
		//TODO do sfx and spawn girders
	}

}