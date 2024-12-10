using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace TileMap.Behaviours
{

	public abstract class ItemMatrixSystemInit : NetworkBehaviour, IInitialiseSystem
	{

		public virtual int Priority => 0;

		public virtual void Initialize() { }

		[NonSerialized] protected MetaTileMap metaTileMap;
		[NonSerialized] protected MatrixSystemManager subsystemManager;
		[NonSerialized] protected TileChangeManager tileChangeManager;
		[NonSerialized] protected NetworkedMatrix networkedMatrix;
		[NonSerialized] protected MatrixMove matrixMove;

		public MatrixMove MatrixMove => matrixMove;

		public virtual void Start()
		{
			metaTileMap = GetComponentInParent<MetaTileMap>();
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

		}
	}

}
