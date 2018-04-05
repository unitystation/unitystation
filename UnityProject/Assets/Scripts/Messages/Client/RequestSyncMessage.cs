using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     A message that is supposed to be sent from client when he's ready to accept sync data(transforms, etc.)
///     Should probably be sent only once
/// </summary>
public class RequestSyncMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestSyncMessage;

	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());

		yield return WaitFor(SentBy);

		ConnectedPlayer connectedPlayer = PlayerList.Instance.Get( NetworkObject );
		Debug.Log($"{connectedPlayer} requested sync");
		
		//not sending out sync data for players not ingame 
		if ( connectedPlayer.Job != JobType.NULL && !connectedPlayer.Synced ) {
			CustomNetworkManager.Instance.SyncPlayerData(NetworkObject);
			
			//marking player as synced to avoid sending that data pile again
			connectedPlayer.Synced = true;
		}
	}

	public override string ToString()
	{
		return $"[RequestSyncMessage Type={MessageType} SentBy={SentBy}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
	}
}