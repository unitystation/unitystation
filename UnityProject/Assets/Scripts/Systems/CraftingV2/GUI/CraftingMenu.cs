using System;
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

		private readonly RecipesInCategory[] recipesInCategories =
			new RecipesInCategory[Enum.GetValues(typeof(RecipeCategory)).Length];

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

		#region Lifecycle

		void Awake()
		{
			if (Instance == null)
			{
				InitFields();
				InitCategories();
				InitRecipes(PlayerManager.LocalPlayerScript.PlayerCrafting);
				DeselectRecipe(chosenRecipe);
				return;
			}

			Destroy(gameObject);
		}

		private void InitRecipes(PlayerCrafting playerCrafting)
		{
			foreach (RecipesInCategory recipesInCategory in recipesInCategories)
			{
				foreach (CraftingRecipe craftingRecipe in playerCrafting.GetKnownRecipesInCategory(
					recipesInCategory.CategoryButtonScript.CategoryAndIcon.RecipeCategory
				))
				{
					OnPlayerLearnedRecipe(craftingRecipe);
				}
			}
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
				CategoryButtonScript categoryButtonScript =
					initiatedCategoryButtonGameObject.GetComponent<CategoryButtonScript>();
				if (GetRecipesInCategory(categoryButtonScript.CategoryAndIcon.RecipeCategory) != null)
				{
					Logger.LogError("An attempt to create two same categories in a crafting menu. " +
					                $"The duplicated category: {categoryButtonScript.CategoryAndIcon.RecipeCategory}");
					continue;
				}
				recipesInCategories[(int) categoryButtonScript.CategoryAndIcon.RecipeCategory] =
					new RecipesInCategory(categoryButtonScript);
			}

			CheckCategoriesCompleteness();
			SelectCategory(recipesInCategories[0].CategoryButtonScript);
		}

		private void CheckCategoriesCompleteness()
		{
			for (int i = 0; i < recipesInCategories.Length; i++)
			{
				if (recipesInCategories[i] == null)
				{
					Logger.LogError($"The crafting menu is missing the category: {(RecipeCategory) i}.");
				}
			}
		}

		#endregion

		#region CategoryAndRecipeSelections

		private void SelectCategory(CategoryButtonScript categoryButtonScript)
		{
			categoryButtonScript.OnPressed();
			chosenCategory = categoryButtonScript;
			foreach (RecipeButtonScript recipeButtonScript in
				GetRecipesInCategory(categoryButtonScript.CategoryAndIcon.RecipeCategory).RecipeButtonScripts
			)
			{
				recipeButtonScript.gameObject.SetActive(true);
			}
		}

		private void DeselectCategory(CategoryButtonScript categoryButtonScript)
		{
			categoryButtonScript.OnUnpressed();
			foreach (RecipeButtonScript recipeButtonScript in
				GetRecipesInCategory(categoryButtonScript.CategoryAndIcon.RecipeCategory).RecipeButtonScripts
			)
			{
				recipeButtonScript.gameObject.SetActive(false);
			}
		}

		public void ChangeCategory(CategoryButtonScript categoryButtonScript)
		{
			DeselectCategory(chosenCategory);
			DeselectRecipe(chosenRecipe);
			SelectCategory(categoryButtonScript);
		}

		private void SelectRecipe(RecipeButtonScript recipeButtonScript)
		{
			recipeButtonScript.OnPressed();
			FillRecipeInfo(recipeButtonScript);
			chosenRecipe = recipeButtonScript;
			recipeInfoGameObject.SetActive(true);
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
			DeselectRecipe(chosenRecipe);
			SelectRecipe(recipeButtonScript);
		}

		private void FillRecipeInfo(RecipeButtonScript recipeButtonScript)
		{
			chosenRecipeNameTextComponent.text = recipeButtonScript.CraftingRecipe.RecipeName;
			chosenRecipeDescriptionTextComponent.text = recipeButtonScript.CraftingRecipe.RecipeDescription;
			chosenRecipeIconImageComponent.sprite = recipeButtonScript.RecipeIcon;
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
			if (craftingRecipe.RequiredToolTraits.Count == 0)
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
			if (craftingRecipe.RequiredReagents.Count == 0)
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

		public RecipesInCategory GetRecipesInCategory(RecipeCategory recipeCategory)
		{
			return recipesInCategories[(int) recipeCategory];
		}

		/// <summary>
		/// Open the Crafting Menu
		/// </summary>
		public void Open()
		{
			this.SetActive(true);
			RefreshRecipes();
		}

		/// <summary>
		/// Close the Crafting Menu
		/// </summary>
		public void Close()
		{
			this.SetActive(false);
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

		public void RefreshRecipes()
		{
			DeselectRecipe(chosenRecipe);
			foreach (RecipesInCategory recipesInCategory in recipesInCategories)
			{
				foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
				{
					recipeButtonScript.RefreshCraftable(PlayerManager.LocalPlayerScript.PlayerCrafting);
				}
			}
		}

		public void OnRefreshRecipesButtonClicked()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			RefreshRecipes();
		}

		public void OnCraftButtonPressed()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			PlayerManager.LocalPlayerScript.PlayerCrafting.TryToStartCrafting(chosenRecipe.CraftingRecipe);
		}
	}
}