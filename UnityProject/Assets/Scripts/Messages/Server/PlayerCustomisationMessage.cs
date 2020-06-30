using System.Collections;
using UnityEngine;
using Mirror;

public class PlayerCustomisationMessage : ServerMessage
{
	public CharacterSettings Character;
	public BodyPartSpriteName Part = BodyPartSpriteName.Null;
	public uint EquipmentObject;

	public override void Process()
	{
		LoadNetworkObject(EquipmentObject);
		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<PlayerSprites>().SetupCharacterData(Character);
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
			Character = Character
		};
	}

}
