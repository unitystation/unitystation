using UnityEngine;


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
					SubsystemManager.UpdateAt(PositionServer);
				}
			}
		}

		public override bool IsPassableTo( Vector3Int to, bool isServer )
		{
			if (isClosed && OneDirectionRestricted)
			{
				// OneDirectionRestricted is hardcoded to only be from the negative y position
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				// Returns false if player is bumping door from the restricted direction
				return !(to - (isServer ? PositionServer : PositionClient)).y.Equals(v.y);
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
				return !(from - (isServer ? PositionServer : PositionClient)).y.Equals(v.y);
			}

			return !isClosed;
		}
	}
