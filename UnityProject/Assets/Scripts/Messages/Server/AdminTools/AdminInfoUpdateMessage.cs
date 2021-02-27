using System;
using System.Collections;
using System.Collections.Generic;
using AdminTools;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminInfoUpdateMessage : ServerMessage<AdminInfoUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public bool FullUpdate;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.FullUpdate)
			{
				AdminOverlay.ClientFullUpdate(JsonUtility.FromJson<AdminInfoUpdate>(msg.JsonData));
			}
			else
			{
				AdminOverlay.ClientAddEntry(JsonUtility.FromJson<AdminInfosEntry>(msg.JsonData));
			}
		}

		public static NetMessage SendFullUpdate(GameObject recipient, Dictionary<uint, AdminInfo> infoEntries)
		{
			var update = new AdminInfoUpdate();

			foreach (var e in infoEntries)
			{
				if (e.Value != null)
				{
					update.entries.Add(new AdminInfosEntry
					{
						netId = e.Key,
						infos = e.Value.StringInfo,
						offset = e.Value.OffsetPosition
					});
				}
			}

			NetMessage  msg =
				new NetMessage
				{
					JsonData = JsonUtility.ToJson(update),
					FullUpdate = true
				};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendEntryToAllAdmins(AdminInfosEntry entry)
		{
			NetMessage  msg =
				new NetMessage
				{
					JsonData = JsonUtility.ToJson(entry),
					FullUpdate = false
				};

			SendToAdmins(msg);
			return msg;
		}
	}

	[SerializeField]
	public class AdminInfoUpdate
	{
		public List<AdminInfosEntry> entries = new List<AdminInfosEntry>();
	}

	[Serializable]
	public class AdminInfosEntry
	{
		public uint netId;
		public Vector2 offset;
		public string infos;
	}
}