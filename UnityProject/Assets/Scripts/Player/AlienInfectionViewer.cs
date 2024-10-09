﻿using HealthV2;
using Mirror;

namespace Player
{
	public class AlienInfectionViewer : NetworkBehaviour, IOnPlayerRejoin, IOnControlPlayer, IOnPlayerLeaveBody
	{
		public void OnPlayerRejoin(Mind mind)
		{
			//Resend infected player stuff to player
			XenomorphLarvae.Rejoined(connectionToClient);
		}

		public void OnServerPlayerTransfer(PlayerInfo account)
		{
			//Resend infected player stuff as they've transferred into this body from a different one
			XenomorphLarvae.Rejoined(connectionToClient);
		}

		public void OnPlayerLeaveBody(PlayerInfo account)
		{
			//Player left this body so remove all infected stuff 
			XenomorphLarvae.LeftBody(connectionToClient);
		}
	}
}
