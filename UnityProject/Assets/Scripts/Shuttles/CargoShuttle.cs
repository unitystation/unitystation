using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoShuttle : MonoBehaviour
{
	public static CargoShuttle Instance;

	[SerializeField]
	private Vector2 centcomDest;
	public Vector2 StationDest;
	[SerializeField]
	private int stationOffset = 23;
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

			if (CargoManager.Instance.ShuttleStatus == CargoShuttleStatus.OnRouteCentcom)
			{
				UnloadCargo();
				CargoManager.Instance.OnShuttleArrival();
			}
			else if (CargoManager.Instance.ShuttleStatus == CargoShuttleStatus.OnRouteStation)
			{
				mm.ChangeDir(Orientation.Down);
				StartCoroutine(ReverseIntoStation());
			}
		}
	}

	IEnumerator ReverseIntoStation()
	{
		yield return new WaitForSeconds(3f);
		mm.MoveFor(stationOffset);
		yield return new WaitForSeconds(2f);
		CargoManager.Instance.OnShuttleArrival();
	}

	/// <summary>
	/// Destroys all items on the shuttle and adds credits.
	/// Server only.
	/// </summary>
	void UnloadCargo()
	{
		//Destroy all items on the shuttle and add credits
		Transform objectHolder = mm.MatrixInfo.Objects;
		for (int i = 0; i < objectHolder.childCount; i++)
		{
			ObjectBehaviour item = objectHolder.GetChild(i).GetComponent<ObjectBehaviour>();
			if (item != null && CargoManager.Instance.AddCredits(item))
			{
				Debug.Log("Destroyed " + item.name);
				item.registerTile.Unregister();
				PoolManager.PoolNetworkDestroy(item.gameObject);
			}
		}
	}

	/// <summary>
	/// Spawns the order inside cargo shuttle.
	/// Server only.
	/// </summary>
	/// <param name="order">Order to spawn.</param>
	public void SpawnOrder(CargoOrder order)
	{
		Vector3 pos = GetRandomFreePos();

		PoolManager.PoolNetworkInstantiate(order.Crate, pos);
		for (int i = 0; i < order.Items.Count; i++)
		{
			PoolManager.PoolNetworkInstantiate(order.Items[i], pos);
		}
	}

	/// <summary>
	/// Get random unoccupied position inside shuttle.
	/// Beware - shuttle size is hardcoded.
	/// </summary>
	private Vector3 GetRandomFreePos()
	{
		int width = 2;
		int height = 4;

		Vector3Int spawnPos;
		while (true)
		{
			spawnPos = Vector3Int.RoundToInt(transform.position);
			spawnPos.x += Random.Range(-width, width);
			spawnPos.y += Random.Range(-height, height) + 1;
			if (MatrixManager.Instance.GetFirst<ClosetControl>(spawnPos) == null)
			{
				break;
			}
		}
		return spawnPos;
	}
}