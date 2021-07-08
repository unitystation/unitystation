using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestBodyParts : ClientMessage<RequestBodyParts.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint BeingPerformedOn;
		public BodyPartType TargetBodyPart;
	}

	public override void Process(NetMessage msg)
	{
		//Need to validate has tool
		if (msg.BeingPerformedOn == NetId.Invalid) return;
		LoadNetworkObject(msg.BeingPerformedOn);
		if (Validations.CanApply(SentByPlayer.Script, NetworkObject, NetworkSide.Server) == false) return;
		var Dissectible = NetworkObject.GetComponent<Dissectible>();
		if (Dissectible == null) return;
		var RegisterPlayer = NetworkObject.GetComponent<RegisterPlayer>();

		if (RegisterPlayer == null) return; //Player script changes needed
		if (RegisterPlayer.IsLayingDown ==false) return;


		Dissectible.SendClientBodyParts(SentByPlayer,msg.TargetBodyPart);
	}

	public static NetMessage Send(GameObject InBeingPerformedOn, BodyPartType inTargetBodyPart = BodyPartType.None)
	{
		NetMessage RequestSurgeryMSG = new NetMessage()
		{
			BeingPerformedOn = InBeingPerformedOn
				? InBeingPerformedOn.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			TargetBodyPart = inTargetBodyPart
		};
		Send(RequestSurgeryMSG);
		return RequestSurgeryMSG;
	}
}
