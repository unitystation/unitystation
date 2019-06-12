
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message a client (or server player) sends to the server to request the server to validate
/// and perform an Activate interaction (if validation succeeds).
///
/// The server-side validation and interaction processing is delegated to a gameObject and component of the
/// client's choice. When the message is sent, they specify the gameobject and component that should
/// process the request on the server side.
/// </summary>
public class RequestHandActivateMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestHandActivateMessage;

	//object that will process the interaction
	public NetworkInstanceId ProcessorObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(ProcessorObject);
		var processorObj = NetworkObject;
		var performerObj = SentByPlayer.GameObject;
		//look up item in active hand slot
		var clientPNA = SentByPlayer.Script.playerNetworkActions;
		var handSlot = HandSlot.ForName(clientPNA.activeHand);
		var activatedObject = clientPNA.Inventory[handSlot.SlotName].Item;

		ProcessActivate(activatedObject, processorObj, performerObj, handSlot);
	}


	private void ProcessActivate(GameObject activatedObject, GameObject processorObj, GameObject performerObj, HandSlot handSlot)
	{
		//try to look up the components on the processor that can handle this interaction
		var processorComponents = InteractionMessageUtils.TryGetProcessors<HandActivate>(processorObj);
		//invoke each component that can handle this interaction
		var activate = HandActivate.ByClient(performerObj, activatedObject, handSlot);
		foreach (var processorComponent in processorComponents)
		{
			if (processorComponent.ServerProcessInteraction(activate) ==
			    InteractionControl.STOP_PROCESSING)
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
	/// <param name="handActivate">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processorObject">object who has a component implementing IInteractionProcessor<Activate> which
	/// will process the interaction on the server-side. This object must have a NetworkIdentity and there must only be one instance
	/// of this component on the object. For organization, we suggest that the component which is sending this message
	/// should be on the processorObject, as such this parameter should almost always be passed using "this.gameObject", and
	/// should almost always be either a component on the target object or a component on the used object</param>
	public static void Send(HandActivate handActivate, GameObject processorObject)
	{
		var msg = new RequestHandActivateMessage
		{
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId
		};
		msg.Send();
	}


	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ProcessorObject = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ProcessorObject);
	}

}
