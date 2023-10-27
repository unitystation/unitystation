using System;
using Items;
using JetBrains.Annotations;
using Logs;
using TMPro;
using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class GUI_ChaplainPointShopScreen : NetTab
	{
		[SerializeField] private NetUIDynamicList shopListTransform;
		[SerializeField] private NetText_label costText;
		[SerializeField] private NetText_label titleText;
		[SerializeField] private NetText_label descText;
		[SerializeField] private NetSpriteHandler image;

		private HolyBook provider;

		private ShopItemButton currentlySelectedMiracle;

		private void Start()
		{
			provider = Provider.GetComponent<HolyBook>();
			RefreshShop();
		}


		private void RefreshShop()
		{
			if (IsMasterTab == false) return;
			shopListTransform.Clear();
			foreach (var miracle in FaithManager.Instance.CurrentFaith.FaithMiracles)
			{
				var newItem = shopListTransform.AddItem() as ShopItemButton;
				if (newItem == null)
				{
					Loggy.LogError("[ChaplainPointShopScreen/RefreshShop()] -  Unexpected type detected.");
					return;
				}
				newItem.SetValues(miracle, this);
			}

			if (shopListTransform.Entries.Count == 0) return;
			//sets the data of the first item in the list in the UI as if its selected.
			if (shopListTransform.Entries[0] is ShopItemButton button)
			{
				button.OnCategoryChosen();
			}
		}

		public void SetMasterData(ShopItemButton shopItem)
		{
			currentlySelectedMiracle = shopItem;
			costText.MasterSetValue(shopItem.Miracle.MiracleCost.ToString());
			titleText.MasterSetValue(shopItem.Miracle.FaithMiracleName);
			descText.MasterSetValue(shopItem.Miracle.FaithMiracleDesc);
			image.MasterSetValue(shopItem.Miracle.MiracleIcon.SetID);
		}


		public void OnMasterBuy()
		{
			currentlySelectedMiracle.DoMiracle();
		}
	}
}