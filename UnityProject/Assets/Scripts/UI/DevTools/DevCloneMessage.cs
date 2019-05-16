using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message allowing a client dev / admin to clone something, validated server side.
/// </summary>
public class DevCloneMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.DevCloneMessage;
	// Net ID of the object to destroy
	public NetworkInstanceId ToClone;
	// position to spawn at.
	public Vector2 WorldPosition;

	public override IEnumerator Process()
	{
		//TODO: Validate if player is allowed to spawn things, check if they have admin privs.
		//For now we will let anyone spawn.

		if (ToClone.Equals(NetworkInstanceId.Invalid))
		{
			Logger.LogWarning("Attempted to clone an object with invalid netID, clone will not occur.", Category.ItemSpawn);
		}
		else
		{
			yield return WaitFor(ToClone);
			if (MatrixManager.IsPassableAt(WorldPosition.RoundToInt(), true))
			{
				PoolManager.NetworkClone(NetworkObject, WorldPosition);
			}

		}

		yield return null;
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
	/// <returns></returns>
	public static void Send(GameObject toClone, Vector2 worldPosition)
	{

		DevCloneMessage msg = new DevCloneMessage
		{
			ToClone = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			WorldPosition = worldPosition
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ToClone = reader.ReadNetworkId();
		WorldPosition = reader.ReadVector2();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ToClone);
		writer.Write(WorldPosition);
	}
}
