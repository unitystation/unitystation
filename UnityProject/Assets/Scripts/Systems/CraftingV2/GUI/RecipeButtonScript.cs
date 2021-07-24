using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	public class RecipeButtonScript : MonoBehaviour, IPointerDownHandler
	{
		private CraftingRecipe craftingRecipe;

		private Sprite recipeIcon;

		public Sprite RecipeIcon => recipeIcon;

		[SerializeField] private Color uncraftableColor;

		[SerializeField] private Color craftableColor;

		[SerializeField] private Color selectedColor;

		[SerializeField] private Color deselectedColor;

		[SerializeField] private GameObject backgroundGameObject;

		[SerializeField] private GameObject borderGameObject;

		[SerializeField] private GameObject iconGameObject;

		private Image backgroundImageComponent;

		private Image borderImageComponent;

		private Image iconImageComponent;

		public CraftingRecipe CraftingRecipe => craftingRecipe;

		public void Awake()
		{
			backgroundImageComponent = backgroundGameObject.GetComponent<Image>();
			borderImageComponent = borderGameObject.GetComponent<Image>();
			iconImageComponent = iconGameObject.GetComponent<Image>();
		}

		public static GameObject GenerateNew(
			GameObject recipeButtonTemplate,
			Transform parentTransform,
			CraftingRecipe craftingRecipe
		)
		{
			GameObject generatedButton = Instantiate(recipeButtonTemplate, parentTransform);

			RecipeButtonScript recipeButtonScript = generatedButton.GetComponent<RecipeButtonScript>();

			recipeButtonScript.craftingRecipe = craftingRecipe;
			if (craftingRecipe.RecipeIconOverride != null)
			{
				recipeButtonScript.recipeIcon = craftingRecipe.RecipeIconOverride;
			}
			else
			{
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

			return generatedButton;
		}

		public void OnPressed()
		{
			backgroundImageComponent.color = selectedColor;
		}

		public void OnUnpressed()
		{
			backgroundImageComponent.color = deselectedColor;
		}

		public void SetCraftableBorderColor()
		{
			borderImageComponent.color = craftableColor;
		}

		public void SetUncraftableBorderColor()
		{
			borderImageComponent.color = uncraftableColor;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			CraftingMenu.Instance.ChangeRecipe(this);
		}
	}
}