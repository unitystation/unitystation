
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;

/// <summary>
/// Requests for the server to perform a given interaction
/// </summary>
public class RequestInteractMessage : ClientMessage
{
	//TODO: Is this constant needed anymore
	public static short MessageType = (short) MessageTypes.RequestInteraction;

	//these are always populated
	public Type ComponentType;
	public Type InteractionType;
	//object that will process the interaction
	public uint ProcessorObject;

	//note: below, some of these will be populated depending on the interaction type


	//netid of the object being targeted
	public uint TargetObject;
	//netid of the object being used
	public uint UsedObject;
	//targeted body part
	public BodyPartType TargetBodyPart;
	//target vector (pointing from the performer to the position they are targeting)
	public Vector2 TargetVector;
	//state of the mouse - whether this is initial press or being held down.
	public MouseButtonState MouseButtonState;

	private static readonly Dictionary<ushort, Type> componentIDToComponentType = new Dictionary<ushort, Type>();
	private static readonly Dictionary<Type, ushort> componentTypeToComponentID = new Dictionary<Type, ushort>();
	private static readonly Dictionary<ushort, Type> interactionIDToInteractionType = new Dictionary<ushort, Type>();
	private static readonly Dictionary<Type, ushort> interactionTypeToInteractionID = new Dictionary<Type, ushort>();

	static RequestInteractMessage()
	{
		//initialize id mappings
		var alphabeticalComponentTypes = GetAllTypes(typeof(IInteractable<>));
		ushort i = 0;
		foreach (var componentType in alphabeticalComponentTypes)
		{
			componentIDToComponentType.Add(i, componentType);
			componentTypeToComponentID.Add(componentType, i);
			i++;
		}

		var alphabeticalInteractionTypes =
			typeof(Interaction).Assembly.GetTypes()
				.Where(type => typeof(Interaction).IsAssignableFrom(type))
				.OrderBy(type => type.FullName);
		i = 0;
		foreach (var actionType in alphabeticalInteractionTypes)
		{
			interactionIDToInteractionType.Add(i, actionType);
			interactionTypeToInteractionID.Add(actionType, i);
			i++;
		}
	}

