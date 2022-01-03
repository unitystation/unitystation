﻿using System.Collections;
using UnityEngine;
using Items.Scrolls.TeleportScroll;

namespace UI.Scroll
{
	public class GUI_TeleportScrollEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel destinationLabel = default;

		private GUI_TeleportScroll scrollGUI;

		private TeleportDestination destination;

		public void Init(GUI_TeleportScroll scrollGUI, TeleportDestination destination)
		{
			this.scrollGUI = scrollGUI;
			this.destination = destination;
			destinationLabel.SetValueServer(destination.ToString());
		}

		public void Teleport()
		{
			scrollGUI.TeleportTo(destination);
		}
	}
}
