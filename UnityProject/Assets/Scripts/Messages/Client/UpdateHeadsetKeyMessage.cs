using System.Collections;
using Messages.Client;
using UnityEngine;
using Mirror;

/// <summary>
///     Requests a Headset Encryption Key update
/// </summary>
public class UpdateHeadsetKeyMessage : ClientMessage
{
	public uint EncryptionKey;
	public uint HeadsetItem;

	public override void Process()
	{
		if ( HeadsetItem.Equals(NetId.Invalid) )
		{
			//Failfast

			Logger.LogWarning($"Headset invalid, processing stopped: {ToString()}",Category.Telecoms);
			return;
		}

		if ( EncryptionKey.Equals(NetId.Invalid) )
		{
			//No key passed in message -> Removes EncryptionKey from a headset
			LoadNetworkObject(HeadsetItem);

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
			LoadMultipleObjects(new uint[] {HeadsetItem, EncryptionKey});

			var player = SentByPlayer;
			var headsetGO = NetworkObjects[0];
			var keyGO = NetworkObjects[1];
			if ( ValidUpdate(headsetGO, keyGO) )
			{
				setKey(player, headsetGO, keyGO);
			}
		}
	}

	private static void setKey(ConnectedPlayer player, GameObject headsetGO, GameObject keyGO)
	{
		var pna = player.Script.playerNetworkActions;
		if ( pna.HasItem(keyGO) )
		{
			Headset headset = headsetGO.GetComponent<Headset>();
			EncryptionKey encryptionkey = keyGO.GetComponent<EncryptionKey>();
			headset.EncryptionKey = encryptionkey.Type;
			Inventory.ServerDespawn(keyGO.GetComponent<Pickupable>().ItemSlot);
		}
	}

	private static void detachKey(GameObject headsetGO, ConnectedPlayer player)
	{
		Headset headset = headsetGO.GetComponent<Headset>();
		GameObject encryptionKey =
		Object.Instantiate(Resources.Load("EncryptionKey", typeof( GameObject )),
			headsetGO.transform.position,
			headsetGO.transform.rotation,
			headsetGO.transform.parent) as GameObject;
		if ( encryptionKey == null )
		{
			Logger.LogError($"Headset key instantiation for {player.Name} failed, spawn aborted",Category.Telecoms);
			return;
		}

		encryptionKey.GetComponent<EncryptionKey>().Type = headset.EncryptionKey;
//		Logger.Log($"Spawning headset key {encryptionKey} with type {headset.EncryptionKey}");

		//TODO when added interact with dropped headset, add encryption key to empty hand
		headset.EncryptionKey = EncryptionKeyType.None;

		Spawn.ServerPrefab(encryptionKey, player.Script.WorldPos, player.GameObject.transform.parent);
	}

	public static UpdateHeadsetKeyMessage Send(GameObject headsetItem, GameObject encryptionkey = null)
	{
		UpdateHeadsetKeyMessage msg = new UpdateHeadsetKeyMessage
		{
			HeadsetItem = headsetItem ? headsetItem.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			EncryptionKey = encryptionkey ? encryptionkey.GetComponent<NetworkIdentity>().netId : NetId.Invalid
		};
		msg.Send();

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

	public override string ToString()
	{
		return $"[UpdateHeadsetKeyMessage SentBy={SentByPlayer} HeadsetItem={HeadsetItem} EncryptionKey={EncryptionKey}]";
	}
}
