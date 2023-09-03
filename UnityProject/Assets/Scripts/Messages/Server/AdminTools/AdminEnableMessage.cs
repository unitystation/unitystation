using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Threading;
using Initialisation;
using Logs;
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
			UpdateManager.Instance.StartCoroutine( SendMessageCo(player, adminToken));
		}

		private static IEnumerator SendMessageCo(PlayerInfo player, string adminToken)
		{

			yield return WaitFor.Seconds(10);
			ItemStorage adminGhostItemStorage = null;

			try
			{
				UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
				adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}


			Send(player, adminToken, adminGhostItemStorage?.GetComponent<NetworkIdentity>()?.netId);
		}

		private static NetMessage Send(PlayerInfo player, string adminToken, uint? netId)
		{
			NetMessage msg = new NetMessage
			{
				AdminToken = adminToken,
				AdminGhostStorage = netId ?? NetId.Empty
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}
