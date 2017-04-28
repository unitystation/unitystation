﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using UI;

namespace Weapons
{
	public class Weapon_Ballistic : NetworkBehaviour
	{
		public bool isInHandR = false;
		public bool isInHandL = false;
		private bool allowedToShoot = false;
		public bool isMagazineIn = true;
		private GameObject bullet;

		[Header("0 = fastest")]
		public float firingRate = 1f;

		private MagazineBehaviour Magazine;

		[SyncVar(hook="LoadUnloadAmmo")]
		public NetworkInstanceId magNetID;

		[SyncVar]
		public string controlledByPlayer;

		void Start()
		{
			bullet = Resources.Load("Bullet_12mm") as GameObject;
		}

		public override void OnStartServer()
		{
			GameObject m = GameObject.Instantiate(Resources.Load("Magazine_12mm") as GameObject, Vector3.zero, Quaternion.identity);
			NetworkServer.Spawn(m);
			StartCoroutine(SetMagazineOnStart(m));
			base.OnStartServer();
		}

		//Gives it a chance for weaponNetworkActions to init
		IEnumerator SetMagazineOnStart(GameObject magazine){
			yield return new WaitForEndOfFrame();
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, magazine);
		}

		public void LoadUnloadAmmo(NetworkInstanceId mID){
			if (mID == NetworkInstanceId.Invalid) {
				Magazine = null;
			} else {
				GameObject m = ClientScene.FindLocalObject(mID);
				if (m != null) {
					MagazineBehaviour mB = m.GetComponent<MagazineBehaviour>();
					Magazine = mB;
				} else {
					Debug.LogError("Could not find MagazineBehaviour");
				}
			}
		}

		void Update()
		{
			if (Input.GetMouseButtonDown(0) && allowedToShoot) {
				ShootingFun();
			}
				
			if(Input.GetKeyDown(KeyCode.E)) { //PlaceHolder for click UI
				GameObject currentHandItem = UIManager.Hands.CurrentSlot.Item; 
				GameObject otherHandItem = UIManager.Hands.OtherSlot.Item;
				string hand;

				if (currentHandItem != null) {
					if (isMagazineIn == false) { //RELOAD
						MagazineBehaviour magazine = currentHandItem.GetComponent<MagazineBehaviour>();

						if (magazine != null && otherHandItem.GetComponent<Weapon_Ballistic>() != null) {
							hand = UIManager.Hands.CurrentSlot.eventName;
							Reload(currentHandItem, hand);
						}
					} else { //UNLOAD
						Weapon_Ballistic weapon = currentHandItem.GetComponent<Weapon_Ballistic>();

						if (weapon != null && otherHandItem == null) {
							hand = UIManager.Hands.OtherSlot.eventName;
							UnloadTo(hand);
						}
					}
				}
			}

		}

		void ShootingFun()
		{
			if ((isInHandR && UIManager.Hands.CurrentSlot == UIManager.Hands.RightSlot) ^ (isInHandL && UIManager.Hands.CurrentSlot == UIManager.Hands.LeftSlot)) {
				if (Magazine == null) {
					if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
						PlayEmptySFX();

					return;
				}
				if (Magazine.Usable) {
					//basic way to check with a XOR if the hand and the slot used matches

					if (PlayerManager.LocalPlayerScript.gameObject.name == controlledByPlayer) {
						Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;

						//don't while hovering on the UI
						if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
							//Shoot(dir);
							if (allowedToShoot) {
								allowedToShoot = false;
								PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdShootBullet (dir, bullet.name);
								StartCoroutine ("ShootCoolDown");
							}
							Magazine.ammoRemains--;
						}
					}

				} else {
					if (isMagazineIn) {
						PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemNotInUISlot(Magazine.gameObject);
						isMagazineIn = false;
						PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
						OutOfAmmoSFX();
					}
				} 
			}
		}

		//Moved into ShootingFun(), feel free to revert
		/*void Shoot(Vector2 shootDir)
		{
			if (allowedToShoot) {
				allowedToShoot = false;
				PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdShootBullet (shootDir, bullet.name);
				StartCoroutine ("ShootCoolDown");
			}
		}*/
			
		void Reload(GameObject m, string hand){
				Debug.Log ("Reloading");
				isMagazineIn = true;
				
				LoadUnloadAmmo(magNetID);
				PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, m);
				UIManager.Hands.CurrentSlot.Clear();
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdClearUISlot(hand);
		}

		void UnloadTo(string hand){
			Debug.Log ("Unloading");
			isMagazineIn = false;
			GameObject m = ClientScene.FindLocalObject(magNetID);

			LoadUnloadAmmo(magNetID);
			PlayerManager.LocalPlayerScript.playerNetworkActions.TrySetItem(hand,m);
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
		}

		//Check which slot it was just added too (broadcast from UI_itemSlot
		public void OnAddToInventory(string slotName)
		{
			//This checks to see if a new player who has joined needs to load up any weapon magazines because of missing sync hooks
			if (magNetID != NetworkInstanceId.Invalid && !magNetID.IsEmpty()) {
				LoadUnloadAmmo(magNetID);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryAddToEquipmentPool(Magazine.gameObject);
			}
			if (slotName == "rightHand") {
				isInHandR = true;
				StartCoroutine("ShootCoolDown");
			} else if (slotName == "leftHand") {
				isInHandL = true;
				StartCoroutine("ShootCoolDown");
			} else {
				//Any other slot
				isInHandR = false;
				isInHandL = false;
			}
		}

		//This is only called on the serverside
		public void OnAddToPool(string playerName)
		{
			controlledByPlayer = playerName;
			if (Magazine != null && PlayerManager.LocalPlayer.name == playerName) {
				//As the magazine loaded is part of the weapon, then we do not need to add to server cache, we only need to add the item to the equipment pool
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryAddToEquipmentPool(Magazine.gameObject);
			}
		}

		public void OnRemoveFromPool()
		{
			controlledByPlayer = "";
		}

		//recieve broadcast msg when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			Debug.Log("Dropped Weapon");
			isInHandR = false;
			isInHandL = false;
			allowedToShoot = false;
		}

		void OutOfAmmoSFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("OutOfAmmoAlarm");
		}

		void PlayEmptySFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("EmptyGunClick");
		}

		IEnumerator ShootCoolDown()
		{
			yield return new WaitForSeconds(firingRate);
			allowedToShoot = true;

		}

	}
}
