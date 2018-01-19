using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Adds Encryptionkey to a headset
/// </summary>
public class VendorMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.VendorMessage;
	public GameObject Sell;
	public GameObject Vendor;

	public override IEnumerator Process()
	{
		Debug.Log("processing 1");
		yield return WaitFor(SentBy);
		Debug.Log("processing 2");
		ItemFactory.SpawnItem(Sell, Vendor.transform.position, Vendor.transform.parent);
		Debug.Log("Done");

	}

	public static VendorMessage Send(GameObject sell, GameObject vendor)
	{
		Debug.Log("Message sending started");
		VendorMessage msg = new VendorMessage
		{
			Sell = sell,
			Vendor = vendor
			
		};
		msg.Send();

		return msg;
	}


	public override string ToString()
	{
		return string.Format("[VendorMessage SentBy={0} HeadsetItem={1} Encryptionkey={2}]",
			SentBy, Sell, Vendor);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Sell = reader.ReadGameObject();
		Vendor = reader.ReadGameObject();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Sell);
		writer.Write(Vendor);
	}
}