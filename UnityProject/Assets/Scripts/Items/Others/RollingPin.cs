using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an item as a rolling pin, letting it flatten items on the players other hand based on the recipe list in CraftingManager.Roll.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class RollingPin : MonoBehaviour, ICheckedInteractable<InventoryApply>
{
	//check if item is being applied to offhand with rollable object on it.
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
	
		//can the player act at all?
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//interaction only occurs if cutting target is on a hand slot.
		if (!interaction.IsToHandSlot) return false;

		//if the item isn't a butcher knife, no go.
		if (!Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.RollingPin)) return false;
		
		
		//TargetSlot must not be empty.
		if (interaction.TargetSlot.Item == null) return false;

		return true;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (!CraftingManager.InventoryApplyInteraction(interaction, CraftingManager.Roll))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't roll this out.");
		}
	}
}