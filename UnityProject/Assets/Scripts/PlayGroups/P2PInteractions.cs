﻿using UnityEngine;


/// <summary>
	/// Player 2 Player interactions. Also used for clicking on yourself
	/// </summary>
	public class P2PInteractions : InputTrigger
	{
		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			if(UIManager.Hands.CurrentSlot.Item != null){
				//Is the item edible?
				if(CheckEdible(UIManager.Hands.CurrentSlot.Item)){
					return;
				}

				//Is it a weapon/ballistic or specific hand to hand melee combat with weapons?
				if(CheckWeapon(UIManager.Hands.CurrentSlot.Item)){
					return;
				}
			}
		}

		private bool CheckEdible(GameObject itemInHand)
		{
			FoodBehaviour baseFood = itemInHand.GetComponent<FoodBehaviour>();
			if (baseFood == null || UIManager.CurrentIntent == Intent.Attack){
				return false;
			} 

			if(PlayerManager.LocalPlayer == gameObject){
				//Clicked on yourself, try to eat the food
				baseFood.TryEat();
			} else {
				//Clicked on someone else
				//TODO create a new method on FoodBehaviour for feeding others
				//and use that here
			}
			return true;
		}

		private bool CheckWeapon(GameObject itemInHand){
			Weapon weapon = itemInHand.GetComponent<Weapon>();
			if(weapon == null){
				return false;
			}

			if (PlayerManager.LocalPlayer == gameObject) {
				//You no longer have the desire to live
				weapon.AttemptSuicideShot();
			} else {
				//Someone else
				//TODO any hand to hand specifics or intent based fighting here
				weapon.Trigger();
			}
			return true;
		}
	}
