using Messages.Client;
using Messages.Client.Admin;
using Mirror;
using ServerMessage;
using UnityEngine;

namespace ClientMessage
{


public class RequestMaps : ClientMessage<RequestMaps.NetMessage>
{

	public struct NetMessage : NetworkMessage
	{
	}

	public override void Process(NetMessage msg)
	{
		if (HasPermission(TAG.MANAGE_ROUND_NEXT_MAP) == false) return;
		AvailableMapsMessage.SendTo(SentBy ,SubSceneManager.Instance.MainStationList.MainStations, SubSceneManager.Instance.AwayWorlds.AwayWorlds);
	}

	public static NetMessage Send()
	{
		NetMessage msg = new NetMessage
		{
		};

		Send(msg);
		return msg;
	}

}
}