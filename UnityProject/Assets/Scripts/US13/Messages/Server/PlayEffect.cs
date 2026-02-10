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
				Loggy.Warning("Failed to spawn wind effect!", Category.Particles);
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