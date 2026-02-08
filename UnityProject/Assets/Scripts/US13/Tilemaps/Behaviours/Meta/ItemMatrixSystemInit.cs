using System;
using Mirror;
using US13.Managers.MatrixManager;
using US13.Shuttles;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Tilemaps.Behaviours.Meta
{

	public abstract class ItemMatrixSystemInit : NetworkBehaviour, IInitialiseSystem
	{

		public virtual int Priority => 0;

		public virtual void Initialize() { }

		[NonSerialized] public MetaTileMap metaTileMap;
		[NonSerialized] protected MatrixSystemManager subsystemManager;
		[NonSerialized] protected TileChangeManager tileChangeManager;
		[NonSerialized] protected NetworkedMatrix networkedMatrix;
		[NonSerialized] protected MatrixMove matrixMove;
		[NonSerialized] private Matrix matrix;

		public MatrixMove MatrixMove => matrixMove;
		public MatrixInfo MatrixInfo => matrix.MatrixInfo;

		public virtual void Start()
		{
			metaTileMap = GetComponentInParent<MetaTileMap>();
			matrix = GetComponentInParent<Matrix>();
			tileChangeManager = GetComponentInParent<TileChangeManager>();
			subsystemManager = GetComponentInParent<MatrixSystemManager>();
			matrixMove = GetComponentInParent<MatrixMove>();
			networkedMatrix = GetComponentInParent<NetworkedMatrix>();
			subsystemManager.Register(this);
		}

		public virtual void OnDestroy()
		{
			metaTileMap = null;
			tileChangeManager = null;
			networkedMatrix = null;
			subsystemManager = null;
			matrix = null;
		}
	}
}
