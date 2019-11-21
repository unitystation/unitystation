using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows an item to be stacked, occupying a single inventory slot.
/// </summary>
public class Stackable : NetworkBehaviour, IServerSpawn, ICheckedInteractable<InventoryApply>
{
	[Tooltip("Amount initially in the stack when this is spawned.")]
	[SerializeField]
	private int initialAmount = 1;

	[Tooltip("Max amount allowed in the stack.")]
	[SerializeField]
	private int maxAmount = 50;

	/// <summary>
	/// Amount of things in this stack.
	/// </summary>
	public int Amount => amount;

	/// <summary>
	/// amount currently in the stack
	/// </summary>
	[SyncVar(hook = nameof(SyncAmount))]
	private int amount;

	private Pickupable pickupable;
	private GameObject prefab;
	private PushPull pushPull;
	private RegisterTile registerTile;


	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		prefab = Spawn.DeterminePrefab(gameObject);
		amount = initialAmount;
		pushPull = GetComponent<PushPull>();
		registerTile = GetComponent<RegisterTile>();
		if (CustomNetworkManager.IsServer)
		{
			registerTile.OnLocalPositionChangedServer.AddListener(OnLocalPositionChangedServer);
		}
	}

	private void OnLocalPositionChangedServer(Vector3Int newLocalPos)
	{
		//if we are being pulled, combine the stacks with any on the ground under us.
		if (pushPull.IsBeingPulled)
		{
			foreach (var stackable in registerTile.Matrix.Get<Stackable>(newLocalPos, true))
			{
				if (stackable == this) continue;
				if (stackable.prefab == prefab)
				{
					//combine
					SyncAmount(amount + stackable.amount);
					Despawn.ServerSingle(stackable.gameObject);
				}
			}
		}
	}

	public override void OnStartClient()
	{
		SyncAmount(this.amount);
	}

	public override void OnStartServer()
	{
		SyncAmount(this.amount);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncAmount(initialAmount);
	}

	private void SyncAmount(int newAmount)
	{
		this.amount = newAmount;
		pickupable.RefreshUISlotImage();
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only has logic if this is the target object
		if (interaction.TargetObject != gameObject) return false;

		//clicking on it with an empty hand when stack is in another hand to take one from it,
		//(if there is only one in this stack we will defer to normal inventory transfer logic)
		if (interaction.IsFromHandSlot && interaction.IsToHandSlot && interaction.FromSlot.IsEmpty && amount > 1) return true;

		//combining another stack with this stack.
		if (CanStackWith(interaction.UsedObject)) return true;

		return false;
	}

	private bool CanStackWith(GameObject other)
	{
		if (other == null) return false;
		var otherStack = other.GetComponent<Stackable>();
		return otherStack != null && otherStack.prefab == prefab;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		//clicking on it with an empty hand when stack is in another hand to take one from it
		if (interaction.IsFromHandSlot && interaction.IsToHandSlot && interaction.FromSlot.IsEmpty)
		{
			//spawn a new one and put it into the from slot with a stack size of 1
			var single = Spawn.ServerPrefab(prefab).GameObject;
			single.GetComponent<Stackable>().SyncAmount(1);
			Inventory.ServerAdd(single, interaction.FromSlot);
			//decrease our stack amount by 1.
			SyncAmount(this.amount - 1);
		}
		else if (CanStackWith(interaction.UsedObject))
		{
			//combining the stacks
			var destinationStackable = this;
			var sourceStackable = interaction.UsedObject.GetComponent<Stackable>();

			//increase the destinations amount by the source's amount
			destinationStackable.SyncAmount(amount + sourceStackable.amount);

			//consume the source
			Inventory.ServerDespawn(interaction.FromSlot);
		}
	}
}
