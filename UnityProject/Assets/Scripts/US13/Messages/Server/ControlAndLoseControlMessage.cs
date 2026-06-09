using Mirror;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Systems;
using Util;

namespace US13.Messages.Server
{
	public class ControlAndLoseControlMessage : ServerMessage<ControlAndLoseControlMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint UnPossessingObject;
			public uint PossessingObject;
		}

		public override void Process(NetMessage msg)
		{

			LoadMultipleObjects(new uint[]{msg.UnPossessingObject,msg.PossessingObject });



			if (NetworkObjects[0] != null)
			{

				var Components = NetworkObjects[0].GetComponents<IClientPlayerLeaveBody>();
				foreach (var Component in Components)
				{
					Component.ClientOnPlayerLeaveBody();
				}

				ClientSynchronisedEffectsManager.Instance.LeavingBody(msg.UnPossessingObject);

			}

			if (NetworkObjects[1] != null)
			{
				var Components = NetworkObjects[1].GetComponents<IClientPlayerTransferProcess>();
				foreach (var Component in Components)
				{
					Component.ClientOnPlayerTransferProcess();
				}
				ClientSynchronisedEffectsManager.Instance.EnterBody(msg.PossessingObject);
			}


		}

		public static NetMessage Send(GameObject recipient, GameObject PossessingObject, GameObject UnPossessingObject)
		{
			NetMessage msg = new NetMessage
			{
				UnPossessingObject = UnPossessingObject.NetId(),
				PossessingObject = PossessingObject.NetId(),

			};
			SendTo(recipient, msg);
			return msg;
		}
	}
}
