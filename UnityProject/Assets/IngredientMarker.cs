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
		CraftingManager.MergeInteraction(interaction, CraftingManager.SimpleMeal);
	}
}