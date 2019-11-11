using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class FoldedBodyBag  : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject prefabVariant;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
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

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		// Spawn the opened body bag in the world
		Spawn.ServerPrefab(prefabVariant, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

		// Remove the body bag from the player's inventory
		Inventory.ServerDespawn(interaction.HandSlot);
	}
}
