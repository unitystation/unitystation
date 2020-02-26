using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class RequestGameActionSO : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestGameActionSO;
	public ushort soID;

	public override IEnumerator Process()
	{
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			UIActionSOSingleton.Instance.ActionCallServer(soID, SentByPlayer);
		}
		yield return null;
	}


	public static RequestGameActionSO Send(UIActionScriptableObject uIActionScriptableObject)
	{
		
		RequestGameActionSO msg = new RequestGameActionSO
		{
			soID = UIActionSOSingleton.ActionsTOID[uIActionScriptableObject]
		};
		msg.Send();
		return msg;
	}
}
