using System.Collections;
using Messages.Server;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class PlayerCustomisationMessage : ServerMessage<PlayerCustomisationMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		//Weaver is a steaming pile of Garbo
		public string Character;
		public uint EquipmentObject;
	}

	public override void Process(NetMessage msg)
	{
		if (CustomNetworkManager.Instance._isServer == false)
		{
			CharacterSettings characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(msg.Character);
			LoadNetworkObject(msg.EquipmentObject);
			if (NetworkObject != null)
			{
				NetworkObject.GetComponent<PlayerSprites>().SetupCharacterData(characterSettings);
			}
		}
	}

	public static NetMessage SendToAll(GameObject equipmentObject, CharacterSettings Character = null)
	{
		var msg = CreateMsg(equipmentObject, Character);
		SendToAll(msg);
		return msg;
	}

	public static NetMessage SendTo(GameObject equipmentObject, NetworkConnection recipient,
		CharacterSettings Character = null)
	{
		var msg = CreateMsg(equipmentObject, Character);
		SendTo(recipient, msg);
		return msg;
	}

	public static NetMessage CreateMsg(GameObject equipmentObject, CharacterSettings Character = null)
	{
		return new NetMessage
		{
			EquipmentObject = equipmentObject.NetId(),
			Character = JsonConvert.SerializeObject(Character)
		};
	}
}