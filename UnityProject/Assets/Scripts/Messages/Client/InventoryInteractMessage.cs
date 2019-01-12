using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of inventory mangling
/// </summary>
public class InventoryInteractMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.InventoryInteractMessage;
	public bool ForceSlotUpdate;
	public string SlotUUID;
	public string FromSlotUUID;
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
		if (string.IsNullOrEmpty(SlotUUID))
		{
			//To drop
			if (!pna.ValidateDropItem(InventoryManager.GetSlotFromUUID(FromSlotUUID, true)
			, ForceSlotUpdate))
			{
				pna.RollbackPrediction(SlotUUID, FromSlotUUID, item);
			}
		}
		else
		{
			if (!pna.ValidateInvInteraction(SlotUUID, FromSlotUUID, item, ForceSlotUpdate))
			{
				pna.RollbackPrediction(SlotUUID, FromSlotUUID, item);
			}
		}
	}

	public static InventoryInteractMessage Send(string slotUUID, string fromSlotUUID, GameObject subject, bool forceSlotUpdate)
	{
		InventoryInteractMessage msg = new InventoryInteractMessage
		{
			Subject = subject ? subject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
				SlotUUID = slotUUID,
				FromSlotUUID = fromSlotUUID,
				ForceSlotUpdate = forceSlotUpdate
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		SlotUUID = reader.ReadString();
		FromSlotUUID = reader.ReadString();
		Subject = reader.ReadNetworkId();
		ForceSlotUpdate = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(SlotUUID);
		writer.Write(FromSlotUUID);
		writer.Write(Subject);
		writer.Write(ForceSlotUpdate);
	}
}