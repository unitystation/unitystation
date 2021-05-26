﻿using System;
using Messages.Client.NewPlayer;
using Mirror;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace Shuttles
{
	public class MatrixSync : NetworkBehaviour
	{
		private NetworkedMatrix networkedMatrix;
		public NetworkedMatrix NetworkedMatrix => networkedMatrix;

		private MatrixMove matrixMove;
		public MatrixMove MatrixMove => matrixMove;

		#region MatrixMove SyncVars

			[SyncVar(hook = nameof(SyncInitialPosition))]
			private Vector3 initialPosition;

			[SyncVar(hook = nameof(SyncPivot))]
			private Vector3 pivot;

			[SyncVar(hook = nameof(OnRcsActivated))]
			[HideInInspector]
			public bool rcsModeActive;

		#endregion

		private void Awake()
		{
			if (transform.parent == null)
			{
				Logger.LogError($"{gameObject.name} had null transform parent", Category.Matrix);
			}

			networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();

			if (networkedMatrix == null)
			{
				Logger.LogError($"{gameObject.name} had null networkedMatrix", Category.Matrix);
			}

			networkedMatrix.MatrixSync = this;

			matrixMove = networkedMatrix.GetComponent<MatrixMove>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			networkedMatrix.OnStartClient();
			TileChangeNewPlayer.Send(netId);

			if (matrixMove != null)
			{
				matrixMove.OnStartClient();
			}
		}

		public override void OnStartServer()
		{
			base.OnStartServer();

			networkedMatrix.OnStartServer();

			if (matrixMove != null)
			{
				matrixMove.OnStartServer();
			}
		}

		#region MatrixMove Hooks

			public void SyncInitialPosition(Vector3 oldPos, Vector3 newPos)
			{
				initialPosition = newPos;
				matrixMove.initialPosition = newPos.RoundToInt();
			}

			public void SyncPivot(Vector3 oldPivot, Vector3 newPivot)
			{
				pivot = newPivot;
				matrixMove.pivot = pivot.RoundToInt();
			}

			public void OnRcsActivated(bool oldState, bool newState)
			{
				rcsModeActive = newState;
				matrixMove.OnRcsActivated(oldState, newState);
			}

		#endregion

		#region EscapeShuttle RPC

			[ClientRpc]
			public void RpcStrandedEnd()
			{
				UIManager.Instance.PlayStrandedAnimation();
			}

		#endregion
	}
}
