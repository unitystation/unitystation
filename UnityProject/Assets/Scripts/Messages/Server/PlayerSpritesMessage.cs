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
		yield return null;
	}

	public static PlayerSpritesMessage SendToAll(GameObject equipmentObject, int index, int reference, Color color)
	{
		PlayerSpritesMessage msg = new PlayerSpritesMessage
		{
			Index = index,
			Reference = reference,
			NewColor = color,
			EquipmentObject = equipmentObject.NetId()
		};
		msg.SendToAll();
		return msg;
	}
}
