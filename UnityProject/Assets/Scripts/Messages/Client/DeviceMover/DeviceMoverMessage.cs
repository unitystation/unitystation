using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Messages.Server;
using Mirror;
using Objects.Atmospherics;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

public class DeviceMoverMessage : ClientMessage<DeviceMoverMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint MatrixID;
		public uint ObjectID;
		public Vector3 WorldPosition;
		public Vector3 RelativeMovePosition;
	}

	public static void Send(GameObject Object, Vector3 WorldPosition, GameObject Matrix,Vector3 RelativeMovePosition )
	{
		NetMessage msg = new NetMessage
		{
			ObjectID = Object == null ? NetId.Empty : Object.NetId(),
			MatrixID = Matrix == null ? NetId.Empty : Matrix.NetId(),
			WorldPosition = WorldPosition,
			RelativeMovePosition = RelativeMovePosition
		};
		Send(msg);
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;


		if (msg.ObjectID !=  NetId.Empty &&  msg.ObjectID != NetId.Invalid)
		{
			msg.ObjectID.NetIdToGameObject()
				.GetUniversalObjectPhysics()
				.AppearAtWorldPositionServer(msg.WorldPosition);
		}
		else
		{
			var Matrix = msg.MatrixID.NetIdToGameObject().GetComponent<MatrixSync>().NetworkedMatrix.matrix;

			if (Matrix.MatrixMove != null)
			{
				Matrix.MatrixMove.SetPosition(Matrix.MatrixMove.CurrentState.Position + (msg.RelativeMovePosition ) );
			}
			else
			{
				var Object = Matrix.transform.parent;
				var Offset = msg.RelativeMovePosition.RoundToInt();
				Object.transform.position = Object.transform.position +Offset;

				Matrix.InitialOffset = (Matrix.InitialOffset + Offset);
				Matrix.MatrixInfo.InitialOffset = Matrix.InitialOffset;
				Matrix.MetaTileMap.UpdateTransformMatrix();
				UpdateNonMovableMatrixPositionMessage.Send(msg.MatrixID.NetIdToGameObject().GetComponent<MatrixSync>(),
					msg.RelativeMovePosition);
			}
		}
	}
}
