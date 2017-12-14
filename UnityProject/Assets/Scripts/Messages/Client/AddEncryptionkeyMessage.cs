using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Adds Encryptionkey to a headset
/// </summary>
public class AddEncryptionkeyMessage : ClientMessage<AddEncryptionkeyMessage>
{
    public GameObject Encryptionkey;
    public GameObject HeadsetItem;

    public override IEnumerator Process()
    {
        yield return WaitFor(SentBy);

        var player = NetworkObject;

        if (ValidRequest(HeadsetItem, Encryptionkey))
        {
            var headset = HeadsetItem.GetComponent<Headset>();
            var encryptionkey = Encryptionkey.GetComponent<EncryptionKey>();

            headset.EncryptionKey = encryptionkey.Type;

            NetworkServer.Destroy(Encryptionkey);
        }
    }

    public static AddEncryptionkeyMessage Send(GameObject headsetItem, GameObject encryptionkey)
    {
        var msg = new AddEncryptionkeyMessage
        {
            HeadsetItem = headsetItem,
            Encryptionkey = encryptionkey
        };
        msg.Send();

        return msg;
    }

    public bool ValidRequest(GameObject headset, GameObject encryptionkey)
    {
        var encryptionKeyTypeOfHeadset = headset.GetComponent<Headset>().EncryptionKey;
        var encryptionKeyTypeOfKey = encryptionkey.GetComponent<EncryptionKey>().Type;
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