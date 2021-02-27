using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells client which UI action to perform
	/// </summary>
	public class UpdateHungerStateMessage : ServerMessage<UpdateHungerStateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public HungerState State;
		}

		public override void Process(NetMessage msg)
		{
			MetabolismSystem metabolismSystem = PlayerManager.LocalPlayer.GetComponent<MetabolismSystem>();
			metabolismSystem.SetHungerState(msg.State);
		}

		public static NetMessage Send(GameObject recipient, HungerState state)
		{
			NetMessage msg = new NetMessage
			{
				State = state
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
