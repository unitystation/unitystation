using UnityEngine;


	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private SystemManager systemManager;
		
		public bool OneDirectionRestricted;

		private void Awake()
		{
			systemManager = GetComponentInParent<SystemManager>();
		}

		public override void AfterUpdate()
		{
			systemManager?.UpdateAt(Position);
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
					systemManager.UpdateAt(Position);
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
