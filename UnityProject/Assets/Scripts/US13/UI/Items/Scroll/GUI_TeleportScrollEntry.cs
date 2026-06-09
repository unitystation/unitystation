using UnityEngine;
using US13.Items.Others.Magical;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Items.Scroll
{
	public class GUI_TeleportScrollEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label destinationLabel = default;

		private GUI_TeleportScroll scrollGUI;

		private TeleportDestination destination;

		public void Init(GUI_TeleportScroll scrollGUI, TeleportDestination destination)
		{
			this.scrollGUI = scrollGUI;
			this.destination = destination;
			destinationLabel.MasterSetValue(destination.ToString());
		}

		public void Teleport()
		{
			scrollGUI.TeleportTo(destination);
		}
	}
}
