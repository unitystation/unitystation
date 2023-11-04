using System.Collections.Generic;
using System.Linq;
using Logs;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Research
{
	public class GUI_RemoteSyntheticControl : NetTab
	{
		public RemoteSyntheticControlConsole AssociatedConsole;


		private List<RemotelyControlledBrain> CashedCyborgsOnMatrix = new List<RemotelyControlledBrain>();

		private List<UI_IndividualCyborg> UIElementsLoaded = new List<UI_IndividualCyborg>();

		public EmptyItemList EmptyItemList;

		public void Close()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		private void Start()
		{
			if (Provider != null && IsMasterTab)
			{
				AssociatedConsole = Provider.GetComponentInChildren<RemoteSyntheticControlConsole>();
				UpdateButton();
			}
		}


		public void UpdateButton()
		{
			if (IsMasterTab == false) return;
			if (AssociatedConsole == null)
			{
				Loggy.LogError("AssociatedConsole Was missing or destroyed but GUI_RemoteSyntheticControl Is still alive!!!");
				return;
			}



			// Unregister Cyborgs that were present in the old list but not in the new list
			foreach (var oldBrain in CashedCyborgsOnMatrix)
			{
				if (AssociatedConsole.CyborgsOnMatrix.Contains(oldBrain) == false)
				{
					var UI = UIElementsLoaded.First(x => x.AssociatedBrain == oldBrain);

					UIElementsLoaded.Remove(UI);

					EmptyItemList.MasterRemoveItem(UI);
				}
			}

			// Register Cyborgs that are present in the new list but not in the old list
			foreach (var newBrain in AssociatedConsole.CyborgsOnMatrix)
			{
				if (CashedCyborgsOnMatrix.Contains(newBrain) == false)
				{

					var NewElement  = EmptyItemList.AddItem() as UI_IndividualCyborg;
					NewElement.Setup(newBrain, this);
					UIElementsLoaded.Add(NewElement);
				}
			}

			CashedCyborgsOnMatrix.Clear();
			CashedCyborgsOnMatrix.AddRange(AssociatedConsole.CyborgsOnMatrix);


			foreach (var Element in UIElementsLoaded)
			{
				Element.UpdateValues();
			}
		}

	}
}