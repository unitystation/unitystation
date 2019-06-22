using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSpritesMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.PlayerSpritesMessage;
	public int Reference;
	public int Index;
	public Color NewColor;
	public NetworkInstanceId EquipmentObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(EquipmentObject);
		if (NetworkObject != null)
		{
			var sprite = NetworkObject.GetComponent<PlayerSprites>().characterSprites[Index];
			sprite.SetReference(Reference);
			if(NewColor != null)
			{
				sprite.SetColor(NewColor);
			}
		}
	}

	public static PlayerSpritesMessage SendToAll(GameObject equipmentObject, int index, int reference, Color color)
	{
		var msg = CreateMsg(equipmentObject, index, reference, color);
		msg.SendToAll();
		return msg;
	}

	public static PlayerSpritesMessage SendTo(GameObject equipmentObject, int index, int reference, Color color, GameObject recipient)
	{
		var msg = CreateMsg(equipmentObject, index, reference, color);
		msg.SendTo(recipient);
		return msg;
	}

	public static PlayerSpritesMessage CreateMsg(GameObject equipmentObject, int index, int reference, Color color)
	{
		return new PlayerSpritesMessage
		{
			Index = index,
			Reference = reference,
			NewColor = color,
			EquipmentObject = equipmentObject.NetId()
		};
	}

}
