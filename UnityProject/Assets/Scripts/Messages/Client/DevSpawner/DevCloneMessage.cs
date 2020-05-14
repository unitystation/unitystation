using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Message allowing a client dev / admin to clone something, validated server side.
/// </summary>
public class DevCloneMessage : ClientMessage
{
	// Net ID of the object to clone
	public uint ToClone;
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

		if (ToClone.Equals(NetId.Invalid))
		{
			Logger.LogWarning("Attempted to clone an object with invalid netID, clone will not occur.", Category.ItemSpawn);
		}
		else
		{
			LoadNetworkObject(ToClone);
			if (MatrixManager.IsPassableAt(WorldPosition.RoundToInt(), true))
			{
				Spawn.ServerClone(NetworkObject, WorldPosition);
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{admin.ExpensiveName()} spawned a clone of {NetworkObject} at {WorldPosition}", AdminId);
			}
		}
	}

	public override string ToString()
	{
		return $"[DevCloneMessage ToClone={ToClone} WorldPosition={WorldPosition}]";
	}

	/// <summary>
	/// Ask the server to clone a specific object
	/// </summary>
	/// <param name="toClone">GameObject to clone, must have a network identity</param>
	/// <param name="worldPosition">world position to spawn it at</param>
	/// <param name="adminId">user id of the admin trying to perform this action</param>
	/// <param name="adminToken">token of the admin trying to perform this action</param>
	/// <returns></returns>
	public static void Send(GameObject toClone, Vector2 worldPosition, string adminId, string adminToken)
	{

		DevCloneMessage msg = new DevCloneMessage
		{
			ToClone = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			WorldPosition = worldPosition,
			AdminId = adminId,
			AdminToken = adminToken
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ToClone = reader.ReadUInt32();
		WorldPosition = reader.ReadVector2();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(ToClone);
		writer.WriteVector2(WorldPosition);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}
