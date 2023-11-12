using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine;
using Messages.Server;
using UI;
using UnityEngine.Serialization;
using Util;

/// <summary>
/// Allows an item to be stacked, occupying a single inventory slot.
/// </summary>
public class Stackable : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<InventoryApply>,
	ICheckedInteractable<HandApply>, IExaminable
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

	[FormerlySerializedAs("IsRepresentationOfStack")] [SerializeField][Tooltip("Basically is this a representation of a stack vs an actual stack used in cyborg inventory ")]
	private bool isRepresentationOfStack = false;

	public bool IsRepresentationOfStack => isRepresentationOfStack;

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
	private UniversalObjectPhysics objectPhysics;
	private RegisterTile registerTile;
	private GameObject prefab;
	private SpriteHandler spriteHandler;

	[SerializeField] private List<StackNames> stackNames = new List<StackNames>();
	[SerializeField] private List<StackSprites> stackSprites = new List<StackSprites>();
	[SerializeField] private bool autoStackOnSpawn = true;
	[SerializeField] private bool autoStackOnDrop = true;


	void OnDestroy()
	{
		if (CustomNetworkManager.IsServer)
		{
			registerTile.OnLocalPositionChangedServer.RemoveListener(OnLocalPositionChangedServer);
		}
	}

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
		objectPhysics = GetComponent<UniversalObjectPhysics>();
		registerTile = GetComponent<RegisterTile>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	private void OnLocalPositionChangedServer(Vector3Int newLocalPos)
	{
		//if we are being pulled, combine the stacks with any on the ground under us.
		if (objectPhysics.PulledBy.HasComponent)
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
		Loggy.LogTraceFormat("Spawning {0}", Category.ItemSpawn, GetInstanceID());
		InitStacksWith();
		SyncAmount(amount, initialAmount);
		amountInit = true;
		if(autoStackOnSpawn) ServerStackOnGround(registerTile.LocalPositionServer);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		Loggy.LogTraceFormat("Despawning {0}", Category.ItemSpawn, GetInstanceID());
		amountInit = false;
	}

	public void ServerStackOnGround(Vector3Int localPosition)
	{
		if (autoStackOnDrop == false || registerTile?.Matrix == null) return;
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

	[Server]
	public void ServerSetAmount(int newAmount)
	{
		SyncAmount(amount, newAmount);
	}

	private void SyncAmount(int oldAmount, int newAmount)
	{
		EnsureInit();
		Loggy.LogTraceFormat("Amount {0}->{1} for {2}", Category.Objects, amount, newAmount, GetInstanceID());
		this.amount = newAmount;
		pickupable.RefreshUISlotImage();
		if (CustomNetworkManager.Instance._isServer)
		{
			UpdateStackName(gameObject.Item());
			UpdateStackSprites();
		}
	}

	private void UpdateStackSprites()
	{
		if (stackSprites.Count == 0 || spriteHandler == null) return;
		if (amount > 1)
		{
			bool found = false;


			foreach (var sprite in stackSprites)
			{
				if (sprite.OverAmount <= amount) continue;
				found = true;
				if (spriteHandler.GetCurrentSpriteSO() != sprite.SpriteSO)
				{
					spriteHandler.SetSpriteSO(sprite.SpriteSO);
				}
				break;
			}

			if (found == false)
			{
				spriteHandler.SetSpriteSO(stackSprites.Last().SpriteSO);
			}

		}
		else if(amount == 1)
		{
			spriteHandler.SetSpriteSO(stackSprites[0].SpriteSO);
		}
	}

	public void UpdateStackName(Attributes attributes)
	{
		if (amount > 1 && stackNames.Count > 0)
		{
			var correctName = stackNames[0];
			foreach (var name in stackNames)
			{
				if (amount < name.OverAmount) continue;
				correctName = name;
			}
			attributes.ServerSetArticleName(correctName.Name);
		}
		else if(amount == 1 && stackNames.Any(item => item.Name == gameObject.ExpensiveName()))
		{
			attributes.ServerSetArticleName(attributes.InitialName);
		}
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
			Loggy.LogErrorFormat($"Consumed amount {consumed} is greater than amount in this stack {amount}, will not consume.", Category.Objects);
			return false;
		}
		SyncAmount(amount, amount - consumed);
		if (amount <= 0 && isRepresentationOfStack == false)
		{
			_ = Despawn.ServerSingle(gameObject);
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
			Loggy.LogErrorFormat("Attempted to increase stacks by a negative value, ignored", Category.Objects);
			return 0;
		}

		int overflow = increase - SpareCapacity;

		if (overflow > 0)
		{
			Loggy.LogErrorFormat($"Increased amount {increase} will overfill stack, filled to max",
					Category.Objects);

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
		if ((amount-1) <= 0)
		{
			return gameObject;
		}
		SyncAmount(amount, amount - 1);

		var spawnInfo = Spawn.ServerPrefab(prefab, gameObject.transform.position, gameObject.transform);
		spawnInfo.GameObject.GetComponent<Stackable>().ServerSetAmount(1);
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
			Loggy.LogErrorFormat($"{toAdd} doesn't stack with {this}, cannot combine. Consider adding" +
									" this prefab to stacksWith if these really should be stackable.",
				Category.Objects);
			return 0;
		}
		var amountToConsume = Math.Min(toAdd.amount, SpareCapacity);
		if (amountToConsume <= 0) return 0;
		Loggy.LogTraceFormat("Combining {0} <- {1}", Category.Objects, GetInstanceID(), toAdd.GetInstanceID());
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
	public bool StacksWith(Stackable toCheck)
	{
		if (toCheck == null) return false;

		var Tracker = toCheck.GetComponent<PrefabTracker>();

		if (Tracker != null)
		{
			foreach (var InObject in stacksWith)
			{
				var OtherTracker = InObject.GetComponent<PrefabTracker>();
				if (OtherTracker.ForeverID == Tracker.ForeverID)
				{
					return true;
				}
			}
		}

		return stacksWith.Intersect(toCheck.stacksWith).Any();
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only has logic if this is the target object
		if (interaction.TargetObject != gameObject) return false;

		//Alt clicking with empty hand calls splitting menu UI
		if (side == NetworkSide.Client && interaction.IsFromHandSlot && interaction.IsToHandSlot && interaction.FromSlot.IsEmpty && interaction.IsAltClick)
		{
			UIManager.Instance.SplittingMenu.Enable();
			return true;
		}

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
		if (interaction.IsFromHandSlot && interaction.IsToHandSlot && interaction.FromSlot.IsEmpty && !interaction.IsAltClick)
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

	public string Examine(Vector3 worldPos)
	{
		return $"This {gameObject.ExpensiveName()} contains {Amount} stacks.";
	}

	[Serializable]
	private struct StackNames
	{
		[SerializeField] public string Name;
		[SerializeField] public int OverAmount;
	}

	[Serializable]
	private struct StackSprites
	{
		[SerializeField] public SpriteDataSO SpriteSO;
		[SerializeField] public int OverAmount;
	}
}
