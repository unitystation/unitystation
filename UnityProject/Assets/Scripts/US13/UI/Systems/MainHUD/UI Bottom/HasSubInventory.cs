using UnityEngine;
using UnityEngine.EventSystems;
using US13.Systems.Inventory;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class HasSubInventory : MonoBehaviour, IPointerClickHandler
	{

		public ItemStorage itemStorage;

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
		{
			UIManager.StorageHandler.OpenStorageUI(itemStorage);
		}
	}
}