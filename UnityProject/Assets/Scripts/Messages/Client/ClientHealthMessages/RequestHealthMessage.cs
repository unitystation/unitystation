using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Requests update of all health stats for a living entity from server
/// </summary>
public class RequestHealthMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestHealthStats;
	public NetworkInstanceId LivingEntity;

	public override IEnumerator Process()
	{
		yield return WaitFor(LivingEntity, SentBy);

		NetworkObjects[0].GetComponent<HealthStateMonitor>().ProcessClientUpdateRequest(NetworkObjects[1]);
	}

	public static RequestHealthMessage Send(GameObject entity)
	{
		RequestHealthMessage msg = new RequestHealthMessage
		{
			LivingEntity = entity.GetComponent<NetworkIdentity>().netId,
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		LivingEntity = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(LivingEntity);
	}
}
