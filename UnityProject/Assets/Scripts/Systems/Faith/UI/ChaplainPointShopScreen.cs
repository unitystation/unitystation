using System;
using Items;
using JetBrains.Annotations;
using Logs;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class ChaplainPointShopScreen : NetTab
	{
		[SerializeField] private NetUIDynamicList shopListTransform;
		[SerializeField] private TMP_Text costText;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text descText;
		[SerializeField] private NetText_label currentId;
		[SerializeField] private Image image;

		private HolyBook provider;
		private ShopItemButton currentFocusedElement;

		private void Start()
		{
			provider = Provider.GetComponent<HolyBook>();
			if (CustomNetworkManager.IsServer == false) return;
			RefreshShop();
		}


		private void RefreshShop()
		{
			if (CustomNetworkManager.IsServer == false) return;
			shopListTransform.Clear();
			foreach (var miracle in FaithManager.Instance.CurrentFaith.FaithMiracles)
			{
				var newItem = shopListTransform.AddItem() as ShopItemButton;
				if (newItem == null)
				{
					Loggy.LogError("[ChaplainPointShopScreen/RefreshShop()] -  Unexpected type detected.");
					return;
				}
				newItem.SetValues(miracle);
			}

			if (shopListTransform.Entries.Count == 0) return;
			//sets the data of the first item in the list in the UI as if its selected.
			if (shopListTransform.Entries[0] is ShopItemButton button)
			{
				button.OnClick();
			}
		}

		public void SetData(string title, string cost, string desc, string elementId, Sprite img)
		{
			costText.text = cost;
			titleText.text = title;
			descText.text = desc;
			image.sprite = img;
			FindFocusedElement(elementId);
		}

		private void FindFocusedElement(string focuseID)
		{
			currentFocusedElement = null;
			currentId.MasterSetValue("expand dong");
			foreach (var entry in shopListTransform.Entries)
			{
				if (entry is not ShopItemButton c) continue;
				if (c.ID != focuseID) continue;
				currentFocusedElement = c;
				currentId.MasterSetValue(focuseID);
				Loggy.Log($"Found shitass with id {c.ID} which matches {currentId.Value}");
				break;
			}
		}

		public void OnBuy()
		{
			foreach (var entry in shopListTransform.Entries)
			{
				if (entry is not ShopItemButton c) continue;
				if (c.ID != currentId.Value) continue;
				Loggy.Log($"Found shitass with id {c.ID} which matches {currentId.Value}");
				c.DoMiracle();
				break;
			}
		}
	}
}