using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Chemistry;

namespace UI.Objects.Chemistry
{
	/// <summary>
	/// DynamicEntry for ChemMaster NetTab container page.
	/// </summary>
	public class GUI_ChemContainerEntry : DynamicEntry
	{
		private GUI_ChemMaster chemMasterTab;
		private Reagent reagent = null;
		private float reagentAmount;

		public Reagent Reagent
		{
			get => reagent;
			set => reagent = value;
		}

		[SerializeField]
		private NetLabel reagentName = default;
		[SerializeField]
		private NetLabel reagentAmountDisplay = default;

		public void ReInit(Reagent newReagent, float amount, GUI_ChemMaster tab)
		{
			reagent = newReagent;
			reagentAmount = amount;
			chemMasterTab = tab;
			reagentName.SetValueServer(reagent.Name);
			reagentAmountDisplay.SetValueServer($"{reagentAmount:F2}u");
		}
		public void OpenCustomPrompt()
		{
			chemMasterTab.OpenCustomPrompt(reagent,true);
		}
		public void Transfer(float amount)
		{
			if (amount <= reagentAmount) chemMasterTab.TransferContainerToBuffer(reagent, amount);
			else chemMasterTab.TransferContainerToBuffer(reagent, reagentAmount);
		}
		public void TransferAll()
		{
			Transfer(reagentAmount);
		}
		public void Analyze(ConnectedPlayer player)
		{
			chemMasterTab.Analyze(reagent, player);
		}
	}
}