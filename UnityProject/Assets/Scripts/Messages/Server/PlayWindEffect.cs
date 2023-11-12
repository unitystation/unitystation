using System.Collections.Generic;
using Logs;
using Mirror;
using Shuttles;
using Systems.Atmospherics;
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
			public List<ReactionManager.WindEffectData> Data;
		}

		public override void Process(NetMessage msg)
		{
			//Don't run client side wind on headless
			if(CustomNetworkManager.IsHeadless) return;

			var localPlayer = PlayerManager.LocalPlayerScript;
			if(localPlayer == null) return;

			for (int i = 0; i < msg.Data.Count; i++)
			{
				var toWindAt = msg.Data[i];

				if(LoadNetworkObject(toWindAt.MatrixObject) == false) return;
				if(NetworkObject.TryGetComponent<MatrixSync>(out var matrixSync) == false) return;

				var matrix = matrixSync.NetworkedMatrix.matrix;

				var distance =
					(localPlayer.ObjectPhysics != null ?
						localPlayer.ObjectPhysics.OfficialPosition : localPlayer.transform.position)
					- toWindAt.LocalPosition.ToWorld(matrix);

				//Be in 20 tile radius?
				if (distance.sqrMagnitude > 400) return;

				var windEffect = Spawn.ClientPrefab("WindEffect", parent: matrix.MetaTileMap.ObjectLayer.transform);

				if (windEffect.Successful == false)
				{
					Loggy.LogWarning("Failed to spawn wind effect!", Category.Particles);
					return;
				}

				windEffect.GameObject.transform.localPosition = toWindAt.LocalPosition;

				Effect.ClientPlayParticle(windEffect.GameObject, null, toWindAt.TargetVector, false);
			}
		}

		/// <summary>
		/// Tell all clients + server to play wind particle effect
		/// </summary>
		public static void SendToAll(List<ReactionManager.WindEffectData> data)
		{
			if (data.Count == 0) return;

			NetMessage msg = new NetMessage
			{
				Data = data
			};

			SendToAll(msg);
		}
	}
}
