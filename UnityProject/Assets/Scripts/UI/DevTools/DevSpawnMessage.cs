using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Message allowing a client dev / admin to spawn something, validated server side.
/// </summary>
public class DevSpawnMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.DevSpawnMessage;
	// name of the prefab or hier string to spawn
	public string Name;
	// true iff Name is a hier string for spawning a unicloth. False if Name is a prefab name.
	public bool IsUniCloth;
	// position to spawn at.
	public Vector2 WorldPosition;

	public override IEnumerator Process()
	{
		//TODO: Validate if player is allowed to spawn things, check if they have admin privs.
		//For now we will let anyone spawn.

		var pos = WorldPosition.RoundToInt();
		var isPassable = MatrixManager.IsPassableAt(pos, true);
		var isTableAt = MatrixManager.IsTableAt(pos, true);

		if (isPassable || isTableAt)
		{
			if (IsUniCloth)
			{
				var clothData = Spawn.ClothingStoredData[Name];
				Spawn.ServerCloth(clothData, WorldPosition);
			}
			else
			{
				Spawn.ServerPrefab(Name, WorldPosition);
			}
		}

		yield return null;
	}

	public override string ToString()
	{
		return $"[DevSpawnMessage Name={Name} IsUniCloth={IsUniCloth} WorldPosition={WorldPosition}]";
	}

	/// <summary>
	/// Ask the server to spawn a specific prefab
	/// </summary>
	/// <param name="name">name of the prefab to instantiate, or the hier of the unicloth to instantiate (network synced)</param>
	/// <param name="isUniCloth">true iff name is a hier (for a unicloth), false if name is a prefab</param>
	/// <param name="worldPosition">world position to spawn it at</param>
	/// <returns></returns>
	public static void Send(string name, bool isUniCloth, Vector2 worldPosition)
	{

		DevSpawnMessage msg = new DevSpawnMessage
		{
			Name = name,
			IsUniCloth =  isUniCloth,
			WorldPosition = worldPosition
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Name = reader.ReadString();
		IsUniCloth = reader.ReadBoolean();
		WorldPosition = reader.ReadVector2();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Name);
		writer.WriteBoolean(IsUniCloth);
		writer.WriteVector2(WorldPosition);
	}
}
