using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//TODO: This should be refactored to use IF2 so validations can be used, we shouldn't need a custom net message for deconstruction
public class Deconstruction : MonoBehaviour
{
	public GameObject wallGirderPrefab;
	public GameObject metalPrefab;

	//Server only:
	public void TryTileDeconstruct(TileChangeManager tileChangeManager, TileType tileType, Vector3Int worldPos)
	{
		var cellPosInt = MatrixManager.WorldToLocalInt(worldPos, MatrixManager.AtPoint(worldPos, true));
		switch (tileType)
		{
			case TileType.Wall:
				DoWallDeconstruction(cellPosInt, tileChangeManager, worldPos);
				tileChangeManager.gameObject.GetComponent<SubsystemManager>().UpdateAt(cellPosInt);
				break;
		}
	}

	//Server only
	public void ProcessDeconstructRequest(GameObject player, GameObject matrixRoot, TileType tileType, Vector3Int worldCellPos)
	{

		//Process Wall deconstruct request:
		if (tileType == TileType.Wall)
		{
			//Set up the action to be invoked when progress bar finishes:
			var progressFinishAction = new ProgressCompleteAction(
				() =>
				{
					SoundManager.PlayNetworkedAtPos("Weld", worldCellPos, 0.8f);
					TryTileDeconstruct(
						matrixRoot.GetComponent<TileChangeManager>(), tileType, worldCellPos);
				}
			);

			//Start the progress bar:
			var bar = UIManager.ServerStartProgress(ProgressAction.Construction, worldCellPos,
				10f, progressFinishAction, player);
			if (bar != null)
			{
				SoundManager.PlayNetworkedAtPos("Weld", worldCellPos, Random.Range(0.9f, 1.1f));
			}
		}
	}

	//does the deconstruction when deconstruction progress finishes
	private void DoTileDeconstruction()
	{

	}

	private void DoWallDeconstruction(Vector3Int cellPos, TileChangeManager tcm, Vector3 worldPos)
	{
		tcm.RemoveTile(cellPos, LayerType.Walls);
		SoundManager.PlayNetworkedAtPos("Deconstruct", worldPos, 1f);

		Spawn.ServerPrefab(metalPrefab, worldPos);
		Spawn.ServerPrefab(wallGirderPrefab, worldPos);
	}

}
