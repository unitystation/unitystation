using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminInfoUpdateMessage : ServerMessage
{
	public struct AdminInfoUpdateMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public bool FullUpdate;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminInfoUpdateMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminInfoUpdateMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (newMsg.FullUpdate)
		{
			AdminOverlay.ClientFullUpdate(JsonUtility.FromJson<AdminInfoUpdate>(newMsg.JsonData));
		}
		else
		{
			AdminOverlay.ClientAddEntry(JsonUtility.FromJson<AdminInfosEntry>(newMsg.JsonData));
		}
	}

	public static AdminInfoUpdateMessageNetMessage SendFullUpdate(GameObject recipient, Dictionary<uint, AdminInfo> infoEntries)
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

		AdminInfoUpdateMessageNetMessage  msg =
			new AdminInfoUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(update),
				FullUpdate = true
			};

		new AdminInfoUpdateMessage().SendTo(recipient, msg);
		return msg;
	}

	public static AdminInfoUpdateMessageNetMessage SendEntryToAllAdmins(AdminInfosEntry entry)
	{
		AdminInfoUpdateMessageNetMessage  msg =
			new AdminInfoUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(entry),
				FullUpdate = false
			};

		new AdminInfoUpdateMessage().SendToAdmins(msg);
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
