using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update overall health stats
/// </summary>
public class HealthOverallMessage : ServerMessage
{
	public class HealthOverallMessageNetMessage : NetworkMessage
	{
		public uint EntityToUpdate;
		public float OverallHealth;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as HealthOverallMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientHealthStats(newMsg.OverallHealth);
	}

	public static HealthOverallMessageNetMessage Send(GameObject recipient, GameObject entityToUpdate, float overallHealth)
	{
		HealthOverallMessageNetMessage msg = new HealthOverallMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
		};

		new HealthOverallMessage().SendTo(recipient, msg);
		return msg;
	}

	public static HealthOverallMessageNetMessage SendToAll(GameObject entityToUpdate, float overallHealth)
	{
		HealthOverallMessageNetMessage msg = new HealthOverallMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
		};
		new HealthOverallMessage().SendToAll(msg);
		return msg;
	}
}