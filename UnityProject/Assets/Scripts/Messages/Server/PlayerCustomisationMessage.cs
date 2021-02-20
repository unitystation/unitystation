using System.Collections;
using UnityEngine;
using Mirror;

public class PlayerCustomisationMessage : ServerMessage
{
	public class PlayerCustomisationMessageNetMessage : ActualMessage
	{
		public CharacterSettings Character;
		public BodyPartSpriteName Part = BodyPartSpriteName.Null;
		public uint EquipmentObject;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as PlayerCustomisationMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.EquipmentObject);
		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<PlayerSprites>().SetupCharacterData(newMsg.Character);
		}
	}

	public static PlayerCustomisationMessageNetMessage SendToAll(GameObject equipmentObject,  CharacterSettings Character =  null)
	{
		var msg = CreateMsg(equipmentObject,Character);
		new PlayerCustomisationMessage().SendToAll(msg);
		return msg;
	}

	public static PlayerCustomisationMessageNetMessage SendTo(GameObject equipmentObject,  NetworkConnection recipient, CharacterSettings Character = null)
	{
		var msg = CreateMsg(equipmentObject, Character);
		new PlayerCustomisationMessage().SendTo(recipient, msg);
		return msg;
	}

	public static PlayerCustomisationMessageNetMessage CreateMsg(GameObject equipmentObject, CharacterSettings Character = null)
	{
		return new PlayerCustomisationMessageNetMessage
		{
			EquipmentObject = equipmentObject.NetId(),
			Character = Character
		};
	}

}
