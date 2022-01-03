using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Machines;
using UI.Objects.Robotics;

namespace UI.Objects
{
	public class GUI_AutolatheQueueItem : DynamicEntry
	{
		private GUI_Autolathe ExoFabMasterTab {
			get => MasterTab as GUI_Autolathe;
		}

		private MachineProduct product;

		public MachineProduct Product {
			get => product;
			set {
				product = value;
			}
		}

		private int numberInQueue;

		public int NumberInQueue {
			get => numberInQueue;
			set {
				numberInQueue = value;
			}
		}

		private GUI_ExoFabButton upButton;
		public GUI_ExoFabButton UpButton { get => upButton; }
		private GUI_ExoFabButton downButton;
		public GUI_ExoFabButton DownButton { get => downButton; }
		private GUI_ExoFabQueueLabel numberInQueueColorElement;
		private GUI_ExoFabQueueLabel productTextColorElement;

		public void ForwardInQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_Autolathe>().OnUpQueueClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnUpQueueClicked.Invoke(numberInQueue); }
		}

		public void BackwardsInQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_Autolathe>().OnDownQueueClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnDownQueueClicked.Invoke(numberInQueue); }
		}

		public void RemoveFromQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_Autolathe>().OnRemoveProductClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue); }
		}

		public void SetTextToRed()
		{
		}

		public void ReInit()
		{
			if (product == null)
			{
				return;
			}
			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "QueueNumber":
						numberInQueueColorElement = element as GUI_ExoFabQueueLabel;
						((NetUIElement<string>)element).SetValueServer(NumberInQueue.ToString());
						break;

					case "ProductName":
						productTextColorElement = element as GUI_ExoFabQueueLabel;
						((NetUIElement<string>)element).SetValueServer(Product.Name);
						break;

					case "UpButton":
						upButton = element as GUI_ExoFabButton;
						upButton.SetValueServer("true");
						break;

					case "DownButton":
						downButton = element as GUI_ExoFabButton;
						downButton.SetValueServer("true");
						break;
				}
			}
		}
	}
}
