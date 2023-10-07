using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class FaithPropertyHoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private IFaithProperty property;
		private ChaplainFirstTimeSelectScreen father;
		[SerializeField] private Image icon;
		[SerializeField] private TMP_Text propertyName;

		public void Setup(IFaithProperty newProperty, ChaplainFirstTimeSelectScreen screen)
		{
			property = newProperty;
			father = screen;
			icon.sprite = newProperty.PropertyIcon;
			propertyName.text = newProperty.FaithPropertyName;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			father.SetDesc($"{property.FaithPropertyName}\n\n\n{property.FaithPropertyDesc}");
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			father.SetDesc(father.UnfocusedDescText);
		}
	}
}