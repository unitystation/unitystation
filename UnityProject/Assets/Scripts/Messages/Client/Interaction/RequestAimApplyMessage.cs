
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message a client (or server player) sends to the server to request the server to validate
/// and perform an AimApply interaction (if validation succeeds).
///
/// The server-side validation and interaction processing is delegated to a gameObject and component of the
/// client's choice. When the message is sent, they specify the gameobject and component that should
/// process the request on the server side.
/// </summary>
public class RequestAimApplyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestAimApplyMessage;

	//object that will process the interaction
	public NetworkInstanceId ProcessorObject;
	//target vector (pointing from the client's player to the position they are targeting)
	public Vector2 TargetVector;
	//state of the mouse - whether this is initial press or being held down.
	public MouseButtonState MouseButtonState;

	public override IEnumerator Process()
	{
		//look up item in active hand slot
		var clientPNA = SentByPlayer.Script.playerNetworkActions;
		var usedSlot = HandSlot.ForName(clientPNA.activeHand);
		var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
		yield return WaitFor(ProcessorObject);
		var processorObj = NetworkObject;
		var performerObj = SentByPlayer.GameObject;

		ProcessAimApply(usedObject, TargetVector, processorObj, performerObj, usedSlot, MouseButtonState);
	}

	private void ProcessAimApply(GameObject handObject, Vector2 targetVector, GameObject processorObj,
		GameObject performerObj, HandSlot usedSlot, MouseButtonState buttonState)
	{
		//try to look up the components on the processor that can handle this interaction
		var processorComponents = InteractionMessageUtils.TryGetProcessors<AimApply>(processorObj);
		//invoke each component that can handle this interaction
		var aimApply = AimApply.ByClient(performerObj, targetVector, handObject, usedSlot, buttonState);
		foreach (var processorComponent in processorComponents)
		{
			if (processorComponent.ServerProcessInteraction(aimApply))
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
	/// <param name="aimApply">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processorObject">object who has a component implementing IInteractionProcessor<AimApply> which
	/// will process the interaction on the server-side. This object must have a NetworkIdentity and there must only be one instance
	/// of this component on the object. For organization, we suggest that the component which is sending this message
	/// should be on the processorObject, as such this parameter should almost always be passed using "this.gameObject", and
	/// should almost always be either a component on the target object or a component on the used object</param>
	public static void Send(AimApply aimApply, GameObject processorObject)
	{
		var msg = new RequestAimApplyMessage
		{
			TargetVector = aimApply.TargetVector,
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId,
			MouseButtonState = aimApply.MouseButtonState
		};
		msg.Send();
	}


	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ProcessorObject = reader.ReadNetworkId();
		TargetVector = reader.ReadVector2();
		MouseButtonState = reader.ReadBoolean() ? MouseButtonState.PRESS : MouseButtonState.HOLD;
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ProcessorObject);
		writer.Write(TargetVector);
		writer.Write(MouseButtonState == MouseButtonState.PRESS);
	}

}
