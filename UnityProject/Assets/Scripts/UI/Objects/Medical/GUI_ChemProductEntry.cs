using System;
using Items;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Medical
{
	/// <summary>
	/// DynamicEntry for ChemMaster NetTab product page.
	/// </summary>
	public class GUI_ChemProductEntry : DynamicEntry
	{

		public GameObject PrefabTo;

		private GUI_ChemMaster chemMasterTab;
		[SerializeField]
		private NetSpriteImage productImage = default;

		private bool IsPill = false;

		public NetButton NetButton;

		public void ReInit(GUI_ChemMaster tab, GameObject PrefabToUse)
		{
			PrefabTo = PrefabToUse;
			chemMasterTab = tab;
			productImage.SetSprite(transform.GetSiblingIndex());
		}


		public void PillButtonPressed()
		{
			NetButton.ExecuteClient();

		}

		public void SelectProduct()
		{
			if (productImage.Value.ToString() == "0")
			{
				chemMasterTab.PillSelectionArea.MasterNetSetActive(true);
			}
			else
			{
				chemMasterTab.SelectProduct(transform.GetSiblingIndex(), PrefabTo, -1);
			}
		}
	}
}
