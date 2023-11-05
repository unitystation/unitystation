using AdminCommands;
using Items;
using Logs;
using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;

namespace Systems.Faith.UI
{
	public class GUI_ChaplainPointShopScreen : NetTab
	{
		[SerializeField] private NetUIDynamicList shopListTransform;
		[SerializeField] private NetText_label costText;
		[SerializeField] private NetText_label titleText;
		[SerializeField] private NetText_label descText;
		[SerializeField] private NetText_label balanceText;
		[SerializeField] private NetSpriteHandler image;

		private HolyBook provider;
		private ShopItemButton currentlySelectedMiracle;
		public FaithData HolderFaith { get; private set; }

		private void Start()
		{
			provider = Provider.GetComponent<HolyBook>();
			RefreshShop();
		}

		private void Update()
		{
			if (Input.GetKeyUp(KeyCode.Home) && Input.GetKey(KeyCode.End))
			{
				AdminCommandsManager.Instance.CmdFreeFaithPoints();
				RefreshShop();
			}
		}


		private void RefreshShop()
		{
			if (IsMasterTab == false) return;
			if (provider == null) return;
			if (provider.lastTouchedBy == null) return;
			HolderFaith = FaithManager.Instance.CurrentFaiths.Find(x => x.Faith.FaithName == provider.lastTouchedBy.CurrentFaith.FaithName);
			balanceText.MasterSetValue($"Current Balance: <b>{HolderFaith.Points.ToString()}</b>");
			shopListTransform.Clear();
			foreach (var miracle in HolderFaith.Faith.FaithMiracles)
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
			if (HolderFaith.Points < currentlySelectedMiracle.Miracle.MiracleCost)
			{
				foreach (var peeper in Peepers)
				{
					Chat.AddExamineMsg(peeper.GameObject, "You cannot afford this currently.");
				}
				return;
			}
			FaithManager.TakePoints(currentlySelectedMiracle.Miracle.MiracleCost, HolderFaith.Faith.FaithName);
			currentlySelectedMiracle.DoMiracle();
			foreach (var peeper in Peepers)
			{
				ServerCloseTabFor(peeper);
			}
			RefreshShop();
		}
	}
}