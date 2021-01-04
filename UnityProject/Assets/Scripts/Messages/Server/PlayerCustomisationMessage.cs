using System.Collections;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class PlayerCustomisationMessage : ServerMessage
{
	//Weaver is a steaming pile of Garbo
	public string Character;
	public BodyPartSpriteName Part = BodyPartSpriteName.Null;
	public uint EquipmentObject;

	public override void Process()
	{
		CharacterSettings characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(Character);
		LoadNetworkObject(EquipmentObject);
		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<PlayerSprites>().SetupCharacterData(characterSettings);
		}
	}

	public static PlayerCustomisationMessage SendToAll(GameObject equipmentObject,  CharacterSettings Character =  null)
	{
		var msg = CreateMsg(equipmentObject,Character);
		msg.SendToAll();
		return msg;
	}

	public static PlayerCustomisationMessage SendTo(GameObject equipmentObject,  NetworkConnection recipient, CharacterSettings Character = null)
	{
		var msg = CreateMsg(equipmentObject, Character);
		msg.SendTo(recipient);
		return msg;
	}

	public static PlayerCustomisationMessage CreateMsg(GameObject equipmentObject, CharacterSettings Character = null)
	{
		return new PlayerCustomisationMessage
		{
			EquipmentObject = equipmentObject.NetId(),
			Character = JsonConvert.SerializeObject(Character)
		};
	}

}
