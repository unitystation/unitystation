using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Adds Encryptionkey to a headset
/// </summary>
public class AddEncryptionkeyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.AddEncryptionKeyMessage;
	public GameObject Encryptionkey;
	public GameObject HeadsetItem;

	public override IEnumerator Process()
	{
		yield return WaitFor(SentBy);

		GameObject player = NetworkObject;

		if (ValidRequest(HeadsetItem, Encryptionkey))
		{
			Headset headset = HeadsetItem.GetComponent<Headset>();
			EncryptionKey encryptionkey = Encryptionkey.GetComponent<EncryptionKey>();

			headset.EncryptionKey = encryptionkey.Type;

			NetworkServer.Destroy(Encryptionkey);
		}
	}

	public static AddEncryptionkeyMessage Send(GameObject headsetItem, GameObject encryptionkey)
	{
		AddEncryptionkeyMessage msg = new AddEncryptionkeyMessage
		{
			HeadsetItem = headsetItem,
			Encryptionkey = encryptionkey
		};
		msg.Send();

		return msg;
	}

	public bool ValidRequest(GameObject headset, GameObject encryptionkey)
	{
		EncryptionKeyType encryptionKeyTypeOfHeadset = headset.GetComponent<Headset>().EncryptionKey;
		EncryptionKeyType encryptionKeyTypeOfKey = encryptionkey.GetComponent<EncryptionKey>().Type;
		if (encryptionKeyTypeOfHeadset != EncryptionKeyType.None || encryptionKeyTypeOfKey == EncryptionKeyType.None)
		{
			//TODO add error message for the player
			return false;
		}

		return true;
	}

	public override string ToString()
	{
		return string.Format("[AddEncryptionKeyMessage SentBy={0} HeadsetItem={1} Encryptionkey={2}]",
			SentBy, HeadsetItem, Encryptionkey);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		HeadsetItem = reader.ReadGameObject();
		Encryptionkey = reader.ReadGameObject();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(HeadsetItem);
		writer.Write(Encryptionkey);
	}
}