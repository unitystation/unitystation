using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Message allowing a client dev / admin to spawn something, validated server side.
/// </summary>
public class DevSpawnMessage : ClientMessage
{
	// asset ID of the prefab to spawn
	public Guid PrefabAssetID;
	// position to spawn at.
	public Vector2 WorldPosition;
	public string AdminId;
	public string AdminToken;

	public override void Process()
	{
		ValidateAdmin();
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		//no longer checks impassability, spawn anywhere, go hog wild.
		if (ClientScene.prefabs.TryGetValue(PrefabAssetID, out var prefab))
		{
			Spawn.ServerPrefab(prefab, WorldPosition);
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{admin.ExpensiveName()} spawned a {prefab.name} at {WorldPosition}", AdminId);
		}
		else
		{
			Logger.LogWarningFormat("An admin attempted to spawn prefab with invalid asset ID {0}, which" +
			                        " is not found in Mirror.ClientScene. Spawn will not" +
			                        " occur.", Category.Admin, PrefabAssetID);
		}

	}

	public override string ToString()
	{
		return $"[DevSpawnMessage PrefabAssetID={PrefabAssetID} WorldPosition={WorldPosition}]";
	}

	/// <summary>
	/// Ask the server to spawn a specific prefab
	/// </summary>
	/// <param name="prefab">prefab to instantiate, must be networked (have networkidentity)</param>
	/// <param name="worldPosition">world position to spawn it at</param>
	/// <param name="adminId">user id of the admin trying to perform this action</param>
	/// <param name="adminToken">token of the admin trying to perform this action</param>
	/// <returns></returns>
	public static void Send(GameObject prefab, Vector2 worldPosition, string adminId, string adminToken)
	{
		if (prefab.TryGetComponent<NetworkIdentity>(out var networkIdentity))
		{
			DevSpawnMessage msg = new DevSpawnMessage
			{
				PrefabAssetID = networkIdentity.assetId,
				WorldPosition = worldPosition,
				AdminId = adminId,
				AdminToken = adminToken
			};
			msg.Send();
		}
		else
		{
			Logger.LogWarningFormat("Prefab {0} which you are attempting to spawn has no NetworkIdentity, thus cannot" +
			                        " be spawned.", Category.Admin, prefab);
		}
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PrefabAssetID = reader.ReadGuid();
		WorldPosition = reader.ReadVector2();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteGuid(PrefabAssetID);
		writer.WriteVector2(WorldPosition);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}
