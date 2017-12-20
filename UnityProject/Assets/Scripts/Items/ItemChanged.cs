using Events;
using UnityEngine;

namespace PlayGroup
{
	public class ItemChanged : MonoBehaviour
	{
		private ClothingItem clothingItem;
		public string eventName;
		private PlayerScript playerScript;

		private void Start()
		{
			clothingItem = GetComponent<ClothingItem>();
			playerScript = GetComponentInParent<PlayerScript>();
			EventManager.UI.AddListener(eventName, OnChanged);
		}

		private void OnChanged(GameObject item)
		{
			if (playerScript.isLocalPlayer)
			{
				//Only change the one that is mine
				ChangeItem(item);
			}
			else
			{
				//Dev mode
				ChangeItem(item);
			}
		}

		private void ChangeItem(GameObject item)
		{
			if (item)
			{
			}
			else
			{
				clothingItem.Clear();
			}
		}
	}
}