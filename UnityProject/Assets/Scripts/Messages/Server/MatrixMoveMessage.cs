using Mirror;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace Messages.Server
{
	///     Tells client to
	public class MatrixMoveMessage : ServerMessage<MatrixMoveMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public MatrixState State;
			public uint Matrix;

			public override string ToString()
			{
				return $"[MatrixMoveMessage {State}]";
			}
		}

		//Reset client's prediction queue
		//public bool ResetQueue;

		///To be run on client
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Matrix);

			//Sometimes NetworkObject is gone because of game ending or just before exit
			if (NetworkObject != null)
			{
				var matrixMove = NetworkObject.GetComponent<MatrixSync>().MatrixMove;
				matrixMove.UpdateClientState(msg.State);
			}
		}

		public static NetMessage Send(NetworkConnection recipient, GameObject matrix, MatrixState state)
		{
			var msg = new NetMessage
			{
				Matrix = matrix != null ? matrix.GetComponent<NetworkedMatrix>().MatrixSync.netId : NetId.Invalid,
				State = state,
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject matrix, MatrixState state)
		{
			var msg = new NetMessage
			{
				Matrix = matrix != null ? matrix.GetComponent<NetworkedMatrix>().MatrixSync.netId : NetId.Invalid,
				State = state,
			};

			SendToAll(msg);
			return msg;
		}
	}
}