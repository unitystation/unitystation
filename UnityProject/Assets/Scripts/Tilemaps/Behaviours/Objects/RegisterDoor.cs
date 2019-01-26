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
			get { return isClosed; }
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
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				return !(to - Position).Equals(v);
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

		public override bool IsAtmosPassable()
		{
			return !isClosed;
		}
	}
