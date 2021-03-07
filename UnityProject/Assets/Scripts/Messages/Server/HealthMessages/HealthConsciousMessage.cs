using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update conscious state
	/// </summary>
	public class HealthConsciousMessage : ServerMessage<HealthConsciousMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EntityToUpdate;
			public ConsciousState ConsciousState;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EntityToUpdate);
			if (NetworkObject == null)
			{
				return;
			}

			var healthBehaviour = NetworkObject.GetComponent<LivingHealthBehaviour>();

			if (healthBehaviour != null)
			{
				healthBehaviour.UpdateClientConsciousState(msg.ConsciousState);
			}
			else
			{
				Logger.Log($"Living health behaviour not found for {NetworkObject.ExpensiveName()} skipping conscious state update", Category.Health);
			}
		}

		public static NetMessage Send(GameObject recipient, GameObject entityToUpdate, ConsciousState consciousState)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				ConsciousState = consciousState
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject entityToUpdate, ConsciousState consciousState)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				ConsciousState = consciousState
			};

			SendToAll(msg);
			return msg;
		}
	}
}