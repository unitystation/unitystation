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
	//player's intent
	public Intent Intent;

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

	//these are all used when it's an InventoryApply to denote the target slot
	//netid of targeted storage
	public uint Storage;
	//slot index of slot targeted in storage (-1 if tareting named slot)
	public int SlotIndex;
	//named slot targeted in storage
	public NamedSlot NamedSlot;

	public int TileInteractionIndex;

	private static readonly Dictionary<ushort, Type> componentIDToComponentType = new Dictionary<ushort, Type>();
	private static readonly Dictionary<Type, ushort> componentTypeToComponentID = new Dictionary<Type, ushort>();
	private static readonly Dictionary<byte, Type> interactionIDToInteractionType = new Dictionary<byte, Type>();
	private static readonly Dictionary<Type, byte> interactionTypeToInteractionID = new Dictionary<Type, byte>();

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
		//needed for tile apply, since it is a client-side only interaction it doesn't implement IInteractable<>
		componentIDToComponentType.Add(i, typeof(InteractableTiles));
		componentTypeToComponentID.Add(typeof(InteractableTiles), i);

		var alphabeticalInteractionTypes =
			typeof(Interaction).Assembly.GetTypes()
				.Where(type => typeof(Interaction).IsAssignableFrom(type))
				.OrderBy(type => type.FullName);
		byte j = 0;
		foreach (var actionType in alphabeticalInteractionTypes)
		{
			interactionIDToInteractionType.Add(j, actionType);
			interactionTypeToInteractionID.Add(actionType, j);
			j++;
		}
	}

	private static IEnumerable<Type> GetAllTypes(Type genericType)
	{
		if (!genericType.IsGenericTypeDefinition)
			throw new ArgumentException("Specified type must be a generic type definition.", nameof(genericType));

		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => ImplementsGenericType(t, genericType))
			.Where(t => typeof(Component).IsAssignableFrom(t));
	}

	private static bool ImplementsGenericType(Type toCheck, Type genericType)
	{
		return toCheck.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
	}

	public override IEnumerator Process()
	{
		var performer = SentByPlayer.GameObject;

		if (SentByPlayer == null || SentByPlayer.Script == null || SentByPlayer.Script.ItemStorage == null)
		{
			yield break;
		}

		if (InteractionType == typeof(PositionalHandApply))
		{
			//look up item in active hand slot
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			var interaction = PositionalHandApply.ByClient(performer, usedObject, targetObj, TargetVector, usedSlot, Intent, TargetBodyPart);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(HandApply))
		{
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			yield return WaitFor(TargetObject, ProcessorObject);
			var targetObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			var interaction = HandApply.ByClient(performer, usedObject, targetObj, TargetBodyPart, usedSlot, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(AimApply))
		{
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			var interaction = AimApply.ByClient(performer, TargetVector, usedObject, usedSlot, MouseButtonState, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			yield return WaitFor(UsedObject, TargetObject, ProcessorObject);
			var usedObj = NetworkObjects[0];
			var targetObj = NetworkObjects[1];
			var processorObj = NetworkObjects[2];
			var interaction = MouseDrop.ByClient(performer, usedObj, targetObj, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(HandActivate))
		{
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			var performerObj = SentByPlayer.GameObject;
			//look up item in active hand slot
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			var interaction = HandActivate.ByClient(performer, usedObject, usedSlot, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(InventoryApply))
		{
			yield return WaitFor(ProcessorObject, UsedObject, Storage);
			var processorObj = NetworkObjects[0];
			var usedObj = NetworkObjects[1];
			var storageObj = NetworkObjects[2];

			ItemSlot targetSlot = null;
			if (SlotIndex == -1)
			{
				targetSlot = ItemSlot.GetNamed(storageObj.GetComponent<ItemStorage>(), NamedSlot);
			}
			else
			{
				targetSlot = ItemSlot.GetIndexed(storageObj.GetComponent<ItemStorage>(), SlotIndex);
			}

			//if used object is null, then empty hand was used
			ItemSlot fromSlot = null;
			if (usedObj == null)
			{
				fromSlot = SentByPlayer.Script.ItemStorage.GetActiveHandSlot();
			}
			else
			{
				fromSlot = usedObj.GetComponent<Pickupable>().ItemSlot;
			}
			var interaction = InventoryApply.ByClient(performer, targetSlot, fromSlot, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(TileApply))
		{
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			yield return WaitFor(ProcessorObject);
			var processorObj = NetworkObject;
			processorObj.GetComponent<InteractableTiles>().ServerProcessInteraction(TileInteractionIndex,
				SentByPlayer.GameObject, TargetVector, processorObj, usedSlot, usedObject, Intent);
		}


	}

	private void ProcessInteraction<T>(T interaction, GameObject processorObj)
		where T : Interaction
	{
		if (processorObj == null)
		{
			Logger.LogWarning("processorObj is null, action will not be performed.", Category.Interaction);
			return;
		}
		//find the indicated component
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
		//server side interaction check and cooldown check, and start the cooldown
		if (interactable.ServerCheckInteract(interaction) &&
		    Cooldowns.TryStartServer(interaction, CommonCooldowns.Instance.Interaction))
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
		if (typeof(T) == typeof(TileApply))
		{
			Logger.LogError("Cannot use Send with TileApply, please use SendTileApply instead.", Category.Interaction);
			return;
		}
		//never send anything for client-side-only interactions
		if (interactableComponent is IClientInteractable<T> && !(interactableComponent is IInteractable<T>))
		{
			Logger.LogWarningFormat("Interaction request {0} will not be sent because interactable component {1} is" +
			                      " IClientInteractable only (client-side only).", Category.Interaction, interaction, interactableComponent);
			return;
		}
		//if we are client and the interaction has client prediction, trigger it.
		//Note that client prediction is not triggered for server player.
		if (!CustomNetworkManager.IsServer && interactableComponent is IPredictedInteractable<T> predictedInteractable)
		{
			Logger.LogTraceFormat("Predicting {0} interaction for {1} on {2}", Category.Interaction, typeof(T).Name, interactableComponent.GetType().Name, ((Component) interactableComponent).gameObject.name);
			predictedInteractable.ClientPredictInteraction(interaction);
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
			ProcessorObject = comp.GetComponent<NetworkIdentity>().netId,
			Intent = interaction.Intent
		};
		if (typeof(T) == typeof(PositionalHandApply))
		{
			var casted = interaction as PositionalHandApply;
			msg.TargetObject = casted.TargetObject.NetId();
			msg.TargetVector = casted.TargetVector;
			msg.TargetBodyPart = casted.TargetBodyPart;
		}
		else if (typeof(T) == typeof(HandApply))
		{
			var casted = interaction as HandApply;
			msg.TargetObject = casted.TargetObject.NetId();
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
			msg.TargetObject = casted.TargetObject.NetId();
			msg.UsedObject = casted.UsedObject.NetId();
		}
		else if (typeof(T) == typeof(InventoryApply))
		{
			var casted = interaction as InventoryApply;
			msg.Storage = casted.TargetSlot.ItemStorageNetID;
			msg.SlotIndex = casted.TargetSlot.SlotIdentifier.SlotIndex;
			msg.NamedSlot = casted.TargetSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.none);
			msg.UsedObject = casted.UsedObject.NetId();
		}
		msg.Send();
	}

	//only intended to be used by core if2 classes
	public static void SendTileApply(TileApply tileApply, InteractableTiles interactableTiles, TileInteraction tileInteraction, int tileInteractionIndex)
	{
		//if we are client and the interaction has client prediction, trigger it.
		//Note that client prediction is not triggered for server player.
		if (!CustomNetworkManager.IsServer)
		{
			Logger.LogTraceFormat("Predicting TileApply interaction {0}", Category.Interaction, tileApply);
			tileInteraction.ClientPredictInteraction(tileApply);
		}
		if (!tileApply.Performer.Equals(PlayerManager.LocalPlayer))
		{
			Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
			                " This is not allowed. Client can only perform an interaction as themselves. Message" +
			                " will not be sent.", Category.NetMessage);
			return;
		}

		var msg = new RequestInteractMessage()
		{
			ComponentType = typeof(InteractableTiles),
			InteractionType = typeof(TileApply),
			ProcessorObject = interactableTiles.GetComponent<NetworkIdentity>().netId,
			Intent = tileApply.Intent,
			TargetVector = tileApply.TargetVector,
			TileInteractionIndex = tileInteractionIndex
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{

		base.Deserialize(reader);
		ComponentType = componentIDToComponentType[reader.ReadUInt16()];
		InteractionType = interactionIDToInteractionType[reader.ReadByte()];
		ProcessorObject = reader.ReadUInt32();
		Intent = (Intent) reader.ReadByte();

		if (InteractionType == typeof(PositionalHandApply))
		{
			TargetObject = reader.ReadUInt32();
			TargetVector = reader.ReadVector2();
			TargetBodyPart = (BodyPartType) reader.ReadUInt32();
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
		else if (InteractionType == typeof(InventoryApply))
		{
			UsedObject = reader.ReadUInt32();
			Storage = reader.ReadUInt32();
			SlotIndex = reader.ReadInt32();
			NamedSlot = (NamedSlot) reader.ReadInt32();
		}
		else if (InteractionType == typeof(TileApply))
		{
			TargetVector = reader.ReadVector2();
			TileInteractionIndex = reader.ReadByte();
		}
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt16(componentTypeToComponentID[ComponentType]);
		writer.WriteByte(interactionTypeToInteractionID[InteractionType]);
		writer.WriteUInt32(ProcessorObject);
		writer.WriteByte((byte) Intent);

		if (InteractionType == typeof(PositionalHandApply))
		{
			writer.WriteUInt32(TargetObject);
			writer.WriteVector2(TargetVector);
			writer.WriteInt32((int) TargetBodyPart);
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
		else if (InteractionType == typeof(InventoryApply))
		{
			writer.WriteUInt32(UsedObject);
			writer.WriteUInt32(Storage);
			writer.WriteInt32(SlotIndex);
			writer.WriteInt32((int) NamedSlot);
		}
		else if (InteractionType == typeof(TileApply))
		{
			writer.WriteVector2(TargetVector);
			writer.WriteByte((byte) TileInteractionIndex);
		}
	}

}
