using Items;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;

namespace Systems.Faith.UI
{
	public class ChaplainPointShopScreen : NetTab
	{
		[SerializeField] private NetUIDynamicList shopListTransform;
		[SerializeField] private TMP_Text costText;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text descText;

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
				newItem.SetValues(miracle);
			}

			if (shopListTransform.Entries.Count == 0) return;
			//sets the data of the first item in the list in the UI as if its selected.
			if (shopListTransform.Entries[0] is ShopItemButton button)
			{
				button.OnClick();
			}
		}

		public void SetData(string title, string cost, string desc, ShopItemButton focusedElement)
		{
			costText.text = cost;
			titleText.text = title;
			descText.text = desc;
			currentFocusedElement = focusedElement;
		}

		public void OnBuy()
		{
			if (currentFocusedElement == null) return;
			currentFocusedElement.ExecuteServer(PlayerManager.LocalPlayerScript.PlayerInfo);
		}
	}
}