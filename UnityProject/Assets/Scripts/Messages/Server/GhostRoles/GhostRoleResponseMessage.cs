using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to the specific player, informing them about the outcome of their request for a ghost role.
	/// </summary>
	public class GhostRoleResponseMessage : ServerMessage
	{
		public uint roleID;
		public int responseCode;

		private static readonly Dictionary<GhostRoleResponseCode, string> stringDict = new Dictionary<GhostRoleResponseCode, string>()
		{
			{ GhostRoleResponseCode.Success, "Successfully queued for this role!" },
			{ GhostRoleResponseCode.RoleNotFound, "Unable to give you the role. It may have timed out." },
			{ GhostRoleResponseCode.AlreadyWaiting, "You're already in this role's queue!" },
			{ GhostRoleResponseCode.AlreadyQueued, "You're already queued for a role!" },
			{ GhostRoleResponseCode.QueueFull, "All positions have been filled for this role! You're too late." },
			{ GhostRoleResponseCode.Error, "There was a problem giving you the role." },
		};

		// To be run on client
		public override void Process()
		{
			if (PlayerManager.LocalPlayer == null) return;

			if (MatrixManager.IsInitialized == false) return;

			UIManager.GhostRoleWindow.DisplayResponseMessage(roleID, (GhostRoleResponseCode)responseCode);
		}

		/// <summary>
		/// Sends a message to the specific player, informing them about the outcome of their request for a ghost role.
		/// </summary>
		public static GhostRoleResponseMessage SendTo(ConnectedPlayer player, uint key, GhostRoleResponseCode code)
		{
			GhostRoleResponseMessage msg = new GhostRoleResponseMessage
			{
				roleID = key,
				responseCode = (int) code,
			};

			msg.SendTo(player);
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
	}
}
