
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message a client (or server player) sends to the server to request the server to validate
/// and perform a MouseDrop interaction (if validation succeeds).
///
/// The server-side validation and interaction processing is delegated to a gameObject and component of the
/// client's choice. When the message is sent, they specify the gameobject and component that should
/// process the request on the server side.
/// </summary>
public class RequestMouseDropMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestMouseDropMessage;

	//object that will process the interaction
	public NetworkInstanceId ProcessorObject;
	//netid of the object being targeted
	public NetworkInstanceId TargetObject;
	//netid of the object being dropped
	public NetworkInstanceId UsedObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
		var usedObj = NetworkObjects[0];
		var targetObj = NetworkObjects[1];
		var processorObj = NetworkObjects[2];
		var performerObj = SentByPlayer.GameObject;

		ProcessMouseDrop(usedObj, targetObj, processorObj, performerObj);
	}


	private void ProcessMouseDrop(GameObject handObject, GameObject targetObj, GameObject processorObj, GameObject performerObj)
	{
		//try to look up the components on the processor that can handle this interaction
		var processorComponents = InteractionMessageUtils.TryGetProcessors<MouseDrop>(processorObj);
		//invoke each component that can handle this interaction
		var mouseDrop = MouseDrop.ByClient(performerObj, handObject, targetObj);
		foreach (var processorComponent in processorComponents)
		{
			if (processorComponent.ServerProcessInteraction(mouseDrop))
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
	/// <param name="mouseDrop">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processorObject">object who has a component implementing IInteractionProcessor<MouseDrop> which
	/// will process the interaction on the server-side. This object must have a NetworkIdentity and there must only be one instance
	/// of this component on the object. For organization, we suggest that the component which is sending this message
	/// should be on the processorObject, as such this parameter should almost always be passed using "this.gameObject", and
	/// should almost always be either a component on the target object or a component on the used object</param>
	public static void Send(MouseDrop mouseDrop, GameObject processorObject)
	{
		var msg = new RequestMouseDropMessage
		{
			TargetObject = mouseDrop.TargetObject.GetComponent<NetworkIdentity>().netId,
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId,
			UsedObject = mouseDrop.UsedObject.GetComponent<NetworkIdentity>().netId
		};
		msg.Send();
	}


	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ProcessorObject = reader.ReadNetworkId();
		TargetObject = reader.ReadNetworkId();
		UsedObject = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(ProcessorObject);
		writer.Write(TargetObject);
		writer.Write(UsedObject);
	}

}
