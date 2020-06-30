using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
/// This component will handle adding and removing of speech modifiers
/// when the item is wore.
///</summary>

public class WearableSpeechMod : MonoBehaviour, IServerInventoryMove
{
	[SerializeField]
	[Tooltip("What modifier/s this item adds")]
	private ChatModifier modifier = ChatModifier.UwU;

	[Tooltip("Where in inventory should this item activate its modifier?")]
	[SerializeField]
	private NamedSlot slot = NamedSlot.head;


	public void OnInventoryMoveServer(InventoryMove info)
	{
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
			var mind = info.ToRootPlayer.PlayerScript.mind;
			if(mind != null & info.ToSlot.NamedSlot == slot)
			{
				mind.inventorySpeechModifiers |= modifier;
			}
		}
		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
			var mind = info.FromPlayer.PlayerScript.mind;
			if(mind != null & info.FromSlot.NamedSlot == slot)
			{
				mind.inventorySpeechModifiers &= ~modifier;
			}
		}
	}
}
