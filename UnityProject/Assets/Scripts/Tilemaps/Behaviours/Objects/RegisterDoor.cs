using UnityEngine;


	[RequireComponent(typeof(Integrity))]
	[RequireComponent(typeof(Meleeable))]
	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private SubsystemManager subsystemManager;
		private SubsystemManager SubsystemManager => subsystemManager ? subsystemManager : subsystemManager = GetComponentInParent<SubsystemManager>();

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
					SubsystemManager.UpdateAt(LocalPositionServer);
				}
			}
		}

		private void Awake()
		{
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			//when we're going to be destroyed, need to tell all subsystems that our space is now passable
			isClosed = false;
	        if (SubsystemManager != null)
	        {
	            SubsystemManager.UpdateAt(LocalPositionServer);
	        }
	        //spawn some metal for the door
	        ObjectFactory.SpawnMetal(2, WorldPosition.To2Int(), parent: transform.parent);
		}


		public override bool IsPassableTo( Vector3Int to, bool isServer )
		{
			if (isClosed && OneDirectionRestricted)
			{
				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				return !(to - (isServer ? LocalPositionServer : LocalPositionClient)).y.Equals(v.y);
			}

			return !isClosed;
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
				return !(from - (isServer ? LocalPositionServer : LocalPositionClient)).y.Equals(v.y);
			}

			return !isClosed;
		}

	}
