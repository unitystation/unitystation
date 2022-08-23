using Doors.Modules;
using Mirror;

namespace Player
{
	public class ElectrifiedDoorViewer : NetworkBehaviour, IOnPlayerRejoin, IOnPlayerTransfer, IOnPlayerLeaveBody
	{
	public void OnPlayerRejoin(Mind mind)
	{
		//Resend electrified door  stuff to player
		ElectrifiedDoorModule.Rejoined(connectionToClient);
	}

	public void OnPlayerTransfer(Mind mind)
	{
		//Resend electrified door stuff as they've transferred into this body from a different one
		ElectrifiedDoorModule.Rejoined(connectionToClient);
	}

	public void OnPlayerLeaveBody(Mind mind)
	{
		//Player left this body so remove all electrified door stuff
		ElectrifiedDoorModule.LeftBody(connectionToClient);
	}
	}
}