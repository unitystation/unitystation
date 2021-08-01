using System.Collections.Generic;
using Systems.Clearance;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Systems.Electricity;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Items;


namespace Objects
{
	/// <summary>
	/// Main component for vending machine object (UI logic is in GUI_Vendor). Allows restocking
	/// when clicking on vendor with a VendingRestock item in hand.
	/// </summary>
	[RequireComponent(typeof(HasNetworkTab))]
	public class Vendor : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable, IServerSpawn
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

		[SerializeField] private AddressableAudioSource VendingSound = null;

		[Header("Text messages")]
		[SerializeField]
		private string restockMessage = "Items restocked."; // TODO This is never displayed anywhere.
		[SerializeField]
		private string noAccessMessage = "Access denied!";

		private string tooExpensiveMessage = "This is too expensive!";

		public bool isEmagged;

		[HideInInspector]
		public List<VendorItem> VendorContent = new List<VendorItem>();

		private AccessRestrictions accessRestrictions;
		private ClearanceCheckable clearanceCheckable;

		public VendorUpdateEvent OnRestockUsed = new VendorUpdateEvent();
		public VendorItemUpdateEvent OnItemVended = new VendorItemUpdateEvent();
		public PowerState ActualCurrentPowerState = PowerState.On;
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
			clearanceCheckable = GetComponent<ClearanceCheckable>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			// reset vendor content to initial value
			ResetContentList();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			// Checking if avaliable for restock
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)) return true;
			if (!Validations.HasComponent<VendingRestock>(interaction.HandObject)) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// Checking restock
			var handObj = interaction.HandObject;
			if (handObj == null) return;
			var restock = handObj.GetComponentInChildren<VendingRestock>();
			if (restock != null)
			{
				OnRestockUsed?.Invoke();
				Inventory.ServerDespawn(interaction.HandSlot);
				Chat.AddActionMsgToChat(interaction.Performer, restockMessage, restockMessage);
			}
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
				&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
				&& emag.EmagHasCharges())
			{
				isEmagged = true;
				emag.UseCharge(interaction);
				Chat.AddActionMsgToChat(interaction,
					"The product lock shorts out. light fumes pour from the dispenser...",
							"You can smell caustic smoke from somewhere...");
			}
		}

		/// <summary>
		/// Reset vendor content to initial value
		/// </summary>
		public void ResetContentList()
		{
			if (CustomNetworkManager.IsServer == false) return;

			VendorContent = new List<VendorItem>();
			for (int i = 0; i < InitialVendorContent.Count; i++)
			{
				// protects against missing references
				if (InitialVendorContent[i] != null && InitialVendorContent[i].Item != null)
				{
					VendorContent.Add(new VendorItem(InitialVendorContent[i]));
				}
			}
		}

		/// <summary>
		/// Default cooldown for vending
		/// </summary>
		protected Cooldown VendingCooldown {
			get {
				if (CommonCooldowns.Instance == false)
				{
					return null;
				}

				return CommonCooldowns.Instance.Vending;
			}
		}

		private bool CanSell(VendorItem itemToSpawn, ConnectedPlayer player)
		{
			// check if selected item is valid
			var isSelectionValid = itemToSpawn != null && itemToSpawn.Stock > 0;
			if (isSelectionValid == false)
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

			/* --ACCESS REWORK--
			 *  TODO Remove the AccessRestriction check when we finish migrating!
			 *
			 */
			// check player access
			if (player != null && (accessRestrictions || clearanceCheckable) && !isEmagged)
			{
				var hasAccess = accessRestrictions
					? accessRestrictions.CheckAccess(player.GameObject)
					: clearanceCheckable.HasClearance(player.GameObject);

				if (hasAccess == false && player.Script.PlayerState != PlayerScript.PlayerStates.Ai)
				{
					Chat.AddWarningMsgFromServer(player.GameObject, noAccessMessage);
					return false;
				}
			}

			if (itemToSpawn.Price > 0)
			{
				var playerStorage = player.GameObject.GetComponent<DynamicItemStorage>();
				var itemSlotList = playerStorage.GetNamedItemSlots(NamedSlot.id);
				foreach (var itemSlot in itemSlotList)
				{
					if (itemSlot.ItemObject)
					{
						var idCard = AccessRestrictions.GetIDCard(itemSlot.ItemObject);
						if (idCard.currencies[(int) itemToSpawn.Currency] >= itemToSpawn.Price)
						{
							idCard.currencies[(int) itemToSpawn.Currency] -= itemToSpawn.Price;
							break;
						}
						else
						{
							Chat.AddWarningMsgFromServer(player.GameObject, tooExpensiveMessage);
							return false;
						}
					}
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

			if (CanSell(vendorItem, player) == false)
			{
				return;
			}

			// Spawn item on top of the vending machine
			Vector3 spawnPos = gameObject.RegisterTile().WorldPositionServer;
			var spawnedItem = Spawn.ServerPrefab(vendorItem.Item, spawnPos, transform.parent,
				scatterRadius: DispenseScatterRadius).GameObject;

			// something went wrong trying to spawn the item
			if (spawnedItem == null)
			{
				return;
			}
			vendorItem.Stock--;

			// State sucsess message to chat
			Chat.AddLocalMsgToChat($"The {spawnedItem.ExpensiveName()} was dispensed from the vending machine", gameObject);

			// Play vending sound
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(.75f, 1.1f));
			SoundManager.PlayNetworkedAtPos(VendingSound, gameObject.WorldPosServer(), audioSourceParameters, sourceObj: gameObject);

			// Ejecting in direction
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

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			ActualCurrentPowerState = state;
		}

		#endregion
	}

	public enum EjectDirection { None, Up, Down, Random }

	public class VendorUpdateEvent : UnityEvent { }

	public class VendorItemUpdateEvent : UnityEvent<VendorItem> { }

	// Adding this as a separate class so we can easily extend it in future -
	// add price or required access, stock amount and etc.
	[System.Serializable]
	public class VendorItem
	{
		public string ItemName;
		public GameObject Item;
		public int Stock = 5;
		public CurrencyType Currency = CurrencyType.Credits;
		public int Price = 0;

		public VendorItem(VendorItem item)
		{
			Item = item.Item;
			Stock = item.Stock;
			ItemName = item.ItemName;
			Currency = item.Currency;
			Price = item.Price;
		}
	}
}
