using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;
using US13.Player;
using Util;

namespace US13.Messages.Server
{
	public class PlayEffect : ServerMessage<PlayEffect.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ByPlayer;
			public uint SpawnOn;
			public bool LeanTweenEffect;
			public string EffectName;
		}

		public override void Process(NetMessage msg)
		{
			//Don't run client side wind on headless
			if (CustomNetworkManager.IsHeadless) return;

			var localPlayer = PlayerManager.LocalPlayerScript;
			if (localPlayer == null) return;


			LoadMultipleObjects(new[] {msg.SpawnOn, msg.ByPlayer});

			if (msg.LeanTweenEffect == false)
			{
				var windEffect = Spawn.ClientPrefab(msg.EffectName, parent:NetworkObjects[0].gameObject.transform);

				if (windEffect.Successful == false)
				{
					Loggy.Warning("Failed to spawn wind effect!", Category.Particles);
					return;
				}

				windEffect.GameObject.transform.localPosition = Vector3.zero;
			}
			else
			{
				LeanTweenAnimations.DeEffectClient(msg.EffectName, NetworkObjects[0], NetworkObjects[1]);
			}

		}

		public static void SendToAll(GameObject SpawnOn, string EffectsName, bool  LeanTweenEffect, GameObject Player )
		{
			var NetID = SpawnOn.NetId();

			if (NetID is NetId.Invalid or NetId.Empty) return;

			NetMessage msg = new NetMessage
			{
				ByPlayer = Player == null ? NetId.Empty :  Player.NetId(),
				SpawnOn = NetID,
				EffectName = EffectsName,
				LeanTweenEffect = LeanTweenEffect
			};

			SendToAll(msg);
		}
	}
}