using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using UI;
using InputControl;

namespace Weapons
{
	public class Weapon_Ballistic : InputTrigger
	{
		private bool isInHandR = false;
		private bool isInHandL = false;
		public string ammoType;
		[Header("0 = fastest")]
		public float firingRate;
		private float nextFire; //shooting cooldown
		public int bulletPerShoot; //for shotguns/splitting bullets(?)

		public GameObject bullet;
		private MagazineBehaviour currentMagazine;

		[SyncVar(hook="LoadUnloadAmmo")]
		public NetworkInstanceId magNetID;

		[SyncVar]
		public string controlledByPlayer;

		void Start()
		{
			if (ammoType == null)
				ammoType = "12mm"; //DEFAULT VALUE
			if (bullet == null)
				bullet = Resources.Load("Bullet_12mm") as GameObject;

			//future?
			//bullet = Resources.Load("Bullet_"+ammoType) as GameObject; 
		}

		public override void OnStartServer()
		{
			GameObject m = GameObject.Instantiate(Resources.Load("Magazine_"+ammoType) as GameObject, Vector3.zero, Quaternion.identity);
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
				currentMagazine = null;
			} else {
				GameObject m = ClientScene.FindLocalObject(mID);
				if (m != null) {
					MagazineBehaviour mB = m.GetComponent<MagazineBehaviour>();
					currentMagazine = mB;
				} else {
					Debug.Log("Could not find MagazineBehaviour");
				}
			}
		}

		public override void Interact() {
			//shoot gun interation if its in hand
			if (gameObject == UIManager.Hands.CurrentSlot.GameObject ()) {
				if (Time.time > nextFire && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
					Shoot();
				}
			} 
			//if its not in hands, pick it up
			else {
				PlayerManager.LocalPlayerScript.playerNetworkActions.TryToPickUpObject(gameObject);
			}
		}

		void Shoot()
		{			
			nextFire = Time.time + firingRate;
			if ((isInHandR && UIManager.Hands.CurrentSlot == UIManager.Hands.RightSlot) ^ (isInHandL && UIManager.Hands.CurrentSlot == UIManager.Hands.LeftSlot)) {
				if (currentMagazine == null) {
					PlayEmptySFX();
					return;
				}

				if (currentMagazine.Usable) {
					//basic way to check with a XOR if the hand and the slot used matches
					Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;

					PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdShootBullet (dir, bullet.name);
					currentMagazine.ammoRemains--;
				} else {
					if (currentMagazine != null) {
						ManualUnload (currentMagazine);
						OutOfAmmoSFX ();
					}
				} 
			}
		}

		void Reload(GameObject m, string hand){
			Debug.Log ("Reloading");
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, m);
			UIManager.Hands.CurrentSlot.Clear();
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdClearUISlot(hand);
		}

		//atm unload with shortcut 'e'
		//TODO dev right click unloading so it goes into the opposite hand if it is selected
		void ManualUnload(MagazineBehaviour m){
			Debug.Log ("Unloading");
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemNotInUISlot(m.gameObject);
		}

		//Check which slot it was just added too (broadcast from UI_itemSlot
		public void OnAddToInventory(string slotName)
		{
			//This checks to see if a new player who has joined needs to load up any weapon magazines because of missing sync hooks
			if (magNetID != NetworkInstanceId.Invalid) {
				LoadUnloadAmmo(magNetID);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryAddToEquipmentPool(currentMagazine.gameObject);
			}
			if (slotName == "rightHand") {
				isInHandR = true;
			} else if (slotName == "leftHand") {
				isInHandL = true;
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
			if (currentMagazine != null && PlayerManager.LocalPlayer.name == playerName) {
				//As the magazine loaded is part of the weapon, then we do not need to add to server cache, we only need to add the item to the equipment pool
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryAddToEquipmentPool(currentMagazine.gameObject);
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
		}

		void OutOfAmmoSFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("OutOfAmmoAlarm");
		}

		void PlayEmptySFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("EmptyGunClick");
		}
	}
}
