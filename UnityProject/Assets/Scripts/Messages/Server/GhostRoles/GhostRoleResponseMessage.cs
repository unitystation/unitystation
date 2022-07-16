using System.Collections.Generic;
using Mirror;

namespace Messages.Server.GhostRoles
{
	/// <summary>
	/// Sends a message to the specific player, informing them about the outcome of their request for a ghost role.
	/// </summary>
	public class GhostRoleResponseMessage : ServerMessage<GhostRoleResponseMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint roleID;
			public int responseCode;
		}

		private static readonly Dictionary<GhostRoleResponseCode, string> stringDict = new()
		{
			{ GhostRoleResponseCode.Success, "Successfully queued for this role!" },
			{ GhostRoleResponseCode.RoleNotFound, "Unable to give you the role. It may have timed out." },
			{ GhostRoleResponseCode.AlreadyWaiting, "You're already in this role's queue!" },
			{ GhostRoleResponseCode.AlreadyQueued, "You're already queued for a role!" },
			{ GhostRoleResponseCode.QueueFull, "All positions have been filled for this role! You're too late." },
			{ GhostRoleResponseCode.Error, "There was a problem giving you the role." },
			{ GhostRoleResponseCode.JobBanned, "You are job banned from this role." },
		};

		// To be run on client
		public override void Process(NetMessage msg)
		{
			if (PlayerManager.LocalPlayerObject == null) return;

			if (MatrixManager.IsInitialized == false) return;

			UIManager.GhostRoleWindow.DisplayResponseMessage(msg.roleID, (GhostRoleResponseCode)msg.responseCode);
		}

		/// <summary>
		/// Sends a message to the specific player, informing them about the outcome of their request for a ghost role.
		/// </summary>
		public static NetMessage SendTo(PlayerInfo player, uint key, GhostRoleResponseCode code)
		{
			NetMessage msg = new NetMessage
			{
				roleID = key,
				responseCode = (int) code,
			};

			SendTo(player, msg);
			return msg;
		}

		/// <summary>
		/// Gets the verbose text associated with the given response code.
		/// </summary>
		public static string GetMessageText(GhostRoleResponseCode responseCode)
		{
			return stringDict[responseCode];
		}
	}

	public enum GhostRoleResponseCode
	{
		Success,
		RoleNotFound,
		AlreadyWaiting,
		AlreadyQueued,
		QueueFull,
		Error,
		JobBanned,
		ClearMessage
	}
}
