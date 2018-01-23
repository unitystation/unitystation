using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;


public class RequestAuthMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestAuthMessage;
	public NetworkInstanceId Subject;
	public ulong SteamID;
	public byte[] TicketBinary;

	public override IEnumerator Process()
	{
		//		Debug.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		Debug.Log("SimpleInteractMessage: doing nothing");
		//TODO Point at auth, or add auth to this message
//		NetworkObjects[0].GetComponent<PlayerScript>().ExecuteAuth(TicketBinary, SteamID);
	}

	public static RequestAuthMessage Send(GameObject subject, ulong steamid, byte[] ticketBinary)
	{
		RequestAuthMessage msg = new RequestAuthMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
			SteamID = steamid,
			TicketBinary = ticketBinary
				
		};
		
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[SimpleInteractMessage Subject={0} Type={1} SentBy={2}]", Subject, MessageType, SentBy);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Subject = reader.ReadNetworkId();
		SteamID = reader.ReadUInt64();
		int length = reader.ReadInt32();
		TicketBinary = reader.ReadBytes(length);
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Subject);
		writer.Write(SteamID);
		writer.Write(TicketBinary.Length);
		writer.Write(TicketBinary, TicketBinary.Length);
	}
}