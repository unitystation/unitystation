using System;
using UnityEngine;
using Core.Editor.Attributes;
using Doors;
using Systems.Interaction;
using Util;


	[RequireComponent(typeof(Integrity))]
	[RequireComponent(typeof(Meleeable))]
	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private MatrixSystemManager subsystemManager;
		private MatrixSystemManager SubsystemManager => subsystemManager ? subsystemManager : subsystemManager = GetComponentInParent<MatrixSystemManager>();

		private TileChangeManager tileChangeManager;

		private CheckedComponent<Rotatable> rotatableChecked = new CheckedComponent<Rotatable>();
		public CheckedComponent<Rotatable> RotatableChecked => rotatableChecked;

		[NonSerialized]
		public InteractableDoor InteractableDoor;


		public bool OneDirectionRestricted;

		[SerializeField]
		private bool isClosed = true;

		public bool IsClosed
		{
			get => isClosed;
			set
			{
				if (isClosed != value)
				{
					isClosed = value;
					if (SubsystemManager != null)
					{
						SubsystemManager.UpdateAt(LocalPositionServer);
					}
				}
			}
		}

		protected override void Awake()
		{
			base.Awake();
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
			//Doors/airlocks aren't supposed to switch matrices
			tileChangeManager = GetComponentInParent<TileChangeManager>();
			InteractableDoor = this.GetComponent<InteractableDoor>();
			rotatableChecked.ResetComponent(this);
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			//when we're going to be destroyed, need to tell all subsystems that our space is now passable
			isClosed = false;
			tileChangeManager.MetaTileMap.RemoveTileWithlayer(LocalPositionServer, LayerType.Walls); //for false-wall meta-walls
			if (SubsystemManager != null)
			{
				SubsystemManager.UpdateAt(LocalPositionServer);
			}
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			//spawn some metal for the door
	        Spawn.ServerPrefab("MetalSheet", WorldPosition, transform.parent, count: 2,
		        scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		}


		public override bool DoesNotBlockClick(Vector3Int reachingFrom, bool isServer)
		{
			if (OneDirectionRestricted)
			{
				return DirectionCheck(reachingFrom, isServer);
			}

			return true;
		}

		public override bool IsPassableFromInside(Vector3Int leavingTo, bool isServer, GameObject context = null)
		{
			if (isClosed && OneDirectionRestricted)
			{
				return DirectionCheck(leavingTo, isServer);
			}

			return true; //Should be able to walk out of closed doors
		}

		bool CheckViaDirectional(Rotatable directional, Vector3Int dir)
		{
			var dir2Int = dir.To2Int();

			switch (directional.CurrentDirection)
			{
				case OrientationEnum.Down_By180:
					if (dir2Int == Vector2Int.down) return false;
					return true;
				case OrientationEnum.Left_By90:
					if (dir2Int == Vector2Int.left) return false;
					return true;
				case OrientationEnum.Up_By0:
					if (dir2Int == Vector2Int.up) return false;
					return true;
				case OrientationEnum.Right_By270:
					if (dir2Int == Vector2Int.right) return false;
					return true;
			}

			return true;
		}

		public override bool IsPassableFromOutside(Vector3Int from, bool isServer, GameObject context = null)
		{
			if (isClosed && OneDirectionRestricted)
			{
				return DirectionCheck(from, isServer);
			}

			return !isClosed;
		}

		public override bool IsPassable(bool isServer, GameObject context = null)
		{
			return !isClosed;
		}

		public override bool IsAtmosPassable(Vector3Int from, bool isServer)
		{
			if (isClosed && OneDirectionRestricted)
			{
				return DirectionCheck(from, isServer);
			}

			return !isClosed;
		}

		/// <summary>
		/// DirectionEnum only valid for objects with rotatable
		/// </summary>
		public bool DirectionCheck(Vector3Int from, bool isServer)
		{
			//Returns false if player is bumping door from the restricted direction
			var position = isServer ? LocalPositionServer : LocalPositionClient;
			var direction = from - position;

			//Use Directional component if it exists
			if (rotatableChecked.HasComponent)
			{
				return CheckViaDirectional(rotatableChecked.Component, direction);
			}

			//OneDirectionRestricted is hardcoded to only be from the negative y position
			Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

			return !direction.y.Equals(v.y) || !direction.x.Equals(v.x);
		}
	}
