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
			Chat.AddExamineMsgFromServer(interaction.Performer, $"{microwave.MicrowaveTimer:0} seconds until the {microwave.recipe.Output.name} is cooked.");
		}
		else if (interaction.HandObject != null)
		{
			ItemAttributesV2 attr = interaction.HandObject.GetComponent<ItemAttributesV2>();
			List<ItemAttributesV2> ingredient = new List<ItemAttributesV2>() { attr };
			Recipe recipe = CraftingManager.Meals.FindRecipeFromIngredients(ingredient);

			if (recipe != null)
			{
				int outputMeals = 0;

				// Currently lets you make as many as possible at the same time, instead of just one meal
				while (recipe.Consume(ingredient, out List<ItemAttributesV2> remains))
				{
					outputMeals++;

					if (remains.Count == 0)
					{
						break;
					}
				}

				microwave.ServerSetOutputMealAmount(outputMeals);
				microwave.ServerSetOutputMeal(recipe);
				microwave.RpcStartCooking();
				microwave.MicrowaveTimer = microwave.COOK_TIME;
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You microwave the {microwave.recipe.Output.name} for {microwave.COOK_TIME} seconds.");
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Your {attr.ArticleName} cannot be microwaved.");
			}
		}
		else
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "The microwave is empty.");
		}
	}
}