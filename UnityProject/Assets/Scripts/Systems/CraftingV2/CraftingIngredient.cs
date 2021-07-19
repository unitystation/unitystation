using System.Collections.Generic;
using Items;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	public class CraftingIngredient : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField, ReadOnly] private bool hasSimpleRelatedRecipe;

		[SerializeField, ReadOnly] [Tooltip("Recipes that have this item as an ingredient.")]
		private List<RelatedRecipe> relatedRecipes = new List<RelatedRecipe>();

		/// <summary>
		///     Recipes that have this item as an ingredient.
		/// </summary>
		public List<RelatedRecipe> RelatedRecipes => relatedRecipes;

		public bool HasSimpleRelatedRecipe => hasSimpleRelatedRecipe;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return hasSimpleRelatedRecipe && DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			foreach (RelatedRecipe relatedRecipe in relatedRecipes)
			{
				if (!relatedRecipe.Recipe.IsSimple)
				{
					continue;
				}
				List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();
				List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();

				possibleIngredients.Add(this);
				if (interaction.HandObject.TryGetComponent(out ItemAttributesV2 possibleTool))
				{
					possibleTools.Add(possibleTool);
				}

				if (interaction.HandObject.TryGetComponent(out CraftingIngredient possibleIngredient))
				{
					possibleIngredients.Add(possibleIngredient);
				}
				interaction.PerformerPlayerScript.PlayerCrafting.TryToStartCrafting(
					relatedRecipe.Recipe,
					possibleIngredients,
					possibleTools
				);
			}
		}

		public void UpdateHasSimpleRelatedRecipe()
		{
			foreach (RelatedRecipe relatedRecipe in relatedRecipes)
			{
				if (relatedRecipe.Recipe.IsSimple)
				{
					hasSimpleRelatedRecipe = true;
					return;
				}
			}

			hasSimpleRelatedRecipe = false;
		}
	}
}