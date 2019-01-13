using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public void OnPointerEnter(PointerEventData data)
	{
		UIManager.DragAndDrop.EnteredInteractable();
	}

	public void OnPointerExit(PointerEventData data)
	{
		UIManager.DragAndDrop.ResetInteractable();
	}
}