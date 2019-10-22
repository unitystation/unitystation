using UnityEngine;

public class RackParts : Interactable<PositionalHandApply, InventoryApply>
{

	public GameObject rackPrefab;
	private bool isBuilding;

	protected override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
		{
			return false;
		}

		if (Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			return true;
		}

		// Must be constructing the rack somewhere empty
		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAt(vector, vector, false))
		{
			return false;
		}

		return true;
	}

	protected override bool WillInteractT2(InventoryApply interaction, NetworkSide side)
	{
		if (!base.WillInteractT2(interaction, side))
		{
			return false;
		}

		if (interaction.TargetObject != gameObject
		    || !Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			return false;
		}

		return true;
	}

	protected override void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, 1f);
			ObjectFactory.SpawnMetal(1, interaction.WorldPositionTarget.To2Int(), parent: transform.parent);
			PoolManager.PoolNetworkDestroy(gameObject);

			return;
		}

		if (isBuilding)
		{
			return;
		}

		Chat.AddExamineMsgFromServer(interaction.Performer,
			"You start constructing a rack...");

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
				{
					isBuilding = false;
				}
				else if (reason == FinishProgressAction.FinishReason.COMPLETED)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You assemble a rack.");

					PoolManager.PoolNetworkInstantiate(rackPrefab, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

					var handObj = interaction.HandObject;
					var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
					handObj.GetComponent<Pickupable>().DisappearObject(slot);

					isBuilding = false;
				}
			}
		);
		isBuilding = true;

		UIManager.ProgressBar.StartProgress(interaction.WorldPositionTarget.RoundToInt(),
			5f, progressFinishAction, interaction.Performer);
	}

	protected override void ServerPerformInteraction(InventoryApply interaction)
	{
		SoundManager.PlayNetworkedAtPos("Wrench", interaction.Performer.WorldPosServer(), 1f);
		ObjectFactory.SpawnMetal(1, interaction.Performer.WorldPosServer().To2Int(), parent: transform.parent);

		var rack = interaction.TargetObject;
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.TargetSlot.equipSlot);
		rack.GetComponent<Pickupable>().DisappearObject(slot);
	}
}
