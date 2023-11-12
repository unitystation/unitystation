using System;
using Logs;
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

			[SyncVar(hook = nameof(SyncMatrixID))]
			[HideInInspector]
			public int matrixID;

		#endregion

		public static int matrixIDcounter;

		private void Awake()
		{
			if (transform.parent == null)
			{
				Loggy.LogError($"{gameObject.name} had null transform parent", Category.Matrix);
			}

			networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();

			if (networkedMatrix == null)
			{
				Loggy.LogError($"{gameObject.name} had null networkedMatrix", Category.Matrix);
			}

			networkedMatrix.MatrixSync = this;

			matrixMove = networkedMatrix.GetComponent<MatrixMove>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			networkedMatrix.OnStartClient();

			if (matrixMove != null)
			{
				matrixMove.OnStartClient();
			}
		}

		public override void OnStartServer()
		{
			base.OnStartServer();

			matrixID = matrixIDcounter;
			matrixIDcounter++;

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

			public void SyncMatrixID(int oldID, int newID)
			{
				matrixID = newID;
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
