using System.Collections.Generic;
using Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	/// <summary>
	/// 	The class that handles a recipe button pressing action.
	/// </summary>
	public class RecipeButtonScript : MonoBehaviour, IPointerDownHandler
	{
		private CraftingRecipe craftingRecipe;

		private Sprite recipeIcon;

		public Sprite RecipeIcon => recipeIcon;

		[SerializeField] [Tooltip("The recipe button will be bordered with this color when a player can't craft " +
		                          "according to the recipe.")]
		private Color uncraftableColor;

		[SerializeField] [Tooltip("The recipe button will be bordered with this color when a player can craft " +
		                          "according to the recipe.")]
		private Color craftableColor;

		[SerializeField] [Tooltip("The recipe button will be colored to this color when a player selects the recipe.")]
		private Color selectedColor;

		[SerializeField] [Tooltip("The recipe button will be colored to this color when a player deselects the recipe.")]
		private Color deselectedColor;

		[SerializeField] [Tooltip("The link to an icon's background as a game object.")]
		private GameObject backgroundGameObject;

		[SerializeField] [Tooltip("The link to a background's border as a game object.")]
		private GameObject borderGameObject;

		[SerializeField] [Tooltip("The link to a recipe icon as a game object.")]
		private GameObject iconGameObject;

		[SerializeField] [Tooltip("The link to a recipe name as a game object.")]
		private GameObject textGameObject;

		private Image backgroundImageComponent;

		private Image borderImageComponent;

		private Image iconImageComponent;

		private Text recipeNameTextComponent;

		public CraftingRecipe CraftingRecipe => craftingRecipe;

		/// <summary>
		/// 	Generates new recipe button with unique crafting recipe and its icon.
		/// </summary>
		/// <param name="recipeButtonTemplate">The template to create button from.</param>
		/// <param name="parentTransform">The place(as a transform) where the button will be stored.</param>
		/// <param name="craftingRecipe">The crafting recipe associated with the button.</param>
		/// <returns>A new recipe button.</returns>
		public static GameObject GenerateNew(
			GameObject recipeButtonTemplate,
			Transform parentTransform,
			CraftingRecipe craftingRecipe
		)
		{
			// instantiate a blank crafting button
			GameObject generatedButton = Instantiate(recipeButtonTemplate, parentTransform);

			RecipeButtonScript recipeButtonScript = generatedButton.GetComponent<RecipeButtonScript>();

			// we can't move this to the Awake() event because the button can be inactive, so Awake() will not be called
			recipeButtonScript.backgroundImageComponent = recipeButtonScript.backgroundGameObject.GetComponent<Image>();
			recipeButtonScript.borderImageComponent = recipeButtonScript.borderGameObject.GetComponent<Image>();
			recipeButtonScript.iconImageComponent = recipeButtonScript.iconGameObject.GetComponent<Image>();
			recipeButtonScript.recipeNameTextComponent = recipeButtonScript.textGameObject.GetComponent<Text>();

			recipeButtonScript.craftingRecipe = craftingRecipe;
			// should we use different, overrided icon?
			if (craftingRecipe.RecipeIconOverride != null)
			{
				recipeButtonScript.recipeIcon = craftingRecipe.RecipeIconOverride;
			}
			else
			{
				// ok, let's just try to use an icon of a result game object
				SpriteRenderer spriteRenderer;
				foreach (GameObject resultPart in craftingRecipe.Result)
				{
					spriteRenderer = resultPart.GetComponentInChildren<SpriteRenderer>();
					if (spriteRenderer == null || spriteRenderer.sprite == null)
					{
						continue;
					}
					recipeButtonScript.recipeIcon = spriteRenderer.sprite;
					break;
				}
			}

			recipeButtonScript.iconImageComponent.sprite = recipeButtonScript.recipeIcon;
			recipeButtonScript.backgroundImageComponent.color = recipeButtonScript.deselectedColor;
			recipeButtonScript.borderImageComponent.color = recipeButtonScript.uncraftableColor;
			recipeButtonScript.recipeNameTextComponent.text = craftingRecipe.RecipeName;

			return generatedButton;
		}

		/// <summary>
		/// 	Changes the background color to the selectedColor.
		/// </summary>
		public void OnPressed()
		{
			backgroundImageComponent.color = selectedColor;
		}

		/// <summary>
		/// 	Changes the background color to the deslectedColor.
		/// </summary>
		public void OnUnpressed()
		{
			backgroundImageComponent.color = deselectedColor;
		}

		/// <summary>
		/// 	Changes the recipe button border color to the craftableColor.
		/// </summary>
		public void SetCraftableBorderColor()
		{
			borderImageComponent.color = craftableColor;
		}

		/// <summary>
		/// 	Changes the recipe button border color to the uncraftableColor.
		/// </summary>
		public void SetUncraftableBorderColor()
		{
			borderImageComponent.color = uncraftableColor;
		}

		/// <summary>
		/// 	Handles a player's click on this button(changes the CraftingMenu's chosen recipe).
		/// </summary>
		/// <param name="eventData">Ignored.</param>
		public void OnPointerDown(PointerEventData eventData)
		{
			CraftingMenu.Instance.ChangeRecipe(this);
		}

		/// <summary>
		/// 	Updates the recipe button border color.
		/// </summary>
		/// <param name="possibleIngredients">The ingredients that may be used for crafting.</param>
		/// <param name="possibleTools">The tools that may be used for crafting.</param>
		/// <param name="possibleReagents">
		/// 	The reagents(a pair of values: a regent's index in the sigleton and its amount)
		/// </param>
		public void RefreshCraftable(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<KeyValuePair<int, float>> possibleReagents
		)
		{
			if (
				PlayerManager.LocalPlayerScript.PlayerCrafting.KnowsRecipe(craftingRecipe)
				&& craftingRecipe.CanBeCrafted(
					possibleIngredients,
					possibleTools,
					possibleReagents
				) == CraftingStatus.AllGood
			)
			{
				SetCraftableBorderColor();
				return;
			}

			SetUncraftableBorderColor();
		}
	}
}