using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestBodyParts : ClientMessage
{
	public uint BeingPerformedOn;
	public BodyPartType TargetBodyPart;

	public override void Process()
	{
		//Need to validate has tool
		if (BeingPerformedOn == NetId.Invalid) return;
		LoadNetworkObject(BeingPerformedOn);
		if (Validations.CanApply(SentByPlayer.Script, NetworkObject, NetworkSide.Server) == false) return;
		var Dissectible = NetworkObject.GetComponent<Dissectible>();
		if (Dissectible == null) return;
		Dissectible.SendClientBodyParts(SentByPlayer,TargetBodyPart);
	}

	public static RequestBodyParts Send(GameObject InBeingPerformedOn, BodyPartType inTargetBodyPart = BodyPartType.None)
	{
		RequestBodyParts RequestSurgeryMSG = new RequestBodyParts()
		{
			BeingPerformedOn = InBeingPerformedOn
				? InBeingPerformedOn.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			TargetBodyPart = inTargetBodyPart
		};
		RequestSurgeryMSG.Send();
		return RequestSurgeryMSG;
	}
}
