using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server tells client that a spawn has just occurred and client should invoke the appropriate
/// hooks.
/// </summary>
public class SpawnMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.SpawnMessage;
	public uint SpawnedObject;
	public uint ClonedFrom;

	public override IEnumerator Process()
	{
		yield return WaitFor(SpawnedObject, ClonedFrom);

		if (NetworkObjects[0] == null)
		{
			Logger.LogWarning("Couldn't resolve SpawnedObject!", Category.NetMessage);
			yield break;
		}
		if (NetworkObjects[1] == null)
		{
			Logger.LogWarning("Couldn't resolve ClonedFrom!", Category.NetMessage);
			yield break;
		}

		//call all the hooks!
		var comps = NetworkObjects[0].GetComponents<IClientSpawn>();
		var spawnInfo = ClientSpawnInfo.Create(NetworkObjects[1]);
		if (comps != null)
		{
			foreach (var comp in comps)
			{
				comp.OnSpawnClient(spawnInfo);
			}
		}
	}

	/// <summary>
	/// Inform the client about this spawn event
	/// </summary>
	/// <param name="recipient">client to inform</param>
	/// <param name="result">results of the spawn that was already performed server side</param>
	/// <returns></returns>
	public static void Send(GameObject recipient, SpawnResult result)
	{
		foreach (var spawnedObject in result.GameObjects)
		{
			SpawnMessage msg = new SpawnMessage
			{
				SpawnedObject = spawnedObject.NetId(),
				ClonedFrom = result.SpawnInfo.SpawnType == SpawnType.Clone ? result.SpawnInfo.ClonedFrom.NetId() : NetId.Invalid
			};
			msg.SendTo(recipient);
		}
	}

	/// <summary>
	/// Inform all clients about this spawn event
	/// </summary>
	/// <param name="result">results of the spawn that was already performed server side</param>
	/// <returns></returns>
	public static void SendToAll(SpawnResult result)
	{
		foreach (var spawnedObject in result.GameObjects)
		{
			SpawnMessage msg = new SpawnMessage
			{
				SpawnedObject = spawnedObject.NetId(),
				ClonedFrom = result.SpawnInfo.SpawnType == SpawnType.Clone ? result.SpawnInfo.ClonedFrom.NetId() : NetId.Invalid
			};
			msg.SendToAll();
		}
	}
}