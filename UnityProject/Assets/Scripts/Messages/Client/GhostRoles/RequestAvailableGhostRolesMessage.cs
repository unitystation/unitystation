using System.Collections.Generic;
using Systems.GhostRoles;
using Messages.Server;
using Messages.Server.GhostRoles;
using Mirror;

namespace Messages.Client.GhostRoles
{
	/// <summary>
	/// Allows a network message to be sent to the server, requesting an update on all available ghost roles on the server.
	/// </summary>
	public class RequestAvailableGhostRolesMessage : ClientMessage<RequestAvailableGhostRolesMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			foreach (KeyValuePair<uint, GhostRoleServer> kvp in GhostRoleManager.Instance.serverAvailableRoles)
			{
				GhostRoleUpdateMessage.SendTo(SentByPlayer, kvp.Key, kvp.Value);
			}
		}

		/// <summary>
		/// Sends a message to the server, requesting an update on all available ghost roles on the server.
		/// </summary>
		public static void SendMessage()
		{
			if (NetworkClient.active == false) return;

			var msg = new NetMessage();

			Send(msg);
		}
	}
}
