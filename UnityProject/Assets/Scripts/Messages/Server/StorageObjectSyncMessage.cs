using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class StorageObjectSyncMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.StorageObjectSyncMessage;
	public NetworkInstanceId Recipient;
	public NetworkInstanceId StorageObj;
	public string Data;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient, StorageObj);
		NetworkObjects[1].GetComponent<StorageObject>().RefreshStorageItems(Data);
	}

	/// <param name="recipient">Client GO</param>
	/// <param name="slot"></param>
	/// <param name="objectForSlot">Pass null to clear slot</param>
	/// <param name="forced">
	///     Used for client simulation, use false if client's slot is already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	/// <returns></returns>
	public static StorageObjectSyncMessage Send(GameObject recipient, GameObject storageObj, string data)
	{
		StorageObjectSyncMessage msg = new StorageObjectSyncMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				StorageObj = storageObj.GetComponent<NetworkIdentity>().netId,
				Data = data
		};
		msg.SendTo(recipient);
		return msg;
	}
}