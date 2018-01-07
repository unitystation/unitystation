using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Removes Encryptionkey from a headset
/// </summary>
public class RemoveEncryptionKeyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RemoveEncryptionKeyMessage;
	public GameObject HeadsetItem;

	public override IEnumerator Process()
	{
		yield return WaitFor(SentBy);

		GameObject player = NetworkObject;
		if (ValidRequest(HeadsetItem))
		{
			Headset headset = HeadsetItem.GetComponent<Headset>();
			GameObject encryptionKey = Object.Instantiate(Resources.Load("Encryptionkey", typeof(GameObject)),HeadsetItem.transform.parent) as GameObject;
			encryptionKey.GetComponent<EncryptionKey>().Type = headset.EncryptionKey;
			//TODO when added interact with dropped headset, add encryption key to empty hand
			headset.EncryptionKey = EncryptionKeyType.None;
			ItemFactory.SpawnItem(encryptionKey, player.transform.position, player.transform.parent);
		}
	}

	public static RemoveEncryptionKeyMessage Send(GameObject headsetItem)
	{
		RemoveEncryptionKeyMessage msg = new RemoveEncryptionKeyMessage
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