using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;

/// <summary>
/// Requests for the server to perform examine interaction
/// </summary>
public class RequestExamineMessage : ClientMessage
{
	//members
	// netid of target
	public uint examineTarget;
	public Vector3 mousePosition;
	static RequestExamineMessage()
	{
		//constructor
	}

	public override void Process()
	{
		//TODO: check break conditions
		if (SentByPlayer == null || SentByPlayer.Script == null)
		{
			return;
		}

		LoadNetworkObject(examineTarget);

		if (NetworkObject == null) return;
		// Here we build the message to send, by looking at the target's components.
		// anyone extending IExaminable gets a say in it.
		// Look for examinables.
		var examinables = NetworkObject.GetComponents<IExaminable>();
		string msg = "";
		IExaminable examinable;

		for (int i = 0; i < examinables.Length; i++)
		{
			examinable = examinables[i];

			var examinableMsg = examinable.Examine(mousePosition);
			if (string.IsNullOrEmpty(examinableMsg))
				continue;

			msg += examinableMsg;

			if (i != examinables.Length - 1)
			{
				msg += "\n";
			}
		}

		// Send the message.
		Chat.AddExamineMsgFromServer(SentByPlayer.GameObject, msg);
	}

	public static void Send(uint targetNetId)
	{
		var msg = new RequestExamineMessage()
		{
			examineTarget = targetNetId
		};
		msg.Send();
	}

	public static void Send(uint targetNetId, Vector3 mousePos)
	{
		var msg = new RequestExamineMessage()
		{
			examineTarget = targetNetId,
			mousePosition = mousePos
		};
		msg.Send();
	}
}

