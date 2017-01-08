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
            if (isInHand && Input.GetMouseButtonDown(0))
            {
                Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;
                Debug.Log("Shoot dir: " + dir);

                float shootAngle = Angle(dir);
                //change the facingDirection of player
                CheckPlayerDirection(shootAngle);

                Shoot(dir);
            }
        }

        void Shoot(Vector2 shootDir)
        {
            if (allowedToShoot)
            {
                allowedToShoot = false;

                //TODO Do shooting stuff here
                //TODO shoot a bullet in the dir
                //TODO dispense a casing shell
                //TODO remove a bullet from ammo class
               


                //sound
                shootSFX.transform.position = PlayerManager.LocalPlayer.transform.position;
                shootSFX.Play();
                StartCoroutine("ShootCoolDown");
            }
        }

        //Check which slot it was just added too (broadcast from UI_itemSlot
        public void OnAddToInventory(string slotName)
        {
            if (slotName == "rightHand" || slotName == "leftHand")
            {
                Debug.Log("PickedUp Weapon");
                isInHand = true;
                StartCoroutine("ShootCoolDown");
            }
            else
            {
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

        //Set player facing in direction of shot
        void CheckPlayerDirection(float angle)
        {
            if (angle >= 270f && angle <= 360f || angle >= 0f && angle <= 45f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.up);
            }

            if (angle > 45f && angle <= 135f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.right);
            }

            if (angle > 135f && angle <= 225f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.down);
            }

            if (angle > 225f && angle < 270f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.left);
            }

        }

        //Calculate the shot direction (for facingDirection on PlayerSprites)
        float Angle(Vector2 dir)
        {
            if (dir.x < 0)
            {
                return 360 - (Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            }
        }

        IEnumerator ShootCoolDown()
        {
            yield return new WaitForSeconds(firingRate);
            allowedToShoot = true;

        }
    }
}
