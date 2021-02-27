using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update temperature
	/// </summary>
	public class HealthTemperatureMessage : ServerMessage<HealthTemperatureMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float temperature;
		}

		public override void Process(NetMessage msg)
		{
			if(PlayerManager.LocalPlayerScript.playerHealth == null) return;

			PlayerManager.LocalPlayerScript.playerHealth.UpdateClientTemperatureStats(msg.temperature);
		}

		public static NetMessage Send(GameObject entityToUpdate, float temperatureValue)
		{
			NetMessage msg = new NetMessage
			{
				temperature = temperatureValue
			};

			SendTo(entityToUpdate, msg);
			return msg;
		}
	}
}
