using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class SeedExtractor : ManagedNetworkBehaviour, IInteractable<HandApply>, IServerSpawn, IAPCPowered
{
	private Queue<GrownFood> foodToBeProcessed;
	private int processingProgress;
	private PowerStates currentState = PowerStates.Off;

	[Tooltip("Time it takes to process a single piece of produce")]
	[SerializeField]
	private int processingTime = 0;

	[SerializeField]
	private RegisterObject registerObject = null;

	[Tooltip("Inventory to store food waiting to be processed")]
	[SerializeField]
	private ItemStorage storage = null;

	public bool IsProcessing => foodToBeProcessed.Count != 0;

	/// <summary>
	/// Handles processing produce into seed packets at rate defined by processingTime
	/// </summary>
	[Server]
	public override void FixedUpdateMe()
	{
		//Only run on server and if there is something to process and the device has power
		if (!isServer || !IsProcessing || currentState == PowerStates.Off) { return; }
		//If processing isn't done keep waiting
		if (processingProgress < processingTime)
		{
			processingProgress++;
			return;
		}

		//Handle completed processing
		processingProgress = 0;
		var grownFood = foodToBeProcessed.Dequeue();
		GameObject seedPacket = grownFood.SeedPacket;

		//Create seed packet in world
		Spawn.ServerPrefab(seedPacket, registerObject.WorldPositionServer);

		//De-spawn processed food
		Inventory.ServerDespawn(grownFood.gameObject);
		if (foodToBeProcessed.Count == 0)
		{
			Chat.AddLocalMsgToChat("The seed extractor finishes processing", (Vector2Int)registerObject.WorldPosition, this.gameObject);
		}
	}

	/// <summary>
	/// Sets up seed extractor at round start
	/// </summary>
	[Server]
	public void OnSpawnServer(SpawnInfo info)
	{
		foodToBeProcessed = new Queue<GrownFood>();
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
		}
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
		if(foodToBeProcessed.Count > 0)
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