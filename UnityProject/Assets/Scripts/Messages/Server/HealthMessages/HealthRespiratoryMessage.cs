using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update respiratory health stats
	/// </summary>
	public class HealthRespiratoryMessage : ServerMessage<HealthRespiratoryMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool IsSuffocating;
		}

		public override void Process(NetMessage msg)
		{
			if (PlayerManager.LocalPlayerScript.playerHealth == null) return;

			PlayerManager.LocalPlayerScript.playerHealth.UpdateClientRespiratoryStats(msg.IsSuffocating);
		}

		public static NetMessage Send(GameObject entityToUpdate, bool IsSuffocating)
		{
			NetMessage msg = new NetMessage
			{
				IsSuffocating = IsSuffocating
			};

			SendTo(entityToUpdate, msg);
			return msg;
		}
	}
}
