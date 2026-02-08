using Mirror;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Objects.Doors.Modules;

namespace US13.Player
{
	public class ElectrifiedDoorViewer : NetworkBehaviour, IOnPlayerRejoin, IOnControlPlayer, IOnPlayerLeaveBody
	{
		public void OnPlayerRejoin(Mind mind)
		{
			//Resend electrified door  stuff to player
			ElectrifiedDoorModule.Rejoined(connectionToClient);
		}

		public void OnServerPlayerTransfer(PlayerInfo account)
		{
			//Resend electrified door stuff as they've transferred into this body from a different one
			ElectrifiedDoorModule.Rejoined(connectionToClient);
		}

		public void OnPlayerLeaveBody(PlayerInfo account)
		{
			//Player left this body so remove all electrified door stuff
			ElectrifiedDoorModule.LeftBody(connectionToClient);
		}
	}
}