using System;
using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;


namespace Systems.Faith.UI
{
	public class ShopItemButton : DynamicEntry
	{
		[SerializeField] private NetText_label text;
		[SerializeField] private NetText_label hiddenCost;
		[SerializeField] private NetText_label HiddenDesc;
		[SerializeField] private NetText_label id;
		[SerializeField] private NetSpriteHandler spriteSO;
		[SerializeField] private ChaplainPointShopScreen screen;
		public string ID => id.Value;

		private IFaithMiracle miracle;

		public void SetValues(IFaithMiracle newMiracle)
		{
			miracle = newMiracle;
			text.MasterSetValue(miracle.FaithMiracleName);
			hiddenCost.MasterSetValue(miracle.MiracleCost.ToString());
			HiddenDesc.MasterSetValue(miracle.FaithMiracleDesc);
			spriteSO.MasterSetValue(miracle.MiracleIcon.SetID);
			id.MasterSetValue(Guid.NewGuid().ToString());
		}

		public void DoMiracle()
		{
			miracle.DoMiracle();
		}

		public void OnClick()
		{
			screen.SetData(text.Value, hiddenCost.Value, HiddenDesc.Value, id.Value, spriteSO.Element.CurrentSprite);
		}
	}
}