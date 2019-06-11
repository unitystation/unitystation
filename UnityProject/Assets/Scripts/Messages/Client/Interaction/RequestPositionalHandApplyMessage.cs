
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message a client (or server player) sends to the server to request the server to validate
/// and perform a HandApply interaction (if validation succeeds).
///
/// The server-side validation and interaction processing is delegated to a gameObject and component of the
/// client's choice. When the message is sent, they specify the gameobject and component that should
/// process the request on the server side.
/// </summary>
public class RequestPositionalHandApplyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestPositionalHandApplyMessage;

	//object that will process the interaction
	public NetworkInstanceId ProcessorObject;
	//netid of the object being targeted
	public NetworkInstanceId TargetObject;
	//target vector pointing from performer to targeted position
	public Vector2 TargetVector;

	public override IEnumerator Process()
	{
		//look up item in active hand slot
		var clientPNA = SentByPlayer.Script.playerNetworkActions;
		var usedSlot = HandSlot.ForName(clientPNA.activeHand);
		var usedObject = clientPNA.Inventory[usedSlot.SlotName].Item;
		yield return WaitFor(TargetObject, ProcessorObject);
		var targetObj = NetworkObjects[0];
		var processorObj = NetworkObjects[1];
		var performerObj = SentByPlayer.GameObject;

		ProcessPositionalHandApply(usedObject, targetObj, TargetVector, processorObj, performerObj, usedSlot);
	}


	private void ProcessPositionalHandApply(GameObject handObject, GameObject targetObj, Vector2 targetVector, GameObject processorObj, GameObject performerObj, HandSlot usedSlot)
	{
		//try to look up the components on the processor that can handle this interaction
		var processorComponents = InteractionMessageUtils.TryGetProcessors<PositionalHandApply>(processorObj);
		//invoke each component that can handle this interaction
		var handApply = PositionalHandApply.ByClient(performerObj, handObject, targetObj, targetVector, usedSlot);
		foreach (var processorComponent in processorComponents)
		{
			if (processorComponent.ServerProcessInteraction(handApply) ==
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
	/// <param name="handApply">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processorObject">object who has a component implementing IInteractionProcessor<PositionalHandApply> which
	/// will process the interaction on the server-side. This object must have a NetworkIdentity and there must only be one instance
	/// of this component on the object. For organization, we suggest that the component which is sending this message
	/// should be on the processorObject, as such this parameter should almost always be passed using "this.gameObject", and
	/// should almost always be either a component on the target object or a component on the used object</param>
	public static void Send(PositionalHandApply handApply, GameObject processorObject)
	{
		var msg = new RequestPositionalHandApplyMessage
		{
			TargetObject = handApply.TargetObject.GetComponent<NetworkIdentity>().netId,
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId,
			TargetVector = handApply.TargetVector
		};
		msg.Send();
	}


	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ProcessorObject = reader.ReadNetworkId();
		TargetObject = reader.ReadNetworkId();
		TargetVector = reader.ReadVector2();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ProcessorObject);
		writer.Write(TargetObject);
		writer.Write(TargetVector);
	}

}
