using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class RequestGameActionSO : ClientMessage
{
	public ushort soID;

	public override void Process()
	{
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			UIActionSOSingleton.Instance.ActionCallServer(soID, SentByPlayer);
		}
	}


	public static void Send(UIActionScriptableObject uIActionScriptableObject)
	{

		RequestGameActionSO msg = new RequestGameActionSO
		{
			soID = UIActionSOSingleton.ActionsTOID[uIActionScriptableObject]
		};
		msg.Send();
	}
}
