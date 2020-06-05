using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an item as a hatchet, letting it chop up logs on the players other hand based on the recipe list in CraftingManager.Logs.
/// This component was based off of Knife.cs.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Hatchet : MonoBehaviour, ICheckedInteractable<InventoryApply>
{
	//check if item is being applied to offhand with chopable object on it.
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
	
		//can the player act at all?
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//interaction only occurs if chopping target is on a hand slot.
		if (!interaction.IsToHandSlot) return false;

		//if the item isn't an axe, no go.
		if (!Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Hatchet)) return false;
		
		
		//TargetSlot must not be empty.
		if (interaction.TargetSlot.Item == null) return false;

		return true;
	}
	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (!CraftingManager.InventoryApplyInteraction(interaction, CraftingManager.Logs))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't chop this.");
		}
	}
}