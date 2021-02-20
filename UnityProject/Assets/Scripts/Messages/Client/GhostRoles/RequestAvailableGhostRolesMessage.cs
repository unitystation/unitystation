using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Server;
using Systems.GhostRoles;

namespace Messages.Client
{
	/// <summary>
	/// Allows a network message to be sent to the server, requesting an update on all available ghost roles on the server.
	/// </summary>
	public class RequestAvailableGhostRolesMessage : ClientMessage
	{
		public class RequestAvailableGhostRolesMessageNetMessage : ActualMessage
		{

		}
		public override void Process(ActualMessage msg)
		{
			var newMsg = msg as RequestAvailableGhostRolesMessageNetMessage;
			if(newMsg == null) return;

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
