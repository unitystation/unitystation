using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoManager : MonoBehaviour
{
	public static CargoManager Instance;

	public int Credits = 0;
	public CargoShuttleStatus ShuttleStatus = CargoShuttleStatus.DockedStation;
	private bool shuttleIsMoving;

	//Supplies - all the stuff cargo can order
	public List<CargoOrder> Supplies = new List<CargoOrder>();
	//Orders - payed orders
	public List<CargoOrder> CurrentOrders = new List<CargoOrder>();
	//Request - order requests made by non cargonians
	public List<CargoOrder> CurrentRequests = new List<CargoOrder>();
	//Cart - current orders, that haven't been payed for/ordered yet
	public List<CargoOrder> CurrentCart = new List<CargoOrder>();
	public System.Action OnCartUpdate;
	public System.Action OnShuttleUpdate;
	public System.Action OnCreditsUpdate;

	private void Awake()
	{
		Instance = this;
	}

	/// <summary>
	/// Calls the shuttle.
	/// Server only.
	/// </summary>
	public void CallShuttle()
	{
		if (shuttleIsMoving)
		{
			return;
		}
		if (CustomNetworkManager.Instance._isServer)
		{
			shuttleIsMoving = true;
			if (ShuttleStatus == CargoShuttleStatus.DockedCentcom)
			{
				CargoShuttle.Instance.MoveToStation();
				ShuttleStatus = CargoShuttleStatus.OnRouteStation;
			}
			else if (ShuttleStatus == CargoShuttleStatus.DockedStation)
			{
				CargoShuttle.Instance.MoveToCentcom();
				ShuttleStatus = CargoShuttleStatus.OnRouteCentcom;
			}
		}
		OnShuttleUpdate?.Invoke();
	}

	/// <summary>
	/// Method is called once shuttle arrives to its destination.
	/// Server only.
	/// </summary>
	public void OnShuttleArrival()
	{
		shuttleIsMoving = false;
		if (ShuttleStatus == CargoShuttleStatus.OnRouteCentcom)
		{
			ShuttleStatus = CargoShuttleStatus.DockedCentcom;
			SpawnOrder();
		}
		else if (ShuttleStatus == CargoShuttleStatus.OnRouteStation)
		{
			ShuttleStatus = CargoShuttleStatus.DockedStation;
		}
		OnShuttleUpdate?.Invoke();
	}

	private void SpawnOrder()
	{
		for (int i = 0; i < CurrentOrders.Count; i++)
		{
			CargoShuttle.Instance.SpawnOrder(CurrentOrders[i]);
		}
		CurrentOrders.Clear();
	}

	public void AddToCart(CargoOrder orderToAdd)
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;
		CurrentCart.Add(orderToAdd);
		Debug.Log(orderToAdd.OrderName + " was added to cart.");
		OnCartUpdate?.Invoke();
	}

	public void RemoveFromCart(CargoOrder orderToRemove)
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;
		CurrentCart.Remove(orderToRemove);
		OnCartUpdate?.Invoke();
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
		int totalPrice = TotalCartPrice();

		if (totalPrice <= Credits)
		{
			CurrentOrders.AddRange(CurrentCart);
			CurrentCart.Clear();
			Credits -= totalPrice;
		}
		OnCartUpdate?.Invoke();
		OnCreditsUpdate?.Invoke();
		return;
	}

	/// <summary>
	/// Adds the credits.
	/// </summary>
	/// <returns><c>true</c>, if credits were added, <c>false</c> otherwise.</returns>
	/// <param name="item">Item.</param>
	public bool AddCredits(ObjectBehaviour item)
	{
		Credits += 100;
		OnCreditsUpdate?.Invoke();
		return true;
	}
}

public enum CargoShuttleStatus { DockedCentcom, DockedStation, OnRouteCentcom, OnRouteStation };

[System.Serializable]
public class CargoOrder
{
	public string OrderName = "Crate with beer and steak";
	public int CreditsCost = 1000;
	public GameObject Crate = null;
	public List<GameObject> Items = new List<GameObject>();
}
