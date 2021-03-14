using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Client;
using Mirror;
using UnityEngine;

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
		var Dissectible = NetworkObjects[0].GetComponent<Dissectible>();
		if (Dissectible == null) return;

		var EBodyPart = NetworkObjects[1]?.GetComponent<BodyPart>();


		var InSurgeryProcedureBase = SurgeryProcedureBaseSingleton.Instance.StoredReferences[msg.SurgeryProcedureBase];
		Dissectible.ServerCheck(InSurgeryProcedureBase ,EBodyPart);
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