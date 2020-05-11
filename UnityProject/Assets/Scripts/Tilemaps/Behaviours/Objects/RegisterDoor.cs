using UnityEngine;


	[RequireComponent(typeof(Integrity))]
	[RequireComponent(typeof(Meleeable))]
	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private SubsystemManager subsystemManager;
		private SubsystemManager SubsystemManager => subsystemManager ? subsystemManager : subsystemManager = GetComponentInParent<SubsystemManager>();

		private TileChangeManager tileChangeManager;

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

		private void Awake()
		{
			base.Awake();
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
			//Doors/airlocks aren't supposed to switch matrices
			GetComponent<CustomNetTransform>().IsFixedMatrix = true;
			tileChangeManager = GetComponentInParent<TileChangeManager>();
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			//when we're going to be destroyed, need to tell all subsystems that our space is now passable
			isClosed = false;
			tileChangeManager.RemoveTile(LocalPositionServer, LayerType.Walls); //for false-wall meta-walls
			if (SubsystemManager != null)
			{
				SubsystemManager.UpdateAt(LocalPositionServer);
			}
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			//spawn some metal for the door
	        Spawn.ServerPrefab("Metal", WorldPosition, transform.parent, count: 2,
		        scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		}


		public override bool IsPassableTo( Vector3Int to, bool isServer )
		{
			if (isClosed && OneDirectionRestricted)
			{

				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				var position = isServer? LocalPositionServer : LocalPositionClient;
				var direction = to - position;

				//Use Directional component if it exists
				var tryGetDir = GetComponent<Directional>();
				if (tryGetDir != null)
				{
					return CheckViaDirectional(tryGetDir, direction);
				}

				return !direction.y.Equals(v.y) || !direction.x.Equals(v.x);
			}

			return !isClosed;
		}

		bool CheckViaDirectional(Directional directional, Vector3Int dir)
		{
			var dir2Int = dir.To2Int();
			switch (directional.CurrentDirection.AsEnum())
			{
				case OrientationEnum.Down:
					if (dir2Int == Vector2Int.down) return false;
					return true;
				case OrientationEnum.Left:
					if (dir2Int == Vector2Int.left) return false;
					return true;
				case OrientationEnum.Up:
					if (dir2Int == Vector2Int.up) return false;
					return true;
				case OrientationEnum.Right:
					if (dir2Int == Vector2Int.right) return false;
					return true;
			}

			return true;
		}

		public override bool IsPassable( Vector3Int from, bool isServer )
		{
			// Entering and leaving is the same check
			return IsPassableTo( from, isServer );
		}

		public override bool IsPassable(bool isServer)
		{
			return !isClosed;
		}

		public override bool IsAtmosPassable(Vector3Int from, bool isServer)
		{
			if (isClosed && OneDirectionRestricted)
			{
				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				var position = isServer? LocalPositionServer : LocalPositionClient;
				var direction = from - position;
				return !direction.y.Equals(v.y) || !direction.x.Equals(v.x);
			}

			return !isClosed;
		}

	}
