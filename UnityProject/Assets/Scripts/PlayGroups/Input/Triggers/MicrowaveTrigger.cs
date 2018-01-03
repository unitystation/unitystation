using System.Collections.Generic;
using Crafting;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class MicrowaveTrigger : InputTrigger
{
	private Microwave microwave;

	private void Start()
	{
		microwave = GetComponent<Microwave>();
	}

	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!isServer)
		{
			UI_ItemSlot slot = UIManager.Hands.CurrentSlot;

			// Client pre-approval
			if (!microwave.Cooking && slot.CanPlaceItem())
			{
				//Client informs server of interaction attempt
				InteractMessage.Send(gameObject, position, slot.eventName);
			}
		}
		else
		{
			ValidateMicrowaveInteraction(originator, position, hand);
		}
	}

	[Server]
	private bool ValidateMicrowaveInteraction(GameObject originator, Vector3 position, string hand)
	{
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (ps.canNotInteract() || !ps.IsInReach(position))
		{
			return false;
		}

		GameObject item = ps.playerNetworkActions.Inventory[hand];
		if (item == null)
		{
			return false;
		}
		ItemAttributes attr = item.GetComponent<ItemAttributes>();

		Ingredient ingredient = new Ingredient(attr.itemName);

		GameObject meal = CraftingManager.Meals.FindRecipe(new List<Ingredient> {ingredient});

		if (meal)
		{
			ps.playerNetworkActions.CmdStartMicrowave(hand, gameObject, meal.name);
			item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		}


		return true;
	}
}