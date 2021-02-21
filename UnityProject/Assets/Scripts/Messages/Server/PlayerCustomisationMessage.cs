using System.Collections;
using UnityEngine;
using Mirror;

public class PlayerCustomisationMessage : ServerMessage
{
	public struct PlayerCustomisationMessageNetMessage : NetworkMessage
	{
		public CharacterSettings Character;
		public BodyPartSpriteName Part;
		public uint EquipmentObject;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public PlayerCustomisationMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PlayerCustomisationMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
			Part = BodyPartSpriteName.Null,
			Character = Character
		};
	}

}
