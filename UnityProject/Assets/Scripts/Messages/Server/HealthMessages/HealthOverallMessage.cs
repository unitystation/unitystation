using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update overall health stats and conscious state
/// </summary>
public class HealthOverallMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthOverallStats;

	public NetworkInstanceId EntityToUpdate;
	public int OverallHealth;
	public ConsciousState ConsciousState;

	public UI_PressureAlert.PressureChecker PressureStatus;
	public UI_TempAlert.TempChecker TempStatus;
	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientHealthStats(OverallHealth, ConsciousState);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientGauges(PressureStatus, TempStatus);
	}

	public static HealthOverallMessage Send(GameObject recipient, GameObject entityToUpdate, int overallHealth, ConsciousState consciousState, UI_PressureAlert.PressureChecker pressureStatus, UI_TempAlert.TempChecker tempStatus)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
				ConsciousState = consciousState,
				PressureStatus = pressureStatus,
				TempStatus = tempStatus,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthOverallMessage SendToAll(GameObject entityToUpdate, int overallHealth, ConsciousState consciousState, UI_PressureAlert.PressureChecker pressureStatus, UI_TempAlert.TempChecker tempStatus)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
				ConsciousState = consciousState,
				PressureStatus = pressureStatus,
				TempStatus = tempStatus,

		};
		msg.SendToAll();
		return msg;
	}
}