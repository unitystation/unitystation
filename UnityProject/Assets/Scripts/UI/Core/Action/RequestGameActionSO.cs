using System.Collections;
using Messages.Client;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class RequestGameActionSO : ClientMessage
{
	public struct RequestGameActionSONetMessage : NetworkMessage
	{
		public ushort soID;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestGameActionSONetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestGameActionSONetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			UIActionSOSingleton.Instance.ActionCallServer(newMsg.soID, SentByPlayer);
		}
	}


	public static void Send(UIActionScriptableObject uIActionScriptableObject)
	{

		RequestGameActionSONetMessage msg = new RequestGameActionSONetMessage
		{
			soID = UIActionSOSingleton.ActionsTOID[uIActionScriptableObject]
		};
		new RequestGameActionSO().Send(msg);
	}
}
