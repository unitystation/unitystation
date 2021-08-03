using System;
using System.Collections.Generic;
using Systems.CraftingV2;
using Systems.CraftingV2.ClientServerLogic;
using Items;
using NaughtyAttributes;
using UnityEngine;

namespace Player
{
	/// <summary>
	/// A class that deals with the processing of crafting items by a player.
	/// </summary>
	[RequireComponent(typeof(PlayerScript))]
	public class PlayerCrafting : MonoBehaviour
	{
		/// <summary>
		/// The list of currently known recipes for a player by category.
		/// </summary>
		private List<List<CraftingRecipe>> knownRecipesByCategory = new List<List<CraftingRecipe>>();

		public List<List<CraftingRecipe>> KnownRecipesByCategory => knownRecipesByCategory;

		[SerializeField, ReorderableList] [Tooltip("Default recipes known to a player.")]
		private List<CraftingRecipe> defaultKnownRecipes = new List<CraftingRecipe>();

		private PlayerScript playerScript;

		public PlayerScript PlayerScript => playerScript;

		private StandardProgressActionConfig craftProgressActionConfig = new StandardProgressActionConfig(
			StandardProgressActionType.Craft
		);

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			InitKnownRecipesByCategories();
			InitDefaultRecipes();
		}

		private void InitKnownRecipesByCategories()
		{
			for (int i = Enum.GetValues(typeof(RecipeCategory)).Length - 1; i >= 0; i--)
			{
				knownRecipesByCategory.Add(new List<CraftingRecipe>());
			}
		}

		private void InitDefaultRecipes()
		{
			foreach (CraftingRecipe craftingRecipe in defaultKnownRecipes)
			{
				TryAddRecipeToKnownRecipes(craftingRecipe);
			}
		}

		/// <summary>
		/// 	Gets all the known recipes in the specified recipe category.
		/// </summary>
		/// <param name="recipeCategory">The recipe category.</param>
		/// <returns>All the known recipes in the specified recipe category.</returns>
		public List<CraftingRecipe> GetKnownRecipesInCategory(RecipeCategory recipeCategory)
		{
			return knownRecipesByCategory[(int) recipeCategory];
		}

		/// <summary>
		/// 	Checks if a player already knows the recipe.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True, if a player already knows the recipe, false otherwise.</returns>
		public bool KnowRecipe(CraftingRecipe recipe)
		{
			return GetKnownRecipesInCategory(recipe.Category).Contains(recipe);
		}

		/// <summary>
		/// 	Adds the recipe to the KnownRecipesByCategory[] if it doesn't already contains the recipe.
		/// </summary>
		/// <param name="recipe">The recipe to add.</param>
		public void LearnRecipe(CraftingRecipe recipe)
		{
			if (!TryAddRecipeToKnownRecipes(recipe))
			{
				return;
			}

			SendLearnedCraftingRecipe.SendTo(playerScript.connectedPlayer, recipe);
		}

		/// <summary>
		/// 	Tries to add the recipe to the known recipes list.
		/// </summary>
		/// <param name="craftingRecipe">The recipe to add to the known recipes list.</param>
		/// <returns>True if the recipe was successfully added to the known recipes list, false otherwise</returns>
		public bool TryAddRecipeToKnownRecipes(CraftingRecipe craftingRecipe)
		{
			if (KnowRecipe(craftingRecipe))
			{
				return false;
			}

			UnsafelyAddRecipeToKnownRecipes(craftingRecipe);

			return true;
		}

		/// <summary>
		/// 	Adds the recipe to the known recipes list.
		/// 	This method is unsafe - we may have duplicates!
		/// </summary>
		/// <param name="craftingRecipe">The recipe to add to the known recipes list.</param>
		public void UnsafelyAddRecipeToKnownRecipes(CraftingRecipe craftingRecipe)
		{
			GetKnownRecipesInCategory(craftingRecipe.Category).Add(craftingRecipe);
		}

		/// <summary>
		/// 	Removes the recipe from the KnownRecipesByCategory list.
		/// </summary>
		/// <param name="recipe">The recipe to remove.</param>
		public void ForgetRecipe(CraftingRecipe recipe)
		{
			GetKnownRecipesInCategory(recipe.Category).Remove(recipe);
			SendForgottenCraftingRecipe.SendTo(playerScript.connectedPlayer, recipe);
		}

		/// <summary>
		/// 	Checks if the player able to craft according to the recipe(ignoring its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool IsPlayerAbleToCraft(CraftingRecipe recipe)
		{
			return !PlayerScript.playerMove.IsCuffed
			       && !PlayerScript.playerMove.IsTrapped
			       && !PlayerScript.IsGhost
			       && !PlayerScript.playerHealth.IsCrit
			       && !PlayerScript.playerHealth.IsDead
			       && GetKnownRecipesInCategory(recipe.Category).Contains(recipe);
		}

		/// <summary>
		/// Checks if a player able to craft according to the recipe(including its ingredients, tools, etc).
		/// Will get the possible ingredients from a tile that a player is directed to.
		/// Will get the possible tools from a player's hands.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool CanCraft(CraftingRecipe recipe)
		{
			return CanCraft(recipe, GetPossibleIngredients(), GetPossibleTools());
		}

