using System;
using System.Collections;
using System.Collections.Generic;
using Core.Networking;
using Cysharp.Threading.Tasks;
using Mirror;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace TileMap.Behaviours
{

	public abstract class ItemMatrixSystemInit : NaughtyNetworkBehaviour, IInitialiseSystem
	{

		public virtual int Priority => 0;

		public async virtual UniTask Initialize()
		{

		}

		[NonSerialized] public MetaTileMap metaTileMap;
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
