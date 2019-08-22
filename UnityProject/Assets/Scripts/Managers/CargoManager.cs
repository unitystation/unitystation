using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CargoManager : MonoBehaviour
{
	private static CargoManager cargoManager;
	public static CargoManager Instance
	{
		get
		{
			if (cargoManager == null)
			{
				cargoManager = FindObjectOfType<CargoManager>();
			}
			return cargoManager;
		}
	}

	[SerializeField]
	private CargoData cargoData = null;
	public int Credits = 0;
	public ShuttleStatus ShuttleStatus = ShuttleStatus.DockedStation;
	[SerializeField]
	private float shuttleFlyDuration = 10f;
	public float CurrentFlyTime = 0f;
	//Message that will appear in status tab. Resets on sending shuttle to centcom.
	public string CentcomMessage = "";

	//Supplies - all the stuff cargo can order
	public List<CargoOrderCategory> Supplies = new List<CargoOrderCategory>();
	public CargoOrderCategory CurrentCategory = null;
	//Orders - payed orders that will spawn in shuttle on centcom arrival
	public List<CargoOrder> CurrentOrders = new List<CargoOrder>();
	//Cart - current orders, that haven't been payed for/ordered yet
	public List<CargoOrder> CurrentCart = new List<CargoOrder>();

	public CargoUpdateEvent OnCartUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnShuttleUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnCreditsUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnCategoryUpdate = new CargoUpdateEvent();
	public CargoUpdateEvent OnTimerUpdate = new CargoUpdateEvent();

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

		if (CurrentFlyTime > 0f)
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
			//If we are at station - we start timer and launch shuttle at the same time.
			//Once shuttle arrives centcomDest - CargoShuttle will wait till timer is done
			//and will call OnShuttleArrival()
			else if (ShuttleStatus == ShuttleStatus.DockedStation)
			{
				CargoShuttle.Instance.MoveToCentcom();
				ShuttleStatus = ShuttleStatus.OnRouteCentcom;
				CentcomMessage = "";
				StartCoroutine(Timer(false));
			}
		}
		OnShuttleUpdate?.Invoke();
	}

	public void LoadData()
	{
		Supplies = cargoData.Supplies;
	}

	private IEnumerator Timer(bool launchToStation)
	{
		while (CurrentFlyTime > 0f)
		{
			CurrentFlyTime -= 1f;
			OnTimerUpdate?.Invoke();
			yield return WaitFor.Seconds(1);
		}

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
			ShuttleStatus = ShuttleStatus.DockedCentcom;
		}
		else if (ShuttleStatus == ShuttleStatus.OnRouteStation)
		{
			ShuttleStatus = ShuttleStatus.DockedStation;
		}
		OnShuttleUpdate?.Invoke();
	}

	public void DestroyItem(ObjectBehaviour item)
	{
		//If there is no bounty for the item - we dont destroy it.
		float credits = CargoManager.Instance.AddCredits(item);
		if (credits <= 0f)
		{
			return;
		}

		if (CentcomMessage == "")
			CentcomMessage = "Bounty items received.\n";
		//1 - quantity of items
		CentcomMessage += $"+{credits} credits: received 1 {item.gameObject.ExpensiveName()}.\n";
		item.registerTile.UnregisterClient();
		item.registerTile.UnregisterServer();
		PoolManager.PoolNetworkDestroy(item.gameObject);
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

	/// <summary>
	/// Adds the credits.
	/// </summary>
	/// <returns><c>true</c>, if credits were added, <c>false</c> otherwise.</returns>
	/// <param name="item">Item.</param>
	public float AddCredits(ObjectBehaviour item)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return 0;
		}

		int credits = cargoData.GetBounty(item);
		Credits += credits;
		OnCreditsUpdate?.Invoke();

		return credits;
	}
}

[System.Serializable]
public class CargoOrder
{
	public string OrderName = "Crate with beer and steak";
	public int CreditsCost = 1000;
	public GameObject Crate = null;
	public List<GameObject> Items = new List<GameObject>();
}

[System.Serializable]
public class CargoOrderCategory
{
	public string CategoryName = "";
	public List<CargoOrder> Supplies = new List<CargoOrder>();
}

public class CargoUpdateEvent : UnityEvent {}
