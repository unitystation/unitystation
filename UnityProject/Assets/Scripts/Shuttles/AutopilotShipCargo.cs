using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Items;
using Logs;
using Objects;
using Systems.Cargo;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

public class AutopilotShipCargo : AutopilotShipMachine
{
	private List<Vector3Int> availableSpawnSlots = new List<Vector3Int>();


	public static AutopilotShipCargo Instance;

	public GuidanceBuoy StationStartBuoy;

	public GuidanceBuoy TargetDestinationBuoy;

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


	public override void  Start()
	{
		base.Start();
		MoveDirectionIn = true;

	}

	/// <summary>
	/// Send Shuttle to the station.
	/// Server only.
	/// </summary>
	[NaughtyAttributes.Button]
	public void MoveToStation()
	{
		MoveToTargetBuoy(StationStartBuoy);
	}


	[NaughtyAttributes.Button]
	/// <summary>
	/// Send shuttle to centcom.
	/// Server only.
	/// </summary>
	public void MoveToCentcom()
	{
		MoveToTargetBuoy(TargetDestinationBuoy);
	}


	public override void ReachedEndOfInBuoyChain(GuidanceBuoy GuidanceBuoy, GuidanceBuoy StartOfChain)
	{
		base.ReachedEndOfInBuoyChain(GuidanceBuoy, StartOfChain);

		//Server Side Only
		if (StartOfChain == StationStartBuoy)
		{
			CargoManager.Instance.OnShuttleArrival();
		}

		if (StartOfChain == TargetDestinationBuoy)
		{
			UnloadAndArrive();
		}
	}


	public void UnloadAndArrive()
	{
		UnloadCargo();
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
		Transform ObjectHolder = mm.NetworkedMatrixMove.MetaTileMap.matrix.MatrixInfo.Objects;
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
		var seekingItemTraitsForBounties = new List<ItemTrait>();
		foreach (var bounty in CargoManager.Instance.ActiveBounties)
		{
			seekingItemTraitsForBounties.AddRange(bounty.Demands.Keys);
		}

		bool hasBountyTrait(Attributes attribute)
		{
			if (attribute is ItemAttributesV2 c)
			{
				return c.HasAnyTrait(seekingItemTraitsForBounties);
			}

			return false;
		}

		for (int i = 0; i < objectHolder.childCount; i++)
		{
			var item = objectHolder.GetChild(i).gameObject;
			if (item == null) continue;

			//need VisibleState check because despawned objects still stick around on their matrix transform
			if (item.TryGetComponent<UniversalObjectPhysics>(out var behaviour) == false ||
			    behaviour.IsVisible == false) continue;
			if (item.TryGetComponent<Attributes>(out var attributes) == false) continue;
			switch (attributes.CanBeSoldInCargo)
			{
				// Items that cannot be sold in cargo will be ignored unless they have a trait that is assoicated with a bounty
				case false when hasBountyTrait(attributes) == false:
				// Don't sell secured objects e.g. conveyors.
				case true when behaviour.IsNotPushable:
					continue;
				default:
					CargoManager.Instance.ProcessCargo(item, alreadySold);
					break;
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
					Loggy.Log(
						$"Error with order fulfilment. Can't add items index: {i} for {order.OrderName} as the prefab is null. Skipping..",
						Category.Cargo);
					continue;
				}

				if (!stackableItems.ContainsKey(entryPrefab))
				{
					var orderedItem = Spawn.ServerPrefab(order.Items[i], pos).GameObject;
					if (orderedItem == null)
					{
						//let the shuttle still be able to complete the order empty otherwise it will be stuck permantly
						Loggy.Log($"Can't add ordered item to create because it doesn't have a GameObject",
							Category.Cargo);
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
							Loggy.Log($"Can't add ordered item to create because it doesn't have a GameObject",
								Category.Cargo);
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
			Loggy.LogWarning(
				$"{crate.ExpensiveName()} does not have {nameof(UniversalObjectPhysics)}. Please fix CargoData" +
				$" to ensure that the crate prefab is actually a crate (with {nameof(UniversalObjectPhysics)} component)." +
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
			var items = registerTile.Matrix
				.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, ObjectType.Item, true)
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

		 var PresentTiles = mm.NetworkedMatrixMove.MetaTileMap.PresentTilesNeedsLock;
		 lock (PresentTiles)
		 {
			 var ToLoop = PresentTiles[(int)LayerType.Base];

			 foreach (var Location in ToLoop)
			 {
				 pos = Location.LocalPosition.ToWorld(mm.NetworkedMatrixMove.MetaTileMap.matrix).RoundToInt();
				 if ((MatrixManager.Instance.GetFirst<ClosetControl>(pos, true) == null) &&
				     MatrixManager.IsFloorAt(pos, true))
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
			spawnPos = availableSpawnSlots[UnityEngine.Random.Range(0, availableSpawnSlots.Count)];
			availableSpawnSlots.Remove(spawnPos);
			return spawnPos;
		}

		return TransformState.HiddenPos;
	}
}