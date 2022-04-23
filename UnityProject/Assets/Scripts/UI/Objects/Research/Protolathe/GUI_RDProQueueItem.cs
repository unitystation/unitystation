using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;
using UI.Objects.Robotics;
using Systems.Research;

namespace UI.Objects
{
	public class GUI_RDProQueueItem : DynamicEntry
	{
		private GUI_RDProductionMachine RDProMasterTab => MasterTab as GUI_RDProductionMachine;

		public string DesignID { get; set; }

		public int NumberInQueue { get; set; }

		private NetInteractiveButton upButton;
		public NetInteractiveButton UpButton => upButton;
		private NetInteractiveButton downButton;
		public NetInteractiveButton DownButton => downButton;
		private GUI_ExoFabQueueLabel numberInQueueColorElement;
		private GUI_ExoFabQueueLabel productTextColorElement;

		public void ForwardInQueue()
		{
			if (RDProMasterTab == null)
			{
				MasterTab.GetComponent<GUI_RDProductionMachine>().OnUpQueueClicked.Invoke(NumberInQueue);
			}
			else
			{
				RDProMasterTab?.OnUpQueueClicked.Invoke(NumberInQueue);
			}
		}

		public void BackwardsInQueue()
		{
			if (RDProMasterTab == null)
			{
				MasterTab.GetComponent<GUI_RDProductionMachine>().OnDownQueueClicked.Invoke(NumberInQueue);
			}
			else
			{
				RDProMasterTab?.OnDownQueueClicked.Invoke(NumberInQueue);
			}
		}

		public void RemoveFromQueue()
		{
			if (RDProMasterTab == null)
			{
				MasterTab.GetComponent<GUI_RDProductionMachine>().OnRemoveProductClicked.Invoke(NumberInQueue);
			}
			else
			{
				RDProMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue);
			}
		}

		public void SetTextToRed() { }

		public void ReInit()
		{
			if (DesignID == null) return;

			foreach (var element in Elements)
			{
				Design productDesign = Designs.Globals.InternalIDSearch[DesignID];

				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "QueueNumber":
						numberInQueueColorElement = element as GUI_ExoFabQueueLabel;
						((NetUIElement<string>)element).SetValueServer(NumberInQueue.ToString());
						break;

					case "ProductName":
						productTextColorElement = element as GUI_ExoFabQueueLabel;
						((NetUIElement<string>)element).SetValueServer(productDesign.Name);
						break;

					case "UpButton":
						upButton = element as NetInteractiveButton;
						upButton.SetValueServer("true");
						break;

					case "DownButton":
						downButton = element as NetInteractiveButton;
						downButton.SetValueServer("true");
						break;
				}
			}
		}
	}
}
