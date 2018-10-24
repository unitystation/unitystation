using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class StorageItemSyncMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.StorageItemSyncMessage;
	public NetworkInstanceId Recipient;
	public NetworkInstanceId StorageItem;
	public string Data;

	public override IEnumerator Process()
	{
			yield return WaitFor(Recipient, StorageItem);
			

	}

	/// <param name="recipient">Client GO</param>
	/// <param name="slot"></param>
	/// <param name="objectForSlot">Pass null to clear slot</param>
	/// <param name="forced">
	///     Used for client simulation, use false if client's slot is already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	/// <returns></returns>
	public static StorageItemSyncMessage Send(GameObject recipient, GameObject storageItem, string data)
	{
		StorageItemSyncMessage msg = new StorageItemSyncMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId, 
			StorageItem = storageItem.GetComponent<NetworkIdentity>().netId,
			Data = data
		};
		msg.SendTo(recipient);
		return msg;
	}
}