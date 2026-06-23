using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Logs;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Core.Input_System.InteractionV2;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.Player;
using US13.UI.Core;
using Util;

namespace US13.Systems.CraftingV2.GUI
{
	/// <summary>
	/// The main crafting UI class that handles any client's input(button clicks).
	/// </summary>
	public class CraftingMenu : MonoBehaviour
	{
		/// <summary>
		/// The link to the crafting UI instance. Client can only have one.
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

		[SerializeField]
		private GameObject everythingButtonPrefab;
		private CategoryButtonScript everythingButtonScript;

		private readonly List<RecipesInCategory> recipesInCategories = new();

		private GridLayoutGroup recipesGridLayout;

		private TMP_Text chosenRecipeNameTextComponent;

		private TMP_Text chosenRecipeDescriptionTextComponent;

		private Image chosenRecipeIconImageComponent;

		private TMP_Text ingredientsTextComponent;

		private TMP_Text toolsTextComponent;

		private TMP_Text reagentsTextComponent;

		private TMP_Text craftButtonTextComponent;

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
			chosenRecipeNameTextComponent = chosenRecipeNameGameObject.GetComponent<TMP_Text>();
			chosenRecipeDescriptionTextComponent = chosenRecipeDescriptionGameObject.GetComponent<TMP_Text>();
			chosenRecipeIconImageComponent = chosenRecipeIconGameObject.GetComponent<Image>();
			ingredientsTextComponent = ingredientsTextGameObject.GetComponent<TMP_Text>();
			toolsTextComponent = toolsTextGameObject.GetComponent<TMP_Text>();
			reagentsTextComponent = reagentsTextGameObject.GetComponent<TMP_Text>();
			craftButtonTextComponent = craftButtonTextGameObject.GetComponent<TMP_Text>();
			searchFieldComponent = searchFieldGameObject.GetComponent<InputFieldFocus>();
		}

		private void InitCategories()
		{
			foreach (GameObject categoryButtonPrefab in categoryButtonsPrefabs)
			{
				SpawnCategoryButton(categoryButtonPrefab);
			}

			//optional special button (e.g. Everything/Craftable)
			if (everythingButtonPrefab != null)
			{
				SpawnCategoryButton(everythingButtonPrefab);
			}

			recipesInCategories.Sort((a, b) =>
			{
				var aCai = a.CategoryButtonScript.CategoryAndIcon;
				var bCai = b.CategoryButtonScript.CategoryAndIcon;
				bool aIsEverything = aCai.FilterKind == CategoryFilterKind.Everything;
				bool bIsEverything = bCai.FilterKind == CategoryFilterKind.Everything;
				if (aIsEverything && !bIsEverything) return -1;
				if (!aIsEverything && bIsEverything) return 1;
				return string.Compare(aCai.CategoryName, bCai.CategoryName, StringComparison.OrdinalIgnoreCase);
			});

			for (int i = 0; i < recipesInCategories.Count; i++)
			{
				recipesInCategories[i].CategoryButtonScript.gameObject.transform.SetSiblingIndex(i);
			}
			CheckCategoriesCompleteness();
			if (recipesInCategories.Count > 0)
			{
				SelectCategory(recipesInCategories[0].CategoryButtonScript);
			}
		}

		private void SpawnCategoryButton(GameObject categoryButtonPrefab)
		{
			GameObject initiatedCategoryButtonGameObject = Instantiate(
				categoryButtonPrefab,
				categoriesLayoutGameObject.transform
			);
			CategoryButtonScript categoryButtonScript =
				initiatedCategoryButtonGameObject.GetComponent<CategoryButtonScript>();

			foreach (var existing in recipesInCategories)
			{
				var existingCAI = existing.CategoryButtonScript.CategoryAndIcon;
				var newCAI = categoryButtonScript.CategoryAndIcon;
				if (existingCAI.FilterKind == newCAI.FilterKind &&
					(newCAI.FilterKind != CategoryFilterKind.ByEnum || existingCAI.RecipeCategory == newCAI.RecipeCategory))
				{
					Loggy.Error("An attempt to create two same categories in a crafting menu. " +
						$"The duplicated category: {newCAI.CategoryName}");
					Destroy(initiatedCategoryButtonGameObject);
					return;
				}
			}
			recipesInCategories.Add(new RecipesInCategory(categoryButtonScript));
		}

		// at the moment all categories should be present to a player
		private void CheckCategoriesCompleteness()
		{
			foreach (RecipeCategory rc in System.Enum.GetValues(typeof(RecipeCategory)))
			{
				bool found = recipesInCategories.Exists(r => r.CategoryButtonScript.CategoryAndIcon.FilterKind == CategoryFilterKind.ByEnum && r.CategoryButtonScript.CategoryAndIcon.RecipeCategory == rc);
				if (!found)
				{
					Loggy.Error($"The crafting menu is missing the category: {rc}.");
				}
			}
		}

		#endregion

		#region CategoryAndRecipeSelections

