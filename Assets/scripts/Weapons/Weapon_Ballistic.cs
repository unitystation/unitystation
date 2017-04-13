using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

namespace Weapons
{
	public class Weapon_Ballistic : NetworkBehaviour
	{
		public bool isInHand = false;
		private bool allowedToShoot = false;
        private GameObject bullet;

		[Header("0 = fastest")]
		public float firingRate = 1f;

		public AudioSource shootSFX;
		public AudioSource emptySFX;

        [SyncVar]
        public string controlledByPlayer;

        void Start(){
            bullet = Resources.Load("Bullet_12mm") as GameObject;
        }
		void Update()
		{
            if (isInHand && Input.GetMouseButtonDown(0))
            {
                if (PlayerManager.LocalPlayerScript.gameObject.name == controlledByPlayer)
                {
                    Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;
                    Shoot(dir);
                }
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
			if (slotName == "rightHand" || slotName == "leftHand") {
				Debug.Log("PickedUp Weapon");
				isInHand = true;
				StartCoroutine("ShootCoolDown");
			} else {
				//Any other slot
				isInHand = false;
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
