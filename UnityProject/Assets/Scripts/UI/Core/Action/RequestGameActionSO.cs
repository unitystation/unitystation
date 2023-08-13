using System.Collections;
using Messages.Client;
using UnityEngine;
using Mirror;

public class RequestGameActionSO : ClientMessage<RequestGameActionSO.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public ushort soID;
	}

	public override void Process(NetMessage msg)
	{
		if (SentByPlayer != PlayerInfo.Invalid)
		{
			UIActionSOSingleton.Instance.ActionCallServer(msg.soID, SentByPlayer);
		}
	}


	public static void Send(UIActionScriptableObject uIActionScriptableObject)
	{

		NetMessage msg = new NetMessage
		{
			soID = UIActionSOSingleton.ActionsTOID[uIActionScriptableObject]
		};
		Send(msg);
	}
}
