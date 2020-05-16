using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an item as a knife, letting it cut up items on the players other hand based on the recipe list in CraftingManager.Cuts.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Knife : MonoBehaviour, ICheckedInteractable<InventoryApply>
{
	//check if item is being applied to offhand with cuttable object on it.
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{

		//can the player act at all?
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//interaction only occurs if cutting target is on a hand slot.
		if (!interaction.IsToHandSlot) return false;

		//if the item isn't a butcher knife, no go.
		if (!Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Knife)) return false;


		//TargetSlot must not be empty.
		if (interaction.TargetSlot.Item == null) return false;

		return true;
	}
	public void ServerPerformInteraction(InventoryApply interaction)
	{
		//is the target item cuttable?
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		Ingredient ingredient = new Ingredient(attr.ArticleName);
		GameObject cut = CraftingManager.Cuts.FindRecipe(new List<Ingredient> { ingredient });
		if (cut && interaction.TargetObject.GetComponent<Stackable>() != null && ingredient.requiredAmount
			== interaction.TargetObject.GetComponent<Stackable>().Amount)
		{
			Inventory.ServerDespawn(interaction.TargetSlot);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.Cuts.FindOutputMeal(cut.name),
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				Inventory.ServerAdd(spwn.GameObject, interaction.TargetSlot);
			}
		}
		else if (cut)
		{
			Inventory.ServerDespawn(interaction.TargetSlot);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.Cuts.FindOutputMeal(cut.name),
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				Inventory.ServerAdd(spwn.GameObject, interaction.TargetSlot);
			}
		}
		else
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't cut this.");
		}
	}
}