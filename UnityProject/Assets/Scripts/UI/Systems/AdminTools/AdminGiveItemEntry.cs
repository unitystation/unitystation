using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools
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
			itemName.text = document.SearchableName;
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