using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class StorageObjectUUIDSyncMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.StorageObjectUUIDSyncMessage;
	public NetworkInstanceId Recipient;
	public NetworkInstanceId StorageObj;
	public string Data;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient, StorageObj);
		NetworkObjects[1].GetComponent<StorageObject>().SyncUUIDs(Data);
	}

	public static StorageObjectUUIDSyncMessage Send(GameObject recipient, GameObject storageObj, string data)
	{
		StorageObjectUUIDSyncMessage msg = new StorageObjectUUIDSyncMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				StorageObj = storageObj.GetComponent<NetworkIdentity>().netId,
				Data = data
		};
		msg.SendTo(recipient);
		return msg;
	}
}