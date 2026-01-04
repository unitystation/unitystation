using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

public class AvailableMapsMessage  : ServerMessage<AvailableMapsMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string[] MainStations;
		public string[] AwaySite;
	}
	public override void Process(NetMessage msg)
	{
		RoundManagerPage.Instance.GenerateDropDownOptionsMap(msg.MainStations);
		RoundManagerPage.Instance.GenerateDropDownOptionsAwaySite(msg.AwaySite);
	}

	public static void SendTo(NetworkConnection admin, List<string> MainStations, List<string> AwaySites)
	{
		NetMessage message = new NetMessage()
		{
			MainStations= MainStations.ToArray(),
			AwaySite = AwaySites.ToArray()
		};
		SendTo(admin, message);
	}
}
