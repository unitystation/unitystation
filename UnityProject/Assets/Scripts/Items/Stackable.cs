using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows an item to be stacked, occupying a single inventory slot.
/// </summary>
public class Stackable : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<InventoryApply>, ICheckedInteractable<HandApply>
{
	[Tooltip("Amount initially in the stack when this is spawned.")]
	[SerializeField]
	private int initialAmount = 1;
	public int InitialAmount => initialAmount;

	[Tooltip("Max amount allowed in the stack.")]
	[SerializeField]
	private int maxAmount = 50;

	[Tooltip("Other prefabs which can stack with this object. By default a stackable can stack with its own" +
	         " prefab, but if you create any variants which have a different initial amount you can assign them" +
	         " in this list on either prefab to allow it to recognize that it's stackable with the parent.")]
	[SerializeField]
	private List<GameObject> stacksWith;

	/// <summary>
	/// Amount of things in this stack.
	/// </summary>
	public int Amount => amount;

	public int MaxAmount => maxAmount;

	public int SpareCapacity => MaxAmount - Amount;

	/// <summary>
	/// amount currently in the stack
	/// </summary>
	[SyncVar(hook = nameof(SyncAmount))]
	private int amount;
	//server side, indicates if our amount been initialized after our initial spawn yet,
	//used so auto-stacking works for things when they are spawned simultaneously on top of each other
	private bool amountInit;

	private Pickupable pickupable;
	private PushPull pushPull;
	private RegisterTile registerTile;
	private GameObject prefab;


	private void Awake()
	{
		EnsureInit();
		this.WaitForNetworkManager(() =>
		{
			if (CustomNetworkManager.IsServer)
			{
				registerTile.OnLocalPositionChangedServer.AddListener(OnLocalPositionChangedServer);
			}
		});
	}

