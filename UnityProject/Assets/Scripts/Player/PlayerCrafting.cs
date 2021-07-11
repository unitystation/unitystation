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
			if (!IsPlayerAbleToCraft(recipe))
			{
				return false;
			}

			List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();
			foreach (ItemSlot handSlot in playerScript.DynamicItemStorage.GetHandSlots())
			{
				if (handSlot.ItemObject != null)
				{
					possibleTools.Add(handSlot.ItemObject.GetComponent<ItemAttributesV2>());
				}
			}

			Vector3Int ingredientsSourceVector = playerScript.WorldPos;
			List<ItemAttributesV2> possibleIngredients;
			// it's unlikely that it will be null, but we are not immune from this case
			if (directional != null)
			{
				ingredientsSourceVector = PlayerScript.WorldPos + (Vector3Int) directional.CurrentDirection.VectorInt;
			}

			possibleIngredients = MatrixManager.GetAt<ItemAttributesV2>(ingredientsSourceVector, true);

			// no one knows how to craft through walls yet, so let's ignore the things behind the wall or something else
			for (int i = possibleIngredients.Count - 1; i >= 0; i--)
			{
				if (!Validations.IsReachableByPositions(
					playerScript.WorldPos,
					ingredientsSourceVector,
					true,
					context: playerScript.gameObject
				))
				{
					// sadly we can't get the elements as a LinkedList to make this algorithm more efficient
					possibleIngredients.RemoveAt(i);
				}
			}

			return recipe.CanBeCrafted(possibleIngredients, possibleTools);
		}

		public bool TryToStartCrafting(RecipeV2 recipe)
		{
			if (!CanCraft(recipe))
			{
				// feedback for a player?
			}

			// start crafting action?

			return true;
		}

		public bool TryToFinishCrafting(RecipeV2 recipe)
		{
			if (!CanCraft(recipe))
			{
				// feedback for a player?
			}

			// recipe.CompleteCrafting()?

			return true;
		}
	}

	public enum CraftingCategory
	{
		Weapons = 0,
		Food = 1,
		Misc = 2
	}
}