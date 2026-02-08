using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.UI.Systems.AdminTools.DevTools.Search;
using Util;

namespace US13.UI.Systems.AdminTools
{
	public class AdminGiveItemEntry : MonoBehaviour
	{
		public DevSpawnerDocument doc;
		private AdminGiveItem itemWindow;

		[SerializeField] private TMP_Text itemName;
		[SerializeField] private Image itemIcon;

		public void Initialize(DevSpawnerDocument document, AdminGiveItem window)
		{
			itemWindow = window;
			doc = document;
			itemName.text = document.Name.Capitalize();
			Sprite toUse = doc.Prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
			if (toUse != null) itemIcon.sprite = toUse;
		}

		public void OncClick()
		{
			TellWindowToGiveItem();
		}

		private void TellWindowToGiveItem()
		{
			itemWindow.GiveItem(doc);
		}
	}
}