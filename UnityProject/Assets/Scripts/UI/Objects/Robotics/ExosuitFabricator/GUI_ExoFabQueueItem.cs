using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabQueueItem : DynamicEntry
	{
		private GUI_ExosuitFabricator ExoFabMasterTab => containedInTab as GUI_ExosuitFabricator;

		public MachineProduct Product { get; set; }

		public int NumberInQueue { get; set; }
		public NetInteractiveButton UpButton { get; private set; }
		public NetInteractiveButton DownButton { get; private set; }
		private GUI_ExoFabQueueLabel queueNumberElement;
		private GUI_ExoFabQueueLabel productTextColorElement;

		public void ForwardInQueue()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_ExosuitFabricator>().OnUpQueueClicked.Invoke(NumberInQueue);
			}
			else
			{
				ExoFabMasterTab?.OnUpQueueClicked.Invoke(NumberInQueue);
			}
		}

		public void BackwardsInQueue()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_ExosuitFabricator>().OnDownQueueClicked.Invoke(NumberInQueue);
			}
			else
			{
				ExoFabMasterTab?.OnDownQueueClicked.Invoke(NumberInQueue);
			}
		}

		public void RemoveFromQueue()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_ExosuitFabricator>().OnRemoveProductClicked.Invoke(NumberInQueue);
			}
			else
			{
				ExoFabMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue);
			}
		}

		public void SetTextToRed() { }

		public void ReInit()
		{
			if (Product == null)
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
						queueNumberElement.MasterSetValue(NumberInQueue.ToString());
						break;

					case "ProductName":
						productTextColorElement = (GUI_ExoFabQueueLabel)element;
						productTextColorElement.MasterSetValue(Product.Name);
						break;

					case "UpButton":
						UpButton = (NetInteractiveButton)element;
						UpButton.MasterSetValue("true");
						break;

					case "DownButton":
						DownButton = (NetInteractiveButton)element;
						DownButton.MasterSetValue("true");
						break;
				}
			}
		}
	}
}
