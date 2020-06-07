using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Main component for vending machine object (UI logic is in GUI_Vendor). Allows restocking
/// when clicking on vendor with a VendingRestock item in hand.
/// </summary>
[RequireComponent(typeof(HasNetworkTab))]
public class Vendor : MonoBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
{
	/// <summary>
	/// Scatter spawned items a bit to not allow stacking in one position
	/// </summary>
	private const float DispenseScatterRadius = 0.1f;

	[FormerlySerializedAs("VendorContent")]
	public List<VendorItem> InitialVendorContent = new List<VendorItem>();

	[Tooltip("Background UI color")]
	public Color HullColor = Color.white;

	[Tooltip("Should vended items be thrown and possible injure user?")]
	public bool EjectObjects = false;

	[ConditionalField("EjectObjects")]
	[Tooltip("In which direction object should be thrown?")]
	public EjectDirection EjectDirection = EjectDirection.None;

	[Tooltip("Sound when object vends from vendor")]
	public string VendingSound = "BinOpen";
	[Tooltip("Time between vendings attemps (to avoid spaming)")]
	public float VendingDelay;

	[SerializeField]
	private string restockMessage = "Items restocked.";

	[HideInInspector]
	public List<VendorItem> VendorContent = new List<VendorItem>();

	public VendorUpdateEvent OnRestockUsed = new VendorUpdateEvent();
	public VendorItemUpdateEvent OnItemVended = new VendorItemUpdateEvent();

	private void Awake()
	{
		// ensure we have a net tab set up with the correct type
		// HasNetworkTab will open vendor UI by click if there is no object in active hand
		var hasNetTab = GetComponent<HasNetworkTab>();
		if (hasNetTab == null)
		{
			gameObject.AddComponent<HasNetworkTab>();
		}
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

	private bool CanSell(VendorItem itemToSpawn)
	{
		return (allowSell && itemToSpawn != null && itemToSpawn.Stock > 0);
	}

	/// <summary>
	/// Try spawn vending item and reduce items count in stock
	/// </summary>
	public void TryVendItem(VendorItem vendorItem)
	{
		if (vendorItem == null)
		{
			return;
		}

		if (!CanSell(vendorItem))
		{
			//SendToChat(deniedMessage);
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

		// Play sound
		SoundManager.PlayNetworkedAtPos(VendingSound, gameObject.WorldPosServer(), Random.Range(.8f, 1.2f), sourceObj: gameObject);

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

		//allowSell = false;
		//StartCoroutine(VendorInputCoolDown());
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
	public GameObject Item;
	public int Stock = 5;

	public VendorItem(VendorItem item)
	{
		this.Item = item.Item;
		this.Stock = item.Stock;
	}
}