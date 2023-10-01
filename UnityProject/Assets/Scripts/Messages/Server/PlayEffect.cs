using System.Collections;
using System.Collections.Generic;
using Logs;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class PlayEffect : ServerMessage<PlayEffect.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint SpawnOn;
			public string EffectName;
		}

		public override void Process(NetMessage msg)
		{
			//Don't run client side wind on headless
			if (CustomNetworkManager.IsHeadless) return;

			var localPlayer = PlayerManager.LocalPlayerScript;
			if (localPlayer == null) return;


			if (LoadNetworkObject(msg.SpawnOn) == false) return;

			var windEffect = Spawn.ClientPrefab(msg.EffectName, parent:NetworkObject.gameObject.transform);

			if (windEffect.Successful == false)
			{
				Loggy.LogWarning("Failed to spawn wind effect!", Category.Particles);
				return;
			}

			windEffect.GameObject.transform.localPosition = Vector3.zero;
		}

		public static void SendToAll(GameObject SpawnOn, string EffectsName)
		{
			var NetID = SpawnOn.NetId();

			if (NetID is NetId.Invalid or NetId.Empty) return;

			NetMessage msg = new NetMessage
			{
				SpawnOn = NetID,
				EffectName = EffectsName
			};

			SendToAll(msg);
		}
	}
}