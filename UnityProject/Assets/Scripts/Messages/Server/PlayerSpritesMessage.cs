using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSpritesMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.PlayerSpritesMessage;
	public int Reference;
	public string ClothingItem;
	public Color NewColor;
	public NetworkInstanceId EquipmentObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(EquipmentObject);
		if (NetworkObject != null)
		{
			var sprite = NetworkObject.GetComponent<PlayerSprites>().clothes[ClothingItem];
			sprite.SetReference(Reference, null);
			if(NewColor != null)
			{
				sprite.SetColor(NewColor);
			}
		}
	}

	public static PlayerSpritesMessage SendToAll(GameObject equipmentObject, string ClothingItem, int reference, Color color)
	{
		var msg = CreateMsg(equipmentObject, ClothingItem, reference, color);
		msg.SendToAll();
		return msg;
	}

	public static PlayerSpritesMessage SendTo(GameObject equipmentObject, string ClothingItem, int reference, Color color, GameObject recipient)
	{
		var msg = CreateMsg(equipmentObject, ClothingItem, reference, color);
		msg.SendTo(recipient);
		return msg;
	}

	public static PlayerSpritesMessage CreateMsg(GameObject equipmentObject, string ClothingItem, int reference, Color color)
	{
		return new PlayerSpritesMessage
		{
			ClothingItem = ClothingItem,
			Reference = reference,
			NewColor = color,
			EquipmentObject = equipmentObject.NetId()
		};
	}

}
