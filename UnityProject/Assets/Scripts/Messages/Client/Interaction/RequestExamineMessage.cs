using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Messages.Client;
using Mirror;
using Player;
using UnityEngine;

/// <summary>
/// Requests for the server to perform examine interaction
/// </summary>
public class RequestExamineMessage : ClientMessage
{
	public class RequestExamineMessageNetMessage : NetworkMessage
	{
		//members
		// netid of target
		public uint examineTarget;
		public Vector3 mousePosition;
	}

	static RequestExamineMessage()
	{
		//constructor
	}

	public override void Process<T>(T netMsg)
	{
		var newMsg = netMsg as RequestExamineMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		//TODO: check break conditions
		if (SentByPlayer == null || SentByPlayer.Script == null)
		{
			return;
		}

		LoadNetworkObject(newMsg.examineTarget);

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
			// don't send text message target is player - instead send PlayerExaminationMessage

			// Exception for player examine window.
			//TODO make this be based on a setting clients can turn off
			if (examinable is ExaminablePlayer examinablePlayer)
			{
				examinablePlayer.Examine(SentByPlayer.GameObject);
			}

			var examinableMsg = examinable.Examine(newMsg.mousePosition);
			if (string.IsNullOrEmpty(examinableMsg))
				continue;

			msg += examinableMsg;

			if (i != examinables.Length - 1)
			{
				msg += "\n";
			}
		}

		// Send the message.
		if (msg.Length > 0)
		{
			Chat.AddExamineMsgFromServer(SentByPlayer.GameObject, msg);
		}
	}

	public static void Send(uint targetNetId)
	{
		var msg = new RequestExamineMessageNetMessage()
		{
			examineTarget = targetNetId
		};
		new RequestExamineMessage().Send(msg);
	}

	public static void Send(uint targetNetId, Vector3 mousePos)
	{
		var msg = new RequestExamineMessageNetMessage()
		{
			examineTarget = targetNetId,
			mousePosition = mousePos
		};
		new RequestExamineMessage().Send(msg);
	}
}
