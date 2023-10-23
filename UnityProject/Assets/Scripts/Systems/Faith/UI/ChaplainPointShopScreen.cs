using Items;
using UI.Core.NetUI;
using UnityEngine;

namespace Systems.Faith.UI
{
	public class ChaplainPointShopScreen : NetTab
	{
		[SerializeField] private NetUIDynamicList shopListTransform;

		private HolyBook provider;

		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;
			provider = Provider.GetComponent<HolyBook>();
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
		}
	}
}