using System.Collections.Generic;
using Chemistry.Components;
using Items;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// 	This MonoBehaviour marks GameObject as a crafting ingredient
	/// 	and contains some fields associated with recipes.
	/// </summary>
	public class CraftingIngredient : MonoBehaviour, ICheckedInteractable<HandApply>
	{

		[SerializeField, ReadOnly] [Tooltip("Automated field - don't try to change it manually. " +
		                                    "Has the crafting ingredient simple recipe in its relatedRecipes list?")]
		private bool hasSimpleRelatedRecipe;

		[SerializeField, ReadOnly] [Tooltip("Automated field - don't try to change it manually. " +
		                                    "Recipes that have this item as an ingredient.")]
		private List<RelatedRecipe> relatedRecipes = new List<RelatedRecipe>();

		/// <summary>
		///     Recipes that have this item as an ingredient.
		/// </summary>
		public List<RelatedRecipe> RelatedRecipes => relatedRecipes;

		/// <summary>
		/// 	Has the crafting ingredient simple recipe in its relatedRecipes list?
		/// </summary>
		public bool HasSimpleRelatedRecipe => hasSimpleRelatedRecipe;

		// will a player make an attempt to craft something?
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return HasSimpleRelatedRecipe
			       && interaction.HandObject != null
			       && DefaultWillInteract.Default(interaction, side);
		}

		/// tries to craft something
		public void ServerPerformInteraction(HandApply interaction)
		{
			foreach (RelatedRecipe relatedRecipe in relatedRecipes)
			{
				if (relatedRecipe.Recipe.IsSimple == false)
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

				if (interaction.PerformerPlayerScript.PlayerCrafting.TryToStartCrafting(
					relatedRecipe.Recipe,
					possibleIngredients,
					possibleTools,
					new List<ReagentContainer>()
				))
				{
					// ok we're crafting now. There is no need to try to craft many recipes at one time.
					break;
				}

			}
		}

		/// <summary>
		/// 	Will set hasSimpleRelatedRecipe to true if a relatedRecipes list has at least one simple recipe.
		/// 	Will set hasSimpleRelatedRecipe to false otherwise.
		/// </summary>
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