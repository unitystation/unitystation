using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HealthV2;
using Items;
using Items.Cargo.Wrapping;
using Managers;
using Objects;
using Objects.Atmospherics;
using Strings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using Items.Science;
using Items.Storage.VirtualStorage;
using Logs;

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
		public CargoUpdateEvent OnConnectionChangeToCentComm = new CargoUpdateEvent();

		[SerializeField]
		private CargoData cargoData;

		[SerializeField]
		private float shuttleFlyDuration = 10f;

		private Dictionary<string, ExportedItem> exportedItems = new Dictionary<string, ExportedItem>();

		public Dictionary<ItemTrait, int> SoldHistory = new Dictionary<ItemTrait, int>();

		public bool CargoOffline = false;
		public bool RandomBountiesActive = true;
		private int lastTimeRecorded = 0;
		private int randomBountyTimeCheck = 0;

		[SerializeField, BoxGroup("Random Bounties")] private float checkForTimeCooldown = 50f;
		[SerializeField, BoxGroup("Random Bounties")] private Vector2 randomTimeRangeForRandomBounty = new Vector2(320, 690);
		[SerializeField, BoxGroup("Random Bounties")] private List<CargoBounty> randomBountiesList = new List<CargoBounty>();

		private static readonly List<int> randomJunkPrices = new List<int> { 5, 10, 15 };

		public static List<string> ResearchedArtifacts { get; private set; }

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


			randomBountyTimeCheck = UnityEngine.Random.Range((int)randomTimeRangeForRandomBounty.x, (int)randomTimeRangeForRandomBounty.y);
		}


		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, checkForTimeCooldown);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
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

		public void Start()
		{
			OnRoundRestart();
		}

		public void OnRoundRestart()
		{
			Supplies.Clear();
			ActiveBounties.Clear();
			CurrentOrders.Clear();
			CurrentCart.Clear();
			SoldHistory.Clear();
			ClearStatics();
			ShuttleStatus = ShuttleStatus.DockedStation;
			Credits = 1000;
			CurrentFlyTime = 0f;
			CentcomMessage = "";
			Supplies = new List<CargoCategory>(cargoData.Categories);
			ActiveBounties = new List<CargoBounty>(cargoData.CargoBounties);
			lastTimeRecorded = 0;
			randomBountyTimeCheck = UnityEngine.Random.Range((int)randomTimeRangeForRandomBounty.x, (int)randomTimeRangeForRandomBounty.y);
		}

		void UpdateMe()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(RandomBountiesActive == false || CargoOffline) return;
			lastTimeRecorded += (int) checkForTimeCooldown;
			if(lastTimeRecorded >= randomBountyTimeCheck)
			{
				RollRandomBounties();
				lastTimeRecorded = 0;
				randomBountyTimeCheck = UnityEngine.Random.Range((int)randomTimeRangeForRandomBounty.x, (int)randomTimeRangeForRandomBounty.y);
			}
		}

		private void RollRandomBounties()
		{
			var randomBountiesNumber = UnityEngine.Random.Range(1, 3);
			for (int i = 0; i < randomBountiesNumber; i++)
			{
				var randomBounty = randomBountiesList[UnityEngine.Random.Range(0, randomBountiesList.Count)];
				AddBounty(randomBounty, i == randomBountiesNumber ? true : false);
			}
		}

		/// <summary>
		/// Calls the shuttle.
		/// Server only.
		/// </summary>
		public void CallShuttle()
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (CurrentFlyTime > 0f || ShuttleStatus == ShuttleStatus.OnRouteCentcom
									|| ShuttleStatus == ShuttleStatus.OnRouteStation)
			{
				return;
			}

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
					CentcomMessage = string.Empty;
					exportedItems.Clear();
					StartCoroutine(Timer(false));
				}
			}

			OnShuttleUpdate?.Invoke();
		}

		private IEnumerator Timer(bool launchToStation)
		{
			while (CurrentFlyTime > 0f)
			{
				CurrentFlyTime -= 1f;
				OnTimerUpdate?.Invoke();
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
			if (CustomNetworkManager.IsServer == false) return;

			if (ShuttleStatus == ShuttleStatus.OnRouteCentcom)
			{
				foreach (var entry in exportedItems)
				{
					if (entry.Value.TotalValue <= 0) continue;

					CentcomMessage += $"+{entry.Value.TotalValue} credits: {entry.Value.Count}";

					if (!string.IsNullOrEmpty(entry.Value.ExportMessage) && string.IsNullOrEmpty(entry.Value.ExportName))
					{
						// Special handling for items that don't want pluralisation after
						CentcomMessage += $" {entry.Value.ExportMessage}\n";
					}
					else
					{
						CentcomMessage += $" {entry.Key}";

						if (entry.Value.Count > 1)
						{
							CentcomMessage += "s";
						}

						CentcomMessage += $" {entry.Value.ExportMessage}\n";
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

		public void ProcessCargo(GameObject obj, HashSet<GameObject> alreadySold)
		{
			if (obj.TryGetComponent<Attributes>(out var attributes))
			{
				if (attributes.CanBeSoldInCargo == false)
				{
					Inventory.ServerDrop(attributes.gameObject);
					return;
				}
			}

			if (obj.TryGetComponent<PlayerScript>(out var playerScript))
			{
				// No one must survive to tell the secrets of Central Command's cargo handling techniques.
				Chat.AddExamineMsg(obj, "<color=red> You feel a strong force of energy run through your body before everything goes to black in the blink of the eye. </color>");
				playerScript.playerHealth.OnGib();
				return;
			}

			if (obj.TryGetComponent<WrappedBase>(out var wrappedObject))
			{
				var wrappedContents = wrappedObject.GetOrGenerateContent();
				ProcessCargo(wrappedContents, alreadySold);
				DespawnItem(obj, alreadySold);
				return;
			}

			// already sold this this sales cycle.
			if (alreadySold.Contains(obj)) return;

			var storages = obj.GetComponents<ItemStorage>();
			{
				foreach (var storage in storages)
				{
					// Check to spawn initial contents, can't just use prefab data due to recursion
					if (storage.ContentsSpawned == false)
					{
						storage.TrySpawnContents();
					}

					foreach (var slot in storage.GetItemSlots())
					{
						if (slot.Item)
						{
							ProcessCargo(slot.Item.gameObject, alreadySold);
						}
					}
				}
			}


			if (obj.TryGetComponent<ObjectContainer>(out var container))
			{
				container.TrySpawnInitialContents(true);

				//Check to spawn initial contents, cant just use prefab data due to recursion
				foreach (var recursiveObj in container.GetStoredObjects())
				{
					ProcessCargo(recursiveObj, alreadySold);
				}
			}

			string exportName;
			if (attributes != null)
			{
				attributes.OnExport();
				exportName = string.IsNullOrEmpty(attributes.ExportName) ? attributes.ArticleName : attributes.ExportName;
			}
			else
			{
				exportName = obj.gameObject.ExpensiveName();
			}

			// If there is no bounty for the item - we dont destroy it.
			var credits = Instance.GetSellPrice(obj);
			if (credits == 0) credits = randomJunkPrices.PickRandom();

			if (exportedItems.TryGetValue(exportName, out ExportedItem export) == false)
			{
				export = new ExportedItem
				{
					ExportMessage = attributes ? attributes.ExportMessage : string.Empty,
					ExportName = attributes ? attributes.ExportName : string.Empty // Need to always use the given export name
				};
				exportedItems.Add(exportName, export);
			}

			Credits += credits;
			OnCreditsUpdate.Invoke();

			var count = obj.TryGetComponent<Stackable>(out var stackable) ? stackable.Amount : 1;

			export.Count += count;
			export.TotalValue += credits;

			if (obj.TryGetComponent<ItemAttributesV2>(out var itemAttributes))
			{

				//charge cargo for getting rid of trash through centeral commmunications.
				if (itemAttributes.HasTrait(CommonTraits.Instance.Trash))
				{
					var chargedPrice = randomJunkPrices.PickRandom();
					Credits -= chargedPrice;
					export.ExportMessage += "\n" + $"{chargedPrice} Charged for junk removal for item : {itemAttributes.ArticleName}.";
				}
				foreach (var itemTrait in itemAttributes.GetTraits())
				{
					if (itemTrait == null)
					{
						Loggy.LogError($"{itemAttributes.name} has null or empty item trait, please fix");
						continue;
					}

					if (SoldHistory.ContainsKey(itemTrait) == false)
					{
						SoldHistory.Add(itemTrait, 0);
					}
					SoldHistory[itemTrait] += count;
					count = TryCompleteBounty(itemTrait, count);

					if (count == 0) break;
				}
			}

			// Add value of mole inside gas container
			if (obj.TryGetComponent<GasContainer>(out var gasContainer) && gasContainer.CargoSealApproved)
			{
				var stringBuilder = new StringBuilder(export.ExportMessage);

				lock (gasContainer.GasMix.GasesArray) //no Double lock
				{
					foreach (var gas in gasContainer.GasMix.GasesArray)  //doesn't appear to modify list while iterating
					{
						int gasValue = (int)gas.Moles * gas.GasSO.ExportPrice;
						stringBuilder.AppendLine($"Exported {gas.Moles} moles of {gas.GasSO.Name} for {gasValue} credits");
						export.TotalValue += gasValue;
						Credits += gasValue;
					}
				}

				export.ExportMessage += "\n" + stringBuilder.ToString();
				OnCreditsUpdate.Invoke();
			}

			DespawnItem(obj, alreadySold);
		}

		private void DespawnItem(GameObject obj, HashSet<GameObject> alreadySold)
		{
			alreadySold.Add(obj);
			var registerTile = obj.RegisterTile();
			registerTile.UnregisterClient();
			registerTile.UnregisterServer();
			_ = Despawn.ServerSingle(obj);
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
			CompleteBounty(cargoBounty);
		}

		public void CompleteBounty(CargoBounty cargoBounty)
		{
			ActiveBounties.Remove(cargoBounty);
			Credits += cargoBounty.Reward;
			CentcomMessage += $"+{cargoBounty.Reward.ToString()} credits: {cargoBounty.TooltipDescription} - completed.\n";
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

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void ClearStatics()
		{
			ResearchedArtifacts = new List<string>();
		}

		public static void AddArtifactToList(string ID)
		{
			ResearchedArtifacts.Add(ID);
		}

		public int GetSellPrice(GameObject obj)
		{
			if (CustomNetworkManager.IsServer == false) return 0;

			if (obj.TryGetComponent<Attributes>(out var attributes))
			{
				return attributes.ExportCost;
			}

			return 0;
		}

		/// <summary>
		/// Adds a new bounty to the bounty list. Returns false if it fails.
		/// </summary>
		public void AddBounty(ItemTrait trait, int amount, string title, string description, int reward, bool announce)
		{
			if(amount < 1 || reward < 1) return;
			CargoBounty newBounty = new CargoBounty();
			newBounty.Demands.Add(trait, amount);
			newBounty.TooltipDescription = description;
			newBounty.Title = title;
			newBounty.Reward = reward;
			ActiveBounties.Add(newBounty);
			if(announce) AnnounceNewBounty();
		}

		public void AddBounty(CargoBounty bounty, bool announce)
		{
			if(bounty == null) return;
			ActiveBounties.Add(bounty);
			if(announce) AnnounceNewBounty();
		}

		private void AnnounceNewBounty()
		{
			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, "A bounty for cargo has been issued from central communications", CentComm.UpdateSound.Notice);
		}

		private class ExportedItem
		{
			public string ExportMessage;
			public string ExportName;
			public int Count;
			public int TotalValue;
		}

		public struct BountySyncData
		{
			public string Title;
			public string Desc;
			public int Reward;
			public int Index;
		}
	}

	public class CargoUpdateEvent : UnityEvent { }
}
