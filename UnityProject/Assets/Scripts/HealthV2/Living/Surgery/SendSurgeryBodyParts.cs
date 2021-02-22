using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class SendSurgeryBodyParts : ServerMessage
{

	public string JsonData;
	public uint dissectible;

    public override void Process()
    {
	    LoadNetworkObject(dissectible);
	    List<uint> NetIDs = JsonConvert.DeserializeObject<List<uint>>(JsonData);
	    LoadMultipleObjects(NetIDs.ToArray());
	    List<BodyPart> BodyParts = new List<BodyPart>();
	    foreach (var OObject in NetworkObjects)
	    {
		    if (OObject == null) continue;
		    BodyParts.Add(OObject.GetComponent<BodyPart>());
	    }

	    NetworkObject.GetComponent<Dissectible>().ReceivedSurgery(BodyParts);

    }

    public static SendSurgeryBodyParts SendTo(List<BodyPart> BodyParts, Dissectible InDissectible ,  ConnectedPlayer Player)
    {

	    List<uint> NetIDs = new List<uint>();
	    foreach (var BodyPart in BodyParts)
	    {
		    NetIDs.Add(BodyPart  ? BodyPart.GetComponent<NetworkIdentity>().netId
			    : NetId.Invalid);
	    }

	    SendSurgeryBodyParts  msg =
		    new SendSurgeryBodyParts
		    {
			    JsonData = JsonConvert.SerializeObject(NetIDs),
			    dissectible = InDissectible  ? InDissectible.GetComponent<NetworkIdentity>().netId
			    : NetId.Invalid,
		    };

	    msg.SendTo(Player);
	    return msg;
    }
}
