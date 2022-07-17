using System;
using System.Collections.Generic;
using Objects.Research;
using UnityEngine;

namespace Items.Others
{
	public class HandTeleporter : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		private int maxPortalPairs = 3;
		private static HashSet<Tuple<Portal, Portal>> portalPairs = new HashSet<Tuple<Portal, Portal>>();

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Don't allow alt click so that the nettab can be opened
			if((side == NetworkSide.Client || CustomNetworkManager.IsServer) && KeyboardInputManager.IsAltPressed()) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			//Open the portals if destination tracking beacon selected
		}
	}
}