using System.Collections;
using UnityEngine;
using Mirror;
using Systems.GhostRoles;

namespace Messages.Client
{
	/// <summary>
	/// Allows a network message to be sent to the server, requesting that the local player be assigned the associated role of the given key.
	/// </summary>
	public class RequestGhostRoleMessage : ClientMessage
	{
		public class RequestGhostRoleMessageNetMessage : NetworkMessage
		{
			public uint roleID;
		}

		public override void Process<T>(T msg)
		{
			var newMsg = msg as RequestGhostRoleMessageNetMessage;
			if(newMsg == null) return;

			GhostRoleManager.Instance.ServerGhostRequestRole(SentByPlayer, newMsg.roleID);
		}

		/// <summary>
		/// Sends a message to the server, requesting that the local player be assigned the associated role of the given key.
		/// </summary>
		/// <param name="key">The unique key the ghost role instance is associated with.</param>
		public static RequestGhostRoleMessageNetMessage Send(uint key)
		{
			var msg = new RequestGhostRoleMessageNetMessage
			{
				roleID = key,
			};

			new RequestGhostRoleMessage().Send(msg);
			return msg;
		}
	}
}
