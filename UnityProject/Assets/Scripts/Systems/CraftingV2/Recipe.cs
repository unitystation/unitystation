using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// A recipe for crafting. Turns a list of items-ingredients into a list of items-results(usually only one item).
	/// </summary>
	[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Crafting/Recipe")]
	public class Recipe : ScriptableObject
	{
		/// <summary>
		/// Items that will be necessary and used for crafting. They will be deleted.
		/// </summary>
		[Tooltip("Items that will be necessary, used and deleted for crafting.")] [SerializeField]
		private List<Ingredient> requiredIngredients = new List<Ingredient>();

		public List<Ingredient> RequiredIngredients => requiredIngredients;

		[Tooltip("What tools(item traits) should be present when creating a thing according to a recipe.")]
		[SerializeField]
		private List<ItemTrait> toolTraitsRequired;

		public List<ItemTrait> ToolTraitsRequired => toolTraitsRequired;

		/// <summary>
		/// The resulting items after crafting.
		/// </summary>
		[Tooltip("The resulting items after crafting.")] [SerializeField]
		private List<GameObject> result;

		public List<GameObject> Result => result;

		public bool CanBeCrafted(List<ItemAttributesV2> possibleIngredients, List<ItemAttributesV2> possibleTools)
		{
			return CheckPossibleIngredients(possibleIngredients) && CheckPossibleTools(possibleTools);
		}

		private bool CheckPossibleTools(List<ItemAttributesV2> possibleTools)
		{
			foreach (ItemTrait itemTrait in toolTraitsRequired)
			{
				bool foundRequiredToolTrait = false;
				foreach (ItemAttributesV2 possibleTool in possibleTools)
				{
					if (possibleTool.HasTrait(itemTrait))
					{
						foundRequiredToolTrait = true;
						break;
					}
				}

				if (!foundRequiredToolTrait)
				{
					return false;
				}
			}

			return true;
		}

		private bool CheckPossibleIngredients(List<ItemAttributesV2> possibleIngredients)
		{
			foreach (Ingredient requiredIngredient in RequiredIngredients)
			{
				int countedAmount = 0;
				for (int counter = 0; counter < possibleIngredients.Count; counter++)
				{
					if (requiredIngredient.RequiredItem.InitialName != possibleIngredients[counter].InitialName)
					{
						continue;
					}

					if (++countedAmount == requiredIngredient.RequiredAmount)
					{
						break;
					}
				}

				if (countedAmount != requiredIngredient.RequiredAmount)
				{
					return false;
				}
			}

			return true;
		}

		public void TryToCraft(
			List<ItemAttributesV2> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			GameObject crafterGameObject
		)
		{
			if (!CanBeCrafted(possibleIngredients, possibleTools))
			{
				return;
			}

			foreach (Ingredient requiredIngredient in requiredIngredients)
			{
				int usedIngredientsCounter = 0;
				foreach (ItemAttributesV2 possibleIngredient in possibleIngredients)
				{
					if (requiredIngredient.RequiredItem.InitialName != possibleIngredient.InitialName)
					{
						continue;
					}

					_ = Despawn.ServerSingle(possibleIngredient.gameObject);

					if (++usedIngredientsCounter >= requiredIngredient.RequiredAmount)
					{
						break;
					}
				}
			}

			CompleteCrafting(crafterGameObject);
		}

		private void CompleteCrafting(GameObject crafterGameObject)
		{
			foreach (GameObject resultedGameObject in Result)
			{
				Spawn.ServerPrefab(resultedGameObject, crafterGameObject.WorldPosServer());
			}
		}
	}
}