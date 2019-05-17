using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deconstruction : MonoBehaviour
{
	public GameObject wallGirderPrefab;
	public GameObject metalPrefab;

	//Server only:
	public void TryTileDeconstruct(TileChangeManager tileChangeManager, TileType tileType, Vector3 cellPos, Vector3 worldPos)
	{
		var cellPosInt = Vector3Int.RoundToInt(cellPos);
		switch (tileType)
		{
			case TileType.Wall:
				DoWallDeconstruction(cellPosInt, tileChangeManager, worldPos);
				tileChangeManager.gameObject.GetComponent<SubsystemManager>().UpdateAt(cellPosInt);
				break;
		}
	}

	//Server only
	public void ProcessDeconstructRequest(GameObject player, GameObject matrixRoot, TileType tileType,
		Vector3 cellPos, Vector3 worldCellPos)
	{
		if (player.Player().Script.IsInReach(worldCellPos, true) == false)
		{
			//Not in range on the server, do not process any further:
			return;
		}

		//Process Wall deconstruct request:
		if (tileType == TileType.Wall)
		{
			//Set up the action to be invoked when progress bar finishes:
			var progressFinishAction = new FinishProgressAction(
				finishReason =>
				{
					if (finishReason == FinishProgressAction.FinishReason.COMPLETED)
					{
						CraftingManager.Deconstruction.TryTileDeconstruct(
							matrixRoot.GetComponent<TileChangeManager>(), tileType, cellPos, worldCellPos);
					}
				}
			);

			//Start the progress bar:
			UIManager.ProgressBar.StartProgress(Vector3Int.RoundToInt(worldCellPos),
				10f, progressFinishAction, player, "Weld", 0.8f);

			SoundManager.PlayNetworkedAtPos("Weld", worldCellPos, Random.Range(0.9f, 1.1f));
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

		PoolManager.PoolNetworkInstantiate(metalPrefab, worldPos, tcm.transform);
		PoolManager.PoolNetworkInstantiate(wallGirderPrefab, worldPos, tcm.transform);
	}

}
