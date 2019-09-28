using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class FoldedBodyBag  : Interactable<PositionalHandApply>
{
	public GameObject prefabVariant;

	protected override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
		{
			return false;
		}

		// Can place the body bag on Tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject))
		{
			return false;
		}

		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAt(vector, vector, false))
		{
			return false;
		}

		return true;
	}

	protected override void ServerPerformInteraction(PositionalHandApply interaction)
	{
		// Place the opened body bag in the world
		PoolManager.PoolNetworkInstantiate(prefabVariant, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

		// Remove the body bag from the player's inventory
		var handObj = interaction.HandObject;
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		handObj.GetComponent<Pickupable>().DisappearObject(slot);
	}
}
