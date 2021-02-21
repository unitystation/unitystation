using System.Collections;
using Mirror;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public struct GibMessageNetMessage : NetworkMessage { }

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public GibMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as GibMessageNetMessage?;
		if(newMsgNull == null) return;

		foreach (LivingHealthBehaviour living in Object.FindObjectsOfType<LivingHealthBehaviour>())
		{
			living.Death();
		}
	}

	public static GibMessageNetMessage Send()
	{
		GibMessageNetMessage msg = new GibMessageNetMessage();
		new GibMessage().SendToAll(msg);
		return msg;
	}
}