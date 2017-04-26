using System.Collections;
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

		public AudioSource shootSFX;
		public AudioSource emptySFX;

		public MagazineBehaviour Magazine;

        [SyncVar]
        public string controlledByPlayer;

		void Start()
		{
			bullet = Resources.Load("Bullet_12mm") as GameObject;
			//GameObject m = (GameObject) GameObject.Instantiate(Resources.Load("Magazine_12mm"), Vector3.zero, Quaternion.identity);
			//Magazine = m.GetComponent<MagazineBehaviour>();
		}

		public override void OnStartServer()
		{
			if (isServer) {
				GameObject m = (GameObject) GameObject.Instantiate(Resources.Load("Magazine_12mm"), Vector3.zero, Quaternion.identity);
				Magazine = m.GetComponent<MagazineBehaviour>();
				NetworkServer.Spawn(m); 
			}
			base.OnStartServer();
		}

		void Update()
		{
			if (Input.GetMouseButtonDown(0) && allowedToShoot) {
				ShootingFun();
			}
		}

		void ShootingFun(){
			if (Magazine.Usable){
				//basic way to check with a XOR if the hand and the slot used matches
				if ((isInHandR && UIManager.Hands.CurrentSlot == UIManager.Hands.RightSlot) ^ (isInHandL && UIManager.Hands.CurrentSlot == UIManager.Hands.LeftSlot)) {
					if (PlayerManager.LocalPlayerScript.gameObject.name == controlledByPlayer) {
						Vector2 dir = (Camera.main.ScreenToWorldPoint (Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;

						//don't while hovering on the UI
						if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject ()) {
							Shoot (dir);
							Magazine.ammoRemains--;
						}
					}
				}
			} else {
				if(isMagazineIn) {
					//spawn new magazine(obj or sprite?) under the player
					Magazine.transform.Translate(PlayerManager.LocalPlayerScript.transform.position);
					isMagazineIn = false;
				}
				emptySFX.Play();
			} 
		}

		void Shoot(Vector2 shootDir)
		{
			if (allowedToShoot) {
				allowedToShoot = false;
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdShootBullet(shootDir, bullet.name);
              
				StartCoroutine("ShootCoolDown");
			}
		}

		//Check which slot it was just added too (broadcast from UI_itemSlot
		public void OnAddToInventory(string slotName)
		{
			if (slotName == "rightHand") {
				Debug.Log ("PickedUp Weapon");
				isInHandR = true;
				StartCoroutine ("ShootCoolDown");
			} else if (slotName == "leftHand") {
				Debug.Log ("PickedUp Weapon");
				isInHandL = true;
				StartCoroutine ("ShootCoolDown");
			} else {
				//Any other slot
				isInHandR = false;
				isInHandL = false;
			}
		}

        public void OnAddToPool(string playerName){
            controlledByPlayer = playerName;
        }

        public void OnRemoveFromPool(){
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

		IEnumerator ShootCoolDown()
		{
			yield return new WaitForSeconds(firingRate);
			allowedToShoot = true;

		}
			
	}
}
