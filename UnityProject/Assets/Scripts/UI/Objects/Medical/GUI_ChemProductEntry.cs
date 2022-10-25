using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Medical
{
	/// <summary>
	/// DynamicEntry for ChemMaster NetTab product page.
	/// </summary>
	public class GUI_ChemProductEntry : DynamicEntry
	{
		private GUI_ChemMaster chemMasterTab;
		[SerializeField]
		private NetSpriteImage productImage = default;

		public void ReInit(GUI_ChemMaster tab)
		{
			chemMasterTab = tab;
			productImage.SetSprite(transform.GetSiblingIndex());
		}
		public void SelectProduct()
		{
			chemMasterTab.SelectProduct(transform.GetSiblingIndex());
		}
	}
}
