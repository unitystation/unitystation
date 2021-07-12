using System.Collections.Generic;
using System.Linq;
using Items;
using UnityEngine;

namespace Player
{
	[RequireComponent(typeof(PlayerScript))]
	public class PlayerCrafting : MonoBehaviour
	{
		[SerializeField] private List<RecipeV2>[] knownRecipesByCategory =
		{
			// see enum CraftingCategory
			new List<RecipeV2>(),
			new List<RecipeV2>(),
			new List<RecipeV2>()
		};

		public List<RecipeV2>[] KnownRecipesByCategory => knownRecipesByCategory;

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

		public void LearnRecipe(RecipeV2 recipe)
		{
			KnownRecipesByCategory[(int) recipe.Category].Add(recipe);
		}

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

		public bool CanCraft(RecipeV2 recipe)
		{
			return CanCraft(recipe, GetPossibleIngredients(), GetPossibleTools());
		}

		public bool CanCraft(
			RecipeV2 recipe,
			List<ItemAttributesV2> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			return IsPlayerAbleToCraft(recipe) && recipe.CanBeCrafted(possibleIngredients, possibleTools);
		}

		private List<ItemAttributesV2> GetPossibleIngredients()
		{
			Vector3Int ingredientsSourceVector = playerScript.WorldPos;
			List<ItemAttributesV2> possibleIngredients = new List<ItemAttributesV2>();
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
				possibleIngredients = MatrixManager.GetAt<ItemAttributesV2>(ingredientsSourceVector, true);
			}

			return possibleIngredients;
		}

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

		public void TryToFinishCrafting(RecipeV2 recipe)
		{
			List<ItemAttributesV2> possibleIngredients = GetPossibleIngredients();
			List<ItemAttributesV2> possibleTools = GetPossibleTools();
			if (!CanCraft(recipe, possibleIngredients, possibleTools))
			{
				Chat.AddExamineMsgFromServer(
					playerScript.gameObject,
					$"Wait, where's mine... Oh, forget it, I can't craft it anymore."
				);
				return;
			}

			recipe.UnsafelyCraft(possibleIngredients, possibleTools, playerScript.gameObject);

			return;
		}
	}

	public enum CraftingCategory
	{
		Weapons = 0,
		Food = 1,
		Misc = 2
	}
}