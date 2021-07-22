using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	public class CategoryButton : MonoBehaviour, IPointerDownHandler
	{
		[SerializeField]
		private CategoryAndIcon categoryAndIcon;

		[SerializeField] private Color onPressedColor;

		[SerializeField] private Color onUnpressedColor;

		[SerializeField] private GameObject categoryIconImageGameObject;

		[SerializeField] private GameObject backgroundImageGameObject;

		private Image backgroundImageComponent;

		private Image categoryIconImageComponent;

		public CategoryAndIcon CategoryAndIcon => categoryAndIcon;

		public void Awake()
		{
			backgroundImageComponent = backgroundImageGameObject.GetComponent<Image>();
			categoryIconImageComponent = categoryIconImageGameObject.GetComponent<Image>();
			backgroundImageComponent.color = onUnpressedColor;
			if (categoryAndIcon.CategoryIcon != null)
			{
				categoryIconImageComponent.sprite = categoryAndIcon.CategoryIcon;
			}
		}

		public void OnPointerDown(PointerEventData data)
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			CraftingMenu.Instance.ChangeCategory(this);
		}

		public void OnPressed()
		{
			backgroundImageComponent.color = onPressedColor;
		}

		public void OnUnpressed()
		{
			backgroundImageComponent.color = onUnpressedColor;
		}
	}
}