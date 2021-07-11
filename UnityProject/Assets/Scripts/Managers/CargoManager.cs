using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Items;
using Items.Cargo.Wrapping;
using Objects;
using Objects.Atmospherics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Systems.Cargo
{
	public class CargoManager : MonoBehaviour
	{
		public static CargoManager Instance;

		public int Credits;
		public ShuttleStatus ShuttleStatus = ShuttleStatus.DockedStation;
		public float CurrentFlyTime;
		public string CentcomMessage = "";  // Message that will appear in status tab. Resets on sending shuttle to centcom.
		public List<CargoCategory> Supplies = new List<CargoCategory>(); // Supplies - all the stuff cargo can order
		public List<CargoOrderSO> CurrentOrders = new List<CargoOrderSO>(); // Orders - payed orders that will spawn in shuttle on centcom arrival
		public List<CargoOrderSO> CurrentCart = new List<CargoOrderSO>(); // Cart - current orders, that haven't been payed for/ordered yet
		public List<CargoBounty> ActiveBounties = new List<CargoBounty>();

		public int cartSizeLimit = 20;

		public CargoUpdateEvent OnCartUpdate = new CargoUpdateEvent();
		public CargoUpdateEvent OnShuttleUpdate = new CargoUpdateEvent();
		public CargoUpdateEvent OnCreditsUpdate = new CargoUpdateEvent();
		public CargoUpdateEvent OnTimerUpdate = new CargoUpdateEvent();
		public CargoUpdateEvent OnBountiesUpdate = new CargoUpdateEvent();

		[SerializeField]
		private CargoData cargoData;

		[SerializeField]
		private float shuttleFlyDuration = 10f;

		private Dictionary<string, ExportedItem> exportedItems = new Dictionary<string, ExportedItem>();

		public Dictionary<ItemTrait, int> SoldHistory = new Dictionary<ItemTrait, int>();

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnRoundRestart;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnRoundRestart;
		}

		/// <summary>
		/// Checks for any Lifeforms (dead or alive) that might be aboard the shuttle
		/// </summary>
		/// <returns>true if a lifeform is found, false if none is found</returns>
		bool CheckLifeforms()
		{
			LayerMask layersToCheck = LayerMask.GetMask("Players", "NPC");
			Transform ObjectHolder = CargoShuttle.Instance.SearchForObjectsOnShuttle();
			foreach (Transform child in ObjectHolder)
			{
				if (((1 << child.gameObject.layer) & layersToCheck) == 0)
				{
					continue;
				}
				return true;
			}
			return false;
		}

		void OnRoundRestart(Scene oldScene, Scene newScene)
		{
			Supplies.Clear();
			ActiveBounties.Clear();
			CurrentOrders.Clear();
			CurrentCart.Clear();
			SoldHistory.Clear();
			ShuttleStatus = ShuttleStatus.DockedStation;
			Credits = 1000;
			CurrentFlyTime = 0f;
			CentcomMessage = "";
			Supplies = new List<CargoCategory>(cargoData.Categories);
			ActiveBounties = new List<CargoBounty>(cargoData.CargoBounties);
		}

		/// <summary>
		/// Calls the shuttle.
		/// Server only.
		/// </summary>
		public void CallShuttle()
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			if (CurrentFlyTime > 0f || ShuttleStatus == ShuttleStatus.OnRouteCentcom
									|| ShuttleStatus == ShuttleStatus.OnRouteStation)
			{
				return;
			}

			if (CustomNetworkManager.Instance._isServer)
			{
				CurrentFlyTime = shuttleFlyDuration;
				//It works so - shuttle stays in centcomDest until timer is done,
				//then it starts moving to station
				if (ShuttleStatus == ShuttleStatus.DockedCentcom)
				{
					SpawnOrder();
					ShuttleStatus = ShuttleStatus.OnRouteStation;
					CentcomMessage += "Shuttle is sent back with goods." + "\n";
					StartCoroutine(Timer(true));
				}

				//If we are at station - First checks for any people or animals aboard
				//If any, refuses to depart until the lifeform is removed.
				//If none, we start timer and launch shuttle at the same time.
				//Once shuttle arrives centcomDest - CargoShuttle will wait till timer is done
				//and will call OnShuttleArrival()
				else if (ShuttleStatus == ShuttleStatus.DockedStation)
				{
					string warningMessageEnd = "organisms aboard." + "\n";
					if (CheckLifeforms())
					{
						CurrentFlyTime = 0;
						if (CentcomMessage.EndsWith(warningMessageEnd) == false)
						{
							CentcomMessage += "Due to safety and security reasons, the automatic cargo shuttle is unable to depart with any human, alien or animal organisms aboard." + "\n";
						}
					}
					else
					{
						CargoShuttle.Instance.MoveToCentcom();
						ShuttleStatus = ShuttleStatus.OnRouteCentcom;
						CentcomMessage = "";
						exportedItems.Clear();
						StartCoroutine(Timer(false));
					}
				}
			}

			OnShuttleUpdate.Invoke();
		}

		private IEnumerator Timer(bool launchToStation)
		{
			while (CurrentFlyTime > 0f)
			{
				CurrentFlyTime -= 1f;
				OnTimerUpdate.Invoke();
				yield return WaitFor.Seconds(1);
			}

			CurrentFlyTime = 0f;
			if (launchToStation)
			{
				CargoShuttle.Instance.MoveToStation();
			}
		}

		/// <summary>
		/// Method is called once shuttle arrives to its destination.
		/// Server only.
		/// </summary>
		public void OnShuttleArrival()
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			if (ShuttleStatus == ShuttleStatus.OnRouteCentcom)
			{

				foreach (var entry in exportedItems)
				{
					if (entry.Value.TotalValue <= 0)
					{
						continue;
					}

					CentcomMessage += $"+{ entry.Value.TotalValue } credits: " +
									  $"{ entry.Value.Count }";

					if (!string.IsNullOrEmpty(entry.Value.ExportMessage) && string.IsNullOrEmpty(entry.Value.ExportName))
					{ // Special handling for items that don't want pluralisation after
						CentcomMessage += $" { entry.Value.ExportMessage }\n";
					}
					else
					{
						CentcomMessage += $" { entry.Key }";

						if (entry.Value.Count > 1)
						{
							CentcomMessage += "s";
						}

						CentcomMessage += $" { entry.Value.ExportMessage }\n";
					}
				}

				ShuttleStatus = ShuttleStatus.DockedCentcom;
			}
			else if (ShuttleStatus == ShuttleStatus.OnRouteStation)
			{
				ShuttleStatus = ShuttleStatus.DockedStation;
			}

			OnShuttleUpdate.Invoke();
		}

		public void ProcessCargo(PushPull item, HashSet<GameObject> alreadySold)
		{
			var wrappedItem = item.GetComponent<WrappedBase>();
			if (wrappedItem)
			{
				var wrappedContents = wrappedItem.GetOrGenerateContent().GetComponent<PushPull>();
				ProcessCargo(wrappedContents, alreadySold);
				DespawnItem(item, alreadySold);
				return;
			}

			//already sold this this sales cycle.
			if (alreadySold.Contains(item.gameObject)) return;
			var storage = item.GetComponent<InteractableStorage>();
			if (storage)
			{
				//Check to spawn initial contents, cant just use prefab data due to recursion
				if (storage.ItemStorage.ContentsSpawned == false)
				{
					storage.ItemStorage.TrySpawnContents();
				}

				foreach (var slot in storage.ItemStorage.GetItemSlots())
				{
					if (slot.Item)
					{
						ProcessCargo(slot.Item.GetComponent<PushPull>(), alreadySold);
					}
				}
			}

			// note: it seems the held items are also detected in UnloadCargo as being on the matrix but only
			// when they were spawned or moved onto that cargo shuttle (outside of the crate) prior to being placed into the crate. If they
			// were instead placed into the crate and then the crate was moved onto the cargo shuttle, they
			// will only be found with this check and won't be found in UnloadCargo.
			// TODO: Better logic for ClosetControl updating its held items' parent matrix when crossing matrix with items inside.
			var closet = item.GetComponent<ClosetControl>();
			if (closet)
			{
				//Check to spawn initial contents, cant just use prefab data due to recursion
				if (closet.ContentsSpawned == false && closet.InitialContents != null)
				{
					closet.TrySpawnContents(true);
				}

				foreach (var closetItem in closet.ServerHeldItems)
				{
					ProcessCargo(closetItem, alreadySold);
				}
			}

			// If there is no bounty for the item - we dont destroy it.
			var credits = Instance.GetSellPrice(item);
			Credits += credits;
			OnCreditsUpdate.Invoke();

			var attributes = item.gameObject.GetComponent<Attributes>();
			string exportName = String.Empty;
			if (attributes)
			{
				if (string.IsNullOrEmpty(attributes.ExportName))
				{
					exportName = attributes.ArticleName;
				}
				else
				{
					exportName = attributes.ExportName;
				}
			}
			else
			{
				exportName = item.gameObject.ExpensiveName();
			}
			ExportedItem export;
			if (exportedItems.ContainsKey(exportName))
			{
				export = exportedItems[exportName];
			}
			else
			{
				export = new ExportedItem
				{
					ExportMessage = attributes ? attributes.ExportMessage : "",
					ExportName = attributes ? attributes.ExportName : "" // Need to always use the given export name
				};
				exportedItems.Add(exportName, export);
			}

			var stackable = item.gameObject.GetComponent<Stackable>();
			var count = 1;
			if (stackable)
			{
				count = stackable.Amount;
			}

			export.Count += count;
			export.TotalValue += credits;

			var itemAttributes = item.GetComponent<ItemAttributesV2>();
			if (itemAttributes)
			{
				foreach (var itemTrait in itemAttributes.GetTraits())
				{
					if (itemTrait == null)
					{
						Logger.LogError($"{itemAttributes.name} has null or empty item trait, please fix");
						continue;
					}
					if(SoldHistory.ContainsKey(itemTrait) == false)
					{
						SoldHistory.Add(itemTrait, 0);
					}
					SoldHistory[itemTrait] += count;
					count = TryCompleteBounty(itemTrait, count);
					if (count == 0)
					{
						break;
					}
				}
			}

			//Add value of mole inside gas container
			var gasContainer = item.GetComponent<GasContainer>();
			if (gasContainer)
			{
				var stringBuilder = new StringBuilder();
				stringBuilder.Append(export.ExportMessage);

				foreach (var gas in gasContainer.GasMix.GasesArray)
				{
					stringBuilder.AppendLine($"Exported {gas.Moles} moles of {gas.GasSO.Name} for {(int)gas.Moles * gas.GasSO.ExportPrice} credits");
					export.TotalValue += (int)gas.Moles * gas.GasSO.ExportPrice;
				}

				export.ExportMessage = stringBuilder.ToString();
			}

			var playerScript = item.GetComponent<PlayerScript>();
			if (playerScript != null)
			{
				playerScript.playerHealth.ServerGibPlayer();
			}

			DespawnItem(item, alreadySold);
		}

		private void DespawnItem(PushPull item, HashSet<GameObject> alreadySold)
		{
			alreadySold.Add(item.gameObject);
			item.registerTile.UnregisterClient();
			item.registerTile.UnregisterServer();
			_ = Despawn.ServerSingle(item.gameObject);
		}

		private int TryCompleteBounty(ItemTrait itemTrait, int count)
		{
			for (var i = ActiveBounties.Count - 1; i >= 0; i--)
			{
				var activeBounty = ActiveBounties[i];
				if (activeBounty.Demands.ContainsKey(itemTrait))
				{
					if (activeBounty.Demands[itemTrait] >= count)
					{
						activeBounty.Demands[itemTrait] -= count;
						count = 0;
						CheckBountyCompletion(activeBounty);
						break;
					}
					count -= activeBounty.Demands[itemTrait];
					activeBounty.Demands[itemTrait] = 0;
					CheckBountyCompletion(activeBounty);
				}
			}

			return count;
		}

		private void CheckBountyCompletion(CargoBounty cargoBounty)
		{
			foreach (var demand in cargoBounty.Demands.m_dict)
			{
				if (demand.Value > 0)
				{
					return;
				}
			}
			ActiveBounties.Remove(cargoBounty);
			Credits += cargoBounty.Reward;
			OnBountiesUpdate.Invoke();
		}

		private void SpawnOrder()
		{
			CargoShuttle.Instance.PrepareSpawnOrders();
			for (int i = 0; i < CurrentOrders.Count; i++)
			{
				if (CargoShuttle.Instance.SpawnOrder(CurrentOrders[i]))
				{
					CurrentOrders.RemoveAt(i);
					i--;
				}
			}
		}

		public void AddToCart(CargoOrderSO orderToAdd)
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			if (CurrentCart.Count > cartSizeLimit)
			{
				return;
			}

			CurrentCart.Add(orderToAdd);
			OnCartUpdate.Invoke();
		}

		public void RemoveFromCart(CargoOrderSO orderToRemove)
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			CurrentCart.Remove(orderToRemove);
			OnCartUpdate.Invoke();
		}

		public int TotalCartPrice()
		{
			int totalPrice = 0;
			for (int i = 0; i < CurrentCart.Count; i++)
			{
				totalPrice += CurrentCart[i].CreditCost;
			}
			return (totalPrice);
		}

		public void ConfirmCart()
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			int totalPrice = TotalCartPrice();
			if (totalPrice <= Credits)
			{
				CurrentOrders.AddRange(CurrentCart);
				CurrentCart.Clear();
				Credits -= totalPrice;
			}
			OnCreditsUpdate.Invoke();
			OnCartUpdate.Invoke();
		}

		public int GetSellPrice(PushPull item)
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return 0;
			}

			var attributes = item.GetComponent<Attributes>();
			if (attributes == null)
			{
				return 0;
			}

			return attributes.ExportCost;
		}

		private class ExportedItem
		{
			public string ExportMessage;
			public string ExportName;
			public int Count;
			public int TotalValue;
		}
	}

	public class CargoUpdateEvent : UnityEvent { }
}
