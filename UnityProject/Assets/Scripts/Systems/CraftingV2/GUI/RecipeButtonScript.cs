using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	public class RecipeButtonScript : MonoBehaviour, IPointerDownHandler
	{
		private CraftingRecipe craftingRecipe;

		private Sprite icon;

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
			GameObject result = Instantiate(recipeButtonTemplate, parentTransform);

			RecipeButtonScript recipeButtonScript = result.GetComponent<RecipeButtonScript>();

			recipeButtonScript.craftingRecipe = craftingRecipe;
			recipeButtonScript.icon = craftingRecipe.RecipeIcon;

			return result;
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