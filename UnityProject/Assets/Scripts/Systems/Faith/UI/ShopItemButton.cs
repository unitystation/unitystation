using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;


namespace Systems.Faith.UI
{
	public class ShopItemButton : DynamicEntry
	{
		[SerializeField] private NetText_label text;
		[SerializeField] private NetText_label hiddenCost;
		[SerializeField] private NetText_label HiddenDesc;
		[SerializeField] private NetSpriteHandler spriteSO;
		[SerializeField] private ChaplainPointShopScreen screen;

		private IFaithMiracle miracle;

		public void SetValues(IFaithMiracle newMiracle)
		{
			miracle = newMiracle;
			text.MasterSetValue(miracle.FaithMiracleName);
			hiddenCost.MasterSetValue(miracle.MiracleCost.ToString());
			HiddenDesc.MasterSetValue(miracle.FaithMiracleDesc);
			spriteSO.MasterSetValue(miracle.MiracleIcon.SetID);
			ExecuteClient();
		}

		public override void ExecuteClient()
		{
			base.ExecuteClient();
			gameObject.SetActive(true);
		}

		public override void ExecuteServer(PlayerInfo subject)
		{
			base.ExecuteServer(subject);
			miracle.DoMiracle();
		}

		public void OnClick()
		{
			screen.SetData(text.Value, hiddenCost.Value, HiddenDesc.Value, this);
		}
	}
}