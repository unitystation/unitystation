using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

public class AdminInfoUpdateMessage : ServerMessage
{
	public string JsonData;
	public bool FullUpdate;

	public override void Process()
	{
		if (FullUpdate)
		{
			AdminOverlay.ClientFullUpdate(JsonUtility.FromJson<AdminInfoUpdate>(JsonData));
		}
		else
		{
			AdminOverlay.ClientAddEntry(JsonUtility.FromJson<AdminInfosEntry>(JsonData));
		}
	}

	public static AdminInfoUpdateMessage SendFullUpdate(GameObject recipient, Dictionary<uint, AdminInfo> infoEntries)
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

		AdminInfoUpdateMessage  msg =
			new AdminInfoUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
				FullUpdate = true
			};

		msg.SendTo(recipient);
		return msg;
	}

	public static AdminInfoUpdateMessage SendEntryToAllAdmins(AdminInfosEntry entry)
	{
		AdminInfoUpdateMessage  msg =
			new AdminInfoUpdateMessage
			{
				JsonData = JsonUtility.ToJson(entry),
				FullUpdate = false
			};

		msg.SendToAdmins();
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
