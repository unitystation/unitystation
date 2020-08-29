using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Main component for vending machine object (UI logic is in GUI_Vendor). Allows restocking
/// when clicking on vendor with a VendingRestock item in hand.
/// </summary>
[RequireComponent(typeof(HasNetworkTab))]
public class Vendor : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowered, IServerSpawn
{
	/// <summary>
	/// Scatter spawned items a bit to not allow stacking in one position
	/// </summary>
	private const float DispenseScatterRadius = 0.1f;

	[FormerlySerializedAs("VendorContent")]
	public List<VendorItem> InitialVendorContent = new List<VendorItem>();

	[Tooltip("Background color for UI")]
	public Color HullColor = Color.white;

	[Tooltip("Should vended items be thrown and possible injure user?")]
	public bool EjectObjects = false;

	[ConditionalField("EjectObjects")]
	[Tooltip("In which direction object should be thrown?")]
	public EjectDirection EjectDirection = EjectDirection.None;

	[Tooltip("Sound when object vends from vendor")]
	public string VendingSound = "MachineVend";

	[Header("Text messages")]
	[SerializeField]
	private string restockMessage = "Items restocked."; // TODO This is never displayed anywhere.
	[SerializeField]
	private string noAccessMessage = "Access denied!";

	public bool isEmagged;

	[HideInInspector]
	public List<VendorItem> VendorContent = new List<VendorItem>();

	private AccessRestrictions accessRestrictions;

	public VendorUpdateEvent OnRestockUsed = new VendorUpdateEvent();
	public VendorItemUpdateEvent OnItemVended = new VendorItemUpdateEvent();
	public PowerStates ActualCurrentPowerState = PowerStates.On;
	public bool DoesntRequirePower = false;
	private void Awake()
	{
		// ensure we have a net tab set up with the correct type
		// HasNetworkTab will open vendor UI by click if there is no object in active hand
		var hasNetTab = GetComponent<HasNetworkTab>();
		if (hasNetTab == null)
		{
			gameObject.AddComponent<HasNetworkTab>();
		}

		accessRestrictions = GetComponent<AccessRestrictions>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		// reset vendor content to initial value
		ResetContentList();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//Checking if avaliable for restock
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)) return true;
		if (!Validations.HasComponent<VendingRestock>(interaction.HandObject)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Checking restock
		var handObj = interaction.HandObject;
		if (handObj == null) return;
		var restock = handObj.GetComponentInChildren<VendingRestock>();
		if (restock != null)
		{
			OnRestockUsed?.Invoke();
			Inventory.ServerDespawn(interaction.HandSlot);
			Chat.AddActionMsgToChat(interaction.Performer, restockMessage, restockMessage);
		}
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag))
		{
			isEmagged = true;
		}
	}

	/// <summary>
	/// Reset vendor content to initial value
	/// </summary>
	public void ResetContentList()
	{
		if (!CustomNetworkManager.IsServer)
		{
			return;
		}

		VendorContent = new List<VendorItem>();
		for (int i = 0; i < InitialVendorContent.Count; i++)
		{
			//protects against missing references
			if (InitialVendorContent[i] != null && InitialVendorContent[i].Item != null)
			{
				VendorContent.Add(new VendorItem(InitialVendorContent[i]));
			}
		}
	}

	/// <summary>
	/// Default cooldown for vending
	/// </summary>
	protected Cooldown VendingCooldown
	{
		get
		{
			if (!CommonCooldowns.Instance)
			{
				return null;
			}

			return CommonCooldowns.Instance.Vending;
		}
	}

	private bool CanSell(VendorItem itemToSpawn, ConnectedPlayer player)
	{
		// check if selected item is valid
		var isSelectionValid = (itemToSpawn != null && itemToSpawn.Stock > 0);
		if (!isSelectionValid)
		{
			return false;
		}

		// check if this player has vending cooldown right now
		if (VendingCooldown)
		{
			if (player != null && player.Script)
			{
				var hasCooldown = !Cooldowns.TryStartServer(player.Script, VendingCooldown);
				if (hasCooldown)
				{
					return false;
				}
			}
		}

		// check player access
		if (player != null && accessRestrictions && !isEmagged)
		{
			var hasAccess = accessRestrictions.CheckAccess(player.GameObject);
			if (!hasAccess)
			{
				Chat.AddWarningMsgFromServer(player.GameObject, noAccessMessage);
				return false;
			}
		}

		return isSelectionValid;
	}

	/// <summary>
	/// Try spawn vending item and reduce items count in stock
	/// </summary>
	public void TryVendItem(VendorItem vendorItem, ConnectedPlayer player = null)
	{
		if (vendorItem == null)
		{
			return;
		}

		if (!CanSell(vendorItem, player))
		{
			return;
		}

		// Spawn item on top of the vending machine
		Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
		var spawnedItem = Spawn.ServerPrefab(vendorItem.Item, spawnPos, transform.parent,
			scatterRadius: DispenseScatterRadius).GameObject;

		//something went wrong trying to spawn the item
		if (spawnedItem == null)
		{
			return;
		}
		vendorItem.Stock--;

		// State sucsess message to chat
		var itemNameStr = TextUtils.UppercaseFirst(spawnedItem.ExpensiveName());
		Chat.AddLocalMsgToChat($"{itemNameStr} was dispensed from the vending machine", gameObject);

		// Play vending sound
		SoundManager.PlayNetworkedAtPos(VendingSound, gameObject.WorldPosServer(), Random.Range(.75f, 1.1f), sourceObj: gameObject);

		//Ejecting in direction
		if (EjectObjects && EjectDirection != EjectDirection.None &&
			spawnedItem.TryGetComponent<CustomNetTransform>(out var cnt))
		{
			Vector3 offset = Vector3.zero;
			switch (EjectDirection)
			{
				case EjectDirection.Up:
					offset = transform.rotation * Vector3.up / Random.Range(4, 12);
					break;
				case EjectDirection.Down:
					offset = transform.rotation * Vector3.down / Random.Range(4, 12);
					break;
				case EjectDirection.Random:
					offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0);
					break;
			}
			cnt.Throw(new ThrowInfo
			{
				ThrownBy = spawnedItem,
				Aim = BodyPartType.Chest,
				OriginWorldPos = spawnPos,
				WorldTrajectory = offset,
				SpinMode = (EjectDirection == EjectDirection.Random) ? SpinMode.Clockwise : SpinMode.None
			});
		}

		OnItemVended.Invoke(vendorItem);

	}

	public void PowerNetworkUpdate(float Voltage)
	{
	}

	public void StateUpdate(PowerStates State)
	{
		ActualCurrentPowerState = State;
	}
}

public enum EjectDirection { None, Up, Down, Random }

public class VendorUpdateEvent: UnityEvent {}

public class VendorItemUpdateEvent : UnityEvent<VendorItem> { }

//Adding this as a separate class so we can easily extend it in future -
//add price or required access, stock amount and etc.
[System.Serializable]
public class VendorItem
{
	public string ItemName;
	public GameObject Item;
	public int Stock = 5;

	public VendorItem(VendorItem item)
	{
		this.Item = item.Item;
		this.Stock = item.Stock;
		this.ItemName = Item.name;
	}
}