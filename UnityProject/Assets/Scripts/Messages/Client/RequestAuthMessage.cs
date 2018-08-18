using System.Collections;
using UnityEngine.Networking;
using Facepunch.Steamworks;


public class RequestAuthMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestAuthMessage;
	public ulong SteamID;
	public byte[] TicketBinary;

	public override IEnumerator Process()
	{
		//	Logger.Log("Processed " + ToString());
		yield return WaitFor(SentBy);

//		if ( !Managers.instance.isForRelease )
//		{
//			Logger.Log($"Ignoring {this}, not for release");
//			yield break;
//		}
		
		var connectedPlayer = PlayerList.Instance.Get( NetworkObject );

		if ( PlayerList.IsConnWhitelisted(connectedPlayer) )
		{ 
			Logger.Log( $"Player whitelisted: {connectedPlayer}", Category.Steam);
			yield break;
		}
		if ( connectedPlayer.SteamId != 0 )
		{
			Logger.Log( $"Player already authenticated: {connectedPlayer}", Category.Steam );
			yield break;
		}
		
//		Logger.Log("Server Starting Auth for User:" + SteamID);
		if (Server.Instance != null && SteamID != 0 && TicketBinary != null)
		{
			
			//This results in a callback in CustomNetworkManager
			if (!Server.Instance.Auth.StartSession(TicketBinary, SteamID))
			{
				// This can trigger for a lot of reasons
				// More info: http://projectzomboid.com/modding//net/puppygames/steam/BeginAuthSessionResult.html
				// if triggered does prevent the authchange callback.
//				Logger.Log("Start Session returned false, kicking");
				CustomNetworkManager.Kick( connectedPlayer, "Steam auth failed" );
			}
			else
			{
				connectedPlayer.SteamId = SteamID;
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
		return $"[RequestAuthMessage SteamID={SteamID} TicketBinary={TicketBinary} Type={MessageType} SentBy={SentBy}]";
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