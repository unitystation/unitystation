﻿using Mirror;
using UnityEngine;

namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to clone something, validated server side.
	/// </summary>
	public class DevDestroyMessage : ClientMessage<DevDestroyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// Net ID of the object to destroy
			public uint ToDestroy;
			public string AdminId;
			public string AdminToken;

			public override string ToString()
			{
				return $"[DevDestroyMessage ToClone={ToDestroy}]";
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

			if (msg.ToDestroy.Equals(NetId.Invalid))
			{
				Logger.LogWarning("Attempted to destroy an object with invalid netID, destroy will not occur.", Category.ItemSpawn);
			}
			else
			{
				LoadNetworkObject(msg.ToDestroy);

				if (NetworkObject == null) return;

				Vector2Int worldPos = NetworkObject.transform.position.To2Int();
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{admin.Player().Username} destroyed a {NetworkObject} at {worldPos}", msg.AdminId);
				Despawn.ServerSingle(NetworkObject);
			}
		}

		/// <summary>
		/// Ask the server to destroy a specific object
		/// </summary>
		/// <param name="toClone">GameObject to destroy, must have a network identity</param>
		/// <param name="adminId">user id of the admin trying to perform this action</param>
		/// <param name="adminToken">token of the admin trying to perform this action</param>
		/// <returns></returns>
		public static void Send(GameObject toClone, string adminId, string adminToken)
		{

			NetMessage msg = new NetMessage
			{
				ToDestroy = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				AdminId = adminId,
				AdminToken = adminToken
			};
			Send(msg);
		}
	}
}
