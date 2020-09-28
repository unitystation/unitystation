using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Chemistry.Components;

/// <summary>
/// Allows Microwave to be interacted with. Player can put food in the microwave to cook it.
/// The microwave can be interacted with to, for example, check the remaining time.
/// </summary>
[RequireComponent(typeof(AIOGrinder))]
public class InteractableGrinder : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private AIOGrinder grinder;
	private ReagentContainer grinderStorage;

	private void Start()
	{
		grinderStorage = GetComponent<ReagentContainer>();
		grinder = GetComponent<AIOGrinder>();
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
		if (interaction.HandObject != null)
		{
			// Check if the player is holding food that can be ground up
			ItemAttributesV2 attr = interaction.HandObject.GetComponent<ItemAttributesV2>();
			Ingredient ingredient = new Ingredient(attr.ArticleName);
			Chemistry.Reagent meal = CraftingManager.Grind.FindReagentRecipe(new List<Ingredient> { ingredient });
			int count = CraftingManager.Grind.FindReagentAmount(new List<Ingredient> { ingredient });
			if (meal)
			{
				grinder.SetServerStackAmount(count);
				grinder.ServerSetOutputMeal(meal.name);
				Despawn.ServerSingle(interaction.HandObject);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You grind the {attr.ArticleName}.");
				GetComponent<AIOGrinder>().GrindFood();
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Your {attr.ArticleName} can not be ground up.");
			}
		}
		else
		{
			if (!grinderStorage.IsEmpty)
			{
				if (grinderStorage.ReagentMixTotal == grinderStorage.AmountOfReagent(grinderStorage.MajorMixReagent))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"The grinder currently contains {grinderStorage.ReagentMixTotal} " +
		 $"of {grinderStorage.MajorMixReagent}.");
				}
				else if (grinderStorage.ReagentMixTotal != grinderStorage.AmountOfReagent(grinderStorage.MajorMixReagent))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"The grinder currently contains {grinderStorage.AmountOfReagent(grinderStorage.MajorMixReagent)} " +
		 $"of {grinderStorage.MajorMixReagent}, as well as " +
   $"{grinderStorage.ReagentMixTotal - grinderStorage.AmountOfReagent(grinderStorage.MajorMixReagent)} of various other things.");
				}
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The grinder is empty.");
			}
		}
	}
}