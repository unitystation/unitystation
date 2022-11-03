using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class PossessAndUnpossess : ServerMessage<PossessAndUnpossess.NetMessage>
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
			}

			if (NetworkObjects[1] != null)
			{
				var Components = NetworkObjects[1].GetComponents<IClientPlayerTransferProcess>();
				foreach (var Component in Components)
				{
					Component.ClientOnPlayerTransferProcess();
				}
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
