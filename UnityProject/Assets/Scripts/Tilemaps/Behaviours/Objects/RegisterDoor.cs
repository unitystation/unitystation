using UnityEngine;


	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private SubsystemManager subsystemManager;

		public bool OneDirectionRestricted;

		private void Awake()
		{
			subsystemManager = GetComponentInParent<SubsystemManager>();
		}

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
					subsystemManager.UpdateAt(Position);
				}
			}
		}

		public override bool IsPassableTo( Vector3Int to )
		{
			if (isClosed && OneDirectionRestricted)
			{
				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				return !(to - Position).y.Equals(v.y);
			}

			return !isClosed;
		}

		public override bool IsPassable( Vector3Int from )
		{
			// Entering and leaving is the same check
			return IsPassableTo( from );
		}

		public override bool IsPassable()
		{
			return !isClosed;
		}

		public override bool IsAtmosPassable(Vector3Int from)
		{
			if (isClosed && OneDirectionRestricted)
			{
				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				return !(from - Position).y.Equals(v.y);
			}

			return !isClosed;
		}
	}
