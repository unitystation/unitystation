using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
/// This component will handle adding and removing of speech modifiers
/// when the item is wore.
///</summary>

public class WearableSpeechMod : MonoBehaviour, IItemInOutMovedPlayer
{
	[SerializeField]
	[Tooltip("What modifier/s this item adds")]
	private ChatModifier modifier = ChatModifier.UwU;

	[Tooltip("Where in inventory should this item activate its modifier?")]
	[SerializeField]
	private NamedSlot slot = NamedSlot.head;

	private Pickupable pickupable;

	public void Awake()
	{
		pickupable = GetComponent<Pickupable>();
	}


	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (pickupable.ItemSlot == null) return false;
		if (pickupable.ItemSlot?.Player != player.PlayerScript.RegisterPlayer) return false;
		if (pickupable.ItemSlot?.NamedSlot != slot) return false;

		return true;
	}

	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer hideForPlayer, RegisterPlayer showForPlayer)
	{
		if (hideForPlayer != null)
		{
			//TODO AAAAAAA Change not to mind
			hideForPlayer.PlayerScript.mind.inventorySpeechModifiers &= ~modifier;
		}

		if (showForPlayer != null)
		{
			//TODO AAAAAAA Change not to mind
			hideForPlayer.PlayerScript.mind.inventorySpeechModifiers |= modifier;
		}
	}
}
