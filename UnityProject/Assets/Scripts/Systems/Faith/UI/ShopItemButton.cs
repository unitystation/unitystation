using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;


namespace Systems.Faith.UI
{
	public class ShopItemButton : DynamicEntry
	{
		[SerializeField] private NetText_label text;
		[SerializeField] private NetSpriteHandler spriteSO;
		[SerializeField] private ChaplainPointShopScreen screen;

		private IFaithMiracle miracle;

		public void SetValues(IFaithMiracle newMiracle)
		{
			miracle = newMiracle;
			text.MasterSetValue(miracle.FaithMiracleName);
			spriteSO.MasterSetValue(miracle.MiracleIcon.SetID);
		}

		public void OnClick()
		{

		}
	}
}