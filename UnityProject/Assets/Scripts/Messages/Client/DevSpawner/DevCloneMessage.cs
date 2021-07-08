using Mirror;
using UnityEngine;

namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to clone something, validated server side.
	/// </summary>
	public class DevCloneMessage : ClientMessage<DevCloneMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// Net ID of the object to clone
			public uint ToClone;
			// position to spawn at.
			public Vector2 WorldPosition;
			public string AdminId;
			public string AdminToken;

			public override string ToString()
			{
				return $"[DevCloneMessage ToClone={ToClone} WorldPosition={WorldPosition}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		void ValidateAdmin(NetMessage msg)
		{
			var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
			if (admin == null) return;

			if (msg.ToClone.Equals(NetId.Invalid))
			{
				Logger.LogWarning("Attempted to clone an object with invalid netID, clone will not occur.", Category.Admin);
			}
			else
			{
				LoadNetworkObject(msg.ToClone);
				if (MatrixManager.IsPassableAtAllMatricesOneTile(msg.WorldPosition.RoundToInt(), true))
				{
					Spawn.ServerClone(NetworkObject, msg.WorldPosition);
					UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
						$"{admin.Player().Username} spawned a clone of {NetworkObject} at {msg.WorldPosition}", msg.AdminId);
				}
			}
		}

		/// <summary>
		/// Ask the server to clone a specific object
		/// </summary>
		/// <param name="toClone">GameObject to clone, must have a network identity</param>
		/// <param name="worldPosition">world position to spawn it at</param>
		/// <param name="adminId">user id of the admin trying to perform this action</param>
		/// <param name="adminToken">token of the admin trying to perform this action</param>
		/// <returns></returns>
		public static void Send(GameObject toClone, Vector2 worldPosition, string adminId, string adminToken)
		{

			NetMessage msg = new NetMessage
			{
				ToClone = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				WorldPosition = worldPosition,
				AdminId = adminId,
				AdminToken = adminToken
			};

			Send(msg);
		}
	}
}