		/// <summary>
		/// 	Checks if a player able to craft the recipe(including its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or/and reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool CanCraft(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return IsPlayerAbleToCraft(recipe) && recipe.CanBeCrafted(possibleIngredients, possibleTools);
		}

		/// <summary>
		/// 	Gets all reachable items from a tile that a player is directed to.
		/// </summary>
		/// <returns>All reachable items from a tile that a player is directed to.</returns>
		public List<CraftingIngredient> GetPossibleIngredients()
		{
			Vector3Int ingredientsSourceVector = playerScript.WorldPos;
			List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();
			// it's unlikely that it will be null, but we are not immune from this case
			if (playerScript.playerDirectional != null)
			{
				ingredientsSourceVector = PlayerScript.WorldPos
				                          + (Vector3Int) playerScript.playerDirectional.CurrentDirection.VectorInt;
			}

			// no one knows how to craft through walls yet, so let's ignore the things behind the wall or something else
			if (Validations.IsReachableByPositions(
				playerScript.WorldPos,
				ingredientsSourceVector,
				true,
				context: playerScript.gameObject
			))
			{
				possibleIngredients = MatrixManager.GetAt<CraftingIngredient>(ingredientsSourceVector, true);
			}

			return possibleIngredients;
		}

		/// <summary>
		/// 	Gets all tools that a player holds in his hands.
		/// </summary>
		/// <returns>All possible tools that may be used for crafting.</returns>
		public List<ItemAttributesV2> GetPossibleTools()
		{
			List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();
			foreach (ItemSlot handSlot in playerScript.DynamicItemStorage.GetHandSlots())
			{
				if (handSlot.ItemObject != null)
				{
					possibleTools.Add(handSlot.ItemObject.GetComponent<ItemAttributesV2>());
				}
			}

			return possibleTools;
		}

		/// <summary>
		/// 	Tries to start a crafting action.
		/// 	May use all reachable items from a tile that a player is directed to.
		/// 	May use all tools that a player holds in his hands.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <returns>True if a crafting action has started, false otherwise.</returns>
		public bool TryToStartCrafting(CraftingRecipe recipe)
		{
			return TryToStartCrafting(recipe, GetPossibleIngredients(), GetPossibleTools());
		}

		/// <summary>
		///		Tries to start a crafting action.
		/// </summary>
		/// <param name="recipe">The recipe that the player is trying to craft to.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <returns>True if a crafting action has started, false otherwise.</returns>
		public bool TryToStartCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			if (!CanCraft(recipe, possibleIngredients, possibleTools))
			{
				return false;
			}

			StartCrafting(recipe, possibleIngredients, possibleTools);
			return true;
		}

		/// <summary>
		/// 	Unsafely starts a new crafting action even if the recipe's requirements were not fulfilled.
		/// </summary>
		/// <param name="recipe">The recipe that the player is trying to craft to.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		private void StartCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			if (recipe.CraftingTime >= 1) {
				Chat.AddExamineMsgFromServer(
					playerScript.gameObject,
					$"You are trying to craft {recipe.RecipeName}..."
				);
			}

			if (recipe.CraftingTime <= float.Epsilon)
			{
				FinishCrafting(recipe, possibleIngredients, possibleTools);
				return;
			}
			StandardProgressAction.Create(
				craftProgressActionConfig,
				() => TryToFinishCrafting(recipe)
			).ServerStartProgress(playerScript.registerTile, recipe.CraftingTime, playerScript.gameObject);
		}

		/// <summary>
		/// 	Tries to finish a crafting action.
		/// 	May use all reachable items from a tile that a player is directed to.
		/// 	May use all tools that a player holds in his hands.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <returns>True if we can spawn the recipe's result, false otherwise.</returns>
		public bool TryToFinishCrafting(CraftingRecipe recipe)
		{
			return TryToFinishCrafting(recipe, GetPossibleIngredients(), GetPossibleTools());
		}

		/// <summary>
		/// 	Tries to finish a crafting action.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <returns>True if we can spawn the recipe's result, false otherwise.</returns>
		public bool TryToFinishCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			if (!CanCraft(recipe, possibleIngredients, possibleTools))
			{
				Chat.AddExamineMsgFromServer(
					playerScript.gameObject,
					"Wait, where's my... Oh, forget it, I can't craft it anymore."
				);
				return false;
			}

			FinishCrafting(recipe, possibleIngredients, possibleTools);
			return true;
		}

		/// <summary>
		/// 	Unsafely finishes a crafting action even if the recipe's requirements were not fulfilled.
		/// </summary>
		/// <param name="recipe">The recipe that was used to craft.</param>
		/// <param name="possibleIngredients">The ingredients(or reagent containers) that may be used for crafting.</param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		private void FinishCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			Chat.AddExamineMsgFromServer(
				playerScript.gameObject,
				$"You made the {recipe.RecipeName}!"
			);

			recipe.UnsafelyCraft(possibleIngredients, possibleTools, playerScript);
		}
	}
}