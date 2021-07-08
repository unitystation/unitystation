using Mirror;
using System.Collections.Generic;
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
				CustomNetTransform netTransform = packet.GetComponent<CustomNetTransform>();
				netTransform.AppearAtPosition(spawnPos);
				netTransform.AppearAtPositionServer(spawnPos);
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
			Inventory.ServerDespawn(grownFood.gameObject);
			if (foodToBeProcessed.Count == 0)
			{
				Chat.AddLocalMsgToChat("The seed extractor finishes processing", gameObject);
			}
		}

		/// <summary>
		/// Spawns seed packet in world and removes it from internal list
		/// </summary>
		/// <param name="seedPacket">Seed packet to spawn</param>
		public void DispenseSeedPacket(SeedPacket seedPacket)
		{
			// Spawn packet
			Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
			CustomNetTransform netTransform = seedPacket.GetComponent<CustomNetTransform>();
			netTransform.AppearAtPosition(spawnPos);
			netTransform.AppearAtPositionServer(spawnPos);

			// Notify chat
			Chat.AddLocalMsgToChat($"{seedPacket.gameObject.ExpensiveName()} was dispensed from the seed extractor", gameObject);

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
			if (interaction.HandObject != null)
			{
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
					Chat.AddLocalMsgToChat("The seed extractor begins processing", gameObject);
				}
				foodToBeProcessed.Enqueue(grownFood);
				return;
			}
			// If no interaction happens
			networkTab.ServerPerformInteraction(interaction);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			   interaction.TargetObject == gameObject &&
			   interaction.HandObject != null &&
			   interaction.HandObject.TryGetComponent<GrownFood>(out _);
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
					Chat.AddLocalMsgToChat("The seed extractor begins processing", gameObject);
				}
				//Off state
				else if (newState == PowerState.Off)
				{
					Chat.AddLocalMsgToChat("The seed extractor shuts down!", gameObject);
				}
			}
			currentState = newState;
		}

		#endregion
	}

	public class SeedExtractorUpdateEvent : UnityEvent { }
}
