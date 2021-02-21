using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update conscious state
/// </summary>
public class HealthConsciousMessage : ServerMessage
{
	public class HealthConsciousMessageNetMessage : NetworkMessage
	{
		public uint EntityToUpdate;
		public ConsciousState ConsciousState;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as HealthConsciousMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.EntityToUpdate);
		if (NetworkObject == null)
		{
			return;
		}

		var healthBehaviour = NetworkObject.GetComponent<LivingHealthBehaviour>();

		if (healthBehaviour != null)
		{
			healthBehaviour.UpdateClientConsciousState(newMsg.ConsciousState);
		}
		else
		{
			Logger.Log($"Living health behaviour not found for {NetworkObject.ExpensiveName()} skipping conscious state update", Category.Health);
		}
	}

	public static HealthConsciousMessageNetMessage Send(GameObject recipient, GameObject entityToUpdate, ConsciousState consciousState)
	{
		HealthConsciousMessageNetMessage msg = new HealthConsciousMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			ConsciousState = consciousState
		};
		new HealthConsciousMessage().SendTo(recipient, msg);
		return msg;
	}

	public static HealthConsciousMessageNetMessage SendToAll(GameObject entityToUpdate, ConsciousState consciousState)
	{
		HealthConsciousMessageNetMessage msg = new HealthConsciousMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			ConsciousState = consciousState
		};
		new HealthConsciousMessage().SendToAll(msg);
		return msg;
	}
}