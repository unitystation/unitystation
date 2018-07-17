﻿using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Requests a Headset Encryption Key update 
/// </summary>
public class UpdateHeadsetKeyMessage : ClientMessage
{
	public static short MessageType = ( short ) MessageTypes.UpdateHeadsetKeyMessage;
	public NetworkInstanceId EncryptionKey;
	public NetworkInstanceId HeadsetItem;

	public override IEnumerator Process()
	{
		if ( HeadsetItem.Equals(NetworkInstanceId.Invalid) )
		{
			//Failfast

			Logger.LogWarning($"Headset invalid, processing stopped: {ToString()}",Categories.Telecommunications);
			yield break;
		}

		if ( EncryptionKey.Equals(NetworkInstanceId.Invalid) )
		{
			//No key passed in message -> Removes EncryptionKey from a headset
			yield return WaitFor(SentBy, HeadsetItem);
			var player = NetworkObjects[0];
			var headsetGO = NetworkObjects[1];
			if ( ValidRemoval(headsetGO) )
			{
				detachKey(headsetGO, player);
			}
		}
		else
		{
			//Key was passed -> Puts it into headset
			yield return WaitFor(SentBy, HeadsetItem, EncryptionKey);
			var player = NetworkObjects[0];
			var headsetGO = NetworkObjects[1];
			var keyGO = NetworkObjects[2];
			if ( ValidUpdate(headsetGO, keyGO) )
			{
				setKey(player, headsetGO, keyGO);
			}
		}
	}

	private static void setKey(GameObject player, GameObject headsetGO, GameObject keyGO)
	{
		var pna = player.GetComponent<PlayerNetworkActions>();
		if ( pna.HasItem(keyGO) )
		{
			Headset headset = headsetGO.GetComponent<Headset>();
			EncryptionKey encryptionkey = keyGO.GetComponent<EncryptionKey>();
			headset.EncryptionKey = encryptionkey.Type;
			pna.Consume(keyGO);
		}
	}

	private static void detachKey(GameObject headsetGO, GameObject player)
	{
		Headset headset = headsetGO.GetComponent<Headset>();
		GameObject encryptionKey =
		Object.Instantiate(Resources.Load("EncryptionKey", typeof( GameObject )), 
			headsetGO.transform.position, 
			headsetGO.transform.rotation, 
			headsetGO.transform.parent) as GameObject;
		if ( encryptionKey == null )
		{
			Logger.LogError($"Headset key instantiation for {PlayerList.Instance.Get(player).Name} failed, spawn aborted",Categories.Telecommunications);
			return;
		}

		encryptionKey.GetComponent<EncryptionKey>().Type = headset.EncryptionKey;
//		Logger.Log($"Spawning headset key {encryptionKey} with type {headset.EncryptionKey}");
		
		//TODO when added interact with dropped headset, add encryption key to empty hand
		headset.EncryptionKey = EncryptionKeyType.None;
		
		ItemFactory.SpawnItem(encryptionKey, player.transform.position, player.transform.parent);
	}

	public static UpdateHeadsetKeyMessage Send(GameObject headsetItem, GameObject encryptionkey = null)
	{
		UpdateHeadsetKeyMessage msg = new UpdateHeadsetKeyMessage
		{
			HeadsetItem = headsetItem ? headsetItem.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			EncryptionKey = encryptionkey ? encryptionkey.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid
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
		return $"[UpdateHeadsetKeyMessage SentBy={SentBy} HeadsetItem={HeadsetItem} EncryptionKey={EncryptionKey}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		HeadsetItem = reader.ReadNetworkId();
		EncryptionKey = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(HeadsetItem);
		writer.Write(EncryptionKey);
	}
}