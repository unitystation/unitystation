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
	private List<Vector3Int> availableSpawnSlots = new List<Vector3Int>();
	//It is actually (cargoZoneWidth - 1) / 2
	private int shuttleWidth = 2;
	//It is actually (cargoZoneHeight - 1) / 2
	private int shuttleHeight = 4;
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

			if (CargoManager.Instance.ShuttleStatus == ShuttleStatus.OnRouteStation)
			{
				mm.ChangeDir(Orientation.Down);
				StartCoroutine(ReverseIntoStation());
			}
		}
		if (CargoManager.Instance.CurrentFlyTime <= 0f &&
			CargoManager.Instance.ShuttleStatus == ShuttleStatus.OnRouteCentcom)
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
	/// Do some stuff you need to do before spawning orders.
	/// Called once.
	/// </summary>
	public void PrepareSpawnOrders()
	{
		GetAvailablePositions();
	}

	/// <summary>
	/// Spawns the order inside cargo shuttle.
	/// Server only.
	/// </summary>
	/// <param name="order">Order to spawn.</param>
	public bool SpawnOrder(CargoOrder order)
	{
		Vector3 pos = GetRandomFreePos();
		if (pos == TransformState.HiddenPos)
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
	/// Get all unoccupied positions inside shuttle.
	/// Needs to be called before starting to spawn orders.
	/// </summary>
	private void GetAvailablePositions()
	{
		Vector3Int pos;
		availableSpawnSlots = new List<Vector3Int>();

		for (int i = -shuttleHeight; i <= shuttleHeight; i++)
		{
			for (int j = -shuttleWidth; j <= shuttleWidth; j++)
			{
				pos = mm.State.Position.RoundToInt();
				//i + 1 because cargo shuttle center is offseted by 1
				pos += new Vector3Int(j, i + 1, 0);
				if (MatrixManager.Instance.GetFirst<ClosetControl>(pos, true) == null)
				{
					availableSpawnSlots.Add(pos);
				}
			}
		}
	}

	/// <summary>
	/// Gets random unoccupied position inside shuttle.
	/// </summary>
	private Vector3 GetRandomFreePos()
	{
		Vector3Int spawnPos;

		if (availableSpawnSlots.Count > 0)
		{
			spawnPos = availableSpawnSlots[Random.Range(0, availableSpawnSlots.Count)];
			availableSpawnSlots.Remove(spawnPos);
			return spawnPos;
		}

		return TransformState.HiddenPos;
	}
}