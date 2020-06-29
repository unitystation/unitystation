using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking
{
	/// <summary>
	/// Main Component for Machine deconstruction
	/// </summary>
	public class ComplexMeal : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		/// <summary>
		/// Machine parts used to build this machine
		/// </summary>
		public ComplexMealRecipe MealIngredients;

		//Not needed on all machine prefabs
		private IDictionary<GameObject, int> ingredientsUsed = new Dictionary<GameObject, int>();
		private IDictionary<GameObject, int> ingredientsInBase = new Dictionary<GameObject, int>();

		[Tooltip("Meal base this should be pulled apart into.")]
		[SerializeField]
		private GameObject basePrefab = null;

		public IDictionary<GameObject, int> IngredientsUsed => ingredientsUsed;
		public IDictionary<GameObject, int> IngredientsInBase => ingredientsInBase;

		[Tooltip("Time taken to pull it apart.")]
		[SerializeField]
		private float secondsToPullApart = 2f;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (MealIngredients == null) return;

			//unscrew
			ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToPullApart,
				"You start to pull apart the meal...",
				$"{interaction.Performer.ExpensiveName()} starts to pull apart the meal...",
				"You pull apart the meal.",
				$"{interaction.Performer.ExpensiveName()} pulls apart the meal.",
				() =>
				{
					var frame = Spawn.ServerPrefab(basePrefab, SpawnDestination.At(gameObject)).GameObject;

					frame.GetComponent<ComplexMealBase>().ServerInitFromComputer(this);

					Despawn.ServerSingle(gameObject);
				});
		}

		public void SetIngredients(ComplexMealRecipe mealIngredients)
		{
			MealIngredients = mealIngredients;
		}

		public void SetIngredientsUsed(IDictionary<GameObject, int> ingredientsUsed)
		{
			this.ingredientsUsed = ingredientsUsed;
		}

		public void SetIngredientsInBase(IDictionary<GameObject, int> ingredientsInBase)
		{
			this.ingredientsInBase = ingredientsInBase;
		}
	}
}