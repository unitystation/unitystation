using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update pressure
	/// </summary>
	public class HealthPressureMessage : ServerMessage<HealthPressureMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float pressure;
		}

		public override void Process(NetMessage msg)
		{
			if(PlayerManager.LocalPlayerScript.playerHealth == null) return;

			PlayerManager.LocalPlayerScript.playerHealth.UpdateClientPressureStats(msg.pressure);
		}

		public static NetMessage Send(GameObject entityToUpdate, float pressureValue)
		{
			NetMessage msg = new NetMessage
			{
				pressure = pressureValue
			};

			SendTo(entityToUpdate, msg);
			return msg;
		}
	}
}
