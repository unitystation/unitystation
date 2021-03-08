using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Machines;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabQueueItem : DynamicEntry
	{
		private GUI_ExosuitFabricator ExoFabMasterTab {
			get => MasterTab as GUI_ExosuitFabricator;
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
		private GUI_ExoFabQueueLabel queueNumberElement;
		private GUI_ExoFabQueueLabel productTextColorElement;

		public void ForwardInQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnUpQueueClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnUpQueueClicked.Invoke(numberInQueue); }
		}

		public void BackwardsInQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnDownQueueClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnDownQueueClicked.Invoke(numberInQueue); }
		}

		public void RemoveFromQueue()
		{
			if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnRemoveProductClicked.Invoke(numberInQueue); }
			else { ExoFabMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue); }
		}

		public void SetTextToRed()
		{
		}

		public void ReInit()
		{
			if (product == null)
			{
				Logger.Log("ExoFab Product not found", Category.Machines);
				return;
			}
			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "QueueNumber":
						queueNumberElement = (GUI_ExoFabQueueLabel)element;
						queueNumberElement.SetValueServer(NumberInQueue.ToString());
						break;

					case "ProductName":
						productTextColorElement = (GUI_ExoFabQueueLabel)element;
						productTextColorElement.SetValueServer(Product.Name);
						break;

					case "UpButton":
						upButton = (GUI_ExoFabButton)element;
						upButton.SetValueServer("true");
						break;

					case "DownButton":
						downButton = (GUI_ExoFabButton)element;
						downButton.SetValueServer("true");
						break;
				}
			}
		}
	}
}
