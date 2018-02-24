using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class UpdateSlotMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateSlotMessage;
	public bool ForceRefresh;
	public NetworkInstanceId ObjectForSlot;
	public NetworkInstanceId Recipient;
	public string Slot;

	public override IEnumerator Process()
	{
		//To be run on client
		//        Debug.Log("Processed " + ToString());

		if (ObjectForSlot == NetworkInstanceId.Invalid)
		{
			//Clear slot message
			yield return WaitFor(Recipient);
			if (CustomNetworkManager.Instance._isServer || ForceRefresh)
			{
				UIManager.UpdateSlot(new UISlotObject(Slot));
			}
		}
		else
		{
			yield return WaitFor(Recipient, ObjectForSlot);
			if (CustomNetworkManager.Instance._isServer || ForceRefresh)
			{
				UIManager.UpdateSlot(new UISlotObject(Slot, NetworkObjects[1]));
			}
		}
	}

	/// <param name="recipient">Client GO</param>
	/// <param name="slot"></param>
	/// <param name="objectForSlot">Pass null to clear slot</param>
	/// <param name="forced">
	///     Used for client simulation, use false if client's slot is already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	/// <returns></returns>
	public static UpdateSlotMessage Send(GameObject recipient, string slot, GameObject objectForSlot = null, bool forced = true)
	{
		UpdateSlotMessage msg = new UpdateSlotMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId, //?
			Slot = slot,
			ObjectForSlot = objectForSlot != null ? objectForSlot.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			ForceRefresh = forced
		};
		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[UpdateSlotMessage Recipient={0} Method={2} Parameter={3} Type={1} Forced={4}]", Recipient, MessageType, Slot, ObjectForSlot,
			ForceRefresh);
	}
}