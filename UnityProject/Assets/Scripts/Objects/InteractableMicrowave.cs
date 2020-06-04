using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Allows Microwave to be interacted with. Player can put food in the microwave to cook it.
/// The microwave can be interacted with to, for example, check the remaining time.
/// </summary>
[RequireComponent(typeof(Microwave))]
public class InteractableMicrowave : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private Microwave microwave;

	private void Start()
	{
		microwave = GetComponent<Microwave>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		return true;
	}

	/// <summary>
	/// Players can check the remaining microwave time or insert something into the microwave.
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (microwave.MicrowaveTimer > 0)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, $"{microwave.MicrowaveTimer:0} seconds until the {microwave.meal} is cooked.");
		}
		else if (interaction.HandObject != null)
		{
			// Check if the player is holding food that can be cooked
			ItemAttributesV2 attr = interaction.HandObject.GetComponent<ItemAttributesV2>();
			Ingredient ingredient = new Ingredient(attr.ArticleName);

			GameObject meal = CraftingManager.Meals.FindRecipe(new List<Ingredient> { ingredient });

			if (meal)
			{
				// HACK: Currently DOES NOT check how many items are used per meal
				// Blindly assumes each single item in a stack produces a meal

				//If food item is stackable, set output amount to equal input amount.
				Stackable stck = interaction.HandObject.GetComponent<Stackable>();
				if (stck != null)
				{
					microwave.ServerSetOutputStackAmount(stck.Amount);
				}
				else
				{
					microwave.ServerSetOutputStackAmount(1);
				}

				microwave.ServerSetOutputMeal(meal.name);
				Despawn.ServerSingle(interaction.HandObject);
				microwave.RpcStartCooking();
				microwave.MicrowaveTimer = microwave.COOK_TIME;
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You microwave the {microwave.meal} for {microwave.COOK_TIME} seconds.");
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Your {attr.ArticleName} can not be microwaved.");
				// Alternative suggestions:
				// "$"The microwave is not programmed to cook your {attr.ArticleName}."
				// "$"The microwave does not know how to cook your{attr.ArticleName}."
			}
		}
		else
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "The microwave is empty.");
		}
	}
}