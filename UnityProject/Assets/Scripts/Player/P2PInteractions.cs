using UnityEngine;


/// <summary>
/// Player 2 Player interactions. Also used for clicking on yourself
/// </summary>
public class P2PInteractions : InputTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (UIManager.Hands.CurrentSlot.Item != null)
		{
			//Is the item edible?
			if (CheckEdible(UIManager.Hands.CurrentSlot.Item))
			{
				return true;
			}

			//Is it a weapon/ballistic or specific hand to hand melee combat with weapons?
			if (CheckWeapon(UIManager.Hands.CurrentSlot.Item, false))
			{
				return true;
			}
		}

		return true;
	}

	public override bool DragInteract(GameObject originator, Vector3 position, string hand)
	{
		if (UIManager.Hands.CurrentSlot.Item != null)
		{
			//while dragging, can still be firing an automatic
			if (CheckWeapon(UIManager.Hands.CurrentSlot.Item, true))
			{
				return true;
			}
		}

		return false;
	}

	private bool CheckEdible(GameObject itemInHand)
	{
		FoodBehaviour baseFood = itemInHand.GetComponent<FoodBehaviour>();
		if (baseFood == null || UIManager.CurrentIntent == Intent.Harm)
		{
			return false;
		}

		if (PlayerManager.LocalPlayer == gameObject)
		{
			//Clicked on yourself, try to eat the food
			baseFood.TryEat();
		}
		else
		{
			//Clicked on someone else
			//TODO create a new method on FoodBehaviour for feeding others
			//and use that here
		}
		return true;
	}

	private bool CheckWeapon(GameObject itemInHand, bool isDrag)
	{
		Weapon weapon = itemInHand.GetComponent<Weapon>();
		if (weapon == null)
		{
			return false;
		}

		if (PlayerManager.LocalPlayer == gameObject)
		{
			//suicide
			return weapon.AttemptSuicideShot(isDrag);
		}
		else
		{
			return weapon.Trigger();
		}
	}
}
