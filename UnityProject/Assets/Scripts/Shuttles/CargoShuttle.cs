using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoShuttle : MonoBehaviour
{
	public static CargoShuttle Instance;

	[SerializeField]
	private Vector2 centcomDest = new Vector2(4, 150);
	public Vector2 StationDest = new Vector2(4, 85);
	[SerializeField]
	private int dockOffset = 23;
	private Vector3 destination;
	private bool moving;

	private MatrixMove mm;

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

		mm = GetComponent<MatrixMove>();
		mm.SetAccuracy(2);
	}

	/// <summary>
	/// Send Shuttle to the station.
	/// Server only.
	/// </summary>
	public void MoveToStation()
	{
		mm.ChangeDir(Orientation.Down);
		MoveTo(StationDest);
	}

	/// <summary>
	/// Send shuttle to centcom.
	/// Server only.
	/// </summary>
	public void MoveToCentcom()
	{
		mm.ChangeDir(Orientation.Up);
		MoveTo(centcomDest);
	}

	private void MoveTo(Vector3 pos)
	{
		moving = true;
		destination = pos;
		mm.SetSpeed(25);
		mm.AutopilotTo(destination);
	}

	private void Update()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		if (moving && Vector2.Distance(transform.position, destination) < 2)	//arrived to dest
		{
			moving = false;
			mm.SetPosition(destination);
			mm.StopMovement();
			mm.RotateTo(Orientation.Up);

			if (CargoManager.Instance.ShuttleStatus == CargoShuttleStatus.OnRouteStation)
			{
				mm.ChangeDir(Orientation.Down);
				StartCoroutine(ReverseIntoStation());
			}
		}
		if (CargoManager.Instance.CurrentFlyTime <= 0f &&
			CargoManager.Instance.ShuttleStatus == CargoShuttleStatus.OnRouteCentcom)
		{
			UnloadCargo();
			CargoManager.Instance.OnShuttleArrival();
		}
	}

	IEnumerator ReverseIntoStation()
	{
		yield return new WaitForSeconds(3f);
		mm.MoveFor(dockOffset);
		yield return new WaitForSeconds(2f);
		CargoManager.Instance.OnShuttleArrival();
	}

	/// <summary>
	/// Calls CargoManager.DestroyItem() for all items on the shuttle.
	/// Server only.
	/// </summary>
	void UnloadCargo()
	{
		//Destroy all items on the shuttle
		Transform objectHolder = mm.MatrixInfo.Objects;
		for (int i = 0; i < objectHolder.childCount; i++)
		{
			ObjectBehaviour item = objectHolder.GetChild(i).GetComponent<ObjectBehaviour>();
			if (item != null)
			{
				CargoManager.Instance.DestroyItem(item);
			}
		}
	}

	/// <summary>
	/// Spawns the order inside cargo shuttle.
	/// Server only.
	/// </summary>
	/// <param name="order">Order to spawn.</param>
	public bool SpawnOrder(CargoOrder order)
	{
		Vector3 pos = GetRandomFreePos();
		if (pos == Vector3.zero)
			return (false);

		PoolManager.PoolNetworkInstantiate(order.Crate, pos);
		for (int i = 0; i < order.Items.Count; i++)
		{
			PoolManager.PoolNetworkInstantiate(order.Items[i], pos);
		}
		CargoManager.Instance.CentcomMessage += "Loaded " + order.OrderName + " onto shuttle.\n";
		return (true);
	}

	/// <summary>
	/// Get random unoccupied position inside shuttle.
	/// Beware - shuttle size is hardcoded.
	/// </summary>
	private Vector3 GetRandomFreePos()
	{
		int width = 2;
		int height = 4;
		int i = 0;

		Vector3Int spawnPos;
		//temporary max crates in one
		while (i < 40)
		{
			spawnPos = Vector3Int.RoundToInt(transform.position);
			spawnPos.x += Random.Range(-width, width);
			spawnPos.y += Random.Range(-height, height) + 1;
			if (MatrixManager.Instance.GetFirst<ClosetControl>(spawnPos, true) == null)
			{
				return spawnPos;
			}
			i++;
		}
		return Vector3.zero;
	}
}