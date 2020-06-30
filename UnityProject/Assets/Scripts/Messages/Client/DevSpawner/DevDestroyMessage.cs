using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Message allowing a client dev / admin to clone something, validated server side.
/// </summary>
public class DevDestroyMessage : ClientMessage
{
	// Net ID of the object to destroy
	public uint ToDestroy;
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

		if (ToDestroy.Equals(NetId.Invalid))
		{
			Logger.LogWarning("Attempted to destroy an object with invalid netID, destroy will not occur.", Category.ItemSpawn);
		}
		else
		{
			LoadNetworkObject(ToDestroy);

			if (NetworkObject == null) return;

			Vector2Int worldPos = NetworkObject.transform.position.To2Int();
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{admin.ExpensiveName()} destroyed a {NetworkObject} at {worldPos}", AdminId);
			Despawn.ServerSingle(NetworkObject);
		}
	}


	public override string ToString()
	{
		return $"[DevDestroyMessage ToClone={ToDestroy}]";
	}

	/// <summary>
	/// Ask the server to destroy a specific object
	/// </summary>
	/// <param name="toClone">GameObject to destroy, must have a network identity</param>
	/// <param name="adminId">user id of the admin trying to perform this action</param>
	/// <param name="adminToken">token of the admin trying to perform this action</param>
	/// <returns></returns>
	public static void Send(GameObject toClone, string adminId, string adminToken)
	{

		DevDestroyMessage msg = new DevDestroyMessage
		{
			ToDestroy = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			AdminId = adminId,
			AdminToken = adminToken
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ToDestroy = reader.ReadUInt32();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(ToDestroy);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}
