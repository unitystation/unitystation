using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using UI;

/// <summary>
/// Removes Encryptionkey from a headset
/// </summary>
public class RemoveEncryptionKeyMessage : ClientMessage<RemoveEncryptionKeyMessage>
{
	public GameObject HeadsetItem;

	public override IEnumerator Process()
	{
		yield return WaitFor(SentBy);

		GameObject player = NetworkObject;

		if(ValidRequest(HeadsetItem)) {
			Headset headset = HeadsetItem.GetComponent<Headset>();
			GameObject encryptionKey = GameObject.Instantiate(Resources.Load("Encryptionkey", typeof(GameObject)), HeadsetItem.transform.parent) as GameObject;
			encryptionKey.GetComponent<EncryptionKey>().Type = headset.EncryptionKey;

			PlayerNetworkActions pna = player.GetComponent<PlayerNetworkActions>();
			string slot = UIManager.FindEmptySlotForItem(encryptionKey);
			if (pna.AddItem(encryptionKey, slot)) {
				NetworkServer.Spawn(encryptionKey);
				headset.EncryptionKey = EncryptionKeyType.None;
				pna.PlaceInSlot(encryptionKey, slot);
			} else {
				GameObject.Destroy(encryptionKey);
				Debug.LogError("Could not add Encryptionkey item to player.");
			}
		}
	}

	public static RemoveEncryptionKeyMessage Send(GameObject headsetItem)
	{
		var msg = new RemoveEncryptionKeyMessage
		{
			HeadsetItem = headsetItem
		};
		msg.Send();

		return msg;
	}

	public bool ValidRequest(GameObject headset)
	{
		EncryptionKeyType encryptionKeyType = headset.GetComponent<Headset>().EncryptionKey;
		if (encryptionKeyType == EncryptionKeyType.None)
		{
			//TODO add error message for the player
			return false;
		}

		return true;
	}

	public override string ToString()
	{
		return string.Format("[RemoveEncryptionKeyMessage SentBy={0} HeadsetItem={1}]",
			SentBy, HeadsetItem);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		HeadsetItem = reader.ReadGameObject();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(HeadsetItem);
	}
}
