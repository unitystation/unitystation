using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Systems.Interaction;
using Mirror;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEngine;


namespace Messages.Client.Interaction
{
	/// <summary>
	/// Requests for the server to perform a given interaction
	/// </summary>
	public class RequestInteractMessage : ClientMessage<RequestInteractMessage.NetMessage>
	{
		/**
		 * this is sent as the componentID when the client doesn't know
		 * exactly which interaction should be triggered, and will
		 * defer to the server. In this case,
		 * the server will check each interaction of the given interaction type on the involved
		 * objects to see which should occur.
		 */
		public static readonly ushort UNKNOWN_COMPONENT_TYPE_ID = ushort.MaxValue;

		public struct NetMessage : NetworkMessage
		{
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
			//Used to get correct item storage on game object if there's multiple
			public uint StorageIndexOnGameObject;
			//slot index of slot targeted in storage (-1 if tareting named slot)
			public int SlotIndex;
			//named slot targeted in storage
			public NamedSlot NamedSlot;
			// connections used in CableApply
			public Connection connectionPointA, connectionPointB;
			// Requested option of a right-click context menu interaction
			public string RequestedOption;
			// Click Type for AI interaction
			public AiActivate.ClickTypes ClickTypes;
		}

		public static readonly Dictionary<ushort, Type> componentIDToComponentType = new Dictionary<ushort, Type>();
		public static readonly Dictionary<Type, ushort> componentTypeToComponentID = new Dictionary<Type, ushort>();
		public static readonly Dictionary<byte, Type> interactionIDToInteractionType = new Dictionary<byte, Type>();
		public static readonly Dictionary<Type, byte> interactionTypeToInteractionID = new Dictionary<Type, byte>();

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
				typeof(global::Interaction).Assembly.GetTypes()
					.Where(type => typeof(global::Interaction).IsAssignableFrom(type))
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

