using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class CargoManager : MonoBehaviour
{
	public static CargoManager Instance;

	public int Credits;
	public ShuttleStatus ShuttleStatus = ShuttleStatus.DockedStation;
	public float CurrentFlyTime = 0f;
	public string CentcomMessage = "";	// Message that will appear in status tab. Resets on sending shuttle to centcom.
	public List<CargoOrderCategory> Supplies = new List<CargoOrderCategory>(); // Supplies - all the stuff cargo can order
	public CargoOrderCategory CurrentCategory ;
	public List<CargoOrder> CurrentOrders = new List<CargoOrder>(); // Orders - payed orders that will spawn in shuttle on centcom arrival
	public List<CargoOrder> CurrentCart = new List<CargoOrder>(); // Cart - current orders, that haven't been payed for/ordered yet

	public int cartSizeLimit = 20;

	public CargoUpdateEvent OnCartUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnShuttleUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnCreditsUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnCategoryUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnTimerUpdate = new CargoUpdateEvent();

	[SerializeField]
	private CargoData cargoData = null;

	public CargoData CargoData => cargoData;

	[SerializeField]
	private float shuttleFlyDuration = 10f;

	private Dictionary<string, ExportedItem> exportedItems = new Dictionary<string, ExportedItem>();

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
			if(((1<<child.gameObject.layer) & layersToCheck) == 0)
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
		CurrentOrders.Clear();
		CurrentCart.Clear();
		ShuttleStatus = ShuttleStatus.DockedStation;
		Credits = 1000;
		CurrentFlyTime = 0f;
		CentcomMessage = "";
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

		OnShuttleUpdate?.Invoke();
	}

	public void LoadData()
	{
		//need a shallow copy so the actual SO list isn't cleared on round restart!
		Supplies = new List<CargoOrderCategory>(cargoData.Supplies);
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
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		if (ShuttleStatus == ShuttleStatus.OnRouteCentcom)
		{

			foreach(var entry in exportedItems)
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

		OnShuttleUpdate?.Invoke();
	}

	public void DestroyItem(ObjectBehaviour item, HashSet<GameObject> alreadySold)
	{
		//already sold this this sales cycle.
		if (alreadySold.Contains(item.gameObject)) return;
		var storage = item.GetComponent<InteractableStorage>();
		if (storage)
		{
			foreach (var slot in storage.ItemStorage.GetItemSlots())
			{
				if (slot.Item)
				{
					DestroyItem(slot.Item.GetComponent<ObjectBehaviour>(), alreadySold);
				}
			}
		}

		//note: it seems the held items are also detected in UnloadCargo as being on the matrix but only
		//when they were spawned or moved onto that cargo shuttle (outside of the crate) prior to being placed into the crate. If they
		//were instead placed into the crate and then the crate was moved onto the cargo shuttle, they
		//will only be found with this check and won't be found in UnloadCargo.
		//TODO: Better logic for ClosetControl updating its held items' parent matrix when crossing matrix with items inside.
		var closet = item.GetComponent<ClosetControl>();
		if (closet)
		{
			foreach (var closetItem in closet.ServerHeldItems)
			{
				DestroyItem(closetItem, alreadySold);
			}
		}

		// If there is no bounty for the item - we dont destroy it.
		var credits = Instance.GetSellPrice(item);
		Credits += credits;
		OnCreditsUpdate?.Invoke();

		var attributes = item.gameObject.GetComponent<Attributes>();
		string exportName = System.String.Empty;
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

		var playerScript = item.GetComponent<PlayerScript>();
		if (playerScript != null)
		{
			playerScript.playerHealth.ServerGibPlayer();
		}

		item.registerTile.UnregisterClient();
		item.registerTile.UnregisterServer();
		alreadySold.Add(item.gameObject);
		Despawn.ServerSingle(item.gameObject);
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

	public void AddToCart(CargoOrder orderToAdd)
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
		OnCartUpdate?.Invoke();
	}

	public void RemoveFromCart(CargoOrder orderToRemove)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		CurrentCart.Remove(orderToRemove);
		OnCartUpdate?.Invoke();
	}

	public void OpenCategory(CargoOrderCategory categoryToOpen)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		CurrentCategory = categoryToOpen;
		OnCategoryUpdate?.Invoke();
	}

	public int TotalCartPrice()
	{
		int totalPrice = 0;
		for (int i = 0; i < CurrentCart.Count; i++)
		{
			totalPrice += CurrentCart[i].CreditsCost;
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
		OnCreditsUpdate?.Invoke();
		OnCartUpdate?.Invoke();
		return;
	}

	public int GetSellPrice(ObjectBehaviour item)
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

[System.Serializable]
public class CargoOrder
{
	public string OrderName = "Crate with beer and steak";
	public int CreditsCost = 1000;
	public GameObject Crate = null;
	public List<GameObject> Items = new List<GameObject>();

	[ReadOnly]
	public int TotalCreditExport = 0;
}

[System.Serializable]
public class CargoOrderCategory
{
	public string CategoryName = "";
	public List<CargoOrder> Supplies = new List<CargoOrder>();
}

public class CargoUpdateEvent : UnityEvent {}
