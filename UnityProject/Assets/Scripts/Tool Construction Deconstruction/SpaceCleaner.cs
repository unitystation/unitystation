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

	[SerializeField]
	[Range(1,50)]
	private int reagentsPerUse = 5;

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
		if (!(reagentContainer.CurrentCapacity >= reagentsPerUse))
		{
			return;
		}

		Vector2 startPos = gameObject.AssumedWorldPosServer();
		Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
		List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
		StartCoroutine(Fire(positionList));

		Effect.PlayParticleDirectional( this.gameObject, interaction.TargetVector );

		reagentContainer.TakeReagents(reagentsPerUse);
		SoundManager.PlayNetworkedAtPos("Spray2", startPos, 1);

		interaction.Performer.Pushable()?.NewtonianMove((-interaction.TargetVector).NormalizeToInt());
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
		//it actually uses remaining contents of the bottle to react with world
		//instead of the sprayed ones. not sure if this is right
		MatrixManager.ReagentReact(reagentContainer.Contents, worldPos);
	}

}