	private static IEnumerable<Type> GetAllTypes(Type genericType)
	{
		if (!genericType.IsGenericTypeDefinition)
			throw new ArgumentException("Specified type must be a generic type definition.", nameof(genericType));

		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => ImplementsGenericType(t, genericType));
	}

	private static bool ImplementsGenericType(Type toCheck, Type genericType)
	{
		return toCheck.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
	}

	public override IEnumerator Process()
	{
		var performer = SentByPlayer.GameObject;

		if (InteractionType == typeof(PositionalHandApply))
		{
			//look up item in active hand slot
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			var interaction = PositionalHandApply.ByClient(performer, usedObject, targetObj, TargetVector, usedSlot);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(HandApply))
		{
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			var performerObj = SentByPlayer.GameObject;
			var interaction = HandApply.ByClient(performer, usedObject, targetObj, TargetBodyPart, usedSlot);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(AimApply))
		{
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			var interaction = AimApply.ByClient(performer, TargetVector, usedObject, usedSlot, MouseButtonState);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
			var usedObj = NetworkObjects[0];
			var targetObj = NetworkObjects[1];
			var processorObj = NetworkObjects[2];
			var performerObj = SentByPlayer.GameObject;
			var interaction = MouseDrop.ByClient(performer, usedObj, targetObj);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(HandActivate))
		{
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			//look up item in active hand slot
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var handSlot = HandSlot.ForName(clientPNA.activeHand);
			var activatedObject = clientPNA.Inventory[handSlot.equipSlot].Item;
			var interaction = HandActivate.ByClient(performer, activatedObject, handSlot);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(InventoryApply))
		{
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			var pna = SentByPlayer.Script.playerNetworkActions;
			var handSlot = HandSlot.ForName(pna.activeHand);
			var handObject = pna.Inventory[handSlot.equipSlot].Item;
			var interaction = InventoryApply.ByClient(performer, pna.GetInventorySlot(processorObj), handObject, handSlot);
			ProcessInteraction(interaction, processorObj);
		}


	}

	private void ProcessInteraction<T>(T interaction, GameObject processorObj)
		where T : Interaction
	{
		//find the indicated component
		var success = false;
		var component = processorObj.GetComponent(ComponentType);
		if (component == null)
		{
			Logger.LogWarningFormat("No component found of requested type {0} on {1}," +
			                        " action will not be performed.",
				Category.Interaction, ComponentType.Name, processorObj.name);
			return;
		}
		if (!(component is IInteractable<T>))
		{
			Logger.LogWarningFormat("Component of type {0} doesn't implement IInteractable" +
			                        " for interaction type {1} on {2}," +
			                        " action will not be performed.",
				Category.Interaction, ComponentType.Name, typeof(T).Name, processorObj.name);
			return;
		}

		var interactable = (component as IInteractable<T>);
		if (interactable.CheckInteract(interaction, NetworkSide.Server))
		{
			//perform
			interactable.ServerPerformInteraction(interaction);
		}
		else
		{
			//rollback if this component implements it
			if (component is IPredictedInteractable<T> predictedInteractable)
			{
				predictedInteractable.ServerRollbackClient(interaction);
			}
		}
	}

	//only intended to be used by core if2 classes, please use InteractionUtils.RequestInteract instead.
	public static void Send<T>(T interaction, IBaseInteractable<T> interactableComponent)
		where T : Interaction
	{
		//never send anything for client-side-only interactions
		if (interactableComponent is IClientInteractable<T>)
		{
			return;
		}
		if (!interaction.Performer.Equals(PlayerManager.LocalPlayer))
		{
			Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
			                " This is not allowed. Client can only perform an interaction as themselves. Message" +
			                " will not be sent.", Category.NetMessage);
			return;
		}

		if (!(interactableComponent is Component))
		{
			Logger.LogError("interactableComponent must be a component, but isn't. The message will not be sent.",
				Category.NetMessage);
			return;
		}

		var comp = interactableComponent as Component;
		var msg = new RequestInteractMessage()
		{
			ComponentType = interactableComponent.GetType(),
			InteractionType = typeof(T),
			ProcessorObject = comp.GetComponent<NetworkIdentity>().netId
		};
		if (typeof(T) == typeof(PositionalHandApply))
		{
			var casted = interaction as PositionalHandApply;
			msg.TargetObject = casted.TargetObject.GetComponent<NetworkIdentity>().netId;
			msg.TargetVector = casted.TargetVector;
		}
		else if (typeof(T) == typeof(HandApply))
		{
			var casted = interaction as HandApply;
			msg.TargetObject = casted.TargetObject.GetComponent<NetworkIdentity>().netId;
			msg.TargetBodyPart = casted.TargetBodyPart;
		}
		else if (typeof(T) == typeof(AimApply))
		{
			var casted = interaction as AimApply;
			msg.TargetVector = casted.TargetVector;
			msg.MouseButtonState = casted.MouseButtonState;
		}
		else if (typeof(T) == typeof(MouseDrop))
		{
			var casted = interaction as MouseDrop;
			msg.TargetObject = casted.TargetObject.GetComponent<NetworkIdentity>().netId;
			msg.UsedObject = casted.UsedObject.GetComponent<NetworkIdentity>().netId;
		}
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{

		base.Deserialize(reader);
		ComponentType = componentIDToComponentType[reader.ReadUInt16()];
		InteractionType = interactionIDToInteractionType[reader.ReadUInt16()];
		ProcessorObject = reader.ReadUInt32();

		if (InteractionType == typeof(PositionalHandApply))
		{
			TargetObject = reader.ReadUInt32();
			TargetVector = reader.ReadVector2();
		}
		else if (InteractionType == typeof(HandApply))
		{
			TargetObject = reader.ReadUInt32();
			TargetBodyPart = (BodyPartType) reader.ReadUInt32();
		}
		else if (InteractionType == typeof(AimApply))
		{
			TargetVector = reader.ReadVector2();
			MouseButtonState = reader.ReadBoolean() ? MouseButtonState.PRESS : MouseButtonState.HOLD;
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			TargetObject = reader.ReadUInt32();
			UsedObject = reader.ReadUInt32();
		}
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt16(componentTypeToComponentID[ComponentType]);
		writer.WriteUInt16(interactionTypeToInteractionID[InteractionType]);
		writer.WriteUInt32(ProcessorObject);

		if (InteractionType == typeof(PositionalHandApply))
		{
			writer.WriteUInt32(TargetObject);
			writer.WriteVector2(TargetVector);
		}
		else if (InteractionType == typeof(HandApply))
		{
			writer.WriteUInt32(TargetObject);
			writer.WriteInt32((int) TargetBodyPart);
		}
		else if (InteractionType == typeof(AimApply))
		{
			writer.WriteVector2(TargetVector);
			writer.WriteBoolean(MouseButtonState == MouseButtonState.PRESS);
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			writer.WriteUInt32(TargetObject);
			writer.WriteUInt32(UsedObject);
		}
	}
}
