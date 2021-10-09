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

		// will a player start to craft something?
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (HasSimpleRelatedRecipe == false
			    || interaction.HandObject == null
			    || DefaultWillInteract.Default(interaction, side) == false
			)
			{
				return false;
			}

			// we should check related recipes in WillInteract() because otherwise
			// other interactions will be blocked because of an interaction cooldown

			List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();

			possibleIngredients.Add(this);

			if (interaction.HandObject.TryGetComponent(out CraftingIngredient otherPossibleIngredient))
			{
				possibleIngredients.Add(otherPossibleIngredient);
			}

			List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();

			if (TryGetComponent(out ItemAttributesV2 selfPossibleTool))
			{
				possibleTools.Add(selfPossibleTool);
			}

			if (interaction.HandObject.TryGetComponent(out ItemAttributesV2 otherPossibleTool))
			{
				possibleTools.Add(otherPossibleTool);
			}

			foreach (RelatedRecipe relatedRecipe in relatedRecipes)
			{
				if (relatedRecipe.Recipe.IsSimple == false)
				{
					continue;
				}
				if (side == NetworkSide.Client)
				{
					if (interaction.PerformerPlayerScript.PlayerCrafting.CanClientCraft(
						relatedRecipe.Recipe,
						possibleIngredients,
						possibleTools
						) == CraftingStatus.AllGood
					)
					{
						return true;
					}
				}
				else if (side == NetworkSide.Server)
				{
					if (interaction.PerformerPlayerScript.PlayerCrafting.CanCraft(
						relatedRecipe.Recipe,
						possibleIngredients,
						possibleTools,
						new List<ReagentContainer>()
						) == CraftingStatus.AllGood
					)
					{
						return true;
					}
				}
			}

			return false;
		}

		// tries to start a crafting action.
		// Sadly we have to check related recipes again because we can't pass any other args to this method
		public void ServerPerformInteraction(HandApply interaction)
		{
			List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();

			possibleIngredients.Add(this);

			if (interaction.HandObject.TryGetComponent(out CraftingIngredient otherPossibleIngredient))
			{
				possibleIngredients.Add(otherPossibleIngredient);
			}

			List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();

			if (TryGetComponent(out ItemAttributesV2 selfPossibleTool))
			{
				possibleTools.Add(selfPossibleTool);
			}

			if (interaction.HandObject.TryGetComponent(out ItemAttributesV2 otherPossibleTool))
			{
				possibleTools.Add(otherPossibleTool);
			}

			foreach (RelatedRecipe relatedRecipe in relatedRecipes)
			{
				if (relatedRecipe.Recipe.IsSimple == false)
				{
					continue;
				}

				if (
					interaction.PerformerPlayerScript.PlayerCrafting.TryToStartCrafting(
						relatedRecipe.Recipe,
						possibleIngredients,
						possibleTools,
						new List<ReagentContainer>(),
						CraftingActionParameters.QuietParameters
					)
				)
				{
					// alright, we're crafting now. There's no need to check other related recipes
					return;
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