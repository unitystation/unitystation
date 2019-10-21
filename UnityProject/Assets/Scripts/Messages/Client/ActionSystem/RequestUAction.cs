
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;

/// <summary>
/// Requests for the server to perform some action.
/// </summary>
public class RequestUAction : ClientMessage
{
	//TODO: Is this constant needed anymore
	public static short MessageType = (short) MessageTypes.RequestUAction;

	//these are always populated
	public Type ActionType;
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


	private static readonly Dictionary<ushort, Type> actionIDToActionType;
	private static readonly Dictionary<Type, ushort> actionTypeToActionID;
	private static readonly Dictionary<ushort, Type> componentIDToComponentType;
	private static readonly Dictionary<Type, ushort> componentTypeToComponentID;
	private static readonly Dictionary<ushort, Type> interactionIDToInteractionType;
	private static readonly Dictionary<Type, ushort> interactionTypeToInteractionID;

	static RequestUAction()
	{
		//initialize id mappings
		var alphabeticalActionTypes =
			typeof(UAction).Assembly.GetTypes()
				.Where(type => typeof(UAction).IsAssignableFrom(type))
				.OrderBy(type => type.FullName);
		ushort i = 0;
		foreach (var actionType in alphabeticalActionTypes)
		{
			actionIDToActionType.Add(i, actionType);
			actionTypeToActionID.Add(actionType, i);
		}

		var alphabeticalComponentTypes =
			typeof(IActionable<>).Assembly.GetTypes()
				.Where(type => typeof(IActionable<>).IsAssignableFrom(type))
				.OrderBy(type => type.FullName);
		i = 0;
		foreach (var actionType in alphabeticalComponentTypes)
		{
			componentIDToComponentType.Add(i, actionType);
			componentTypeToComponentID.Add(actionType, i);
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
		}
	}
	public override IEnumerator Process()
	{
		var performer = SentByPlayer.GameObject;

		GameObject processorObj = null;
		//build the interaction
		Interaction interaction = null;
		if (InteractionType == typeof(PositionalHandApply))
		{
			//look up item in active hand slot
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			processorObj = NetworkObjects[1];
			interaction = PositionalHandApply.ByClient(performer, usedObject, targetObj, TargetVector, usedSlot);
		}
		else if (InteractionType == typeof(HandApply))
		{
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			processorObj = NetworkObjects[1];
			var performerObj = SentByPlayer.GameObject;
			interaction = HandApply.ByClient(performer, usedObject, targetObj, TargetBodyPart, usedSlot);
		}
		else if (InteractionType == typeof(AimApply))
		{
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var usedSlot = HandSlot.ForName(clientPNA.activeHand);
			var usedObject = clientPNA.Inventory[usedSlot.equipSlot].Item;
			yield return WaitFor(ProcessorObject);
			processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			interaction = AimApply.ByClient(performer, TargetVector, usedObject, usedSlot, MouseButtonState);
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
			var usedObj = NetworkObjects[0];
			var targetObj = NetworkObjects[1];
			processorObj = NetworkObjects[2];
			var performerObj = SentByPlayer.GameObject;
			interaction = MouseDrop.ByClient(performer, usedObj, targetObj);
		}
		else if (InteractionType == typeof(HandActivate))
		{
			yield return WaitFor(ProcessorObject);
			processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			//look up item in active hand slot
			var clientPNA = SentByPlayer.Script.playerNetworkActions;
			var handSlot = HandSlot.ForName(clientPNA.activeHand);
			var activatedObject = clientPNA.Inventory[handSlot.equipSlot].Item;
			interaction = HandActivate.ByClient(performer, activatedObject, handSlot);
		}
		else if (InteractionType == typeof(InventoryApply))
		{
			yield return WaitFor(ProcessorObject);
			processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			var pna = SentByPlayer.Script.playerNetworkActions;
			var handSlot = HandSlot.ForName(pna.activeHand);
			var handObject = pna.Inventory[handSlot.equipSlot].Item;
			interaction = InventoryApply.ByClient(performer, pna.GetInventorySlot(processorObj), handObject, handSlot);
		}

		var action = ActionSystem.FindAction(interaction, processorObj, ComponentType, ActionType);
		if (action == null)
		{
			Logger.LogWarningFormat("No server-side {0} action found on {1} component {2} for interaction {3}," +
			                        " action will not be performed.",
				Category.NetMessage, ActionType.Name, processorObj.name, ComponentType.Name, InteractionType.Name);
		}
		else
		{
			action.ServerCheckAndDo();
		}
	}

	public override void Deserialize(NetworkReader reader)
	{

		base.Deserialize(reader);
		ActionType = actionIDToActionType[reader.ReadUInt16()];
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
		writer.WriteUInt16(actionTypeToActionID[ActionType]);
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
