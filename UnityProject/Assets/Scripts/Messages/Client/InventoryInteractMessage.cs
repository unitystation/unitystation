using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of inventory mangling
/// </summary>
public class InventoryInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.InventoryInteractMessage;
	public bool ForceSlotUpdate;
	public string Slot;
	public NetworkInstanceId Subject;

	//Serverside
	public override IEnumerator Process()
	{
		//		Logger.Log("Processed " + ToString());
		if (Subject.Equals(NetworkInstanceId.Invalid))
		{
			//Drop item message
			yield return WaitFor(SentBy);
			ProcessFurther(NetworkObject);
		}
		else
		{
			yield return WaitFor(SentBy, Subject);
			ProcessFurther(NetworkObjects[0], NetworkObjects[1]);
		}
	}

	private void ProcessFurther(GameObject player, GameObject item = null)
	{
		GameObject clientPlayer = player;
		PlayerNetworkActions pna = clientPlayer.GetComponent<PlayerNetworkActions>();

		if (!pna.ValidateInvInteraction(Slot, item, ForceSlotUpdate))
		{
			pna.RollbackPrediction(Slot);
		}
	}

	/// <summary>
	/// A serverside inventory request, for updating UI slots etc. 
	/// You can give a position to dropWorldPos when dropping an item
	/// or else use Vector3.zero when not placing or dropping to ignore it.
	/// (The world pos is converted to local position automatically)
	/// </summary>
	public static InventoryInteractMessage Send(string slot, GameObject subject /* = null*/, bool forceSlotUpdate /* = false*/)
	{
		InventoryInteractMessage msg = new InventoryInteractMessage {
			Subject = subject ? subject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			Slot = slot,
			ForceSlotUpdate = forceSlotUpdate
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Slot = reader.ReadString();
		Subject = reader.ReadNetworkId();
		ForceSlotUpdate = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Slot);
		writer.Write(Subject);
		writer.Write(ForceSlotUpdate);
	}
}