using UnityEngine;


/// <summary>
/// Player 2 Player interactions. Also used for clicking on yourself
/// </summary>
public class P2PInteractions : InputTrigger {
	public override void Interact(GameObject originator, Vector3 position, string hand) {
		if (UIManager.Hands.CurrentSlot.Item != null) {
			//Is the item edible?
			if (CheckEdible(UIManager.Hands.CurrentSlot.Item)) {
				return;
			}

			//Is it a weapon/ballistic or specific hand to hand melee combat with weapons?
			if (CheckWeapon(UIManager.Hands.CurrentSlot.Item, false)) {
				return;
			}
		}
	}

	public override void DragInteract(GameObject originator, Vector3 position, string hand) {
		if (UIManager.Hands.CurrentSlot.Item != null) {
			//while dragging, can still be firing an automatic
			if (CheckWeapon(UIManager.Hands.CurrentSlot.Item, true)) {
				return;
			}
		}
	}

	private bool CheckEdible(GameObject itemInHand) {
		FoodBehaviour baseFood = itemInHand.GetComponent<FoodBehaviour>();
		if (baseFood == null || UIManager.CurrentIntent == Intent.Attack) {
			return false;
		}

		if (PlayerManager.LocalPlayer == gameObject) {
			//Clicked on yourself, try to eat the food
			baseFood.TryEat();
		} else {
			//Clicked on someone else
			//TODO create a new method on FoodBehaviour for feeding others
			//and use that here
		}
		return true;
	}

	private bool CheckWeapon(GameObject itemInHand, bool isDrag) {
		Weapon weapon = itemInHand.GetComponent<Weapon>();
		if (weapon == null) {
			return false;
		}

		if (PlayerManager.LocalPlayer == gameObject) {
			//suicide
			weapon.AttemptSuicideShot();
		} else {
			weapon.Trigger();
		}
		
		return true;
	}
}
