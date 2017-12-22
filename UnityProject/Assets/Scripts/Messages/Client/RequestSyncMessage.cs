using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// A message that is supposed to be sent from client when he's ready to accept sync data(transforms, etc.)
/// Should probably be sent only once 
/// </summary>
public class RequestSyncMessage : ClientMessage<RequestSyncMessage>
{
//	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());

		yield return WaitFor(/*Subject, */SentBy);
		//verify that the message isn't being abused here!
//		Debug.Log("Requested sync");
		CustomNetworkManager.Instance.SyncPlayerData(NetworkObject);
	}

//	public static RequestSyncMessage Send(/*GameObject subject*/)
//	{
//		var msg = new RequestSyncMessage{ /*Subject = subject.GetComponent<NetworkIdentity>().netId*/ };
//		msg.Send();
//		return msg;
//	}

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
