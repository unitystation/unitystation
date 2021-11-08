using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Objects;
using Items;

namespace Systems.Cargo
{
	public class CargoShuttle : MonoBehaviour
	{
		public static CargoShuttle Instance;

		[SerializeField]
		private Vector2 centcomDest = new Vector2(4, 150);
		public Vector2 StationDest = new Vector2(4, 85);
		[SerializeField]
		private int dockOffset = 23;
		[SerializeField]
		private bool ChangeDirectionAtOffset = false;
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

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		/// <summary>
		/// Send Shuttle to the station.
		/// Server only.
		/// </summary>
		public void MoveToStation()
		{
			mm.ChangeFlyingDirection(Orientation.Down);
			MoveTo(StationDest);
		}

		/// <summary>
		/// Send shuttle to centcom.
		/// Server only.
		/// </summary>
		public void MoveToCentcom()
		{
			mm.ChangeFlyingDirection(Orientation.Up);
			MoveTo(centcomDest);
		}

		private void MoveTo(Vector3 pos)
		{
			moving = true;
			destination = pos;
			mm.SetSpeed(25);
			mm.AutopilotTo(destination);
		}

		//Server Side Only
		private void UpdateMe()
		{
			if (moving && Vector2.Distance(transform.position, destination) < 2)    //arrived to dest
			{
				moving = false;
				mm.SetPosition(destination);
				mm.StopMovement();
				mm.SteerTo(Orientation.Up);

				if (CargoManager.Instance.ShuttleStatus == ShuttleStatus.OnRouteStation)
				{
					mm.ChangeFlyingDirection(Orientation.Down);
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
			if (ChangeDirectionAtOffset)
			{
				mm.SteerTo(Orientation.Down);
				mm.ChangeFlyingDirection(Orientation.Up);
			}

			if (dockOffset != 0)
			{
				yield return new WaitForSeconds(3f);
				mm.MoveFor(dockOffset);
				yield return new WaitForSeconds(2f);
			}
			CargoManager.Instance.OnShuttleArrival();
		}

		/// <summary>
		/// Scans and finds all objects that are aboard the shuttle.
		/// <returns>All objects found as children of a Transform variable.</returns>
		/// </summary>
		public Transform SearchForObjectsOnShuttle()
		{
			//note: This scan also seems to find objects contained inside closets only if the object was placed
			//into the crate after the crate was already on the cargo shuttle. Hence we are using alreadySold
			//to avoid duplicate selling in lieu of a more thorough fix to closet held items logic.
			Transform ObjectHolder = mm.MatrixInfo.Objects;
			return ObjectHolder;
		}

		/// <summary>
		/// Calls CargoManager.DestroyItem() for all items on the shuttle.
		/// Server only.
		/// </summary>
		private void UnloadCargo()
		{
			Transform objectHolder = SearchForObjectsOnShuttle();
			//track what we've already sold so it's not sold twice.
			HashSet<GameObject> alreadySold = new HashSet<GameObject>();
			for (int i = 0; i < objectHolder.childCount; i++)
			{
				var item = objectHolder.GetChild(i).gameObject;
				if (item == null) continue;

				//need VisibleState check because despawned objects still stick around on their matrix transform
				if (item.TryGetComponent<ObjectBehaviour>(out var behaviour) && behaviour.VisibleState)
				{
					if (item.TryGetComponent<Attributes>(out var attributes))
					{
						if (attributes.ExportType == Attributes.CargoExportType.Never) continue;

						// Don't sell secured objects e.g. conveyors.
						if (attributes.ExportType != Attributes.CargoExportType.Always && behaviour.IsNotPushable) continue;
					}

					CargoManager.Instance.ProcessCargo(item, alreadySold);
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
		public bool SpawnOrder(CargoOrderSO order)
		{
			Vector3 pos = GetRandomFreePos();
			if (pos == TransformState.HiddenPos)
				return (false);

			var crate = Spawn.ServerPrefab(order.Crate, pos).GameObject;
			Dictionary<GameObject, Stackable> stackableItems = new Dictionary<GameObject, Stackable>();
			//error occurred trying to spawn, just ignore this order.
			if (crate == null) return true;
			if (crate.TryGetComponent<ObjectContainer>(out var container))
			{
				for (int i = 0; i < order.Items.Count; i++)
				{
					var entryPrefab = order.Items[i];
					if (entryPrefab == null)
					{
						Logger.Log($"Error with order fulfilment. Can't add items index: {i} for {order.OrderName} as the prefab is null. Skipping..", Category.Cargo);
						continue;
					}

					if (!stackableItems.ContainsKey(entryPrefab))
					{
						var orderedItem = Spawn.ServerPrefab(order.Items[i], pos).GameObject;
						if (orderedItem == null)
						{
							//let the shuttle still be able to complete the order empty otherwise it will be stuck permantly
							Logger.Log($"Can't add ordered item to create because it doesn't have a GameObject", Category.Cargo);
							continue;
						}

						var stackableItem = orderedItem.GetComponent<Stackable>();
						if (stackableItem != null)
						{
							stackableItems.Add(entryPrefab, stackableItem);
						}

						AddItemToCrate(container, orderedItem);
					}
					else
					{
						if (stackableItems[entryPrefab].Amount < stackableItems[entryPrefab].MaxAmount)
						{
							stackableItems[entryPrefab].ServerIncrease(1);
						}
						else
						{
							//Start a new one to start stacking
							var orderedItem = Spawn.ServerPrefab(entryPrefab, pos).GameObject;
							if (orderedItem == null)
							{
								//let the shuttle still be able to complete the order empty otherwise it will be stuck permantly
								Logger.Log($"Can't add ordered item to create because it doesn't have a GameObject", Category.Cargo);
								continue;
							}

							var stackableItem = orderedItem.GetComponent<Stackable>();
							stackableItems[entryPrefab] = stackableItem;

							AddItemToCrate(container, orderedItem);
						}
					}
				}
			}
			else
			{
				Logger.LogWarning($"{crate.ExpensiveName()} does not have {nameof(ObjectBehaviour)}. Please fix CargoData" +
								  $" to ensure that the crate prefab is actually a crate (with {nameof(ObjectBehaviour)} component)." +
								  $" This order will be ignored.", Category.Cargo);
				return true;
			}

			CargoManager.Instance.CentcomMessage += "Loaded " + order.OrderName + " onto shuttle.\n";
			return (true);
		}

		private void AddItemToCrate(ObjectContainer container, GameObject obj)
		{
			//ensure it is added to crate
			if (obj.TryGetComponent<RandomItemSpot>(out var randomItem))
			{
				var registerTile = container.gameObject.RegisterTile();
				var items = registerTile.Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true)
						.Select(ob => ob.gameObject).Where(go => go != obj);

				container.StoreObjects(items);
			}
			else
			{
				container.StoreObject(obj);
			}
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
					pos = mm.ServerState.Position.RoundToInt();
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
}
