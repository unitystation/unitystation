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

	//Server only
	public void ProcessDeconstructRequest(GameObject player, GameObject matrixRoot, TileType tileType,
		Vector3 cellPos, Vector3 worldCellPos)
	{
		if (Vector3.Distance(player.transform.position, worldCellPos) > 1.5f)
		{
			//Not in range on the server, do not process any further:
			return;
		}
		//Process Wall deconstruct request:
		if (tileType == TileType.Wall)
		{
			//Set up the action to be invoked when progress bar finishes:
			var progressFinishAction = new FinishProgressAction(
				FinishProgressAction.Action.TileDeconstruction,
				matrixRoot.GetComponent<TileChangeManager>(),
				tileType,
				cellPos,
				player
			);

			//Start the progress bar:
			UIManager.ProgressBar.StartProgress(Vector3Int.RoundToInt(worldCellPos),
				10f, progressFinishAction, player);
		}
	}

	private void DoWallDeconstruction(Vector3Int cellPos, TileChangeManager tcm)
	{
		tcm.RemoveTile(cellPos, TileChangeLayer.Wall);
		//TODO do sfx and spawn girders
	}

}