using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityHUDItem : HUDItemBase
{
	public override bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (player != null && player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player &&
		    pickupable.ItemSlot is
			    {NamedSlot: NamedSlot.eyes}) // Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
		{
			return true;
		}

		return false;
	}

	public override void ApplyEffects(bool State)
	{
		var HudType = typeof(SecurityHUD);
		if (HUDHandler.Categorys.ContainsKey(HudType))
		{
			var Listy = HUDHandler.Categorys[HudType];
			foreach (var HUD in Listy)
			{
				HUD.SetVisible(State);
			}
		}
		HUDHandler.CategoryEnabled[HudType] = State;
	}
}
