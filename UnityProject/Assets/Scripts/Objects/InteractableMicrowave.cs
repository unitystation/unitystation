using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Allows Microwave to be interacted with. Can put something in it and it will start cooking.
/// </summary>
[RequireComponent(typeof(Microwave))]
public class InteractableMicrowave : Interactable<HandApply>
{
	private Microwave microwave;

	private void Start()
	{
		microwave = GetComponent<Microwave>();
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject == null) return false;
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		ItemAttributes attr = interaction.HandObject.GetComponent<ItemAttributes>();

		Ingredient ingredient = new Ingredient(attr.itemName);

		GameObject meal = CraftingManager.Meals.FindRecipe(new List<Ingredient> {ingredient});

		if (meal)
		{
			interaction.Performer.GetComponent<PlayerNetworkActions>().CmdStartMicrowave(interaction.HandSlot.equipSlot, gameObject, meal.name);
			interaction.HandObject.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		}
	}
}