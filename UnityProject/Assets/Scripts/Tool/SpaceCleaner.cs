using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class SpaceCleaner : NetworkBehaviour, ICheckedInteractable<AimApply>
{
	public int travelDistance = 6;

	private float travelTime => 1f / travelDistance;

	[SerializeField]
	[Range(1,50)]
	private int reagentsPerUse = 5;

	private ReagentContainer reagentContainer;

	private void Awake()
	{
		reagentContainer = GetComponent<ReagentContainer>();
	}

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
		//just in case
		if (reagentContainer == null) return;

		if (reagentContainer.CurrentCapacity < reagentsPerUse)
		{
			return;
		}

		Vector2 startPos = gameObject.AssumedWorldPosServer();
		Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
		List<Vector3Int> positionList = CheckPassableTiles(startPos, targetPos);
		StartCoroutine(Fire(positionList));

		Effect.PlayParticleDirectional( this.gameObject, interaction.TargetVector );

		reagentContainer.TakeReagents(reagentsPerUse);
		SoundManager.PlayNetworkedAtPos("Spray2", startPos, 1);

		interaction.Performer.Pushable()?.NewtonianMove((-interaction.TargetVector).NormalizeToInt(), speed: 1f);
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
	private List<Vector3Int> CheckPassableTiles(Vector2 startPos, Vector2 targetPos)
	{
		List<Vector3Int> passableTiles = new List<Vector3Int>();
		List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
		for (int i = 0; i < positionList.Count; i++)
		{
			if (!MatrixManager.IsAtmosPassableAt(positionList[i], true))
			{
				return passableTiles;
			}
			passableTiles.Add(positionList[i]);
		}
		return passableTiles;
	}
}
