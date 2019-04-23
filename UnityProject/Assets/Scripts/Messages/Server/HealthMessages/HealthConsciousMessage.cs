using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update conscious state
/// </summary>
public class HealthConsciousMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthConsciousState;

	public NetworkInstanceId EntityToUpdate;
	public ConsciousState ConsciousState;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientConsciousState(ConsciousState);
	}

	public static HealthConsciousMessage Send(GameObject recipient, GameObject entityToUpdate, ConsciousState consciousState)
	{
		HealthConsciousMessage msg = new HealthConsciousMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			ConsciousState = consciousState
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthConsciousMessage SendToAll(GameObject entityToUpdate, ConsciousState consciousState)
	{
		HealthConsciousMessage msg = new HealthConsciousMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			ConsciousState = consciousState
		};
		msg.SendToAll();
		return msg;
	}
}