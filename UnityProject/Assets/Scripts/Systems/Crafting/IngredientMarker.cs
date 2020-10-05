using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an item as an ingredient, allowing it to be combined with other ingredients as marked in CraftingManager.Meals.
/// Note: This only applies to two-ingredient recipes.
/// This component was based off of Knife.cs.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class IngredientMarker : MonoBehaviour, ICheckedInteractable<InventoryApply>
{
	//check if item is being applied to offhand with chopable object on it.
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
	
		//can the player act at all?
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//make sure both items are ingredients!
		if (!Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Ingredient)) return false;
		if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Ingredient)) return false;

		//make sure at least the target is in a hand slot
		if (!interaction.IsToHandSlot) return false;

		//TargetSlot must not be empty.
		if (interaction.TargetSlot.Item == null) return false;

		return true;
	}
	public void ServerPerformInteraction(InventoryApply interaction)
	{
		//is the target item another ingredient?
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		ItemAttributesV2 selfattr = interaction.UsedObject.GetComponent<ItemAttributesV2>();
		Ingredient ingredient = new Ingredient(attr.ArticleName);
		Ingredient self = new Ingredient(selfattr.ArticleName);
		GameObject cut = CraftingManager.SimpleMeal.FindRecipe(new List<Ingredient> { ingredient, self });
		GameObject cut2 = CraftingManager.SimpleMeal.FindRecipe(new List<Ingredient> { self, ingredient });
		if (cut)
		{
			Inventory.ServerDespawn(interaction.TargetObject);
			Inventory.ServerDespawn(interaction.UsedObject);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.SimpleMeal.FindOutputMeal(cut.name),
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				Inventory.ServerAdd(spwn.GameObject, interaction.TargetSlot);
			}
		}
		else if (cut2)
		{
			Inventory.ServerDespawn(interaction.TargetSlot);
			Inventory.ServerDespawn(interaction.Performer);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.SimpleMeal.FindOutputMeal(cut2.name),
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				Inventory.ServerAdd(spwn.GameObject, interaction.TargetSlot);
			}

		}
	}
}