using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class RequestGameActionSO : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestGameActionSO;
	public string soName;

	public override IEnumerator Process()
	{
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			UIActionSOSingleton.Instance.ActionCallServer(soName, SentByPlayer);
		}
		yield return null;
	}


	public static RequestGameActionSO Send(UIActionScriptableObject uIActionScriptableObject)
	{
		RequestGameActionSO msg = new RequestGameActionSO
		{
			soName = uIActionScriptableObject.name
		};
		msg.Send();
		return msg;
	}
}
