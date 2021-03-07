﻿using System.Collections;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	/// <summary>
	/// Allows the client to use admin only features. A valid admin token is required
	/// to use admin tools.
	/// </summary>
	public class AdminEnableMessage : ServerMessage<AdminEnableMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string AdminToken;
			public uint AdminGhostStorage;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.AdminGhostStorage);

			if (NetworkObject == null)
			{
				Debug.LogError("Could not load adminGhostItemStorage");
			}
			else
			{
				AdminManager.Instance.LocalAdminGhostStorage = NetworkObject.GetComponent<ItemStorage>();
			}

			PlayerList.Instance.SetClientAsAdmin(msg.AdminToken);
			UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
			UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
		}

		public static void SendMessage(ConnectedPlayer player, string adminToken)
		{
			SendMessageCo(player, adminToken);
		}

		private static async Task SendMessageCo(ConnectedPlayer player, string adminToken)
		{
			UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
			var adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);

			await Task.Delay(3000);

			Send(player, adminToken, adminGhostItemStorage.GetComponent<NetworkIdentity>().netId);
		}

		private static NetMessage Send(ConnectedPlayer player, string adminToken, uint netId)
		{
			NetMessage msg = new NetMessage
			{
				AdminToken = adminToken,
				AdminGhostStorage = netId
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}