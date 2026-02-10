using System.Collections.Generic;
using Mirror;
using US13.Messages.Server.GhostRoles;
using US13.Systems.GhostRoles;

namespace US13.Messages.Client.GhostRoles
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
				if (kvp.Value.TimeRemaining != -1 && kvp.Value.TimeRemaining <= 0) continue;
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
