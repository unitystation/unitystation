using System;
using System.Collections.Generic;
using Systems.CraftingV2;
using Systems.CraftingV2.ClientServerLogic;
using Systems.CraftingV2.GUI;
using Items;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace Player
{
	/// <summary>
	/// A class that deals with the processing of crafting items by a player.
	/// </summary>
	[RequireComponent(typeof(PlayerScript))]
	public class PlayerCrafting : NetworkBehaviour
	{
		private readonly List<List<CraftingRecipe>> knownRecipesByCategory = new List<List<CraftingRecipe>>();

		/// <summary>
		/// The list of currently known recipes for a player by category.
		/// </summary>
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
		}

		public override void OnStartServer()
		{
			InitDefaultRecipes();
		}

		public override void OnStartLocalPlayer()
		{
			RequestInitRecipes.Send(new RequestInitRecipes.NetMessage());
		}

		private void InitKnownRecipesByCategories()
		{
			for (int i = Enum.GetValues(typeof(RecipeCategory)).Length - 1; i >= 0; i--)
			{
				knownRecipesByCategory.Add(new List<CraftingRecipe>());
			}
		}

		[Server]
		private void InitDefaultRecipes()
		{
			foreach (CraftingRecipe craftingRecipe in defaultKnownRecipes)
			{
				TryAddRecipeToKnownRecipes(craftingRecipe);
			}
		}

		[Client]
		public void InitRecipes(List<CraftingRecipe> knownRecipes)
		{
			// we should clear known recipes firstly because a non-headless server will also
			// init default recipes on a client side, which it shouldn't do on a headless server.
			// On the headless server the known recipes list will be empty,
			// so don't worry about unnecessary algorithm complexity.
			knownRecipesByCategory.ForEach(recipesInCategory => recipesInCategory.Clear());
			knownRecipes.ForEach(UnsafelyAddRecipeToKnownRecipes);
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
		public bool KnowsRecipe(CraftingRecipe recipe)
		{
			return GetKnownRecipesInCategory(recipe.Category).Contains(recipe);
		}

		/// <summary>
		/// 	Adds the recipe to the KnownRecipesByCategory[] if it doesn't already contains the recipe.
		/// </summary>
		/// <param name="recipe">The recipe to add.</param>
		[Server]
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
			if (KnowsRecipe(craftingRecipe))
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
		/// 	Removes the recipe from the known recipes list.
		/// </summary>
		/// <param name="craftingRecipe">The recipe to remove.</param>
		public void RemoveRecipeFromKnownRecipes(CraftingRecipe craftingRecipe)
		{
			GetKnownRecipesInCategory(craftingRecipe.Category).Remove(craftingRecipe);
		}

		/// <summary>
		/// 	Removes the recipe from the KnownRecipesByCategory list.
		/// </summary>
		/// <param name="recipe">The recipe to remove.</param>
		[Server]
		public void ForgetRecipe(CraftingRecipe recipe)
		{
			GetKnownRecipesInCategory(recipe.Category).Remove(recipe);
			SendForgottenCraftingRecipe.SendTo(playerScript.connectedPlayer, recipe);
		}

		/// <summary>
		/// 	Checks if the player able to craft at all.
		/// </summary>
		/// <returns>True if a player can craft something, false otherwise.</returns>
		public bool IsPlayerAbleToCraft()
		{
			return !PlayerScript.playerMove.IsCuffed
			       && !PlayerScript.playerMove.IsTrapped
			       && !PlayerScript.IsGhost
			       && !PlayerScript.playerHealth.IsCrit
			       && !PlayerScript.playerHealth.IsDead;
		}

		/// <summary>
		/// 	Checks if the player able to craft according to the recipe(ignoring its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool IsPlayerAbleToCraft(CraftingRecipe recipe)
		{
			return IsPlayerAbleToCraft() && KnowsRecipe(recipe);
		}

		/// <summary>
		/// 	Checks if a player able to craft according to the recipe(ignoring its ingredients, tools, etc).
		/// 	This static method assumes that a player is already able to craft at all.
		/// </summary>
		/// <param name="recipeIndex">The recipe index to check.</param>
		/// <param name="knownRecipeIndexes">The known to a player recipe indexes.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or/and reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public static CraftingStatus CanCraft(
			int recipeIndex,
			List<int> knownRecipeIndexes,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return knownRecipeIndexes.Contains(recipeIndex) == false
				? CraftingStatus.NotAbleToCraft
				: CraftingRecipeSingleton
					.Instance
					.GetRecipeByIndex(recipeIndex)
					.CanBeCrafted(possibleIngredients, possibleTools);
		}

		/// <summary>
		/// Checks if a player able to craft according to the recipe(including its ingredients, tools, etc).
		/// Will get the possible ingredients from a tile that a player is directed to.
		/// Will get the possible tools from a player's hands.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>CraftingStatus.AllGood if can craft, other statuses otherwise.</returns>
		public CraftingStatus CanCraft(CraftingRecipe recipe)
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
		/// <returns>CraftingStatus.AllGood if can craft, other statuses otherwise.</returns>
		public CraftingStatus CanCraft(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return IsPlayerAbleToCraft(recipe) == false
				? CraftingStatus.NotAbleToCraft
				: recipe.CanBeCrafted(possibleIngredients, possibleTools);
		}

		/// <summary>
		/// 	Gets all reachable items.
		/// </summary>
		/// <returns>All reachable items.</returns>
		public List<CraftingIngredient> GetPossibleIngredients()
		{
			Vector3Int ingredientsSourceVector = PlayerScript.WorldPos;
			List<CraftingIngredient> possibleIngredients = MatrixManager.GetReachableAdjacent<CraftingIngredient>(
				ingredientsSourceVector, true
			);

			possibleIngredients.AddRange(MatrixManager.GetAt<CraftingIngredient>(PlayerScript.WorldPos, true));

			foreach (ItemSlot handSlot in playerScript.DynamicItemStorage.GetHandSlots())
			{
				if (
					handSlot.ItemObject != null
					&& handSlot.ItemObject.TryGetComponent(out CraftingIngredient possibleIngredient)
				)
				{
					possibleIngredients.Add(possibleIngredient);
				}
			}

			foreach (ItemSlot pocketsSlot in playerScript.DynamicItemStorage.GetPocketsSlots())
			{
				if (
					pocketsSlot.ItemObject != null
					&& pocketsSlot.ItemObject.TryGetComponent(out CraftingIngredient possibleIngredient)
				)
				{
					possibleIngredients.Add(possibleIngredient);
				}
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
				if (
					handSlot.ItemObject != null
				    && handSlot.ItemObject.TryGetComponent(out ItemAttributesV2 possibleTool)
				)
				{
					possibleTools.Add(possibleTool);
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
		///		Tries to start a crafting action. Gives feedback to the player.
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
			switch (CanCraft(recipe, possibleIngredients, possibleTools))
			{
				case CraftingStatus.AllGood:
					if (recipe.CraftingTime > 1) {
						Chat.AddExamineMsgFromServer(
							playerScript.gameObject,
							$"You are trying to craft \"{recipe.RecipeName}\"..."
						);
					}
					StartCrafting(recipe, possibleIngredients, possibleTools);
					return true;
				case CraftingStatus.NotEnoughIngredients:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough ingredients."
					);
					return false;
				case CraftingStatus.NotEnoughTools:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough tools."
					);
					return false;
				case CraftingStatus.NotEnoughReagents:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough reagents."
					);
					return false;
				case CraftingStatus.NotAbleToCraft:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because your character can't craft this."
					);
					return false;
				case CraftingStatus.UnspecifiedImpossibility:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\"."
					);
					return false;
			}

			Chat.AddExamineMsgFromServer(
				playerScript.gameObject,
				$"You can't craft the \"{recipe.RecipeName}\". Report this message to developers."
			);

			Logger.LogError($"Something went wrong when the player({PlayerScript.connectedPlayer.Name}) " +
			                $"was trying to craft according to the recipe({recipe}).");

			return false;
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
		/// 	Tries to finish a crafting action. Gives feedback for the player.
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
			switch (CanCraft(recipe, possibleIngredients, possibleTools))
			{
				case CraftingStatus.AllGood:
					if (recipe.CraftingTime > 1) {
						Chat.AddExamineMsgFromServer(
							playerScript.gameObject,
							$"You made the {recipe.RecipeName}!"
						);
					}
					FinishCrafting(recipe, possibleIngredients, possibleTools);
					return true;
				case CraftingStatus.NotEnoughIngredients:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" anymore - there aren't enough ingredients."
					);
					return false;
				case CraftingStatus.NotEnoughTools:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" anymore - there aren't enough tools."
					);
					return false;
				case CraftingStatus.NotEnoughReagents:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" anymore - there aren't enough reagents."
					);
					return false;
				case CraftingStatus.NotAbleToCraft:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" anymore - " +
						$"your character can no longer craft this."
					);
					return false;
				case CraftingStatus.UnspecifiedImpossibility:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" anymore."
					);
					return false;
			}

			Chat.AddExamineMsgFromServer(
				playerScript.gameObject,
				$"You can't craft the \"{recipe.RecipeName}\" anymore. Report this message to developers."
			);

			Logger.LogError($"Something went wrong when the player({PlayerScript.connectedPlayer.Name}) " +
			                $"was trying to finish crafting according to the recipe({recipe}).");

			return false;
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
			recipe.UnsafelyCraft(possibleIngredients, possibleTools, playerScript);
		}
	}
}