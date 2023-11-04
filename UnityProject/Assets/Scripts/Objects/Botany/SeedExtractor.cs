using Mirror;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using Systems.Electricity;
using Systems.Botany;
using Items;
using Items.Botany;

namespace Objects.Botany
{
	[RequireComponent(typeof(HasNetworkTab))]
	public class SeedExtractor : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerLifecycle, IAPCPowerable
	{
		private Queue<GrownFood> foodToBeProcessed;
		private float processingProgress;
		private PowerState currentState = PowerState.Off;

		[SerializeField, Tooltip("Time it takes to process a single piece of produce.")]
		private float processingTime = 3f;

		[Tooltip("Inventory to store food waiting to be processed")]
		[SerializeField]
		private ItemStorage storage = null;
		private HasNetworkTab networkTab;

		public bool IsProcessing => foodToBeProcessed.Count != 0;
		public List<SeedPacket> seedPackets;
		public SeedExtractorUpdateEvent UpdateEvent { get; } = new SeedExtractorUpdateEvent();

		#region Lifecycle

		private void Awake()
		{
			networkTab = GetComponent<HasNetworkTab>();
			foodToBeProcessed = new Queue<GrownFood>();
			seedPackets = new List<SeedPacket>();
		}

		/// <summary>
		/// Sets up seed extractor at round start
		/// </summary>
		[Server]
		public void OnSpawnServer(SpawnInfo info)
		{
			foodToBeProcessed = new Queue<GrownFood>();
			seedPackets = new List<SeedPacket>();
		}

		/// <summary>
		/// Ejects all the seed packets when extractor is deconstructed, but only will eject produce you
		/// put in only if it hasn't been processed by the extractor
		/// </summary>
		[Server]
		public void OnDespawnServer(DespawnInfo info)
		{
			Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
			foreach (var packet in seedPackets)
			{
				UniversalObjectPhysics ObjectPhysics = packet.GetComponent<UniversalObjectPhysics>();
				ObjectPhysics.AppearAtWorldPositionServer(spawnPos);
			}
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		/// <summary>
		/// Handles processing produce into seed packets at rate defined by processingTime
		/// Server Side Only
		/// </summary>
		private void UpdateMe()
		{
			// Only run on server and if there is something to process and the device has power
			if (IsProcessing == false || currentState == PowerState.Off) return;
			// If processing isn't done keep waiting
			if (processingProgress < processingTime)
			{
				processingProgress += Time.deltaTime;
				return;
			}

			// Handle completed processing
			processingProgress = 0;
			var grownFood = foodToBeProcessed.Dequeue();
			var seedPacket = Spawn.ServerPrefab(grownFood.SeedPacket).GameObject.GetComponent<SeedPacket>();
			seedPacket.plantData = PlantData.CreateNewPlant(grownFood.GetPlantData());

			// Add seed packet to dispenser
			seedPackets.Add(seedPacket);
			UpdateEvent.Invoke();

			// De-spawn processed food
			_ = Inventory.ServerDespawn(grownFood.gameObject);
			if (foodToBeProcessed.Count == 0)
			{
				Chat.AddActionMsgToChat(gameObject, "The seed extractor finishes processing.");
			}
		}

		/// <summary>
		/// Spawns seed packet in world and removes it from internal list
		/// </summary>
		/// <param name="seedPacket">Seed packet to spawn</param>
		public void DispenseSeedPacket(SeedPacket seedPacket)
		{
			Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
			//spawn packet if added directly into inventory by player
			//this is to fix a bug where the packet no longer becomes pickupable after adding it back into an extractor.
			if (seedPacket.gameObject.TryGetComponent<Pickupable>(out var packet))
			{
				if (packet.ItemSlot != null)
				{
					packet.ItemSlot.ItemStorage.ServerTryRemove(packet.gameObject, false, spawnPos);
					return;
				}
			}

			// Spawn packet if not added directly into inventory
			UniversalObjectPhysics ObjectPhysics = seedPacket.GetComponent<UniversalObjectPhysics>();
			ObjectPhysics.AppearAtWorldPositionServer(spawnPos);

			// Notify chat
			Chat.AddActionMsgToChat(gameObject, $"{seedPacket.gameObject.ExpensiveName()} was dispensed from the seed extractor.");

			// Remove spawned entry from list
			seedPackets.Remove(seedPacket);
			UpdateEvent.Invoke();
		}

		/// <summary>
		/// Handles placing produce into the seed extractor
		/// </summary>
		/// <param name="interaction">contains information about the interaction</param>
		[Server]
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject.TryGetComponent<SeedPacket>(out var packet))
			{
				AddSeedPacketToStorage(packet, interaction);
				return;
			}
			var grownFood = interaction.HandObject.GetComponent<GrownFood>();
			var foodAtributes = grownFood.GetComponentInParent<ItemAttributesV2>();

			if (!Inventory.ServerTransfer(interaction.HandSlot, storage.GetBestSlotFor(interaction.HandObject)))
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You try and place the {foodAtributes.ArticleName} into the seed extractor but it is full!",
					$"{interaction.Performer.name} tries to place the {foodAtributes.ArticleName} into the seed extractor but it is full!");
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer,
				$"You place the {foodAtributes.ArticleName} into the seed extractor",
				$"{interaction.Performer.name} places the {foodAtributes.name} into the seed extractor");
			if (foodToBeProcessed.Count == 0 && currentState != PowerState.Off)
			{
				Chat.AddActionMsgToChat(gameObject, "The seed extractor begins processing.");
			}
			foodToBeProcessed.Enqueue(grownFood);
		}

		private void AddSeedPacketToStorage(SeedPacket packet, HandApply interaction)
		{
			if (Inventory.ServerTransfer(interaction.HandSlot, storage.GetBestSlotFor(interaction.HandObject)))
			{
				seedPackets.Add(packet);
				UpdateEvent.Invoke();
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You place the {packet.gameObject.ExpensiveName()} into the seed extractor.",
					$"{interaction.Performer.name} places the {packet.gameObject.ExpensiveName()} into the seed extractor.");
				return;
			}
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You try and place the {packet.gameObject.ExpensiveName()} into the seed extractor but it is full!",
				$"{interaction.Performer.name} tries to place the {packet.gameObject.ExpensiveName()} into the seed extractor but it is full!");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject == null) return false;
			return DefaultWillInteract.Default(interaction, side) &&
			   interaction.TargetObject == gameObject &&
			   (interaction.HandObject.TryGetComponent<GrownFood>(out _) || interaction.HandObject.TryGetComponent<SeedPacket>(out _));
		}

		#region IAPCPowerable

		/// <summary>
		/// IS NOT USED BUT REQUIRED BY IAPCPowerable
		/// </summary>
		void IAPCPowerable.PowerNetworkUpdate(float voltage)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Triggers on device power state change
		/// </summary>
		/// <param name="newState">New state</param>
		void IAPCPowerable.StateUpdate(PowerState newState)
		{
			//Ignore if state hasn't changed
			if (newState == currentState) { return; }

			//Show processing state change
			if (foodToBeProcessed?.Count > 0)
			{
				//Any state other than off
				if (currentState == PowerState.Off)
				{
					Chat.AddActionMsgToChat(gameObject, "The seed extractor begins processing.");
				}
				//Off state
				else if (newState == PowerState.Off)
				{
					Chat.AddActionMsgToChat(gameObject, "The seed extractor shuts down!");
				}
			}
			currentState = newState;
		}

		#endregion
	}

	public class SeedExtractorUpdateEvent : UnityEvent { }
}
