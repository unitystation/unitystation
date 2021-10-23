using System;
using System.Collections.Generic;
using Systems.CraftingV2;
using Systems.CraftingV2.ClientServerLogic;
using Chemistry;
using Chemistry.Components;
using Items;
using Mirror;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
		private CraftingRecipeList defaultKnownRecipes = null;

		private PlayerScript playerScript;

		public PlayerScript PlayerScript => playerScript;

		private StandardProgressActionConfig craftProgressActionConfig = new StandardProgressActionConfig(
			StandardProgressActionType.Craft
		);

		#region Lifecycle

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
			foreach (CraftingRecipe craftingRecipe in defaultKnownRecipes.CraftingRecipes)
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

		#endregion

		#region RecipeLearningAndForgetting

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
			// if the player is a host and a client at the same time...
			if (playerScript == PlayerManager.LocalPlayerScript)
			{
				// ...then we'll handle the recipe learning on the "client" side,
				// so we won't have duplicates in the known recipes list
				// (because the server and the client have one known recipes list for two)
				SendLearnedCraftingRecipe.SendTo(playerScript.connectedPlayer, recipe);
				return;
			}

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

		#endregion

		#region CraftingChecks

		/// <summary>
		/// 	Checks if the player able to craft at all.
		/// </summary>
		/// <returns>True if a player can craft something, false otherwise.</returns>
		[Server]
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
		[Server]
		public bool IsPlayerAbleToCraft(CraftingRecipe recipe)
		{
			return IsPlayerAbleToCraft() && KnowsRecipe(recipe);
		}

		/// <summary>
		/// 	Checks if a player able to craft according to the recipe(including its ingredients, tools, etc).
		/// 	Will get the possible ingredients from a tile that a player is directed to.
		/// 	Will get the possible tools from a player's hands.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>CraftingStatus.AllGood if can craft, other statuses otherwise.</returns>
		[Server]
		public CraftingStatus CanCraft(CraftingRecipe recipe)
		{
			return CanCraft(
				recipe,
				GetPossibleIngredients(NetworkSide.Server),
				GetPossibleTools(NetworkSide.Server),
				GetReagentContainers()
			);
		}

		/// <summary>
		/// 	Checks if a player able to craft the recipe(including its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or/and reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="reagentContainers">The reagent containers that may be used for crafting.</param>
		/// <returns>CraftingStatus.AllGood if can craft, other statuses otherwise.</returns>
		[Server]
		public CraftingStatus CanCraft(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers
		)
		{
			return IsPlayerAbleToCraft(recipe) == false
				? CraftingStatus.NotAbleToCraft
				: recipe.CanBeCrafted(possibleIngredients, possibleTools, reagentContainers);
		}

		[Client]
		public CraftingStatus CanClientCraft(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return KnowsRecipe(recipe) == false
				? CraftingStatus.NotAbleToCraft
				: recipe.CanBeCraftedIgnoringReagents(possibleIngredients, possibleTools);
		}

		[Server]
		private CraftingStatus CanServerCraft(CraftingRecipe recipe, List<ReagentContainer> reagentsContainers)
		{
			if (IsPlayerAbleToCraft() == false)
			{
				return CraftingStatus.NotAbleToCraft;
			}

			return recipe.CheckPossibleReagents(reagentsContainers)
				? CraftingStatus.AllGood
				: CraftingStatus.NotEnoughReagents;
		}

		#endregion

		#region RequirementsGetters

		/// <summary>
		/// 	Gets all reachable items, including items in player's hands and pockets.
		/// </summary>
		/// <param name="networkSide">On which side we're executing the method?</param>
		/// <returns>All reachable items.</returns>
		public List<CraftingIngredient> GetPossibleIngredients(NetworkSide networkSide)
		{
			List<CraftingIngredient> possibleIngredients = MatrixManager.GetReachableAdjacent<CraftingIngredient>(
				playerScript.PlayerSync.ClientPosition, networkSide == NetworkSide.Server
			);

			possibleIngredients.AddRange(MatrixManager.GetAt<CraftingIngredient>(
				playerScript.PlayerSync.ClientPosition, networkSide == NetworkSide.Server
			));

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
		/// 	Gets all reachable possible tools, including player's hands and pockets.
		/// </summary>
		/// <param name="networkSide">On which side we're executing the method?</param>
		/// <returns>All possible tools that may be used for crafting.</returns>
		public List<ItemAttributesV2> GetPossibleTools(NetworkSide networkSide)
		{
			List<ItemAttributesV2> possibleTools = MatrixManager.GetReachableAdjacent<ItemAttributesV2>(
				playerScript.PlayerSync.ClientPosition, networkSide == NetworkSide.Server
			);

			possibleTools.AddRange(MatrixManager.GetAt<ItemAttributesV2>(
				playerScript.PlayerSync.ClientPosition, networkSide == NetworkSide.Server
			));

			foreach (ItemSlot handSlot in playerScript.DynamicItemStorage.GetHandSlots())
			{
				if (
					handSlot.ItemObject != null
					&& handSlot.ItemObject.TryGetComponent(out ItemAttributesV2 possibleTool)
					&& possibleTool.InitialTraits.Count > 0
				)
				{
					possibleTools.Add(possibleTool);
				}
			}

			foreach (ItemSlot pocketsSlot in playerScript.DynamicItemStorage.GetPocketsSlots())
			{
				if (
					pocketsSlot.ItemObject != null
					&& pocketsSlot.ItemObject.TryGetComponent(out ItemAttributesV2 possibleTool)
					&& possibleTool.InitialTraits.Count > 0
				)
				{
					possibleTools.Add(possibleTool);
				}
			}

			return possibleTools;
		}

		/// <summary>
		/// 	Gets all reachable reagent containers.
		/// </summary>
		/// <returns></returns>
		public List<ReagentContainer> GetReagentContainers()
		{
			List<ReagentContainer> reagentContainers = MatrixManager.GetReachableAdjacent<ReagentContainer>(
				playerScript.PlayerSync.ClientPosition, true
			);

			reagentContainers.AddRange(MatrixManager.GetAt<ReagentContainer>(
				PlayerScript.PlayerSync.ClientPosition,
				true
			));

			foreach (ItemSlot handSlot in playerScript.DynamicItemStorage.GetHandSlots())
			{
				if (
					handSlot.ItemObject != null
					&& handSlot.ItemObject.TryGetComponent(out ReagentContainer reagentContainer)
				)
				{
					reagentContainers.Add(reagentContainer);
				}
			}

			foreach (ItemSlot pocketsSlot in playerScript.DynamicItemStorage.GetPocketsSlots())
			{
				if (
					pocketsSlot.ItemObject != null
					&& pocketsSlot.ItemObject.TryGetComponent(out ReagentContainer reagentContainer)
				)
				{
					reagentContainers.Add(reagentContainer);
				}
			}

			return reagentContainers;
		}

		/// <summary>
		/// 	Gets all reachable reagents.
		/// </summary>
		/// <returns>A list of pairs of values: a reagent's index in the singleton and its amount.</returns>
		[Server]
		public List<KeyValuePair<int, float>> GetPossibleReagents()
		{
			List<KeyValuePair<int, float>> possibleReagents = new List<KeyValuePair<int, float>>();

			foreach (ReagentContainer reagentContainer in GetReagentContainers())
			{
				foreach (KeyValuePair<Reagent, float> reagent in reagentContainer.CurrentReagentMix.reagents)
				{
					if (reagent.Value.Approx(0))
					{
						continue;
					}

					possibleReagents.Add(new KeyValuePair<int, float>(reagent.Key.IndexInSingleton, reagent.Value));
				}
			}

			return possibleReagents;
		}

		#endregion

		#region StartingCrafting

		/// <summary>
		/// 	Tries to start a crafting action.
		/// 	May use all reachable items.
		/// 	May use all tools that a player holds in his hands.
		/// 	May use all reachable reagents.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <param name="networkSide">On which side we're executing the method?</param>
		/// <param name="craftingActionParameters"></param>
		/// <returns>True if a crafting action has started, false otherwise.</returns>
		public void TryToStartCrafting(
			CraftingRecipe recipe,
			NetworkSide networkSide,
			CraftingActionParameters craftingActionParameters
		)
		{
			if (networkSide == NetworkSide.Client)
			{
				CraftingStatus craftingStatus = CanClientCraft(
					recipe,
					GetPossibleIngredients(networkSide),
					GetPossibleTools(networkSide)
				);
				if (craftingActionParameters.Feedback == FeedbackType.GiveAllFeedback || (craftingActionParameters.Feedback == FeedbackType.GiveOnlySuccess && craftingStatus == CraftingStatus.AllGood))
				{
					GiveClientSidedFeedback(craftingStatus, recipe, false);
				}

				if (craftingStatus != CraftingStatus.AllGood)
				{
					return;
				}

				RequestStartCraftingAction.Send(recipe);
				return;
			}

			TryToStartCrafting(
				recipe,
				GetPossibleIngredients(networkSide),
				GetPossibleTools(networkSide),
				GetReagentContainers(),
				craftingActionParameters
			);
		}

		/// <summary>
		/// 	Tries to start a crafting action.
		/// 	May use all reachable items.
		/// 	May use all tools that a player holds in his hands.
		/// 	May use all reachable reagents.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <param name="craftingActionParameters"></param>
		[Server]
		public void TryToStartCrafting(CraftingRecipe recipe, CraftingActionParameters craftingActionParameters)
		{
			TryToStartCrafting(recipe, NetworkSide.Server, craftingActionParameters);
		}

		/// <summary>
		/// 	Tries to start a crafting action. Gives feedback to the player.
		/// </summary>
		/// <param name="recipe">The recipe that the player is trying to craft to.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="reagentContainers">The reagent containers that may be used for crafting.</param>
		/// <param name="craftingActionParameters"></param>
		/// <returns>True if a crafting action has started, false otherwise.</returns>
		[Server]
		public bool TryToStartCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers,
			CraftingActionParameters craftingActionParameters
		)
		{
			CraftingStatus craftingStatus =
				craftingActionParameters.IgnoreToolsAndIngredients
					? CanServerCraft(recipe, reagentContainers)
					: CanCraft(recipe, possibleIngredients, possibleTools, reagentContainers);

			if (craftingActionParameters.Feedback == FeedbackType.GiveAllFeedback || (craftingActionParameters.Feedback == FeedbackType.GiveOnlySuccess && craftingStatus == CraftingStatus.AllGood))
			{
				GiveServerSidedFeedback(craftingStatus, recipe, false);
			}

			if (craftingStatus != CraftingStatus.AllGood)
			{
				return false;
			}

			StartCrafting(recipe, craftingActionParameters);
			return true;
		}

		/// <summary>
		/// 	Unsafely starts a new crafting action even if the recipe's requirements were not fulfilled.
		/// </summary>
		/// <param name="recipe">The recipe that the player is trying to craft to.</param>
		/// <param name="craftingActionParameters"></param>
		[Server]
		private void StartCrafting(CraftingRecipe recipe, CraftingActionParameters craftingActionParameters)
		{
			if (recipe.CraftingTime.Approx(0))
			{
				// ok then there is no need to create a special progress action
				TryToFinishCrafting(recipe, craftingActionParameters);
				return;
			}

			StandardProgressAction.Create(
				craftProgressActionConfig,
				() => TryToFinishCrafting(recipe, craftingActionParameters)
			).ServerStartProgress(playerScript.registerTile, recipe.CraftingTime, playerScript.gameObject);
		}

		#endregion

		#region FinishingCrafting

		/// <summary>
		/// 	Tries to finish a crafting action.
		/// 	May use all reachable items.
		/// 	May use all tools that a player holds in his hands.
		/// 	May use all reachable reagents.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <param name="craftingActionParameters"></param>
		/// <returns>True if we can spawn the recipe's result, false otherwise.</returns>
		[Server]
		public void TryToFinishCrafting(CraftingRecipe recipe, CraftingActionParameters craftingActionParameters)
		{
			TryToFinishCrafting(
				recipe,
				GetPossibleIngredients(NetworkSide.Server),
				GetPossibleTools(NetworkSide.Server),
				GetReagentContainers(),
				craftingActionParameters
			);
		}

		/// <summary>
		/// 	Tries to finish a crafting action. Gives feedback for the player.
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that may be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="reagentContainers">The reagent containers that may be used for crafting.</param>
		/// <param name="craftingActionParameters"></param>
		/// <returns>True if we can spawn the recipe's result, false otherwise.</returns>
		[Server]
		public bool TryToFinishCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers,
			CraftingActionParameters craftingActionParameters
		)
		{
			CraftingStatus craftingStatus =
				craftingActionParameters.IgnoreToolsAndIngredients
					? CanServerCraft(recipe, reagentContainers)
					: CanCraft(recipe, possibleIngredients, possibleTools, reagentContainers);

			if (craftingActionParameters.Feedback == FeedbackType.GiveAllFeedback || (craftingActionParameters.Feedback == FeedbackType.GiveOnlySuccess && craftingStatus == CraftingStatus.AllGood))
			{
				GiveServerSidedFeedback(craftingStatus, recipe, true);
			}

			if (craftingStatus != CraftingStatus.AllGood)
			{
				return false;
			}

			FinishCrafting(recipe, possibleIngredients, possibleTools, reagentContainers);
			return true;
		}

		/// <summary>
		/// 	Unsafely finishes a crafting action even if the recipe's requirements were not fulfilled.
		/// </summary>
		/// <param name="recipe">The recipe that was used to craft.</param>
		/// <param name="possibleIngredients">The ingredients(or reagent containers) that may be used for crafting.</param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="reagentContainers">The reagent containers that may be used for crafting.</param>
		private void FinishCrafting(
			CraftingRecipe recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers
		)
		{
			recipe.UnsafelyCraft(playerScript, possibleIngredients, possibleTools, reagentContainers);
		}

		#endregion

		#region Feedback

		public void GiveClientSidedFeedback(
			CraftingStatus craftingStatus,
			CraftingRecipe recipe,
			bool completingCrafting
		)
		{
			switch (craftingStatus)
			{
				case CraftingStatus.AllGood:
					if (completingCrafting)
					{
						Chat.AddExamineMsgToClient(
							$"You made \"{recipe.RecipeName}\"."
						);
						return;
					}

					if (recipe.CraftingTime > 0)
					{
						Chat.AddExamineMsgToClient(
							$"You are trying to craft \"{recipe.RecipeName}\"..."
						);
					}

					return;
				case CraftingStatus.NotEnoughIngredients:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough ingredients."
					);
					return;
				case CraftingStatus.NotEnoughTools:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough tools."
					);
					return;
				case CraftingStatus.NotEnoughReagents:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough reagents."
					);
					return;
				case CraftingStatus.NotAbleToCraft:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\" because your character can't craft this."
					);
					return;
				case CraftingStatus.UnspecifiedImpossibility:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\"."
					);
					return;
				default:
					Chat.AddExamineMsgToClient(
						$"You can't craft \"{recipe.RecipeName}\" for some reason. " +
						"Report this message to developers."
					);
					return;
			}
		}

		public void GiveServerSidedFeedback(
			CraftingStatus craftingStatus,
			CraftingRecipe recipe,
			bool completingCrafting
		)
		{
			switch (craftingStatus)
			{
				case CraftingStatus.AllGood:
					if (completingCrafting)
					{
						Chat.AddExamineMsgFromServer(
							playerScript.gameObject,
							$"You made \"{recipe.RecipeName}\"."
						);
						return;
					}

					if (recipe.CraftingTime > 1)
					{
						Chat.AddExamineMsgFromServer(
							playerScript.gameObject,
							$"You are trying to craft \"{recipe.RecipeName}\"..."
						);
					}

					return;
				case CraftingStatus.NotEnoughIngredients:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough ingredients."
					);
					return;
				case CraftingStatus.NotEnoughTools:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough tools."
					);
					return;
				case CraftingStatus.NotEnoughReagents:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because there are not enough reagents."
					);
					return;
				case CraftingStatus.NotAbleToCraft:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" because your character can't craft this."
					);
					return;
				case CraftingStatus.UnspecifiedImpossibility:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\"."
					);
					return;
				default:
					Chat.AddExamineMsgFromServer(
						playerScript.gameObject,
						$"You can't craft \"{recipe.RecipeName}\" for some reason. " +
						"Report this message to developers."
					);
					return;
			}
		}

		#endregion
	}
}

