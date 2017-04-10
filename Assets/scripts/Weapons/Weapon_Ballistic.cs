using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

namespace Weapons
{
	public class Weapon_Ballistic : MonoBehaviour
	{
		private bool isInHand = false;
		private bool allowedToShoot = false;

		[Header("0 = fastest")]
		public float firingRate = 1f;

		public AudioSource shootSFX;
		public AudioSource emptySFX;


		void Update()
		{
			if (isInHand && Input.GetMouseButtonDown(0)) {
				Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;
				Shoot(dir);
			}
		}

		void Shoot(Vector2 shootDir)
		{
			if (allowedToShoot) {
				allowedToShoot = false;

			  //TODO SHOOT STUFF
				Debug.Log("TODO: Shoot Stuff");

				StartCoroutine("ShootCoolDown");
			}
		}


		void ShootWeapon()
		{
			//TODO Do shooting stuff here
			//TODO shoot a bullet in the dir
			//TODO dispense a casing shell
			//TODO remove a bullet from ammo class
			//sound
			shootSFX.transform.position = PlayerManager.LocalPlayer.transform.position;
			shootSFX.Play();
		}

		//Check which slot it was just added too (broadcast from UI_itemSlot
		public void OnAddToInventory(string slotName)
		{
			if (slotName == "rightHand" || slotName == "leftHand") {
				Debug.Log("PickedUp Weapon");
				isInHand = true;
				StartCoroutine("ShootCoolDown");
			} else {
				//Any other slot
				isInHand = false;
			}
		}

		//recieve broadcast msg when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			Debug.Log("Dropped Weapon");
			isInHand = false;
			allowedToShoot = false;
		}

		IEnumerator ShootCoolDown()
		{
			yield return new WaitForSeconds(firingRate);
			allowedToShoot = true;

		}
			
	}
}
