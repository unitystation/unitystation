using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Chemistry;

namespace UI.Objects.Chemistry
{
	/// <summary>
	/// DynamicEntry for ChemMaster NetTab container page.
	/// </summary>
	public class GUI_ChemContainerEntry : DynamicEntry
	{
		private GUI_ChemMaster chemMasterTab;
		private float reagentAmount;

		public Reagent Reagent { get; set; } = null;

		[SerializeField]
		private NetText_label reagentName = default;
		[SerializeField]
		private NetText_label reagentAmountDisplay = default;

		public void ReInit(Reagent newReagent, float amount, GUI_ChemMaster tab)
		{
			Reagent = newReagent;
			reagentAmount = amount;
			chemMasterTab = tab;
			reagentName.SetValueServer(Reagent.Name);
			reagentAmountDisplay.SetValueServer($"{reagentAmount:F2}u");
		}

		public void OpenCustomPrompt()
		{
			chemMasterTab.OpenCustomPrompt(Reagent,true);
		}

		public void Transfer(float amount)
		{
			if (amount <= reagentAmount) chemMasterTab.TransferContainerToBuffer(Reagent, amount);
			else chemMasterTab.TransferContainerToBuffer(Reagent, reagentAmount);
		}

		public void TransferAll()
		{
			Transfer(reagentAmount);
		}

		public void Analyze(PlayerInfo player)
		{
			chemMasterTab.Analyze(Reagent, player);
		}
	}
}
