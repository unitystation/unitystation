using System.Collections.Generic;
using System.Text;
using Chemistry;
using NaughtyAttributes;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	public class CraftingMenu : MonoBehaviour
	{
		public static CraftingMenu Instance;

		[SerializeField] private GameObject recipeButtonTemplatePrefab;

		[SerializeField] private GameObject categoriesLayoutGameObject;

		[SerializeField] private GameObject recipesLayoutGameObject;

		[SerializeField] private GameObject recipeInfoGameObject;

		[SerializeField] private GameObject chosenRecipeIconGameObject;

		[SerializeField] private GameObject chosenRecipeNameGameObject;

		[SerializeField] private GameObject chosenRecipeDescriptionGameObject;

		[SerializeField] private GameObject ingredientsTextGameObject;

		[SerializeField] private GameObject toolsTextGameObject;

		[SerializeField] private GameObject reagentsTextGameObject;

		[SerializeField] private GameObject craftButtonTextGameObject;

		[SerializeField, ReorderableList] private List<GameObject> categoryButtonsPrefabs;

		private readonly List<RecipesInCategory> recipesInCategories = new List<RecipesInCategory>();

		private GridLayoutGroup recipesGridLayout;

		private HorizontalLayoutGroup categoriesLayout;

		private Text chosenRecipeNameTextComponent;

		private Text chosenRecipeDescriptionTextComponent;

		private Image chosenRecipeIconImageComponent;

		private Text ingredientsTextComponent;

		private Text toolsTextComponent;

		private Text reagentsTextComponent;

		private Text craftButtonTextComponent;

		private CategoryButtonScript chosenCategory;

		private RecipeButtonScript chosenRecipe;

		private bool recipesInitIsNecessary = true;

		#region Lifecycle

		void Awake()
		{
			if (Instance == null)
			{
				InitFields();
				InitCategories();
				return;
			}

			Destroy(gameObject);
		}

		private void InitFields()
		{
			Instance = this;
			recipesGridLayout = recipesLayoutGameObject.GetComponent<GridLayoutGroup>();
			categoriesLayout = categoriesLayoutGameObject.GetComponent<HorizontalLayoutGroup>();
			chosenRecipeNameTextComponent = chosenRecipeNameGameObject.GetComponent<Text>();
			chosenRecipeDescriptionTextComponent = chosenRecipeDescriptionGameObject.GetComponent<Text>();
			chosenRecipeIconImageComponent = chosenRecipeIconGameObject.GetComponent<Image>();
			ingredientsTextComponent = ingredientsTextGameObject.GetComponent<Text>();
			toolsTextComponent = toolsTextGameObject.GetComponent<Text>();
			reagentsTextComponent = reagentsTextGameObject.GetComponent<Text>();
			craftButtonTextComponent = craftButtonTextGameObject.GetComponent<Text>();
		}

		private void InitCategories()
		{
			foreach (GameObject categoryButtonPrefab in categoryButtonsPrefabs)
			{
				GameObject initiatedCategoryButtonGameObject = Instantiate(
					categoryButtonPrefab,
					categoriesLayoutGameObject.transform
				);
				recipesInCategories.Add(new RecipesInCategory(
					initiatedCategoryButtonGameObject.GetComponent<CategoryButtonScript>()
				));
			}

			CheckCategoriesDuplicates();
			ReSortCategories();
			SelectCategory(recipesInCategories[0].CategoryButtonScript);
		}

		private void ReSortCategories()
		{
			recipesInCategories.Sort(
				(first, second) =>
					((int) first.CategoryButtonScript.CategoryAndIcon.RecipeCategory)
					.CompareTo(((int) second.CategoryButtonScript.CategoryAndIcon.RecipeCategory))
			);
		}

		private void CheckCategoriesDuplicates()
		{
			for (int i = 0; i < recipesInCategories.Count; i++)
			{
				for (int j = i + 1; j < recipesInCategories.Count; j++)
				{
					if (recipesInCategories[i].CategoryButtonScript.CategoryAndIcon.RecipeCategory !=
					    recipesInCategories[j].CategoryButtonScript.CategoryAndIcon.RecipeCategory)
					{
						continue;
					}

					Logger.LogError(
						"An attempt to create two same categories in a crafting menu. " +
						"The duplicated category: " +
						$"{recipesInCategories[j].CategoryButtonScript.CategoryAndIcon.RecipeCategory}."
					);
					recipesInCategories.RemoveAt(j);
					j--;
				}
			}
		}

		#endregion

		#region CategoryAndRecipeSelections

		private void SelectCategory(CategoryButtonScript categoryButtonScript)
		{
			categoryButtonScript.OnPressed();
			chosenCategory = categoryButtonScript;
		}

		private void DeselectCategory(CategoryButtonScript categoryButtonScript)
		{
			categoryButtonScript.OnUnpressed();
		}

		public void ChangeCategory(CategoryButtonScript categoryButtonScript)
		{
			DeselectCategory(chosenCategory);
			DeselectRecipe(chosenRecipe);
			SelectCategory(categoryButtonScript);
		}

		private void DeselectRecipe(RecipeButtonScript recipeButtonScript)
		{
			if (recipeButtonScript != null)
			{
				chosenRecipe.OnUnpressed();
			}

			recipeInfoGameObject.SetActive(false);
		}

		public void ChangeRecipe(RecipeButtonScript recipeButtonScript)
		{
			recipeButtonScript.OnPressed();
			DeselectRecipe(chosenRecipe);

			chosenRecipe = recipeButtonScript;

			FillRecipeInfo(recipeButtonScript);
			recipeInfoGameObject.SetActive(true);
		}

		private void FillRecipeInfo(RecipeButtonScript recipeButtonScript)
		{
			chosenRecipeNameTextComponent.text = recipeButtonScript.CraftingRecipe.RecipeName;
			chosenRecipeDescriptionTextComponent.text = recipeButtonScript.CraftingRecipe.RecipeDescription;
			chosenRecipeIconImageComponent.sprite = recipeButtonScript.CraftingRecipe.RecipeIcon;
			ingredientsTextComponent.text = GenerateIngredientsList(recipeButtonScript.CraftingRecipe);
			toolsTextComponent.text = GenerateToolsList(recipeButtonScript.CraftingRecipe);
			reagentsTextComponent.text = GenerateReagentsList(recipeButtonScript.CraftingRecipe);
			craftButtonTextComponent.text = GenerateButtonText(recipeButtonScript.CraftingRecipe);
		}

		#endregion

		#region RecipeInfoGenerators

		private static string GenerateButtonText(CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.CraftingTime < float.Epsilon)
			{
				return "Craft";
			}

			StringBuilder stringBuilder = new StringBuilder();

			return stringBuilder
				.Append("Craft (")
				.Append(DMMath.Round(craftingRecipe.CraftingTime, 0.1))
				.Append(" sec.)").ToString();
		}

		private static string GenerateIngredientsList(CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.RequiredIngredients.Count == 0)
			{
				return "None";
			}

			StringBuilder stringBuilder = new StringBuilder();

			foreach (RecipeIngredient recipeIngredient in craftingRecipe.RequiredIngredients)
			{
				stringBuilder
					.Append("- ")
					.Append(recipeIngredient.RequiredAmount)
					.Append("x ")
					.Append(recipeIngredient.RequiredItem.ExpensiveName())
					.AppendLine()
					.AppendLine();
			}

			return stringBuilder.ToString();
		}

		private static string GenerateToolsList(CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.RequiredIngredients.Count == 0)
			{
				return "None";
			}

			StringBuilder stringBuilder = new StringBuilder();

			foreach (ItemTrait toolTrait in craftingRecipe.RequiredToolTraits)
			{
				stringBuilder
					.Append("- ")
					.Append(toolTrait.ToString())
					.AppendLine()
					.AppendLine();
			}

			return stringBuilder.ToString();
		}

		private static string GenerateReagentsList(CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.RequiredIngredients.Count == 0)
			{
				return "None";
			}

			StringBuilder stringBuilder = new StringBuilder();

			foreach (RecipeIngredientReagent ingredientReagent in craftingRecipe.RequiredReagents)
			{
				stringBuilder
					.Append("- ")
					.Append(ingredientReagent.RequiredAmount)
					.Append("u ")
					.Append(ingredientReagent.RequiredReagent.Name)
					.AppendLine()
					.AppendLine();
			}

			return stringBuilder.ToString();
		}

		#endregion

		/// <summary>
		/// Open the Crafting Menu
		/// </summary>
		public void Open(PlayerCrafting playerCrafting)
		{
			if (recipesInitIsNecessary)
			{
				InitRecipes(playerCrafting);
				DeselectRecipe(chosenRecipe);
				recipesInitIsNecessary = false;
			}
			this.SetActive(true);
		}

		/// <summary>
		/// Close the Crafting Menu
		/// </summary>
		public void Close()
		{
			this.SetActive(false);
		}

		private void InitRecipes(PlayerCrafting playerCrafting)
		{
			foreach (List<CraftingRecipe> craftingRecipesInCategory in playerCrafting.KnownRecipesByCategory)
			{
				foreach (CraftingRecipe craftingRecipe in craftingRecipesInCategory)
				{
					OnPlayerLearnedRecipe(craftingRecipe);
				}
			}
		}

		public void OnPlayerLearnedRecipe(CraftingRecipe craftingRecipe)
		{
			recipesInCategories[(int) craftingRecipe.Category].RecipeButtonScripts.Add(
				RecipeButtonScript.GenerateNew(
					recipeButtonTemplatePrefab,
					recipesGridLayout.transform,
					craftingRecipe
				).GetComponent<RecipeButtonScript>()
			);
		}

		public void OnPlayerForgotRecipe(CraftingRecipe craftingRecipe)
		{
			int recipeIndexToForgot = recipesInCategories[(int) craftingRecipe.Category].RecipeButtonScripts.FindIndex(
				recipeButtonScript => recipeButtonScript.CraftingRecipe
			);
			if (chosenRecipe != null && craftingRecipe == chosenRecipe.CraftingRecipe)
			{
				DeselectRecipe(chosenRecipe);
			}
			Destroy(recipesInCategories[(int) craftingRecipe.Category].RecipeButtonScripts[recipeIndexToForgot]);
			recipesInCategories[(int) craftingRecipe.Category].RecipeButtonScripts.RemoveAt(recipeIndexToForgot);
		}
	}
}