		private void SelectCategory(CategoryButtonScript categoryButtonScript)
		{
			if (categoryButtonScript == null)
			{
				Loggy.Error("An attempt to select a null category in a crafting menu.");
				return;
			}
			categoryButtonScript.OnPressed();
			chosenCategory = categoryButtonScript;
			if (chosenCategory.CategoryAndIcon.FilterKind == CategoryFilterKind.Everything)
			{
				foreach (RecipesInCategory recipesInCategory in recipesInCategories)
				{
					foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
					{
						recipeButtonScript.gameObject.SetActive(true);
					}
				}
				return;
			}
			foreach (RecipeButtonScript recipeButtonScript in
				GetRecipesInCategory(categoryButtonScript.CategoryAndIcon.RecipeCategory).RecipeButtonScripts
			)
			{
				recipeButtonScript.gameObject.SetActive(true);
			}
		}

		private void SelectCategory(string catagoryName)
		{
			foreach (var ric in recipesInCategories)
			{
				if (ric.CategoryButtonScript.CategoryAndIcon.CategoryName == catagoryName)
				{
					SelectCategory(ric.CategoryButtonScript);
					return;
				}
			}
			if (recipesInCategories.Count > 0) SelectCategory(recipesInCategories[0].CategoryButtonScript);
		}

		private void DeselectCategory(CategoryButtonScript categoryButtonScript)
		{
			if (categoryButtonScript == null)
			{
				return;
			}
			categoryButtonScript.OnUnpressed();
			var cai = categoryButtonScript.CategoryAndIcon;
			if (cai.FilterKind == CategoryFilterKind.Everything)
			{
				foreach (RecipesInCategory recipesInCategory in recipesInCategories)
				{
					foreach (RecipeButtonScript recipeButtonScript in recipesInCategory.RecipeButtonScripts)
					{
						recipeButtonScript.gameObject.SetActive(false);
					}
				}
			}
			else if (cai.FilterKind == CategoryFilterKind.ByEnum)
			{
				var ric = GetRecipesInCategory(cai.RecipeCategory);
				if (ric != null)
				{
					foreach (var recipeButtonScript in ric.RecipeButtonScripts)
					{
						recipeButtonScript.gameObject.SetActive(false);
					}
				}
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

		public void OnSearchButtonClicked()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ApplySearchFilters();
		}

		public void OnRefreshRecipesButtonClicked()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			RequestRefreshRecipes();
		}

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
		/// Get all recipes in the category.
		/// </summary>
		/// <param name="recipeCategory">The category to get recipes from.</param>
		/// <returns>All recipes in the category.</returns>
		private RecipesInCategory GetRecipesInCategory(RecipeCategory recipeCategory)
		{
			var found = recipesInCategories.Find(r =>
				r.CategoryButtonScript.CategoryAndIcon.FilterKind == CategoryFilterKind.ByEnum && r.CategoryButtonScript.CategoryAndIcon.RecipeCategory == recipeCategory);
			if (found == null)
			{
				Loggy.Error($"The crafting menu is missing the category: {recipeCategory}.");
			}
			return found;
		}

		public void Open()
		{
			this.SetActive(true);

			foreach (var categoryButtons in recipesInCategories)
			{
				categoryButtons.CategoryButtonScript.gameObject.SetActive(categoryButtons.RecipeButtonScripts.Count > 0);
			}

			RequestRefreshRecipes();
			SelectCategory(chosenCategory ?? recipesInCategories[0].CategoryButtonScript);
		}

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

			//add the new recipe button to any category whose filter matches the recipe (ByEnum match or aggregate categories)
			foreach (var ric in recipesInCategories)
			{
				var cai = ric.CategoryButtonScript.CategoryAndIcon;
				bool matches = false;
				if (cai.FilterKind == CategoryFilterKind.ByEnum && cai.RecipeCategory == craftingRecipe.Category) matches = true;
				//aggregate categories include all recipes; visibility decided elsewhere
				if (cai.FilterKind is CategoryFilterKind.Everything or CategoryFilterKind.Craftable) matches = true;
				if (matches)
				{
					ric.RecipeButtonScripts.Add(newRecipeButton.GetComponent<RecipeButtonScript>());
					ric.CategoryButtonScript.gameObject.SetActive(true);
					if (chosenCategory == null || ric.CategoryButtonScript != chosenCategory) newRecipeButton.SetActive(false);
				}
			}
		}

		/// <summary>
		/// Removes a recipe button from a craftingRecipe.Category.
		/// </summary>
		/// <param name="craftingRecipe">The associated crafting recipe.</param>
		public void OnPlayerForgotRecipe(CraftingRecipe craftingRecipe)
		{
			if (chosenRecipe != null && craftingRecipe == chosenRecipe.CraftingRecipe)
			{
				DeselectRecipe(chosenRecipe);
			}

			foreach (var ric in recipesInCategories)
			{
				int idx = ric.RecipeButtonScripts.FindIndex(r => r.CraftingRecipe == craftingRecipe);
				if (idx >= 0)
				{
					Destroy(ric.RecipeButtonScripts[idx].gameObject);
					ric.RecipeButtonScripts.RemoveAt(idx);
					ric.CategoryButtonScript.gameObject.SetActive(ric.RecipeButtonScripts.Count > 0);
				}
			}
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
		/// Handles a search command, edits player's search request if necessary, applies search filters,
		/// shows recipes that match the search request.
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