using System.Collections;
using UnityEngine;
using Items.Scrolls.TeleportScroll;

namespace UI.Scroll
{
	public class GUI_TeleportScrollEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel destinationLabel = default;

		private GUI_TeleportScroll ScrollGUI;

		private TeleportDestination destination;

		public void Init(GUI_TeleportScroll scrollGUI, TeleportDestination destinationInit)
		{
			ScrollGUI = scrollGUI;
			destination = destinationInit;
			destinationLabel.SetValueServer(destination.ToString());
		}

		public void Teleport()
		{
			ScrollGUI.TeleportTo(destination);
		}
	}
}
