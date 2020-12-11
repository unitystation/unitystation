using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HasSubInventory : MonoBehaviour, IPointerClickHandler
{

	public ItemStorage itemStorage;

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
	{
		UIManager.StorageHandler.OpenStorageUI(itemStorage);
	}
}