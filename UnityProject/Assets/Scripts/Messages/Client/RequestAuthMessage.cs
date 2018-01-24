using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using Facepunch.Steamworks;


public class RequestAuthMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestAuthMessage;
	public ulong SteamID;
	public byte[] TicketBinary;

	public override IEnumerator Process()
	{
		//	Debug.Log("Processed " + ToString());

		yield return WaitFor(SentBy);
		Debug.Log("Server Starting Auth for User:" + SteamID);

		if (Server.Instance != null)
		{
			if (!Server.Instance.Auth.StartSession(TicketBinary, SteamID))
			{
				Debug.Log("Start Session returned false");
			}
		}

	}
	
	public static RequestAuthMessage Send(ulong steamid, byte[] ticketBinary)
	{
		RequestAuthMessage msg = new RequestAuthMessage
		{
			SteamID = steamid,
			TicketBinary = ticketBinary			
		};
		
		msg.Send();
		return msg;
	}


	public override string ToString()
	{
		return string.Format("[RequestAuthMessage SteamID={0} TicketBinary={1} Type={2} SentBy={3}]", SteamID, TicketBinary, MessageType, SentBy);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		SteamID = reader.ReadUInt64();
		int length = reader.ReadInt32();
		TicketBinary = reader.ReadBytes(length);
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(SteamID);
		writer.Write(TicketBinary.Length);
		writer.Write(TicketBinary, TicketBinary.Length);
	}
}