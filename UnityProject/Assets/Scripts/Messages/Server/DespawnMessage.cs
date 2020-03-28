using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server tells client that a despawn has just occurred and client should invoke the appropriate
/// hooks.
/// </summary>
public class DespawnMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.DespawnMessage;
	public uint DespawnedObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(DespawnedObject);

		//call all the hooks!
		if (NetworkObject == null)
		{
			Logger.LogTraceFormat("Not calling client side despawn hooks, object no longer exists", Category.Inventory);
		}
		else
		{
			var comps = NetworkObject.GetComponents<IClientDespawn>();
			var despawnInfo = ClientDespawnInfo.Default();
			if (comps != null)
			{
				foreach (var comp in comps)
				{
					comp.OnDespawnClient(despawnInfo);
				}
			}
		}
	}

	/// <summary>
	/// Inform all clients about this despawn event
	/// </summary>
	/// <param name="recipient">client to inform</param>
	/// <param name="result">results of the spawn that was already performed server side</param>
	/// <returns></returns>
	public static void Send(GameObject recipient, DespawnResult result)
	{
		DespawnMessage msg = new DespawnMessage
		{
			DespawnedObject = result.GameObject.NetId()
		};
		msg.SendTo(recipient);
	}

	/// <summary>
	/// Inform all clients about this despawn event
	/// </summary>
	/// <param name="result">results of the despawn that was already performed server side</param>
	/// <returns></returns>
	public static void SendToAll(DespawnResult result)
	{
		DespawnMessage msg = new DespawnMessage
		{
			DespawnedObject = result.GameObject.NetId()
		};
		msg.SendToAll();
	}
}