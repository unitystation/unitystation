using System.Collections;
using Mirror;
using UnityEngine;

public class RequestInventoryApplyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestInventoryApplyMessage;

	//object that will process the interaction
	public uint ProcessorObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(ProcessorObject);
		var processorObj = NetworkObject;
		var performerObj = SentByPlayer.GameObject;
		var pna = SentByPlayer.Script.playerNetworkActions;
		var handSlot = HandSlot.ForName(pna.activeHand);
		var handObject = pna.Inventory[handSlot.equipSlot].Item;

		ProcessActivate(processorObj, performerObj, pna.GetInventorySlot(processorObj), handObject, handSlot);
	}

	private void ProcessActivate(GameObject processorObj, GameObject performerObj, InventorySlot targetSlot,
		GameObject handObject, HandSlot handSlot)
	{
		//try to look up the components on the processor that can handle this interaction
		var processorComponents = InteractionMessageUtils.TryGetProcessors<InventoryApply>(processorObj);

		//invoke each component that can handle this interaction
		// GameObject clientPlayer, InventorySlot targetObjectSlot, GameObject handObject, HandSlot handSlot
		var activate = InventoryApply.ByClient(performerObj, targetSlot, handObject, handSlot);
		foreach (var processorComponent in processorComponents)
		{
			if (processorComponent.ServerProcessInteraction(activate))
			{
				//something happened, don't check further components
				return;
			}
		}
	}

	/// <summary>
	/// For most cases you should use InteractionMessageUtils.SendRequest() instead of this.
	///
	/// Sends a request to the server to validate + perform the interaction.
	/// </summary>
	/// <param name="inventoryApply">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processorObject">object who has a component implementing IInteractionProcessor<Activate> which
	/// will process the interaction on the server-side. This object must have a NetworkIdentity and there must only be one instance
	/// of this component on the object. For organization, we suggest that the component which is sending this message
	/// should be on the processorObject, as such this parameter should almost always be passed using "this.gameObject", and
	/// should almost always be either a component on the target object or a component on the used object</param>
	public static void Send(InventoryApply inventoryApply, GameObject processorObject)
	{
		var msg = new RequestInventoryApplyMessage
		{
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId
		};
		msg.Send();
	}


	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ProcessorObject = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(ProcessorObject);
	}

}
