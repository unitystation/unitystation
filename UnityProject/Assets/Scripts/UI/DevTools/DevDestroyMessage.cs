using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message allowing a client dev / admin to clone something, validated server side.
/// </summary>
public class DevDestroyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.DevDestroyMessage;
	// Net ID of the object to destroy
	public NetworkInstanceId ToDestroy;

	public override IEnumerator Process()
	{
		//TODO: Validate if player is allowed to destroy things, check if they have admin privs.
		//For now we will let anyone spawn.

		if (ToDestroy.Equals(NetworkInstanceId.Invalid))
		{
			Logger.LogWarning("Attempted to destroy an object with invalid netID, destroy will not occur.", Category.ItemSpawn);
		}
		else
		{
			yield return WaitFor(ToDestroy);
			PoolManager.PoolNetworkDestroy(NetworkObject);
		}

		yield return null;
	}

	public override string ToString()
	{
		return $"[DevDestroyMessage ToClone={ToDestroy}]";
	}

	/// <summary>
	/// Ask the server to destroy a specific object
	/// </summary>
	/// <param name="toClone">GameObject to destroy, must have a network identity</param>
	/// <returns></returns>
	public static void Send(GameObject toClone)
	{

		DevDestroyMessage msg = new DevDestroyMessage
		{
			ToDestroy = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ToDestroy = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ToDestroy);
	}
}
