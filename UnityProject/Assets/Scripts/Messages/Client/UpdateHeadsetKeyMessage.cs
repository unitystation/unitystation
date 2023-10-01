using Mirror;
using UnityEngine;
using Items;
using Logs;

namespace Messages.Client
{
	/// <summary>
	///     Requests a Headset Encryption Key update
	/// </summary>
	public class UpdateHeadsetKeyMessage : ClientMessage<UpdateHeadsetKeyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EncryptionKey;
			public uint HeadsetItem;
		}

		public override void Process(NetMessage msg)
		{
			if ( msg.HeadsetItem.Equals(NetId.Invalid) )
			{
				//Failfast

				Loggy.LogWarning($"Headset invalid, processing stopped: {ToString()}",Category.Chat);
				return;
			}

			if ( msg.EncryptionKey.Equals(NetId.Invalid) )
			{
				//No key passed in message -> Removes EncryptionKey from a headset
				LoadNetworkObject(msg.HeadsetItem);

				var player = SentByPlayer;
				var headsetGO = NetworkObject;
				if ( ValidRemoval(headsetGO) )
				{
					detachKey(headsetGO, player);
				}
			}
			else
			{
				//Key was passed -> Puts it into headset
				LoadMultipleObjects(new uint[] {msg.HeadsetItem, msg.EncryptionKey});

				var player = SentByPlayer;
				var headsetGO = NetworkObjects[0];
				var keyGO = NetworkObjects[1];
				if ( ValidUpdate(headsetGO, keyGO) )
				{
					setKey(player, headsetGO, keyGO);
				}
			}
		}

		private static void setKey(PlayerInfo player, GameObject headsetGO, GameObject keyGO)
		{
			var pna = player.Script.PlayerNetworkActions;
			if ( pna.HasItem(keyGO) )
			{
				Headset headset = headsetGO.GetComponent<Headset>();
				EncryptionKey encryptionkey = keyGO.GetComponent<EncryptionKey>();
				headset.EncryptionKey = encryptionkey.Type;
				Inventory.ServerDespawn(keyGO.GetComponent<Pickupable>().ItemSlot);
			}
		}

		private static void detachKey(GameObject headsetGO, PlayerInfo player)
		{
			Headset headset = headsetGO.GetComponent<Headset>();
			var encryptionKey =
				Spawn.ServerPrefab(CustomNetworkManager.Instance.GetSpawnablePrefabFromName("EncryptionKey"),
					player.Script.WorldPos, player.GameObject.transform.parent);

			if (encryptionKey.Successful == false)
			{
				Loggy.LogError($"Headset key instantiation for {player.Name} failed, spawn aborted",Category.Chat);
				return;
			}

			encryptionKey.GameObject.GetComponent<EncryptionKey>().Type = headset.EncryptionKey;
			headset.EncryptionKey = EncryptionKeyType.None;

			var emptyHand = player.Script.DynamicItemStorage.GetBestHand();
			if (emptyHand != null)
			{
				Inventory.ServerAdd(encryptionKey.GameObject, emptyHand);
			}
		}

		public static NetMessage Send(GameObject headsetItem, GameObject encryptionkey = null)
		{
			NetMessage msg = new NetMessage
			{
				HeadsetItem = headsetItem ? headsetItem.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				EncryptionKey = encryptionkey ? encryptionkey.GetComponent<NetworkIdentity>().netId : NetId.Invalid
			};

			Send(msg);
			return msg;
		}

		private bool ValidUpdate(GameObject headset, GameObject encryptionkey)
		{
			EncryptionKeyType encryptionKeyTypeOfHeadset = headset.GetComponent<Headset>().EncryptionKey;
			EncryptionKeyType encryptionKeyTypeOfKey = encryptionkey.GetComponent<EncryptionKey>().Type;
			if ( encryptionKeyTypeOfHeadset != EncryptionKeyType.None || encryptionKeyTypeOfKey == EncryptionKeyType.None )
			{
//			Logger.LogWarning($"Failed to validate update of {headset.name} {encryptionkey.name} ({ToString()})");
				return false;
			}

			return true;
		}


		private bool ValidRemoval(GameObject headset)
		{
			EncryptionKeyType encryptionKeyType = headset.GetComponent<Headset>().EncryptionKey;
			if ( encryptionKeyType == EncryptionKeyType.None )
			{
//			Logger.LogWarning($"Failed to validate removal of encryption key from {headset.name} ({ToString()})");
				return false;
			}

			return true;
		}
	}
}
