using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows an item to be stacked, occupying a single inventory slot.
/// </summary>
public class Stackable : NetworkBehaviour, IServerSpawn, ICheckedInteractable<InventoryApply>, ICheckedInteractable<HandApply>
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
					ServerCombine(stackable);
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

	/// <summary>
	/// Consumes the specified amount of quantity from this stack. Despawns if entirely consumed.
	/// Does nothing if consumed is greater than the amount in this stack.
	/// </summary>
	/// <param name="consumed"></param>
	[Server]
	public void ServerConsume(int consumed)
	{
		if (consumed > amount)
		{
			Logger.LogErrorFormat("Consumed amount {0} is greater than amount in this stack {1}, will not consume.",
				 Category.Inventory, consumed, amount);
			return;
		}
		SyncAmount(amount - consumed);
		if (amount <= 0)
		{
			Despawn.ServerSingle(gameObject);
		}
	}

	/// <summary>
	/// Adds the quantity in toAdd to this stackable (up to maxAmount) and despawns toAdd
	/// if it is entirely used up.
	/// Does nothing if they aren't the same thing
	/// </summary>
	/// <param name="toAdd"></param>
	[Server]
	public void ServerCombine(Stackable toAdd)
	{
		if (toAdd.prefab != prefab)
		{
			Logger.LogErrorFormat("toAdd {0} with prefab {1} doesn't match this {2} with prefab {3}, cannot comine.",
				Category.Inventory, toAdd, toAdd.prefab, this, prefab);
			return;
		}
		var amountToConsume = Math.Min(toAdd.amount, maxAmount - amount);
		if (amountToConsume <= 0) return;

		toAdd.ServerConsume(amountToConsume);
		SyncAmount(amount + amountToConsume);
	}

	private bool CanCombine(GameObject toAdd)
	{
		if (toAdd == null) return false;
		var otherStack = toAdd.GetComponent<Stackable>();
		return otherStack != null && otherStack.prefab == prefab && amount < maxAmount;
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
		if (CanCombine(interaction.UsedObject)) return true;

		return false;
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
			ServerConsume(1);
		}
		else if (CanCombine(interaction.UsedObject))
		{
			//combining the stacks
			var destinationStackable = this;
			var sourceStackable = interaction.UsedObject.GetComponent<Stackable>();

			ServerCombine(sourceStackable);
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//can only hand apply if this stackable is in hand and another stackable of
		//same type is being targeted
		if (interaction.HandObject != gameObject) return false;

		if (CanCombine(interaction.TargetObject)) return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		ServerCombine(interaction.TargetObject.GetComponent<Stackable>());
	}
}
