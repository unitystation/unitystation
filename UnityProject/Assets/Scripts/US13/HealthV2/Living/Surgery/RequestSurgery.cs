using Mirror;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Messages.Client;
using Util;

namespace US13.HealthV2.Living.Surgery
{
	public class RequestSurgery : ClientMessage<RequestSurgery.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint BeingPerformedOn;
			public int SurgeryProcedureBase;
			public uint BodyPart;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.BeingPerformedOn == NetId.Invalid) return;
			LoadMultipleObjects(new uint[]{msg.BeingPerformedOn,msg.BodyPart} );
			if (msg.SurgeryProcedureBase >= SurgeryProcedureBaseSingleton.Instance.StoredReferences.Count) return;
			if (Validations.CanApply(SentByPlayer.Script, NetworkObjects[0], NetworkSide.Server) == false) return;
			var dissectible = NetworkObjects[0].GetComponent<Dissectible>();
			if (dissectible == null) return;

			var EBodyPart = NetworkObjects[1]?.GetComponent<BodyPart>();


			var inSurgeryProcedureBase = SurgeryProcedureBaseSingleton.Instance.StoredReferences[msg.SurgeryProcedureBase];
			dissectible.ServerCheck(inSurgeryProcedureBase ,EBodyPart);
		}

		public static NetMessage Send(GameObject bodyPart, GameObject InBeingPerformedOn,
			SurgeryProcedureBase InSurgeryProcedureBase)
		{
			NetMessage RequestSurgeryMSG = new NetMessage()
			{
				SurgeryProcedureBase =
					SurgeryProcedureBaseSingleton.Instance.StoredReferences.IndexOf(InSurgeryProcedureBase),
				BeingPerformedOn = InBeingPerformedOn
					? InBeingPerformedOn.GetComponent<NetworkIdentity>().netId
					: NetId.Invalid,
				BodyPart = bodyPart
					? bodyPart.GetComponent<NetworkIdentity>().netId
					: NetId.Invalid
			};
			Send(RequestSurgeryMSG);
			return RequestSurgeryMSG;
		}
	}
}