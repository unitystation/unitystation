using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Items;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	/// <summary>
	/// 	The main crafting UI class that handles any client's input(button clicks).
	/// </summary>
	public class CraftingMenu : MonoBehaviour
	{
		/// <summary>
		/// 	The link to the crafting UI instance. Client can only have one.
		/// </summary>
		public static CraftingMenu Instance;

		[SerializeField] [Tooltip("The link to a prefab-template of a recipe button.")]
		private GameObject recipeButtonTemplatePrefab;

		[SerializeField] [Tooltip("The link to a layout as a game object that contains all category buttons.")]
		private GameObject categoriesLayoutGameObject;

		[SerializeField] [Tooltip("The link to a layout as a game object that contains all recipe buttons.")]
		private GameObject recipesLayoutGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains information about a selected recipe.")]
		private GameObject recipeInfoGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains a selected recipe's icon.")]
		private GameObject chosenRecipeIconGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains a selected recipe's name.")]
		private GameObject chosenRecipeNameGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains a selected recipe's description.")]
		private GameObject chosenRecipeDescriptionGameObject;

		[SerializeField] [Tooltip("The link to a dame object that contains information about all ingredients " +
		                          "required for a selected recipe.")]
		private GameObject ingredientsTextGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains information about all tools " +
		                          "required for a selected recipe.")]
		private GameObject toolsTextGameObject;

		[SerializeField] [Tooltip("The link to a game object that contains information about all reagents " +
		                          "required for a selected recipe.")]
		private GameObject reagentsTextGameObject;

		[SerializeField] [Tooltip("The link to a craft button as a game object.")]
		private GameObject craftButtonTextGameObject;

		[SerializeField] [Tooltip("The link to a search field as a game object.")]
		private GameObject searchFieldGameObject;

		[SerializeField, ReorderableList] [Tooltip("A list of category buttons that will be displayed in " +
		                                           "the crafting menu.")]
		private List<GameObject> categoryButtonsPrefabs;

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

		// the field used to prepare search field's content.
		// The regex means "Any symbol that isn't a number or a word character."
		private readonly Regex preSearchRegex = new Regex("[^\\w\\s]");

		#region Lifecycle

		public void Awake()
		{
			if (Instance != null)
			{
				return;
			}
			InitFields();
			InitCategories();
			InitRecipes();
			recipeInfoGameObject.SetActive(false);
		}

		private void InitRecipes()
		{
			foreach (List<CraftingRecipe> recipesInCategory
				in PlayerManager.LocalPlayerScript.PlayerCrafting.KnownRecipesByCategory)
			{
				foreach (CraftingRecipe craftingRecipe in recipesInCategory)
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
					Loggy.LogError("An attempt to create two same categories in a crafting menu. " +
					                $"The duplicated category: {categoryButtonScript.CategoryAndIcon.RecipeCategory}");
					continue;
				}
				recipesInCategories[(int) categoryButtonScript.CategoryAndIcon.RecipeCategory] =
					new RecipesInCategory(categoryButtonScript);
			}

			CheckCategoriesCompleteness();
			SelectCategory(recipesInCategories[0].CategoryButtonScript);
		}

		// at the moment all categories should be present to a player
		private void CheckCategoriesCompleteness()
		{
			for (int i = 0; i < recipesInCategories.Length; i++)
			{
				if (recipesInCategories[i] == null)
				{
					Loggy.LogError($"The crafting menu is missing the category: {(RecipeCategory) i}.");
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

		/// <summary>
		/// 	Deselects a chosen category or, if it's null, deselects all categories.
		/// 	Selects the new category.
		/// </summary>
		/// <param name="categoryButtonScript">The category button to select.</param>
		public void ChangeCategory(CategoryButtonScript categoryButtonScript)
		{
			if (chosenCategory == null)
			{
				// ok we don't know where we should clear recipe buttons, so let's clear all possible recipe buttons.
				foreach (RecipesInCategory recipesInCategory in recipesInCategories)
				{
					DeselectCategory(recipesInCategory.CategoryButtonScript);
				}
			}
			else
			{
				// ok we know where we should clear recipe buttons.
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

		/// <summary>
		/// 	Deselects a chosen recipe.
		/// 	Selects the new recipe.
		/// </summary>
		/// <param name="recipeButtonScript">The recipe to select.</param>
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
			if (craftingRecipe.CraftingTime.Approx(0))
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

		#region OtherButtonPressingHandlers

		/// <summary>
		/// 	Handles a player's search button pressing.
		/// </summary>
		public void OnSearchButtonClicked()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ApplySearchFilters();
		}

		/// <summary>
		/// 	Handles a player's refresh button pressing.
		/// </summary>
		public void OnRefreshRecipesButtonClicked()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			RequestRefreshRecipes();
		}

		/// <summary>
		/// 	Handles a player's craft button pressing.
		/// </summary>
		public void OnCraftButtonPressed()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			PlayerManager.LocalPlayerScript.PlayerCrafting.TryToStartCrafting(
				chosenRecipe.CraftingRecipe,
				NetworkSide.Client,
				CraftingActionParameters.DefaultParameters
			);
		}

		#endregion

		/// <summary>
		/// 	Get all recipes in the category.
		/// </summary>
		/// <param name="recipeCategory">The category to get recipes from.</param>
		/// <returns>All recipes in the category.</returns>
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

			foreach (var categoryButtons in recipesInCategories)
			{
				categoryButtons.CategoryButtonScript.gameObject.SetActive(categoryButtons.RecipeButtonScripts.Count > 0);
			}

			RequestRefreshRecipes();
		}

		/// <summary>
		/// Close the Crafting Menu
		/// </summary>
		public void Close()
		{
			this.SetActive(false);
		}

		/// <summary>
		/// 	Generates a new recipe button in craftingRecipe.Category.
		/// </summary>
		/// <param name="craftingRecipe">The associated crafting recipe.</param>
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

			var category = GetRecipesInCategory(craftingRecipe.Category);

			category.RecipeButtonScripts.Add(newRecipeButton.GetComponent<RecipeButtonScript>());
			category.CategoryButtonScript.gameObject.SetActive(true);
		}

		/// <summary>
		/// 	Removes a recipe button from a craftingRecipe.Category.
		/// </summary>
		/// <param name="craftingRecipe">The associated crafting recipe.</param>
		public void OnPlayerForgotRecipe(CraftingRecipe craftingRecipe)
		{
			var category = GetRecipesInCategory(craftingRecipe.Category);

			int recipeIndexToForgot = category.RecipeButtonScripts.FindIndex(
				recipeButtonScript => recipeButtonScript.CraftingRecipe
			);

			if (chosenRecipe != null && craftingRecipe == chosenRecipe.CraftingRecipe)
			{
				DeselectRecipe(chosenRecipe);
			}

			Destroy(category.RecipeButtonScripts[recipeIndexToForgot].gameObject);
			category.RecipeButtonScripts.RemoveAt(recipeIndexToForgot);

			category.CategoryButtonScript.gameObject.SetActive(category.RecipeButtonScripts.Count > 0);
		}

		/// <summary>
		/// Removes all recipe buttons
		/// </summary>
		public void OnPlayerForgetAllRecipes()
		{
			foreach (var recipesInCategory in recipesInCategories)
			{
				foreach (var recipeButton in recipesInCategory.RecipeButtonScripts)
				{
					if (chosenRecipe != null && recipeButton.CraftingRecipe == chosenRecipe.CraftingRecipe)
					{
						DeselectRecipe(chosenRecipe);
					}

					Destroy(recipeButton.gameObject);
				}
			}

			foreach (var recipesInCategory in recipesInCategories)
			{
				recipesInCategory.RecipeButtonScripts.Clear();
			}
		}

		/// <summary>
		/// 	Requests a server to refresh craftable recipes.
		/// </summary>
		public void RequestRefreshRecipes()
		{
			DeselectRecipe(chosenRecipe);
			ClientServerLogic.RequestRefreshRecipes.Send();
		}

		/// <summary>
		/// 	Refreshes craftable recipes according to the possible ingredients and tools.
		/// 	This method assumes that a player is already able to craft at all.
		/// </summary>
		/// <param name="possibleIngredients">The ingredients that may be used for crafting.</param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="possibleReagents">
		/// 	The reagents(a pair of values: a reagent's index in the singleton and its amount)
		/// 	that may be used for crafting.
		/// </param>
		public void RefreshRecipes(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<KeyValuePair<int, float>> possibleReagents
		)
		{
			foreach (RecipesInCategory recipesInCategory in recipesInCategories)
			{
				foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
				{
					recipeButtonScript.RefreshCraftable(possibleIngredients, possibleTools, possibleReagents);
				}
			}
		}

		public void SetAllRecipesUncraftable()
		{
			foreach (RecipesInCategory recipesInCategory in recipesInCategories)
			{
				foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
				{
					recipeButtonScript.SetUncraftableBorderColor();
				}
			}
		}

		/// <summary>
		/// 	Handles a search command, edits player's search request if necessary, applies search filters,
		/// 	shows recipes that match the search request.
		/// </summary>
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
	}
}