using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Allows Microwave to be interacted with. Can put something in it and it will start cooking.
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
		if (interaction.HandObject == null) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		IItemAttributes attr = interaction.HandObject.GetComponent<IItemAttributes>();

		Ingredient ingredient = new Ingredient(attr.ItemName);

		GameObject meal = CraftingManager.Meals.FindRecipe(new List<Ingredient> {ingredient});

		if (meal)
		{
			microwave.ServerSetOutputMeal(meal.name);
			Despawn.ServerSingle(interaction.HandObject);
			microwave.RpcStartCooking();
		}
	}
}