
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message a client (or server player) sends to the server to request the server to validate
/// and perform an interaction (if validation succeeds).
///
/// The server-side validation and interaction processing is delegated to a gameObject and component of the
/// client's choice. When the message is sent, they specify the gameobject and component that should
/// process the request on the server side.
/// </summary>
public class RequestInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestInteractMessage;

	//ID of the type of component that should be used to process the
	//message on ProcessorObject, looked up in the dictionaries
	public short ProcessorTypeID;
	//object that will process the interaction
	public NetworkInstanceId ProcessorObject;
	//netid of the object being dropped, applied, or activated
	public NetworkInstanceId UsedObject;
	//netid of the object being targeted if this is a mouse drop or hand
	//apply or combine
	public NetworkInstanceId TargetObject;

	//used for efficiently sending the type of the Interaction subtype that this message concerns
	private static readonly Dictionary<Type, short> InteractionTypeToID;
	private static readonly Dictionary<short, Type> InteractionIDToType;

	//initializes the type<->id dictionaries
	static RequestInteractMessage()
	{
		InteractionTypeToID = new Dictionary<Type, short>();
		InteractionIDToType = new Dictionary<short, Type>();

		var types = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(p => typeof(Interaction).IsAssignableFrom(p))
			.OrderBy(t => t.FullName);
		short i = 0;
		foreach (var infoType in types)
		{
			InteractionTypeToID.Add(infoType, i);
			InteractionIDToType.Add(i, infoType);
			i++;
		}
	}


	public override IEnumerator Process()
	{
		if (InteractionIDToType.TryGetValue(ProcessorTypeID, out var interactionType))
		{

			//determine which type of interaction to process
			if (interactionType == typeof(MouseDrop))
			{
				yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
				var droppedObj = NetworkObjects[0];
				var targetObj = NetworkObjects[1];
				var processorObj = NetworkObjects[2];
				var performerObj = SentByPlayer.GameObject;
				ProcessMouseDrop(droppedObj, targetObj, processorObj, performerObj);

			}
			else if ((interactionType == typeof(HandApply)))
			{
				if (UsedObject == NetworkInstanceId.Invalid)
				{
					//empty hand
					yield return WaitFor(TargetObject, ProcessorObject);
					var targetObj = NetworkObjects[0];
					var processorObj = NetworkObjects[1];
					var performerObj = SentByPlayer.GameObject;
					ProcessHandApply(null, targetObj, processorObj, performerObj);
				}
				else
				{
					yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
					var handObj = NetworkObjects[0];
					var targetObj = NetworkObjects[1];
					var processorObj = NetworkObjects[2];
					var performerObj = SentByPlayer.GameObject;
					ProcessHandApply(handObj, targetObj, processorObj, performerObj);
				}
			}
			//TODO: Other interaction types
			else
			{
				Logger.LogErrorFormat("Interaction type was {0} - we couldn't determine what to do for this interaction" +
				                      " type, most likely because it hasn't been implemented yet." +
				                      " Please implement handling for this interaction type in RequestInteractMessage.Send()", Category.NetMessage, interactionType);
			}
		}
		else
		{
			Logger.LogErrorFormat("Interaction subtype could not be looked up by the ID sent by the client: {0}. " +
			                      "this is most likely a programming error. Message will not be processed", Category.NetMessage, ProcessorTypeID);
		}
	}

	private void ProcessMouseDrop(GameObject droppedObj, GameObject targetObj, GameObject processorObj, GameObject performerObj)
	{
		//try to look up the component on the processor
		var processorComponent = TryGetProcessor<MouseDrop>(processorObj);
		processorComponent.ServerProcessInteraction(
			new MouseDrop(performerObj, droppedObj, targetObj));
	}

	private void ProcessHandApply(GameObject handObject, GameObject targetObj, GameObject processorObj, GameObject performerObj)
	{
		//try to look up the component on the processor
		var processorComponent = TryGetProcessor<HandApply>(processorObj);
		processorComponent.ServerProcessInteraction(
			new HandApply(performerObj, handObject, targetObj));
	}

	private IInteractionProcessor<T> TryGetProcessor<T>(GameObject processorObj)
		where T : Interaction
	{
		var processorComponent = processorObj.GetComponent<IInteractionProcessor<T>>();
		if (processorComponent == null)
		{
			Logger.LogError("Processor component could not be looked up by the ID sent by the client, " +
			                "this is most likely a programming error. Message will not be processed", Category.NetMessage);
			return null;
		}

		return processorComponent;
	}

	/// <summary>
	/// Send a request to the server to validate + perform the interaction.
	/// </summary>
	/// <param name="info">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processor">component which will process the interaction on the server-side. The processor's
	/// info and result types must match the info and result type of one of the interaction type constants
	/// defined in InteractionType.
	///
	/// This component
	/// must live on a GameObject with a network identity, and there must only be one instance of this component
	/// on the object. For organization, we suggest that the component which is sending this message
	/// should be the processor, as such this parameter should almost always be passed using the "this" keyword, and
	/// should almost always be either a component on the target object or a component on the used object</param>
	/// <typeparamref name="T">Interaction subtype
	/// for the interaction that the processor can handle (such as MouseDropInfo for a mouse drop interaction).
	/// Must be a subtype of Interaction.</typeparamref>
	/// <returns></returns>
	public static RequestInteractMessage Send<T>(T info, IInteractionProcessor<T> processor)
		where T : Interaction
	{
		if (!info.Performer.Equals(PlayerManager.LocalPlayer))
		{
			Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
			                " This is not allowed. Client can only perform an interaction as themselves. Message" +
			                " will not be sent.", Category.NetMessage);
			return null;
		}

		if (!(processor is Component))
		{
			Logger.LogError("processor must be a component, but isn't. The message will not be sent.", Category.NetMessage);
			return null;
		}

		short typeID;
		var processorObject = (processor as Component).gameObject;
		if (InteractionTypeToID.TryGetValue(typeof(T), out typeID))
		{
			RequestInteractMessage msg = null;
			//send the message appropriate to the specific interaction type
			if (typeof(T) == typeof(MouseDrop))
			{
				msg = CreateMouseDropMessage(info as MouseDrop, processorObject, typeID);
			}
			else if (typeof(T) == typeof(HandApply))
			{
				msg = CreateHandApplyMessage(info as HandApply, processorObject, typeID);
			}
			//TODO: Other types

			if (msg != null)
			{
				msg.Send();
				return msg;
			}
			else
			{
				Logger.LogErrorFormat("Interaction type was {0} - we couldn't determine what to do for this interaction" +
				                      " type, most likely because it hasn't been implemented yet." +
				                      " Please implement handling for this interaction type in RequestInteractMessage.Send()", Category.NetMessage, nameof(T));
				return null;
			}
		}
		else
		{
			Logger.LogError("Interaction's concrete type could not be mapped to an ID, this is most likely" +
			                " a programming error. Message will not be sent", Category.NetMessage);
			return null;
		}

	}

	private static RequestInteractMessage CreateMouseDropMessage(MouseDrop mouseDrop, GameObject processorObject,
		short typeId)
	{
		return new RequestInteractMessage()
		{
			UsedObject = mouseDrop.UsedObject.GetComponent<NetworkIdentity>().netId,
			TargetObject = mouseDrop.TargetObject.GetComponent<NetworkIdentity>().netId,
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId,
			ProcessorTypeID = typeId
		};
	}

	private static RequestInteractMessage CreateHandApplyMessage(HandApply handApply, GameObject processorObject,
		short typeId)
	{
		return new RequestInteractMessage()
		{
			UsedObject = handApply.UsedObject != null ? handApply.UsedObject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			TargetObject = handApply.TargetObject.GetComponent<NetworkIdentity>().netId,
			ProcessorObject = processorObject.GetComponent<NetworkIdentity>().netId,
			ProcessorTypeID = typeId
		};
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);

		ProcessorTypeID = reader.ReadInt16();
		ProcessorObject = reader.ReadNetworkId();
		UsedObject = reader.ReadNetworkId();
		TargetObject = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		//typeID comes first so we can determine what will be next (for when other interaction types are supported)
		writer.Write(ProcessorTypeID);
		writer.Write(ProcessorObject);
		writer.Write(UsedObject);
		writer.Write(TargetObject);
	}
}
