using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_AutolatheQueueItem : DynamicEntry
	{
		private GUI_Autolathe ExoFabMasterTab => containedInTab as GUI_Autolathe;

		public MachineProduct Product { get; set; }

		public int NumberInQueue { get; set; }

		private NetInteractiveButton upButton;
		public NetInteractiveButton UpButton => upButton;
		private NetInteractiveButton downButton;
		public NetInteractiveButton DownButton => downButton;
		private GUI_RDProQueueLabel numberInQueueColorElement;
		private GUI_RDProQueueLabel productTextColorElement;

		public void ForwardInQueue()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_Autolathe>().OnUpQueueClicked.Invoke(NumberInQueue);
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
				containedInTab.GetComponent<GUI_Autolathe>().OnDownQueueClicked.Invoke(NumberInQueue);
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
				containedInTab.GetComponent<GUI_Autolathe>().OnRemoveProductClicked.Invoke(NumberInQueue);
			}
			else
			{
				ExoFabMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue);
			}
		}

		public void SetTextToRed() { }

		public void ReInit()
		{
			if (Product == null) return;

			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "QueueNumber":
						numberInQueueColorElement = element as GUI_RDProQueueLabel;
						((NetUIElement<string>)element).MasterSetValue(NumberInQueue.ToString());
						break;

					case "ProductName":
						productTextColorElement = element as GUI_RDProQueueLabel;
						((NetUIElement<string>)element).MasterSetValue(Product.Name);
						break;

					case "UpButton":
						upButton = element as NetInteractiveButton;
						upButton.MasterSetValue("true");
						break;

					case "DownButton":
						downButton = element as NetInteractiveButton;
						downButton.MasterSetValue("true");
						break;
				}
			}
		}
	}
}
