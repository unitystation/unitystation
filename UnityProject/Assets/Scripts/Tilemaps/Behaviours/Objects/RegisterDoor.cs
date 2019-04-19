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
					subsystemManager.UpdateAt(PositionS);
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
				return !(to - (isServer ? PositionS : PositionC)).y.Equals(v.y);
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
				return !(from - (isServer ? PositionS : PositionC)).y.Equals(v.y);
			}

			return !isClosed;
		}

		private CustomNetTransform cnt;
		protected override void InitDerived()
		{
			cnt = GetComponent<CustomNetTransform>();
		}

		public override void UpdatePositionServer()
        {
        	if ( !cnt )
        	{
        		base.UpdatePositionServer();
        	}
        	else
        	{
        		PositionS = cnt.ServerLocalPosition;
        	}
        }
		public override void UpdatePositionClient()
		{
			if ( !cnt )
			{
				base.UpdatePositionClient();
			}
			else
			{
				PositionC = cnt.ClientLocalPosition;
			}
		}
	}
