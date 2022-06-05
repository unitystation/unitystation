using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class SendSurgeryBodyParts : ServerMessage<SendSurgeryBodyParts.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string JsonData;
		public uint dissectible;
	}

	public override void Process(NetMessage msg)
    {
	    LoadNetworkObject(msg.dissectible);
	    List<uint> NetIDs = JsonConvert.DeserializeObject<List<uint>>(msg.JsonData);
	    LoadMultipleObjects(NetIDs.ToArray());
	    List<BodyPart> BodyParts = new List<BodyPart>();
	    foreach (var OObject in NetworkObjects)
	    {
		    if (OObject == null) continue;
		    BodyParts.Add(OObject.GetComponent<BodyPart>());
	    }

	    NetworkObject.GetComponent<Dissectible>().ReceivedSurgery(BodyParts);

    }

    public static NetMessage SendTo(List<BodyPart> BodyParts, Dissectible InDissectible ,  PlayerInfo Player)
    {

	    List<uint> NetIDs = new List<uint>();
	    foreach (var BodyPart in BodyParts)
	    {
		    NetIDs.Add(BodyPart  ? BodyPart.GetComponent<NetworkIdentity>().netId
			    : NetId.Invalid);
	    }

	    NetMessage  msg =
		    new NetMessage
		    {
			    JsonData = JsonConvert.SerializeObject(NetIDs),
			    dissectible = InDissectible  ? InDissectible.GetComponent<NetworkIdentity>().netId
			    : NetId.Invalid,
		    };

	    SendTo(Player,msg);
	    return msg;
    }
}
