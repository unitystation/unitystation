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

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(IsHand.OCCUPIED);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		ItemAttributes attr = interaction.UsedObject.GetComponent<ItemAttributes>();

		Ingredient ingredient = new Ingredient(attr.itemName);

		GameObject meal = CraftingManager.Meals.FindRecipe(new List<Ingredient> {ingredient});

		if (meal)
		{
			interaction.Performer.GetComponent<PlayerNetworkActions>().CmdStartMicrowave(interaction.HandSlot.SlotName, gameObject, meal.name);
			interaction.UsedObject.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		}
	}
}