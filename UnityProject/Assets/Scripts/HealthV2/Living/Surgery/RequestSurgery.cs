using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestSurgery : ClientMessage
{
	public uint BeingPerformedOn;
	public int SurgeryProcedureBase;
	public uint BodyPart;
	public override void Process()
	{
		if (BeingPerformedOn == NetId.Invalid) return;
		LoadMultipleObjects(new uint[]{BeingPerformedOn,BodyPart} );
		if (SurgeryProcedureBase >= SurgeryProcedureBaseSingleton.Instance.StoredReferences.Count) return;
		if (Validations.CanApply(SentByPlayer.Script, NetworkObjects[0], NetworkSide.Server) == false) return;
		var Dissectible = NetworkObjects[0].GetComponent<Dissectible>();
		if (Dissectible == null) return;

		var EBodyPart = NetworkObjects[1].GetComponent<BodyPart>();
		if (EBodyPart == null) return;

		var InSurgeryProcedureBase = SurgeryProcedureBaseSingleton.Instance.StoredReferences[SurgeryProcedureBase];
		Dissectible.ServerCheck(InSurgeryProcedureBase ,EBodyPart);
	}

	public static RequestSurgery Send(GameObject bodyPart, GameObject InBeingPerformedOn,
		SurgeryProcedureBase InSurgeryProcedureBase)
	{
		RequestSurgery RequestSurgeryMSG = new RequestSurgery()
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
		RequestSurgeryMSG.Send();
		return RequestSurgeryMSG;
	}
}