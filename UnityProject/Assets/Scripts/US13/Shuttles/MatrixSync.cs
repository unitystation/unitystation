using Logs;
using Mirror;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Managers.SubSceneManager;
using US13.Tilemaps.Behaviours.Layers;
using US13.UI.Systems;

namespace US13.Shuttles
{
	public class MatrixSync : NetworkBehaviour
	{
		private NetworkedMatrix networkedMatrix;
		public NetworkedMatrix NetworkedMatrix => networkedMatrix;

		private MatrixMove matrixMove;
		public MatrixMove MatrixMove => matrixMove;

		#region MatrixMove SyncVars

			[SyncVar(hook = nameof(SyncMatrixID))]
			[HideInInspector]
			public int matrixID;

		#endregion

		[SyncVar(hook = nameof(SyncIsSpaceMatrix))] public bool IsSpaceMatrix;

		[SyncVar] public bool IsMainStationMatrix;

		public static int matrixIDcounter;

		private void SyncIsSpaceMatrix(bool oldState, bool newState)
		{
			IsSpaceMatrix = newState;
			if (IsSpaceMatrix)
			{
				MatrixManager.Instance.spaceMatrix = this.networkedMatrix.matrix;
			}
		}



		private void Awake()
		{
			if (transform.parent != null)
			{
				networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();
			}


			if (networkedMatrix == null)
			{
				var MatrixFrame = Instantiate(SubSceneManager.Instance.MatrixPrefab, null);
				this.transform.parent = MatrixFrame.transform;
				networkedMatrix = transform.parent.GetComponent<NetworkedMatrix>();
			}

			if (networkedMatrix == null)
			{
				Loggy.Error($"{gameObject.name} had null networkedMatrix", Category.Matrix);
			}

			networkedMatrix.MatrixSync = this;

			matrixMove = networkedMatrix.GetComponent<MatrixMove>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			networkedMatrix.OnStartClient();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();

			matrixID = matrixIDcounter;
			matrixIDcounter++;

			networkedMatrix.OnStartServer();
		}

		#region MatrixMove Hooks


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
