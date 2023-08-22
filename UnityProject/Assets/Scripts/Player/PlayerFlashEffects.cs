using UnityEngine;
using Mirror;
using CameraEffects;
using Messages.Server;

namespace Player
{
	public class PlayerFlashEffects : NetworkBehaviour
	{
		[SyncVar,HideInInspector] public int WeldingShieldImplants = 0;

		[Server]
		public bool ServerSendMessageToClient(GameObject client, float flashDuration, bool checkForProtectiveCloth, bool stunPlayer = true, float stunDuration = 4f)
		{
			if (WeldingShieldImplants > 0) return false;

			PlayerFlashEffectsMessage.Send(client, flashDuration, checkForProtectiveCloth, stunPlayer, stunDuration);

			return true;
		}
	}

	public class PlayerFlashEffectsMessage : ServerMessage<PlayerFlashEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float FlashValue;
			public float StunDuration;
			public bool CheckForProtectiveCloth;
			public bool StunPlayer;
		}

		public override void Process(NetMessage msg)
		{
			var camera = Camera.main;
			if (camera == null) return;
			if (msg.CheckForProtectiveCloth && HasProtectiveCloth(PlayerManager.LocalPlayerObject, msg.StunPlayer, msg.StunDuration)) return;
			camera.GetComponent<CameraEffectControlScript>().FlashEyes(msg.FlashValue);
		}

		public static bool HasProtectiveCloth(GameObject target, bool stunPlayer = true, float stunDuration = 4f)
		{
			if (target == null) return false;
			if (target.HasComponent<PlayerFlashEffects>() == false) return true;
			if (target.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) return false;

			foreach (var slots in playerStorage.ServerContents)
			{
				if (slots.Key != NamedSlot.eyes && slots.Key != NamedSlot.mask) continue;
				foreach (ItemSlot onSlots in slots.Value)
				{
					if (onSlots.IsEmpty) continue;
					if (onSlots.ItemAttributes.HasTrait(CommonTraits.Instance.Sunglasses))
					{
						return true;
					}
				}
			}
			if (stunPlayer) target.GetComponent<RegisterPlayer>()?.ServerStun(stunDuration);
			return false;
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue, bool checkForProtectiveCloth, bool stunPlayer, float stunDuration)
		{
			NetMessage msg = new NetMessage
			{
				FlashValue = newflashValue,
				CheckForProtectiveCloth = checkForProtectiveCloth,
				StunPlayer = stunPlayer,
				StunDuration = stunDuration
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}