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
			public Vector3? LocalPOS;
			public float? z360;
			public Color? Colour;
		}

		public override void Process(NetMessage msg)
		{
			//Don't run client side wind on headless
			if (CustomNetworkManager.IsHeadless) return;

			var localPlayer = PlayerManager.LocalPlayerScript;
			if (localPlayer == null) return;


			if (msg.z360 == null)
			{
				msg.z360 = 0;
			}

			LoadMultipleObjects(new[] {msg.SpawnOn, msg.ByPlayer});

			if (msg.LeanTweenEffect == false)
			{
				var windEffect = Spawn.ClientPrefab(msg.EffectName, parent:NetworkObjects[0].gameObject.transform.parent);

				if (windEffect.Successful == false)
				{
					Loggy.Warning("Failed to spawn wind effect!", Category.Particles);
					return;
				}

				if (msg.LocalPOS != null)
				{
					windEffect.GameObject.transform.localPosition = msg.LocalPOS.Value;
					windEffect.GameObject.transform.SetParent(NetworkObjects[0].gameObject.transform, true);
				}
				else
				{
					windEffect.GameObject.transform.SetParent(NetworkObjects[0].gameObject.transform, false);
					windEffect.GameObject.transform.localPosition = Vector3.zero;
				}

				windEffect.GameObject.transform.localEulerAngles = new Vector3(0, 0, msg.z360.Value);

				foreach (var wantInfo in windEffect.GameObject.GetComponents<IWantMoreEffectInfo>())
				{
					wantInfo.ReceiveMoreEffectInfo(msg.Colour.GetValueOrDefault(),NetworkObjects[1] );
				}
			}
			else
			{
				LeanTweenAnimations.DeEffectClient(msg.EffectName, NetworkObjects[0], NetworkObjects[1]);
			}

		}

		public static void SendToAll(GameObject SpawnOn, string EffectsName, bool  LeanTweenEffect, GameObject Player, Vector3? LocalPOS, float? RotationX, Color? Colour = null)
		{
			var NetID = SpawnOn.NetId();

			if (NetID is NetId.Invalid or NetId.Empty) return;

			NetMessage msg = new NetMessage
			{
				ByPlayer = Player == null ? NetId.Empty :  Player.NetId(),
				SpawnOn = NetID,
				EffectName = EffectsName,
				LeanTweenEffect = LeanTweenEffect,
				LocalPOS =  LocalPOS,
				z360 =  RotationX,
				Colour = Colour

			};

			SendToAll(msg);
		}
	}
}