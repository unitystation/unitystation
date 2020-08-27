using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HasNetworkTab))]
public class SeedExtractor : ManagedNetworkBehaviour, IInteractable<HandApply>, IServerSpawn, IAPCPowered
{
	private Queue<GrownFood> foodToBeProcessed;
	private float processingProgress;
	private PowerStates currentState = PowerStates.Off;
	private SeedExtractorUpdateEvent updateEvent = new SeedExtractorUpdateEvent();


	//Time it takes to process a single piece of produce
	private float processingTime = 3f;

	[SerializeField]
	private RegisterObject registerObject = null;

	[Tooltip("Inventory to store food waiting to be processed")]
	[SerializeField]
	private ItemStorage storage = null;
	private HasNetworkTab networkTab;

	public bool IsProcessing => foodToBeProcessed.Count != 0;
	public List<SeedPacket> seedPackets;
	public SeedExtractorUpdateEvent UpdateEvent => updateEvent;
	private void Awake()
	{
		networkTab = GetComponent<HasNetworkTab>();
		foodToBeProcessed = new Queue<GrownFood>();
		seedPackets = new List<SeedPacket>();
	}

	/// <summary>
	/// Handles processing produce into seed packets at rate defined by processingTime
	/// </summary>
	public override void UpdateMe()
	{
		//Only run on server and if there is something to process and the device has power
		if (!isServer || !IsProcessing || currentState == PowerStates.Off) { return; }
		//If processing isn't done keep waiting
		if (processingProgress < processingTime)
		{
			processingProgress += Time.deltaTime;
			return;
		}

		//Handle completed processing
		processingProgress = 0;
		var grownFood = foodToBeProcessed.Dequeue();
		var seedPacket = Spawn.ServerPrefab(grownFood.SeedPacket).GameObject.GetComponent<SeedPacket>();
		seedPacket.plantData = PlantData.CreateNewPlant(grownFood.GetPlantData());

		//Add seed packet to dispenser
		seedPackets.Add(seedPacket);
		updateEvent.Invoke();

		//De-spawn processed food
		Inventory.ServerDespawn(grownFood.gameObject);
		if (foodToBeProcessed.Count == 0)
		{
			Chat.AddLocalMsgToChat("The seed extractor finishes processing", (Vector2Int)registerObject.WorldPosition, this.gameObject);
		}
	}

	/// <summary>
	/// Spawns seed packet in world and removes it from internal list
	/// </summary>
	/// <param name="seedPacket">Seed packet to spawn</param>
	public void DispenseSeedPacket(SeedPacket seedPacket)
	{
		//Spawn packet
		Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
		CustomNetTransform netTransform = seedPacket.GetComponent<CustomNetTransform>();
		netTransform.AppearAtPosition(spawnPos);
		netTransform.AppearAtPositionServer(spawnPos);

		//Notify chat
		Chat.AddLocalMsgToChat($"{seedPacket.gameObject.ExpensiveName()} was dispensed from the seed extractor", gameObject.RegisterTile().WorldPosition.To2Int(), gameObject);

		//Remove spawned entry from list
		seedPackets.Remove(seedPacket);
		updateEvent.Invoke();
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
	/// Handles placing produce into the seed extractor
	/// </summary>
	/// <param name="interaction">contains information about the interaction</param>
	[Server]
	public void ServerPerformInteraction(HandApply interaction)
	{
		var grownFood = interaction.HandObject?.GetComponent<GrownFood>();
		if (grownFood != null)
		{
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
			if (foodToBeProcessed.Count == 0 && currentState != PowerStates.Off)
			{
				Chat.AddLocalMsgToChat("The seed extractor begins processing", (Vector2Int)registerObject.WorldPosition, this.gameObject);
			}
			foodToBeProcessed.Enqueue(grownFood);
			return;
		}
		//If no interaction happens
		networkTab.ServerPerformInteraction(interaction);
	}

	/// <summary>
	/// IS NOT USED BUT REQUIRED BY IAPCPowered
	/// </summary>
	void IAPCPowered.PowerNetworkUpdate(float Voltage)
	{
		throw new System.NotImplementedException();
	}

	/// <summary>
	/// Triggers on device power state change
	/// </summary>
	/// <param name="newState">New state</param>
	void IAPCPowered.StateUpdate(PowerStates newState)
	{
		//Ignore if state hasn't changed
		if(newState == currentState) { return; }

		//Show processing state change
		if(foodToBeProcessed?.Count > 0)
		{
			//Any state other than off
			if(currentState == PowerStates.Off)
			{
				Chat.AddLocalMsgToChat("The seed extractor begins processing", (Vector2Int)registerObject.WorldPosition, this.gameObject);
			}
			//Off state
			else if(newState == PowerStates.Off)
			{
				Chat.AddLocalMsgToChat("The seed extractor shuts down!", (Vector2Int)registerObject.WorldPosition, this.gameObject);
			}
		}
		currentState = newState;
	}
}
public class SeedExtractorUpdateEvent : UnityEvent { }