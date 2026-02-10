using Mirror;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Shuttles;
using Util;

namespace US13.Messages.Server
{
	public class UpdateNonMovableMatrixPositionMessage : ServerMessage<UpdateNonMovableMatrixPositionMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MatrixID;
			public Vector3 RelativePosition;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return;
			var Matrix = msg.MatrixID.NetIdToGameObject().GetComponent<MatrixSync>().NetworkedMatrix.matrix;
			var Object = Matrix.transform.parent;
			var Offset = msg.RelativePosition.RoundToInt();
			Object.transform.position = Object.transform.position +Offset;

			Matrix.InitialOffset = (Matrix.InitialOffset + Offset);
			Matrix.MatrixInfo.InitialOffset = Matrix.InitialOffset;
			Matrix.MetaTileMap.UpdateTransformMatrix();
		}

		public static NetMessage Send(MatrixSync Matrix, Vector3 RelativePosition)
		{
			NetMessage msg = new NetMessage()
			{
				MatrixID = Matrix.netId,
				RelativePosition = RelativePosition
			};

			SendToAll(msg, 3);
			return msg;
		}

	}
}