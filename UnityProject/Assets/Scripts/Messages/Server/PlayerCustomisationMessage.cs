using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class PlayerCustomisationMessage : ServerMessage<PlayerCustomisationMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public CharacterSettings Character;
			public BodyPartSpriteName Part;
			public uint EquipmentObject;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EquipmentObject);
			if (NetworkObject != null)
			{
				NetworkObject.GetComponent<PlayerSprites>().SetupCharacterData(msg.Character);
			}
		}

		public static NetMessage SendToAll(GameObject equipmentObject,  CharacterSettings Character =  null)
		{
			var msg = CreateMsg(equipmentObject,Character);

			SendToAll(msg);
			return msg;
		}

		public static NetMessage SendTo(GameObject equipmentObject,  NetworkConnection recipient, CharacterSettings Character = null)
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
				Part = BodyPartSpriteName.Null,
				Character = Character
			};
		}

	}
}
