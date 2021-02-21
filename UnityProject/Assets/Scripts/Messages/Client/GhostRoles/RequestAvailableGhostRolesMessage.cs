using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Server;
using Systems.GhostRoles;
using Mirror;

namespace Messages.Client
{
	/// <summary>
	/// Allows a network message to be sent to the server, requesting an update on all available ghost roles on the server.
	/// </summary>
	public class RequestAvailableGhostRolesMessage : ClientMessage
	{
		public struct RequestAvailableGhostRolesMessageNetMessage : NetworkMessage { }

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public RequestAvailableGhostRolesMessageNetMessage IgnoreMe;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as RequestAvailableGhostRolesMessageNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			foreach (KeyValuePair<uint, GhostRoleServer> kvp in GhostRoleManager.Instance.serverAvailableRoles)
			{
				GhostRoleUpdateMessage.SendTo(SentByPlayer, kvp.Key, kvp.Value);
			}
		}

		/// <summary>
		/// Sends a message to the server, requesting an update on all available ghost roles on the server.
		/// </summary>
		public static RequestAvailableGhostRolesMessageNetMessage SendMessage()
		{
			var msg = new RequestAvailableGhostRolesMessageNetMessage();
			new RequestAvailableGhostRolesMessage().Send(msg);

			return msg;
		}
	}
}
