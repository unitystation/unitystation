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
		public override void Process()
		{
			foreach (KeyValuePair<uint, GhostRoleServer> kvp in GhostRoleManager.Instance.serverAvailableRoles)
			{
				GhostRoleUpdateMessage.SendTo(SentByPlayer, kvp.Key, kvp.Value);
			}
		}

		/// <summary>
		/// Sends a message to the server, requesting an update on all available ghost roles on the server.
		/// </summary>
		public static RequestAvailableGhostRolesMessage SendMessage()
		{
			var msg = new RequestAvailableGhostRolesMessage();
			msg.Send();

			return msg;
		}
	}
}
