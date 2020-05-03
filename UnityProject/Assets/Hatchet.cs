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

		//is the target item chopable?
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		Ingredient ingredient = new Ingredient(attr.ArticleName);
		GameObject cut = CraftingManager.Logs.FindRecipe(new List<Ingredient> { ingredient });
		if (cut)
		{
			Inventory.ServerDespawn(interaction.TargetSlot);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.Logs.FindOutputMeal(cut.name), 
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				
				//foreach (GameObject obj in spwn.GameObjects)
				//{
				//	Inventory.ServerAdd(obj,interaction.TargetSlot);
				//}

				Inventory.ServerAdd(spwn.GameObject ,interaction.TargetSlot);

			}

		} else {

			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't chop this.");
		}

	}
}