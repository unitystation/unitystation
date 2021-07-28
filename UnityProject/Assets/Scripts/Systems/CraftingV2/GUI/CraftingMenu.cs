using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

		[SerializeField] private GameObject searchFieldGameObject;

		[SerializeField, ReorderableList] private List<GameObject> categoryButtonsPrefabs;

		private readonly RecipesInCategory[] recipesInCategories =
			new RecipesInCategory[Enum.GetValues(typeof(RecipeCategory)).Length];

		private GridLayoutGroup recipesGridLayout;

		private Text chosenRecipeNameTextComponent;

		private Text chosenRecipeDescriptionTextComponent;

		private Image chosenRecipeIconImageComponent;

		private Text ingredientsTextComponent;

		private Text toolsTextComponent;

		private Text reagentsTextComponent;

		private Text craftButtonTextComponent;

		private InputFieldFocus searchFieldComponent;

		private CategoryButtonScript chosenCategory;

		private RecipeButtonScript chosenRecipe;

		private readonly Regex preSearchRegex = new Regex("[^\\w\\s]");

		#region Lifecycle

		void Awake()
		{
			if (Instance == null)
			{
				InitFields();
				InitCategories();
				InitRecipes(PlayerManager.LocalPlayerScript.PlayerCrafting);
				recipeInfoGameObject.SetActive(false);
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
			chosenRecipeNameTextComponent = chosenRecipeNameGameObject.GetComponent<Text>();
			chosenRecipeDescriptionTextComponent = chosenRecipeDescriptionGameObject.GetComponent<Text>();
			chosenRecipeIconImageComponent = chosenRecipeIconGameObject.GetComponent<Image>();
			ingredientsTextComponent = ingredientsTextGameObject.GetComponent<Text>();
			toolsTextComponent = toolsTextGameObject.GetComponent<Text>();
			reagentsTextComponent = reagentsTextGameObject.GetComponent<Text>();
			craftButtonTextComponent = craftButtonTextGameObject.GetComponent<Text>();
			searchFieldComponent = searchFieldGameObject.GetComponent<InputFieldFocus>();
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
			if (categoryButtonScript == null)
			{
				return;
			}

			categoryButtonScript.OnUnpressed();
			foreach (RecipeButtonScript recipeButtonScript in
				GetRecipesInCategory(categoryButtonScript.CategoryAndIcon.RecipeCategory).RecipeButtonScripts
			)
			{
				recipeButtonScript.gameObject.SetActive(false);
			}

			chosenCategory = null;
		}

		public void ChangeCategory(CategoryButtonScript categoryButtonScript)
		{
			if (chosenCategory == null)
			{
				foreach (RecipesInCategory recipesInCategory in recipesInCategories)
				{
					DeselectCategory(recipesInCategory.CategoryButtonScript);
				}
			}
			else
			{
				DeselectCategory(chosenCategory);
			}

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
			if (recipeButtonScript == null)
			{
				return;
			}
			recipeButtonScript.OnUnpressed();
			chosenRecipe = null;
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
					.Append(toolTrait.name)
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
			GameObject newRecipeButton = RecipeButtonScript.GenerateNew(
				recipeButtonTemplatePrefab,
				recipesGridLayout.transform,
				craftingRecipe
			);

			if (chosenCategory.CategoryAndIcon.RecipeCategory != craftingRecipe.Category)
			{
				newRecipeButton.SetActive(false);
			}

			GetRecipesInCategory(craftingRecipe.Category)
				.RecipeButtonScripts
				.Add(newRecipeButton.GetComponent<RecipeButtonScript>());
		}

		public void OnPlayerForgotRecipe(CraftingRecipe craftingRecipe)
		{
			int recipeIndexToForgot = GetRecipesInCategory(craftingRecipe.Category).RecipeButtonScripts.FindIndex(
				recipeButtonScript => recipeButtonScript.CraftingRecipe
			);
			if (chosenRecipe != null && craftingRecipe == chosenRecipe.CraftingRecipe)
			{
				DeselectRecipe(chosenRecipe);
			}

			Destroy(GetRecipesInCategory(craftingRecipe.Category).RecipeButtonScripts[recipeIndexToForgot]);
			GetRecipesInCategory(craftingRecipe.Category).RecipeButtonScripts.RemoveAt(recipeIndexToForgot);
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

		public void ApplySearchFilters()
		{
			if (searchFieldComponent.text.Length == 0)
			{
				return;
			}

			DeselectCategory(chosenCategory);
			DeselectRecipe(chosenRecipe);

			searchFieldComponent.text = preSearchRegex.Replace(
				searchFieldComponent.text,
				""
			).ToLower();

			Regex searchRegex = new Regex(searchFieldComponent.text);

			foreach (RecipesInCategory recipesInCategory in recipesInCategories)
			{
				foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
				{
					if (searchRegex.IsMatch(recipeButtonScript.CraftingRecipe.RecipeName.ToLower()))
					{
						recipeButtonScript.gameObject.SetActive(true);
						continue;
					}
					recipeButtonScript.gameObject.SetActive(false);
				}
			}
		}

		public void OnSearchButtonClicked()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			ApplySearchFilters();
		}

		public void OnRefreshRecipesButtonClicked()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			RefreshRecipes();
		}

		public void OnCraftButtonPressed()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			RequestCraft.Send(chosenRecipe.CraftingRecipe);
		}
	}
}