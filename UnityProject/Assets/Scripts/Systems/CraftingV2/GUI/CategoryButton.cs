using UnityEngine;
using UnityEngine.EventSystems;

namespace Systems.CraftingV2.GUI
{
	public class CategoryButton : MonoBehaviour, IPointerDownHandler
	{
		[SerializeField]
		private GameObject selectedLine = null;
		[SerializeField]
		[Tooltip("This is the actual window that contains all of the" +
		         " GUI buttons/inputs for this particular settings option")]
		private GameObject contentWindow = null;
		public bool IsActive => selectedLine.activeSelf;

		public void OnPointerDown(PointerEventData data)
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			CraftingMenu.Instance.ChooseCategory(this);
		}

		public void Toggle(bool activeState)
		{
			selectedLine.SetActive(activeState);
			if (contentWindow) contentWindow.SetActive(activeState);
		}
	}
}