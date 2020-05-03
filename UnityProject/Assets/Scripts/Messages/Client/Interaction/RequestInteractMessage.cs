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

	/**
	 * this is sent as the componentID when the client doesn't know
	 * exactly which interaction should be triggered, and will
	 * defer to the server. In this case,
	 * the server will check each interaction of the given interaction type on the involved
	 * objects to see which should occur.
	 */
	private static readonly ushort UNKNOWN_COMPONENT_TYPE_ID = ushort.MaxValue;

	//these are always populated
	public Type ComponentType;
	public Type InteractionType;
	//object that will process the interaction. NetId.Invalid if the server should determine this.
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
	//whether or not the player had the alt key pressed when performing the interaction.
	public bool IsAltUsed;
	//these are all used when it's an InventoryApply to denote the target slot
	//netid of targeted storage
	public uint Storage;
	//slot index of slot targeted in storage (-1 if tareting named slot)
	public int SlotIndex;
	//named slot targeted in storage
	public NamedSlot NamedSlot;

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

	public override void Process()
	{
		var performer = SentByPlayer.GameObject;

		if (SentByPlayer == null || SentByPlayer.Script == null || SentByPlayer.Script.ItemStorage == null)
		{
			return;
		}

		if (InteractionType == typeof(PositionalHandApply))
		{
			//look up item in active hand slot
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			LoadMultipleObjects(new uint[]{
				TargetObject, ProcessorObject
			});
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
			LoadMultipleObjects(new uint[]{
				TargetObject, ProcessorObject
			});
			var targetObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			var interaction = HandApply.ByClient(performer, usedObject, targetObj, TargetBodyPart, usedSlot, Intent, IsAltUsed);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(AimApply))
		{
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			LoadNetworkObject(ProcessorObject);
			var processorObj = NetworkObject;
			var interaction = AimApply.ByClient(performer, TargetVector, usedObject, usedSlot, MouseButtonState, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(MouseDrop))
		{
			LoadMultipleObjects(new uint[]{UsedObject,
				TargetObject, ProcessorObject
			});
			var usedObj = NetworkObjects[0];
			var targetObj = NetworkObjects[1];
			var processorObj = NetworkObjects[2];
			var interaction = MouseDrop.ByClient(performer, usedObj, targetObj, Intent);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(HandActivate))
		{
			LoadNetworkObject(ProcessorObject);

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
			LoadMultipleObjects(new uint[]{ProcessorObject, UsedObject,
				Storage
			});
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
			var interaction = InventoryApply.ByClient(performer, targetSlot, fromSlot, Intent, IsAltUsed);
			ProcessInteraction(interaction, processorObj);
		}
		else if (InteractionType == typeof(TileApply))
		{
			var clientStorage = SentByPlayer.Script.ItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
			LoadNetworkObject(ProcessorObject);
			var processorObj = NetworkObject;
			processorObj.GetComponent<InteractableTiles>().ServerProcessInteraction(SentByPlayer.GameObject,
				TargetVector, processorObj, usedSlot, usedObject, Intent,
				TileApply.ApplyType.HandApply);
		}
		else if(InteractionType == typeof(TileMouseDrop))
		{
			LoadMultipleObjects(new uint[]{UsedObject,
				ProcessorObject
			});

			var usedObj = NetworkObjects[0];
			var processorObj = NetworkObjects[1];
			processorObj.GetComponent<InteractableTiles>().ServerProcessInteraction(SentByPlayer.GameObject,
				TargetVector, processorObj, null, usedObj, Intent,
				TileApply.ApplyType.MouseDrop);
		}
	}

	private void ProcessInteraction<T>(T interaction, GameObject processorObj)
		where T : Interaction
	{
		//find the indicated component if one was indicated
		if (ComponentType != null)
		{
			if (processorObj == null)
			{
				Logger.LogWarning("processorObj is null, action will not be performed.", Category.Interaction);
				return;
			}
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
		else
		{
			// client wasn't sure which component of which object should be triggered, check them in the proper order
			// and trigger the first one that should happen, rolling back any that shouldn't happen.

			//always check used object first
			if (interaction.UsedObject)
			{
				var interactables = interaction.UsedObject.GetComponents<IInteractable<T>>()
					.Where(c => c != null && (c as MonoBehaviour).enabled);
				Logger.LogTraceFormat("Server checking which component to trigger for {0} on object {1}", Category.Interaction,
					typeof(T).Name, interaction.UsedObject.name);
				if (ServerCheckAndTrigger(interaction, interactables))
				{
					return;
				}
			}
			//check the target object if there is one
			if (interaction is TargetedInteraction targetedInteraction)
			{
				var interactables = targetedInteraction.TargetObject.GetComponents<IInteractable<T>>()
					.Where(c => c != null && (c as MonoBehaviour).enabled);
				Logger.LogTraceFormat("Server checking which component to trigger for {0} on object {1}", Category.Interaction,
					typeof(T).Name, targetedInteraction.TargetObject.name);
				if (ServerCheckAndTrigger(interaction, interactables))
				{
					return;
				}
			}
		}
	}


	private static bool ServerCheckAndTrigger<T>(T interaction, IEnumerable<IInteractable<T>> interactables) where T : Interaction
	{
		foreach (var interactable in interactables.Reverse())
		{
			if (interactable.ServerCheckInteract(interaction))
			{
				//perform if not on cooldown
				if (Cooldowns.TryStartServer(interaction, CommonCooldowns.Instance.Interaction))
				{
					interactable.ServerPerformInteraction(interaction);
				}
				else
				{
					//hit a cooldown, rollback if this component implements it, in case client tried to predict it
					if (interactable is IPredictedInteractable<T> predictedInteractable)
					{
						predictedInteractable.ServerRollbackClient(interaction);
					}
				}

				// An interaction should've triggered and either did or hit a cooldown, so we're done
				// checking this request.
				return true;
			}
			else
			{
				//rollback if this component implements it, in case client tried to predict it
				if (interactable is IPredictedInteractable<T> predictedInteractable)
				{
					predictedInteractable.ServerRollbackClient(interaction);
				}
			}
		}

		// no interactions triggered
		return false;
	}


	//only intended to be used by core if2 classes, please use InteractionUtils.RequestInteract instead.
	//pass null for interactableComponent if you want the server to determine which component of the involved objects should be triggered.
	//(which can be useful when client doesn't have enough info to know which one to trigger)
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

		if (interactableComponent != null && !(interactableComponent is Component))
		{
			Logger.LogError("interactableComponent must be a component, but isn't. The message will not be sent.",
				Category.NetMessage);
			return;
		}

		var comp = interactableComponent as Component;
		var msg = new RequestInteractMessage()
		{
			ComponentType = interactableComponent == null ? null : interactableComponent.GetType(),
			InteractionType = typeof(T),
			ProcessorObject = comp == null ? NetId.Invalid : comp.GetComponent<NetworkIdentity>().netId,
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
			msg.IsAltUsed = casted.IsAltClick;
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
			msg.IsAltUsed = casted.IsAltClick;
		}
		msg.Send();
	}

	//only intended to be used by core if2 classes
	public static void SendTileApply(TileApply tileApply, InteractableTiles interactableTiles, TileInteraction tileInteraction)
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
			TargetVector = tileApply.TargetVector
		};
		msg.Send();
	}

	public static void SendTileMouseDrop(TileMouseDrop mouseDrop, InteractableTiles interactableTiles)
	{
		if (!mouseDrop.Performer.Equals(PlayerManager.LocalPlayer))
		{
			Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
							" This is not allowed. Client can only perform an interaction as themselves. Message" +
							" will not be sent.", Category.NetMessage);
			return;
		}

		var msg = new RequestInteractMessage()
		{
			ComponentType = typeof(InteractableTiles),
			InteractionType = typeof(TileMouseDrop),
			ProcessorObject = interactableTiles.GetComponent<NetworkIdentity>().netId,
			Intent = mouseDrop.Intent,
			UsedObject = mouseDrop.UsedObject.NetId(),
			TargetVector = mouseDrop.TargetVector
		};
		msg.Send();
	}

	public override void Deserialize(NetworkReader reader)
	{

		base.Deserialize(reader);
		var componentID = reader.ReadUInt16();
		if (componentID == UNKNOWN_COMPONENT_TYPE_ID)
		{
			//client didn't know which to trigger, leave ComponentType null
			ComponentType = null;
		}
		else
		{
			//client requested a specific component.
			ComponentType = componentIDToComponentType[componentID];
		}

		InteractionType = interactionIDToInteractionType[reader.ReadByte()];
		if (componentID != UNKNOWN_COMPONENT_TYPE_ID)
		{
			// client specified exact component
			ProcessorObject = reader.ReadUInt32();
		}
		else
		{
			// client requested server to check the interaction
			ProcessorObject = NetId.Invalid;
		}
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
			IsAltUsed = reader.ReadBoolean();
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
			IsAltUsed = reader.ReadBoolean();
		}
		else if (InteractionType == typeof(TileApply))
		{
			TargetVector = reader.ReadVector2();
		}
		else if(InteractionType == typeof(TileMouseDrop))
		{
			UsedObject = reader.ReadUInt32();
			TargetVector = reader.ReadVector2();
		}
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		// indicate unknown component if client requested it
		if (ComponentType == null)
		{
			writer.WriteUInt16(UNKNOWN_COMPONENT_TYPE_ID);
		}
		else
		{
			writer.WriteUInt16(componentTypeToComponentID[ComponentType]);
		}
		writer.WriteByte(interactionTypeToInteractionID[InteractionType]);
		//server determines processor object if client specified unknown component
		if (ComponentType != null)
		{
			writer.WriteUInt32(ProcessorObject);
		}
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
			writer.WriteBoolean(IsAltUsed);
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
			writer.WriteBoolean(IsAltUsed);
		}
		else if (InteractionType == typeof(TileApply))
		{
			writer.WriteVector2(TargetVector);
		}
		else if(InteractionType == typeof(TileMouseDrop))
		{
			writer.WriteUInt32(UsedObject);
			writer.WriteVector2(TargetVector);
		}
	}

}
