using System;
using UnityEngine;
using Mirror;


namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to spawn something, validated server side.
	/// </summary>
	public class DevSpawnMessage : ClientMessage<DevSpawnMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// asset ID of the prefab to spawn
			public Guid PrefabAssetID;
			// position to spawn at.
			public Vector2 WorldPosition;

			public override string ToString()
			{
				return $"[DevSpawnMessage PrefabAssetID={PrefabAssetID} WorldPosition={WorldPosition}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			//no longer checks impassability, spawn anywhere, go hog wild.
			if (ClientScene.prefabs.TryGetValue(msg.PrefabAssetID, out var prefab))
			{
				Spawn.ServerPrefab(prefab, msg.WorldPosition);
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{SentByPlayer.Username} spawned a {prefab.name} at {msg.WorldPosition}", SentByPlayer.UserId);
			}
			else
			{
				Logger.LogWarningFormat("An admin attempted to spawn prefab with invalid asset ID {0}, which" +
				                        " is not found in Mirror.ClientScene. Spawn will not" +
				                        " occur.", Category.Admin, msg.PrefabAssetID);
			}
		}

		/// <summary>
		/// Ask the server to spawn a specific prefab
		/// </summary>
		/// <param name="prefab">prefab to instantiate, must be networked (have networkidentity)</param>
		/// <param name="worldPosition">world position to spawn it at</param>
		/// <param name="adminId">user id of the admin trying to perform this action</param>
		/// <param name="adminToken">token of the admin trying to perform this action</param>
		/// <returns></returns>
		public static void Send(GameObject prefab, Vector2 worldPosition)
		{
			if (prefab.TryGetComponent<NetworkIdentity>(out var networkIdentity))
			{
				NetMessage msg = new NetMessage
				{
					PrefabAssetID = networkIdentity.assetId,
					WorldPosition = worldPosition,
				};
				Send(msg);
			}
			else
			{
				Logger.LogWarningFormat(
						"Prefab {0} which you are attempting to spawn has no NetworkIdentity, thus cannot be spawned.",
						Category.Admin, prefab);
			}
		}
	}
}
