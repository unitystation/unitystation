using Systems.Ai;
using Blob;
using HealthV2;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public class SuicideMessage : ClientMessage<SuicideMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			Logger.Log("Player '" + SentByPlayer.Name + "' has committed suicide", Category.Health);

			if (SentByPlayer.Script.TryGetComponent<LivingHealthMasterBase>(out var livingHealthBehaviour))
			{
				if (livingHealthBehaviour.IsDead)
				{
					Logger.LogWarning("Player '" + SentByPlayer.Name + "' is attempting to commit suicide but is already dead.", Category.Health);
				}
				else
				{
					livingHealthBehaviour.Death();
				}

				return;
			}

			if (SentByPlayer.Script.TryGetComponent<AiPlayer>(out var aiPlayer))
			{
				aiPlayer.Suicide();
				return;
			}

			if (SentByPlayer.Script.TryGetComponent<BlobPlayer>(out var blobPlayer))
			{
				blobPlayer.Death();
			}
		}


		/// <summary>
		/// Tells the server to kill the player that sent this message
		/// </summary>
		/// <param name="obj">Dummy variable that is required to make this signiture different
		/// from the non-static function of the same name. Just pass null. </param>
		/// <returns></returns>
		public static NetMessage Send(Object obj)
		{
			NetMessage msg = new NetMessage();
			Send(msg);
			return msg;
		}


	}
}
