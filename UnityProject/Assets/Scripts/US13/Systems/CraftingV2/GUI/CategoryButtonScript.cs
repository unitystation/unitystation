using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Managers;

namespace US13.Systems.CraftingV2.GUI
{
	/// <summary>
	/// The class that handles client's clicks onto the button as a game object.
	/// </summary>
	public class CategoryButtonScript : MonoBehaviour, IPointerDownHandler
	{
		[SerializeField] [Tooltip("A pair of values: a recipe category and its icon.")]
		private CategoryAndIcon categoryAndIcon;

		[SerializeField] [Tooltip("The button will be colored to this color when the button will have be selected.")]
		private Color onPressedColor;

		[SerializeField] [Tooltip("The button will be colored to this color when the button will have be deselected.")]
		private Color onUnpressedColor;

		[SerializeField] [Tooltip("A link to a game object that contains an Image component for a recipe's icon.")]
		private GameObject categoryIconImageGameObject;

		[SerializeField] [Tooltip("A link to a game object that contains an Image component for an icon's background.")]
		private GameObject backgroundImageGameObject;

		[SerializeField]
		private TMP_Text categoryNameText;

		private Image backgroundImageComponent;

		public CategoryAndIcon CategoryAndIcon => categoryAndIcon;

		public void Awake()
		{
			backgroundImageComponent = backgroundImageGameObject.GetComponent<Image>();
			backgroundImageComponent.color = onUnpressedColor;
			if (categoryAndIcon.CategoryIcon != null)
			{
				categoryIconImageGameObject.GetComponent<Image>().sprite = categoryAndIcon.CategoryIcon;
			}
			if (categoryNameText == null)
			{
				categoryNameText = GetComponentInChildren<TMP_Text>();
			}
			if (categoryNameText != null)
			{
				categoryNameText.text = categoryAndIcon.CategoryName;
			}
		}

		public void OnPointerDown(PointerEventData data)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			CraftingMenu.Instance.ChangeCategory(this);
		}

		public void OnPressed()
		{
			if (backgroundImageComponent != null) backgroundImageComponent.color = onPressedColor;
		}

		public void OnUnpressed()
		{
			if (backgroundImageComponent != null) backgroundImageComponent.color = onUnpressedColor;
		}
	}
}