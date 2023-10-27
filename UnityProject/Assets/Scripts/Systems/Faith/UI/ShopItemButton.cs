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
		[SerializeField] private NetSpriteHandler spriteSO;
		[SerializeField] private GUI_ChaplainPointShopScreen screen;

		public IFaithMiracle Miracle;

		public void SetValues(IFaithMiracle newMiracle, GUI_ChaplainPointShopScreen inscreen )
		{
			screen = inscreen;
			Miracle = newMiracle;
			text.MasterSetValue(Miracle.FaithMiracleName);
			spriteSO.MasterSetValue(Miracle.MiracleIcon.SetID);
		}

		public void DoMiracle()
		{
			Miracle.DoMiracle();
		}

		public void OnCategoryChosen()
		{
			screen.SetMasterData(this);
		}
	}
}