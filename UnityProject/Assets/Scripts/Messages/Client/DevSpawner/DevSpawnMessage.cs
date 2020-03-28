using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Message allowing a client dev / admin to spawn something, validated server side.
/// </summary>
public class DevSpawnMessage : ClientMessage
{
	public override short MessageType => (short) MessageTypes.DevSpawnMessage;
	// name of the prefab or hier string to spawn
	public string Name;
	// position to spawn at.
	public Vector2 WorldPosition;
	public string AdminId;
	public string AdminToken;

	public override IEnumerator Process()
	{
		ValidateAdmin();
		yield return null;
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		//no longer checks impassability, spawn anywhere, go hog wild.
		Spawn.ServerPrefab(Name, WorldPosition);
	}

	public override string ToString()
	{
		return $"[DevSpawnMessage Name={Name} WorldPosition={WorldPosition}]";
	}

	/// <summary>
	/// Ask the server to spawn a specific prefab
	/// </summary>
	/// <param name="name">name of the prefab to instantiate, or the hier of the unicloth to instantiate (network synced)</param>
	/// <param name="isUniCloth">true iff name is a hier (for a unicloth), false if name is a prefab</param>
	/// <param name="worldPosition">world position to spawn it at</param>
	/// <returns></returns>
	public static void Send(string name, Vector2 worldPosition, string adminId, string adminToken)
	{

		DevSpawnMessage msg = new DevSpawnMessage
		{
			Name = name,
			WorldPosition = worldPosition,
			AdminId = adminId,
			AdminToken = adminToken
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Name = reader.ReadString();
		WorldPosition = reader.ReadVector2();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Name);
		writer.WriteVector2(WorldPosition);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}
