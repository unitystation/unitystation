using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class PlaceItemAsObject  : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IInteractable<HandActivate>
{

	/// <summary>
	/// Script that makes an item spawn a single object when used.
	/// Used for pickupable and placeable objects such as body bags, chairs, and roller beds.
	/// </summary>


	public GameObject prefabVariant;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		// Can place the object on Tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject))
		{
			return false;
		}

		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAtAllMatrices(vector, vector, false))
		{
			return false;
		}

		return true;
	}

	//For pressing Z to place the object on the tile you're on.
	public void ServerPerformInteraction(HandActivate interaction)
	{
		// Spawns object at player's position.
		Spawn.ServerPrefab(prefabVariant, interaction.PerformerPlayerScript.WorldPos);

		// Remove the used item from the player's inventory
		Inventory.ServerDespawn(interaction.HandSlot);
	}

	//For clicking on a tile to place the object there.
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		// Spawn the object in the world
		Spawn.ServerPrefab(prefabVariant, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

		// Remove the used item from the player's inventory
		Inventory.ServerDespawn(interaction.HandSlot);
	}
}
