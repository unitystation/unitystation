using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drink : FoodBehaviour {

	// Use this for initialization
	public override void TryEat()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdEatFood(gameObject,
			UIManager.Hands.CurrentSlot.eventName, true);
	}
}
