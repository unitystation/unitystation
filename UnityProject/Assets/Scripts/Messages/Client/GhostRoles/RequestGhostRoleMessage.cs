using Systems.GhostRoles;
using Mirror;

namespace Messages.Client.GhostRoles
{
	/// <summary>
	/// Allows a network message to be sent to the server, requesting that the local player be assigned the associated role of the given key.
	/// </summary>
	public class RequestGhostRoleMessage : ClientMessage<RequestGhostRoleMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint roleID;
		}

		public override void Process(NetMessage msg)
		{
			GhostRoleManager.Instance.ServerGhostRequestRole(SentByPlayer, msg.roleID);
		}

		/// <summary>
		/// Sends a message to the server, requesting that the local player be assigned the associated role of the given key.
		/// </summary>
		/// <param name="key">The unique key the ghost role instance is associated with.</param>
		public static NetMessage Send(uint key)
		{
			var msg = new NetMessage
			{
				roleID = key,
			};

			Send(msg);
			return msg;
		}
	}
}
