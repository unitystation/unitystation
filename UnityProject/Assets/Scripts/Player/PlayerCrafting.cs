using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2;
using Items;
using UnityEngine;

namespace Player
{
	/// <summary>
	/// A class that deals with the processing of crafting items by a player.
	/// </summary>
	[RequireComponent(typeof(PlayerScript))]
	public class PlayerCrafting : MonoBehaviour
	{
		[SerializeField] [Tooltip("Default recipes known to a player.")]
		private List<List<RecipeV2>> knownRecipesByCategory = new List<List<RecipeV2>>();

		/// <summary>
		/// The list of currently known recipes for a player by category.
		/// </summary>
		public List<List<RecipeV2>> KnownRecipesByCategory => knownRecipesByCategory;

		private PlayerScript playerScript;

		public PlayerScript PlayerScript => playerScript;

		private Directional directional;

		public Directional Directional => directional;

		private StandardProgressActionConfig craftProgressActionConfig = new StandardProgressActionConfig(
			StandardProgressActionType.Craft
		);

		public StandardProgressActionConfig CraftProgressActionConfig => craftProgressActionConfig;

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			directional = GetComponent<Directional>();
		}

		/// <summary>
		/// Checks if a player already knows the recipe.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True, if a player already knows the recipe, false otherwise.</returns>
		public bool KnowRecipe(RecipeV2 recipe)
		{
			return KnownRecipesByCategory[(int) recipe.Category].Contains(recipe);
		}

		/// <summary>
		/// Adds the recipe to the KnownRecipesByCategory[] if it doesn't already contains the recipe.
		/// </summary>
		/// <param name="recipe">The recipe to add.</param>
		public void LearnRecipe(RecipeV2 recipe)
		{
			if (KnowRecipe(recipe))
			{
				return;
			}
			KnownRecipesByCategory[(int) recipe.Category].Add(recipe);
		}

		/// <summary>
		/// Removes the recipe from the KnownRecipesByCategory[]
		/// </summary>
		/// <param name="recipe">The recipe to remove.</param>
		public void ForgetRecipe(RecipeV2 recipe)
		{
			KnownRecipesByCategory[(int) recipe.Category].Remove(recipe);
		}

		/// <summary>
		/// Checks if a player able to craft according to the recipe(ignoring its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool IsPlayerAbleToCraft(RecipeV2 recipe)
		{
			return !PlayerScript.playerMove.IsCuffed
			       && !PlayerScript.playerMove.IsTrapped
			       && !PlayerScript.IsGhost
			       && !PlayerScript.playerHealth.IsCrit
			       && !PlayerScript.playerHealth.IsDead
			       && PlayerScript.playerHealth.HasBodyPart(BodyPartType.RightHand)
			       && PlayerScript.playerHealth.HasBodyPart(BodyPartType.LeftHand)
			       && KnownRecipesByCategory[(int) recipe.Category].Contains(recipe);
		}

		/// <summary>
		/// Checks if a player able to craft according to the recipe(including its ingredients, tools, etc).
		/// Will get the possible ingredients from a tile that a player is directed to.
		/// Will get the possible tools from a player's hands.
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool CanCraft(RecipeV2 recipe)
		{
			return CanCraft(recipe, GetPossibleIngredients(), GetPossibleTools());
		}

		/// <summary>
		/// Checks if a player able to craft the recipe(including its ingredients, tools, etc).
		/// </summary>
		/// <param name="recipe">The recipe to check.</param>
		/// <param name="possibleIngredients"></param>
		/// <param name="possibleTools"></param>
		/// <returns>True if a player can craft according to the recipe, false otherwise.</returns>
		public bool CanCraft(
			RecipeV2 recipe,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return IsPlayerAbleToCraft(recipe) && recipe.CanBeCrafted(possibleIngredients, possibleTools);
		}

		/// <summary>
		/// Gets all reachable items from a tile that a player is directed to.
		/// </summary>
		/// <returns>All reachable items from a tile that a player is directed to.</returns>
		private List<CraftingIngredient> GetPossibleIngredients()
		{
			Vector3Int ingredientsSourceVector = playerScript.WorldPos;
			List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();
			// it's unlikely that it will be null, but we are not immune from this case
			if (directional != null)
			{
				ingredientsSourceVector = PlayerScript.WorldPos + (Vector3Int) directional.CurrentDirection.VectorInt;
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
		/// Gets all tools that a player holds in his hands.
		/// </summary>
		/// <returns></returns>
		private List<ItemAttributesV2> GetPossibleTools()
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
		/// Tries to start a crafting action
		/// </summary>
		/// <param name="recipe">The recipe to try to craft.</param>
		public void TryToStartCrafting(RecipeV2 recipe)
		{
			if (!CanCraft(recipe))
			{
				return;
			}

			Chat.AddExamineMsgFromServer(
				playerScript.gameObject,
				$"You are trying to craft {recipe.RecipeName}..."
			);

			StandardProgressAction.Create(
				craftProgressActionConfig,
				() => TryToFinishCrafting(recipe)
			).ServerStartProgress(playerScript.registerTile, recipe.CraftingTime, playerScript.gameObject);
		}

		/// <summary>
		/// Tries to finish a crafting action.
		/// </summary>
		/// <param name="recipe">The recipe to try to finish crafting.</param>
		public void TryToFinishCrafting(RecipeV2 recipe)
		{
			List<CraftingIngredient> possibleIngredients = GetPossibleIngredients();
			List<ItemAttributesV2> possibleTools = GetPossibleTools();
			if (!CanCraft(recipe, possibleIngredients, possibleTools))
			{
				Chat.AddExamineMsgFromServer(
					playerScript.gameObject,
					"Wait, where's mine... Oh, forget it, I can't craft it anymore."
				);
				return;
			}

			recipe.UnsafelyCraft(possibleIngredients, possibleTools, playerScript.gameObject);
		}
	}

	public enum CraftingCategory
	{
		Weapons = 0,
		Food = 1,
		Misc = 2
	}
}