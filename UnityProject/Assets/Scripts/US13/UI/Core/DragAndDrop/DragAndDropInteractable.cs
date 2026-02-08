using UnityEngine;
using UnityEngine.EventSystems;
using US13.UI.Systems;

namespace US13.UI.Core.DragAndDrop
{
	public class DragAndDropInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public void OnPointerEnter(PointerEventData data)
		{
			UIManager.UiDragAndDrop.EnteredInteractable();
		}

		public void OnPointerExit(PointerEventData data)
		{
			UIManager.UiDragAndDrop.ResetInteractable();
		}
	}
}