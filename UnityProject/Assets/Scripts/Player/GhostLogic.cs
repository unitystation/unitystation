using HealthV2;
using Mirror;
using UnityEngine;

namespace Player
{
	public class GhostLogic : NetworkBehaviour, IOnPlayerRejoin, IOnPlayerTransfer, IOnPlayerLeaveBody
	{
		public void OnPlayerRejoin()
		{
			//Resend infected player stuff to ghost
			XenomorphLarvae.Rejoined(connectionToClient);
		}

		public void OnPlayerTransfer()
		{
			//Resend infected player stuff as they've transferred into this body from a different one
			XenomorphLarvae.Rejoined(connectionToClient);
		}

		public void OnPlayerLeaveBody()
		{
			//Player left this body so remove all infected stuff
			XenomorphLarvae.LeftBody(connectionToClient);
		}
	}
}