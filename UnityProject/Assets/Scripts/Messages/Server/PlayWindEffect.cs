using Mirror;
using Shuttles;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Play a wind effect on client at a position
	/// </summary>
	public class PlayWindEffect : ServerMessage<PlayWindEffect.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint 	MatrixObject;
			public Vector3	LocalPosition;
			public Vector2	TargetVector;
		}

		public override void Process(NetMessage msg)
		{
			//Don't run client side wind on headless
			if(CustomNetworkManager.IsHeadless) return;

			var localPlayer = PlayerManager.LocalPlayerScript;
			if(localPlayer == null) return;

			if(LoadNetworkObject(msg.MatrixObject) == false) return;
			if(NetworkObject.TryGetComponent<MatrixSync>(out var matrixSync) == false) return;

			var matrix = matrixSync.NetworkedMatrix.matrix;

			var distance =
				(localPlayer.objectPhysics != null ?
					localPlayer.objectPhysics.OfficialPosition : localPlayer.transform.position)
				- msg.LocalPosition.ToWorld(matrix);

			//Be in 20 tile radius?
			if (distance.sqrMagnitude > 400) return;

			var windEffect = Spawn.ClientPrefab("WindEffect", parent: matrix.MetaTileMap.ObjectLayer.transform);

			if (windEffect.Successful == false)
			{
				Logger.LogWarning("Failed to spawn wind effect!", Category.Particles);
				return;
			}

			windEffect.GameObject.transform.localPosition = msg.LocalPosition;

			Effect.ClientPlayParticle(windEffect.GameObject, null, msg.TargetVector, false);
		}

		/// <summary>
		/// Tell all clients + server to play wind particle effect
		/// </summary>
		public static NetMessage SendToAll(Matrix matrix, Vector3 localPosition, Vector2 targetVector)
		{

			NetMessage msg = new NetMessage
			{
				MatrixObject = matrix.NetworkedMatrix.MatrixSync.netId,
				LocalPosition = localPosition,
				TargetVector = targetVector
			};

			SendToAll(msg);
			return msg;
		}
	}
}