using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AdminTools;

namespace Messages.Server.AdminTools
{
	public class AdminInfoUpdateMessage : ServerMessage<AdminInfoUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public AdminInfosEntry[] Entries;
			public bool FullUpdate;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.FullUpdate)
			{
				AdminOverlay.ClientFullUpdate(msg.Entries);
			}
			else
			{
				AdminOverlay.ClientAddEntry(msg.Entries[0]);
			}
		}

		public static NetMessage SendFullUpdate(GameObject recipient, Dictionary<uint, AdminInfo> infoEntries)
		{
			var msg = new NetMessage
			{
				Entries = new AdminInfosEntry[infoEntries.Count],
				FullUpdate = true,
			};

			int i = 0;
			foreach (var kvp in infoEntries)
			{
				if (kvp.Value == null) continue;

				msg.Entries[i] = new AdminInfosEntry
				{
					netId = kvp.Key,
					infos = kvp.Value.StringInfo,
					offset = kvp.Value.OffsetPosition
				};

				i++;
			}

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendEntryToAllAdmins(AdminInfosEntry entry)
		{
			var msg = new NetMessage
			{
				Entries = new AdminInfosEntry[1] { entry },
				FullUpdate = false
			};

			SendToAdmins(msg);
			return msg;
		}
	}

	[Serializable]
	public struct AdminInfosEntry
	{
		public uint netId;
		public Vector2 offset;
		public string infos;
	}
}