		public override void Process(NetMessage msg)
		{
			var ComponentType = msg.ComponentType;
			var InteractionType = msg.InteractionType;
			var ProcessorObject = msg.ProcessorObject;
			var Intent = msg.Intent;
			var TargetObject = msg.TargetObject;
			var UsedObject = msg.UsedObject;
			var TargetBodyPart = msg.TargetBodyPart;
			var TargetVector = msg.TargetVector;
			var MouseButtonState = msg.MouseButtonState;
			var IsAltUsed = msg.IsAltUsed;
			var Storage = msg.Storage;
			var SlotIndex = msg.SlotIndex;
			var NamedSlot = msg.NamedSlot;
			var connectionPointA = msg.connectionPointA;
			var connectionPointB = msg.connectionPointB;
			var RequestedOption = msg.RequestedOption;

			var performer = SentByPlayer.GameObject;

			if (SentByPlayer == null || SentByPlayer.Script == null)
			{
				return;
			}

			if (SentByPlayer.Script.DynamicItemStorage == null)
			{
				if (InteractionType == typeof(AiActivate))
				{
					LoadMultipleObjects(new uint[] { TargetObject, ProcessorObject });
					var targetObj = NetworkObjects[0];
					var processorObj = NetworkObjects[1];

					var interaction = new AiActivate(performer, null, targetObj, Intent, msg.ClickTypes);
					ProcessInteraction(interaction, processorObj, ComponentType);
				}

				return;
			}

			if (InteractionType == typeof(PositionalHandApply))
			{
				//look up item in active hand slot
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedSlot = clientStorage.GetActiveHandSlot();
				var usedObject = clientStorage.GetActiveHandSlot()?.ItemObject;
				LoadMultipleObjects(new uint[]{
					TargetObject, ProcessorObject
				});
				var targetObj = NetworkObjects[0];
				var processorObj = NetworkObjects[1];
				CheckMatrixSync(ref targetObj);
				CheckMatrixSync(ref processorObj);

				var interaction = PositionalHandApply.ByClient(
						performer, usedObject, targetObj, TargetVector, usedSlot, Intent, TargetBodyPart, IsAltUsed);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(HandApply))
			{
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedSlot = clientStorage.GetActiveHandSlot();
				var usedObject = clientStorage.GetActiveHandSlot()?.ItemObject;
				LoadMultipleObjects(new uint[]{
					TargetObject, ProcessorObject
				});
				var targetObj = NetworkObjects[0];
				var processorObj = NetworkObjects[1];
				CheckMatrixSync(ref targetObj);
				CheckMatrixSync(ref processorObj);

				var interaction = HandApply.ByClient(performer, usedObject, targetObj, TargetBodyPart, usedSlot, Intent, IsAltUsed);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(AimApply))
			{
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedSlot = clientStorage.GetActiveHandSlot();
				var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
				LoadNetworkObject(ProcessorObject);
				var processorObj = NetworkObject;
				CheckMatrixSync(ref processorObj);

				var interaction = AimApply.ByClient(performer, TargetVector, usedObject, usedSlot, MouseButtonState, Intent);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(MouseDrop))
			{
				LoadMultipleObjects(new uint[]{UsedObject,
					TargetObject, ProcessorObject
				});

				var usedObj = NetworkObjects[0];
				var targetObj = NetworkObjects[1];
				var processorObj = NetworkObjects[2];
				CheckMatrixSync(ref targetObj);
				CheckMatrixSync(ref processorObj);

				var interaction = MouseDrop.ByClient(performer, usedObj, targetObj, Intent);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(HandActivate))
			{
				LoadNetworkObject(ProcessorObject);

				var processorObj = NetworkObject;
				CheckMatrixSync(ref processorObj);

				var performerObj = SentByPlayer.GameObject;
				//look up item in active hand slot
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedSlot = clientStorage.GetActiveHandSlot();
				var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
				var interaction = HandActivate.ByClient(performer, usedObject, usedSlot, Intent);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(InventoryApply))
			{
				LoadMultipleObjects(new uint[]{ProcessorObject, UsedObject,
					Storage
				});
				var processorObj = NetworkObjects[0];
				var usedObj = NetworkObjects[1];
				var storageObj = NetworkObjects[2];
				CheckMatrixSync(ref processorObj);

				ItemSlot targetSlot = null;
				if (SlotIndex == -1)
				{
					targetSlot = ItemSlot.GetNamed(storageObj.GetComponents<ItemStorage>()[msg.StorageIndexOnGameObject], NamedSlot);
				}
				else
				{
					targetSlot = ItemSlot.GetIndexed(storageObj.GetComponents<ItemStorage>()[msg.StorageIndexOnGameObject], SlotIndex);
				}

				//if used object is null, then empty hand was used
				ItemSlot fromSlot = null;
				if (usedObj == null)
				{
					fromSlot = SentByPlayer.Script.DynamicItemStorage.GetActiveHandSlot();
				}
				else
				{
					fromSlot = usedObj.GetComponent<Pickupable>().ItemSlot;
				}
				var interaction = InventoryApply.ByClient(performer, targetSlot, fromSlot, Intent, IsAltUsed);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(TileApply))
			{
				try
				{
					var clientStorage = SentByPlayer.Script.DynamicItemStorage;
					var usedSlot = clientStorage.GetActiveHandSlot();
					var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
					LoadNetworkObject(ProcessorObject);
					var processorObj = NetworkObject;
					CheckMatrixSync(ref processorObj);

					processorObj.GetComponent<InteractableTiles>().ServerProcessInteraction(SentByPlayer.GameObject,
						TargetVector, processorObj, usedSlot, usedObject, Intent,
						TileApply.ApplyType.HandApply);
				}
				catch (NullReferenceException exception)
				{
					Logger.LogError("Caught a NRE in RequestInteractMessage.Process(): " + exception.Message, Category.Interaction);
				}
			}
			else if (InteractionType == typeof(TileMouseDrop))
			{
				LoadMultipleObjects(new uint[]{UsedObject,
					ProcessorObject
				});

				var usedObj = NetworkObjects[0];
				var processorObj = NetworkObjects[1];
				CheckMatrixSync(ref processorObj);

				processorObj.GetComponent<InteractableTiles>().ServerProcessInteraction(SentByPlayer.GameObject,
					TargetVector, processorObj, null, usedObj, Intent,
					TileApply.ApplyType.MouseDrop);
			}
			else if (InteractionType == typeof(ConnectionApply))
			{
				//look up item in active hand slot
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedSlot = clientStorage.GetActiveHandSlot();
				var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
				LoadMultipleObjects(new uint[]{
					TargetObject, ProcessorObject
				});
				var targetObj = NetworkObjects[0];
				var processorObj = NetworkObjects[1];
				CheckMatrixSync(ref targetObj);
				CheckMatrixSync(ref processorObj);

				var interaction = ConnectionApply.ByClient(performer, usedObject, targetObj, connectionPointA, connectionPointB, TargetVector, usedSlot, Intent);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
			else if (InteractionType == typeof(ContextMenuApply))
			{
				LoadMultipleObjects(new uint[] { TargetObject, ProcessorObject });
				var clientStorage = SentByPlayer.Script.DynamicItemStorage;
				var usedObj = clientStorage.GetActiveHandSlot().ItemObject;
				var targetObj = NetworkObjects[0];
				var processorObj = NetworkObjects[1];
				CheckMatrixSync(ref targetObj);
				CheckMatrixSync(ref processorObj);

				var interaction = ContextMenuApply.ByClient(performer, usedObj, targetObj, RequestedOption, Intent);
				ProcessInteraction(interaction, processorObj, ComponentType);
			}
		}

		private void CheckMatrixSync(ref GameObject toCheck)
		{
			//If it is a matrix sync, then grab the top level matrix instead as that is what we want
			if (toCheck != null && toCheck.GetComponent<MatrixSync>() != null)
			{
				toCheck = toCheck.transform.parent.gameObject;
			}
		}

		private void ProcessInteraction<T>(T interaction, GameObject processorObj, Type ComponentType)
			where T : global::Interaction
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
					if(targetedInteraction.TargetObject == null) return;
					var interactables = targetedInteraction.TargetObject.GetComponents<IInteractable<T>>()
						.Where(c => c != null && (c as MonoBehaviour)?.enabled == true);
					Logger.LogTraceFormat("Server checking which component to trigger for {0} on object {1}", Category.Interaction,
						typeof(T).Name, targetedInteraction.TargetObject.name);
					if (ServerCheckAndTrigger(interaction, interactables))
					{
						return;
					}
				}
			}
		}

		private static bool ServerCheckAndTrigger<T>(T interaction, IEnumerable<IInteractable<T>> interactables) where T : global::Interaction
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
			where T : global::Interaction
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
				                " will not be sent.", Category.Exploits);
				return;
			}

			if (interactableComponent != null && !(interactableComponent is Component))
			{
				Logger.LogError("interactableComponent must be a component, but isn't. The message will not be sent.",
					Category.Exploits);
				return;
			}

			var comp = interactableComponent as Component;
			var msg = new NetMessage()
			{
				ComponentType = interactableComponent == null ? null : interactableComponent.GetType(),
				InteractionType = typeof(T),
				ProcessorObject = comp == null ? NetId.Invalid : GetNetId(comp.gameObject),
				Intent = interaction.Intent
			};
			if (typeof(T) == typeof(PositionalHandApply))
			{
				var casted = interaction as PositionalHandApply;
				msg.TargetObject = GetNetId(casted.TargetObject);
				msg.TargetVector = casted.TargetVector;
				msg.TargetBodyPart = casted.TargetBodyPart;
			}
			else if (typeof(T) == typeof(HandApply))
			{
				var casted = interaction as HandApply;
				msg.TargetObject = GetNetId(casted.TargetObject);
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
				msg.TargetObject = GetNetId(casted.TargetObject);
				msg.UsedObject = GetNetId(casted.UsedObject);
			}
			else if (typeof(T) == typeof(InventoryApply))
			{
				var casted = interaction as InventoryApply;

				//StorageIndexOnGameObject
				msg.StorageIndexOnGameObject = 0;
				foreach (var itemStorage in NetworkIdentity.spawned[casted.TargetSlot.ItemStorageNetID].GetComponents<ItemStorage>())
				{
					if (itemStorage == casted.TargetSlot.ItemStorage)
					{
						break;
					}

					msg.StorageIndexOnGameObject++;
				}
				msg.Storage = casted.TargetSlot.ItemStorageNetID;
				msg.SlotIndex = casted.TargetSlot.SlotIdentifier.SlotIndex;
				msg.NamedSlot = casted.TargetSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.none);
				msg.UsedObject = GetNetId(casted.UsedObject);
				msg.IsAltUsed = casted.IsAltClick;
			}
			else if (typeof(T) == typeof(ConnectionApply))
			{
				var casted = interaction as ConnectionApply;
				msg.TargetObject = GetNetId(casted.TargetObject);
				msg.TargetVector = casted.TargetVector;
				msg.connectionPointA = casted.WireEndA;
				msg.connectionPointB = casted.WireEndB;
			}
			else if (typeof(T) == typeof(ContextMenuApply))
			{
				var casted = interaction as ContextMenuApply;
				msg.TargetObject = GetNetId(casted.TargetObject);
				msg.RequestedOption = casted.RequestedOption;
			}
			else if (typeof(T) == typeof(AiActivate))
			{
				var casted = interaction as AiActivate;
				msg.TargetObject = GetNetId(casted.TargetObject);
				msg.ClickTypes = casted.ClickType;
			}

			Send(msg);
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
				                " will not be sent.", Category.Exploits);
				return;
			}

			var msg = new NetMessage()
			{
				ComponentType = typeof(InteractableTiles),
				InteractionType = typeof(TileApply),
				ProcessorObject = GetNetId(interactableTiles.gameObject),
				Intent = tileApply.Intent,
				TargetVector = tileApply.TargetVector
			};
			Send(msg);
		}

		public static void SendTileMouseDrop(TileMouseDrop mouseDrop, InteractableTiles interactableTiles)
		{
			if (!mouseDrop.Performer.Equals(PlayerManager.LocalPlayer))
			{
				Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
				                " This is not allowed. Client can only perform an interaction as themselves. Message" +
				                " will not be sent.", Category.Exploits);
				return;
			}

			var msg = new NetMessage()
			{
				ComponentType = typeof(InteractableTiles),
				InteractionType = typeof(TileMouseDrop),
				ProcessorObject = GetNetId(interactableTiles.gameObject),
				Intent = mouseDrop.Intent,
				UsedObject = GetNetId(mouseDrop.UsedObject),
				TargetVector = mouseDrop.TargetVector
			};
			Send(msg);
		}

		private static uint GetNetId(GameObject objectNetIdWanted)
		{
			//If object null, which is allowed, send invalid
			if (objectNetIdWanted == null)
			{
				return NetId.Invalid;
			}

			//If this gameobject has a net id we want that one
			if (objectNetIdWanted.TryGetComponent<NetworkIdentity>(out var net))
			{
				return net.netId;
			}

			//However if it doesnt check to see if its a matrix asking for one, but the net id is on the matrix sync child
			//So grab that one instead
			if (objectNetIdWanted.TryGetComponent<NetworkedMatrix>(out var netMatrix))
			{
				if (netMatrix.MatrixSync == null)
				{
					netMatrix.BackUpSetMatrixSync();
				}

				return netMatrix.MatrixSync.netId;
			}

			Logger.LogError($"Failed to find netId for {objectNetIdWanted.name}");

			return NetId.Invalid;
		}
	}

	public static class InteractMessageReaderWriters
	{
		public static RequestInteractMessage.NetMessage Deserialize(this NetworkReader reader)
		{
			var message = new RequestInteractMessage.NetMessage();

			var componentID = reader.ReadUInt16();
			if (componentID == RequestInteractMessage.UNKNOWN_COMPONENT_TYPE_ID)
			{
				//client didn't know which to trigger, leave ComponentType null
				message.ComponentType = null;
			}
			else
			{
				//client requested a specific component.
				message.ComponentType = RequestInteractMessage.componentIDToComponentType[componentID];
			}

			message.InteractionType = RequestInteractMessage.interactionIDToInteractionType[reader.ReadByte()];
			if (componentID != RequestInteractMessage.UNKNOWN_COMPONENT_TYPE_ID)
			{
				// client specified exact component
				message.ProcessorObject = reader.ReadUInt32();
			}
			else
			{
				// client requested server to check the interaction
				message.ProcessorObject = NetId.Invalid;
			}
			message.Intent = (Intent) reader.ReadByte();

			if (message.InteractionType == typeof(PositionalHandApply))
			{
				message.TargetObject = reader.ReadUInt32();
				message.TargetVector = reader.ReadVector2();
				message.TargetBodyPart = (BodyPartType) reader.ReadUInt32();
			}
			else if (message.InteractionType == typeof(HandApply))
			{
				message.TargetObject = reader.ReadUInt32();
				message.TargetBodyPart = (BodyPartType) reader.ReadUInt32();
				message.IsAltUsed = reader.ReadBoolean();
			}
			else if (message.InteractionType == typeof(AimApply))
			{
				message.TargetVector = reader.ReadVector2();
				message.MouseButtonState = reader.ReadBoolean() ? MouseButtonState.PRESS : MouseButtonState.HOLD;
			}
			else if (message.InteractionType == typeof(MouseDrop))
			{
				message.TargetObject = reader.ReadUInt32();
				message.UsedObject = reader.ReadUInt32();
			}
			else if (message.InteractionType == typeof(InventoryApply))
			{
				message.StorageIndexOnGameObject = reader.ReadUInt32();
				message.UsedObject = reader.ReadUInt32();
				message.Storage = reader.ReadUInt32();
				message.SlotIndex = reader.ReadInt32();
				message.NamedSlot = (NamedSlot) reader.ReadInt32();
				message.IsAltUsed = reader.ReadBoolean();
			}
			else if (message.InteractionType == typeof(TileApply))
			{
				message.TargetVector = reader.ReadVector2();
			}
			else if (message.InteractionType == typeof(TileMouseDrop))
			{
				message.UsedObject = reader.ReadUInt32();
				message.TargetVector = reader.ReadVector2();
			}
			else if (message.InteractionType == typeof(ConnectionApply))
			{
				message.TargetObject = reader.ReadUInt32();
				message.TargetVector = reader.ReadVector2();
				message.connectionPointA = (Connection)reader.ReadByte();
				message.connectionPointB = (Connection)reader.ReadByte();
			}
			else if (message.InteractionType == typeof(ContextMenuApply))
			{
				message.TargetObject = reader.ReadUInt32();
				message.RequestedOption = reader.ReadString();
			}
			else if (message.InteractionType == typeof(AiActivate))
			{
				message.TargetObject = reader.ReadUInt32();
				message.ClickTypes = (AiActivate.ClickTypes)reader.ReadByte();
			}

			return message;
		}

		public static void Serialize(this NetworkWriter writer, RequestInteractMessage.NetMessage message)
		{
			// indicate unknown component if client requested it
			if (message.ComponentType == null)
			{
				writer.WriteUInt16(RequestInteractMessage.UNKNOWN_COMPONENT_TYPE_ID);
			}
			else
			{
				writer.WriteUInt16(RequestInteractMessage.componentTypeToComponentID[message.ComponentType]);
			}
			writer.WriteByte(RequestInteractMessage.interactionTypeToInteractionID[message.InteractionType]);
			//server determines processor object if client specified unknown component
			if (message.ComponentType != null)
			{
				writer.WriteUInt32(message.ProcessorObject);
			}
			writer.WriteByte((byte) message.Intent);

			if (message.InteractionType == typeof(PositionalHandApply))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteVector2(message.TargetVector);
				writer.WriteInt32((int) message.TargetBodyPart);
			}
			else if (message.InteractionType == typeof(HandApply))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteInt32((int) message.TargetBodyPart);
				writer.WriteBoolean(message.IsAltUsed);
			}
			else if (message.InteractionType == typeof(AimApply))
			{
				writer.WriteVector2(message.TargetVector);
				writer.WriteBoolean(message.MouseButtonState == MouseButtonState.PRESS);
			}
			else if (message.InteractionType == typeof(MouseDrop))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteUInt32(message.UsedObject);
			}
			else if (message.InteractionType == typeof(InventoryApply))
			{
				writer.WriteUInt32(message.StorageIndexOnGameObject);
				writer.WriteUInt32(message.UsedObject);
				writer.WriteUInt32(message.Storage);
				writer.WriteInt32(message.SlotIndex);
				writer.WriteInt32((int) message.NamedSlot);
				writer.WriteBoolean(message.IsAltUsed);
			}
			else if (message.InteractionType == typeof(TileApply))
			{
				writer.WriteVector2(message.TargetVector);
			}
			else if (message.InteractionType == typeof(TileMouseDrop))
			{
				writer.WriteUInt32(message.UsedObject);
				writer.WriteVector2(message.TargetVector);
			}
			else if (message.InteractionType == typeof(ConnectionApply))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteVector2(message.TargetVector);
				writer.WriteByte((byte)message.connectionPointA);
				writer.WriteByte((byte)message.connectionPointB);
			}
			else if (message.InteractionType == typeof(ContextMenuApply))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteString(message.RequestedOption);
			}
			else if (message.InteractionType == typeof(AiActivate))
			{
				writer.WriteUInt32(message.TargetObject);
				writer.WriteByte((byte)message.ClickTypes);
			}
		}
	}
}
