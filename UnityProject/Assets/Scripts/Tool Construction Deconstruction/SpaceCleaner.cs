using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class SpaceCleaner : NetworkBehaviour, ICheckedInteractable<AimApply>
{
	public int travelDistance = 6;
	public ReagentContainer reagentContainer;
	private float travelTime => 1f / travelDistance;

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (interaction.MouseButtonState == MouseButtonState.PRESS)
		{
			return true;
		}
		return false;
	}

	public void ServerPerformInteraction(AimApply interaction)
	{
		if (reagentContainer.CurrentCapacity >= 5)
		{
			Vector2 startPos = gameObject.AssumedWorldPosServer();
			Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			StartCoroutine(Fire(positionList));

			Effect.PlayParticleDirectional( this.gameObject, interaction.TargetVector );

			reagentContainer.MoveReagentsTo(5);
			SoundManager.PlayNetworkedAtPos("Spray2", startPos, 1);
		}
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			SprayTile(positionList[i]);
			yield return WaitFor.Seconds(travelTime);
		}
	}

	void SprayTile(Vector3Int worldPos)
	{
		var matrix = MatrixManager.AtPoint(worldPos, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPos, matrix);
		matrix.MetaDataLayer.ReagentReact(reagentContainer.Contents, worldPos, localPosInt);
	}

}
