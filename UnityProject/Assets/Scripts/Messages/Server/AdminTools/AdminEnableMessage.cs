using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
			public string[] AdminToken;
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

			PlayerList.Instance.SetClientTAGS(msg.AdminToken);
			if (PlayerList.HasTAGClient(TAG.ADMIN_CHAT))
			{
				UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
			}

			if (PlayerList.HasTAGClient(TAG.MENTOR_MESSAGE))
			{
				UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
			}

		}

		public static void SendMessage(PlayerInfo player, List<string> Tags)
		{
			UpdateManager.Instance.StartCoroutine( SendMessageCo(player, Tags));
		}

		private static IEnumerator SendMessageCo(PlayerInfo player, List<string> Tags)
		{

			yield return WaitFor.Seconds(5);
			ItemStorage adminGhostItemStorage = null;

			try
			{
				UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
				adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}


			Send(player, Tags, adminGhostItemStorage?.GetComponent<NetworkIdentity>()?.netId);
		}

		private static NetMessage Send(PlayerInfo player, List<string> Tags, uint? netId)
		{
			NetMessage msg = new NetMessage
			{
				AdminToken = Tags.ToArray(),
				AdminGhostStorage = netId ?? NetId.Empty
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}