	private void EnsureInit()
	{
		if (pickupable != null) return;
		pickupable = GetComponent<Pickupable>();
		amount = initialAmount;
		pushPull = GetComponent<PushPull>();
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnLocalPositionChangedServer(Vector3Int newLocalPos)
	{
		//if we are being pulled, combine the stacks with any on the ground under us.
		if (pushPull.IsBeingPulled)
		{
			//check for stacking with things on the ground
			ServerStackOnGround(newLocalPos);
		}
	}

	public override void OnStartClient()
	{
		InitStacksWith();
		SyncAmount(amount, this.amount);
	}

	private void InitStacksWith()
	{
		if (stacksWith == null) stacksWith = new List<GameObject>();
		prefab = Spawn.DeterminePrefab(gameObject);
		if (prefab != null && !stacksWith.Contains(prefab))
		{
			stacksWith.Add(prefab);
		}
	}

	public override void OnStartServer()
	{
		SyncAmount(amount, this.amount);
	}

	public bool IsFull()
	{
		return Amount >= maxAmount;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		Logger.LogTraceFormat("Spawning {0}", Category.Inventory, GetInstanceID());
		InitStacksWith();
		SyncAmount(amount, initialAmount);
		amountInit = true;
		//check for stacking with things on the ground
		ServerStackOnGround(registerTile.LocalPositionServer);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		Logger.LogTraceFormat("Despawning {0}", Category.Inventory, GetInstanceID());
		amountInit = false;
	}

	private void ServerStackOnGround(Vector3Int localPosition)
	{
		//stacks with things on the same tile
		foreach (var stackable in registerTile.Matrix.Get<Stackable>(localPosition, true))
		{
			if (stackable == this) continue;
			if (StacksWith(stackable) && stackable.amountInit)
			{
				//combine
				ServerCombine(stackable);
			}
		}
	}

	private void SyncAmount(int oldAmount, int newAmount)
	{
		EnsureInit();
		Logger.LogTraceFormat("Amount {0}->{1} for {2}", Category.Inventory, amount, newAmount, GetInstanceID());
		this.amount = newAmount;
		pickupable.RefreshUISlotImage();

	}

	/// <summary>
	/// Consumes the specified amount of quantity from this stack. Despawns if entirely consumed.
	/// Does nothing if consumed is greater than the amount in this stack.
	/// </summary>
	/// <param name="consumed">Amount to consume</param>
	/// <returns>If stackable contained enough stacks and they were consumed</returns>
	[Server]
	public bool ServerConsume(int consumed)
	{
		if (consumed > amount)
		{
			Logger.LogErrorFormat("Consumed amount {0} is greater than amount in this stack {1}, will not consume.", Category.Inventory, consumed, amount);
			return false;
		}
		SyncAmount(amount, amount - consumed);
		if (amount <= 0)
		{
			Despawn.ServerSingle(gameObject);
		}
		return true;
	}

	/// <summary>
	/// Increments the amount by a specified quantity, does not go above the max.
	/// Cannot be used to reduce stacks
	/// </summary>
	/// <param name="increase">Amount to add</param>
	/// <returns>The remaining number of stacks which could not fit in the stackable</returns>
	[Server]
	public int ServerIncrease(int increase)
	{
		if (increase == 0)
		{
			return 0;
		}

		if (increase < 0)
		{
			Logger.LogErrorFormat("Attempted to increase stacks by a negative value, ignored", Category.Inventory);
			return 0;
		}

		int overflow = increase - SpareCapacity;

		if (overflow > 0)
		{
			Logger.LogErrorFormat("Increased amount {0} will overfill stack, filled to max",
				 Category.Inventory, increase);

			SyncAmount(amount, MaxAmount);
			return overflow;
		}

		SyncAmount(amount, amount + increase);
		return 0;
	}

	/// <summary>
	/// Removes one item from a stack and returns it
	/// </summary>
	/// <returns></returns>
	[Server]
	public GameObject ServerRemoveOne()
	{
		SyncAmount(amount, amount - 1);
		if (amount <= 0)
		{
			return gameObject;
		}

		var spawnInfo = Spawn.ServerPrefab(prefab, gameObject.transform.position, gameObject.transform);
		return spawnInfo.GameObject;
	}

	/// <summary>
	/// Adds the quantity in toAdd to this stackable (up to maxAmount) and despawns toAdd
	/// if it is entirely used up.
	/// Does nothing if they aren't the same thing
	/// </summary>
	/// <param name="toAdd"></param>
	[Server]
	public int ServerCombine(Stackable toAdd)
	{
		if (!StacksWith(toAdd))
		{
			Logger.LogErrorFormat("toAdd {0} doesn't stack with this {2}, cannot combine. Consider adding" +
			                      " this prefab to stacksWith if these really should be stackable.",
				Category.Inventory, toAdd, this);
			return 0;
		}
		var amountToConsume = Math.Min(toAdd.amount, SpareCapacity);
		if (amountToConsume <= 0) return 0;
		Logger.LogTraceFormat("Combining {0} <- {1}", Category.Inventory, GetInstanceID(), toAdd.GetInstanceID());
		toAdd.ServerConsume(amountToConsume);
		SyncAmount(amount, amount + amountToConsume);
		return amountToConsume;
	}

	/// <summary>
	/// Returns true iff other can be added to this stackable, as long as there is space for at least
	/// one item.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool CanAccommodate(GameObject other)
	{
		if (other == null) return false;
		return CanAccommodate(other.GetComponent<Stackable>());

	}
	/// <summary>
	/// Returns true iff toAdd can be added to this stackable, as long as there is space for at least
	/// one item.
	/// </summary>
	/// <param name="toAdd"></param>
	/// <returns></returns>
	public bool CanAccommodate(Stackable toAdd)
	{
		return (toAdd != null) && StacksWith(toAdd) && (amount < maxAmount);
	}

	/// <summary>
	/// returns tru iff toCheck is allowed to be combined with this stackable. Does not check
	/// the current stacked amount.
	/// </summary>
	/// <param name="toCheck"></param>
	/// <returns></returns>
	private bool StacksWith(Stackable toCheck)
	{
		if (toCheck == null) return false;

		return stacksWith.Intersect(toCheck.stacksWith).Any();
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
		if (CanAccommodate(interaction.UsedObject)) return true;

		return false;
	}


	public void ServerPerformInteraction(InventoryApply interaction)
	{
		//clicking on it with an empty hand when stack is in another hand to take one from it
		if (interaction.IsFromHandSlot && interaction.IsToHandSlot && interaction.FromSlot.IsEmpty)
		{
			//spawn a new one and put it into the from slot with a stack size of 1
			var single = Spawn.ServerPrefab(prefab).GameObject;
			if (single == null || single.GetComponent<Stackable>() == null) return;
			single.GetComponent<Stackable>().SyncAmount(amount, 1);
			Inventory.ServerAdd(single, interaction.FromSlot);
			ServerConsume(1);
		}
		else if (CanAccommodate(interaction.UsedObject))
		{
			//combining the stacks
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

		if (CanAccommodate(interaction.TargetObject)) return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		ServerCombine(interaction.TargetObject.GetComponent<Stackable>());
	}
}
