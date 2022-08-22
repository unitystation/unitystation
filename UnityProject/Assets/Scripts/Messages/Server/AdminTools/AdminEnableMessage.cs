using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Threading;
using Managers;
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

		public static void SendMessage(PlayerInfo player, string adminToken)
		{
			_ = SendMessageCo(player, adminToken);
		}

		private static async Task SendMessageCo(PlayerInfo player, string adminToken)
		{

			await Task.Delay(3000);
			ItemStorage adminGhostItemStorage = null;

			UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
			adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);

			Send(player, adminToken, adminGhostItemStorage.GetComponent<NetworkIdentity>().netId);
		}

		private static NetMessage Send(PlayerInfo player, string adminToken, uint netId)
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
