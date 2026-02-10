using Mirror;
using UnityEngine;
using US13.Core.Transform;

namespace US13.Messages.Client
{
	public class RequestGhostMove : ClientMessage<RequestGhostMove.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Vector3 WorldPosition;
			public int matrixID;
			public OrientationEnum direction;
		}

		public override void Process(NetMessage msg)
		{
			SentByPlayer?.Mind?.Move?.SetServerPosition(msg.WorldPosition, msg.matrixID, msg.direction);
		}

		public static void Send(Vector3 WorldPosition, int matrixID, OrientationEnum direction)
		{

			var Net = new NetMessage()
			{
				WorldPosition = WorldPosition,
				matrixID = matrixID,
				direction = direction
			};


			Send(Net);
		}

	}
}
