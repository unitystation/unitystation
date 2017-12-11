using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using UI;
using Items;
using UnityEngine.EventSystems;

namespace Weapons
{
    /// <summary>
    ///  Generic weapon types
    /// </summary>
    public enum WeaponType
    {
        Melee, //TODO: SUPPORT MELEE WEAPONS
        SemiAutomatic,
        FullyAutomatic,
        Burst //TODO: SUPPORT BURST WEAPONS
    };

    /// <summary>
    ///  Generic weapon base
    /// </summary>
    public class Weapon : PickUpTrigger
    {
        /// <summary>
        ///  Current Weapon Type
        /// </summary>
        public WeaponType WeaponType;

        /// <summary>
        ///  The projectile fired from this weapon
        /// </summary>
        public GameObject Projectile;

        /// <summary>
        ///  The damage for this weapon
        /// </summary>
        public int ProjectileDamage;

        /// <summary>
        ///  The traveling speed for this weapons projectile
        /// </summary>
        public int ProjectileVelocity;

        /// <summary>
        /// The amount of times per second this weapon can fire
        /// </summary>
        public double FireRate;

        /// <summary>
        /// The amount of projectiles expended per shot
        /// </summary>
        public int ProjectilesFired;

        /// <summary>
        /// The max recoil angle this weapon can reach with sustained fire
        /// </summary>
        public float MaxRecoilVariance;

        /// <summary>
        /// The the name of the sound this gun makes when shooting
        /// </summary>
        public string FireingSound;

        /// <summary>
        /// The type of ammo this weapon will allow, this is a string and not an enum for diversity
        /// </summary>
        public string AmmoType;

        /// <summary>
        /// The current magazine for this weapon, null means empty
        /// </summary>
        private MagazineBehaviour CurrentMagazine;

        /// <summary>
        /// The countdown untill we can shoot again
        /// </summary>
        [HideInInspector]
        public double FireCountDown;

        /// <summary>
        /// If the weapon is currently in automatic action
        /// </summary>
        [HideInInspector]
        public bool InAutomaticAction;

        /// <summary>
        /// The the current recoil variance this weapon has reached
        /// </summary>
        [SyncVar]
        [HideInInspector]
        public float CurrentRecoilVariance;

        [SyncVar(hook = "LoadUnloadAmmo")]
        public NetworkInstanceId MagNetID;

        [SyncVar]
        public NetworkInstanceId ControlledByPlayer;

        void Start()
        {
            InAutomaticAction = false;
            //init weapon with missing settings
            if (AmmoType == null)
                AmmoType = "12mm";
            if (Projectile == null)
                Projectile = Resources.Load("Bullet_12mm") as GameObject;
        }

        void Update()
        {
            if (ControlledByPlayer == NetworkInstanceId.Invalid)
                return;

            //don't start it too early:
            if (!PlayerManager.LocalPlayer)
                return;

            //Only update if it is inhand of localplayer
            if (PlayerManager.LocalPlayer != ClientScene.FindLocalObject(ControlledByPlayer))
                return;

            if (FireCountDown > 0)
            {
                FireCountDown -= Time.deltaTime;
                //prevents the next projectile taking miliseconds longer than it should
                if (FireCountDown < 0)
                {
                    FireCountDown = 0;
                }
            }

            //Check if magazine in opposite hand or if unloading
            if (Input.GetKeyDown(KeyCode.R))
            {
                //PlaceHolder for click UI
                GameObject currentHandItem = UIManager.Hands.CurrentSlot.Item;
                GameObject otherHandItem = UIManager.Hands.OtherSlot.Item;
                string hand;

                if (currentHandItem != null)
                {
                    if (CurrentMagazine == null)
                    {
                        //RELOAD
                        MagazineBehaviour magazine = currentHandItem.GetComponent<MagazineBehaviour>();

                        if (magazine != null && otherHandItem.GetComponent<Weapon>() != null)
                        {
                            hand = UIManager.Hands.CurrentSlot.eventName;
                            Reload(currentHandItem, hand);
                        }
                    }
                    else
                    {
                        //UNLOAD
                        Weapon weapon = currentHandItem.GetComponent<Weapon>();

                        if (weapon != null && otherHandItem == null)
                        {
                            ManualUnload(CurrentMagazine);
                        }
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                InAutomaticAction = false;

                //remove recoil after shooting is released
                CurrentRecoilVariance = 0;
            }

            if (InAutomaticAction && FireCountDown <= 0)
            {
                AttemptToFireWeapon();
            }
        }

        #region Weapon Server Init

        public override void OnStartServer()
        {
            var ammoPrefab = Resources.Load("Rifles/Magazine_" + AmmoType);
            GameObject m = GameObject.Instantiate(ammoPrefab as GameObject, Vector3.zero, Quaternion.identity);
            //spean the magazine
            NetworkServer.Spawn(m);
            StartCoroutine(SetMagazineOnStart(m));

            base.OnStartServer();
        }

        //Gives it a chance for weaponNetworkActions to init
        IEnumerator SetMagazineOnStart(GameObject magazine)
        {
            yield return new WaitForSeconds(2f);
            //			if (GameData.IsHeadlessServer || GameData.Instance.testServer) {
            NetworkInstanceId networkID = magazine.GetComponent<NetworkIdentity>().netId;
            MagNetID = networkID;
            //			} else {
            //				PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, magazine);
            //			}
        }

        #endregion

        //Do all the weapon init for connecting clients
        public override void OnStartClient()
        {
            StartCoroutine(WaitForLoad());
            base.OnStartClient();
        }

        IEnumerator WaitForLoad()
        {
            yield return new WaitForSeconds(3f);
            LoadUnloadAmmo(MagNetID);
        }

        #region Weapon Firing Mechanism

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            //todo: validate fire attempts on server
            if (Input.GetKey(KeyCode.LeftControl))
                return;
            //shoot gun interation if its in hand
            if (gameObject == UIManager.Hands.CurrentSlot.GameObject())
            {
                AttemptToFireWeapon();
            }
            //if the weapon is not in our hands not in hands, pick it up
            else
            {
                base.Interact(originator, position, hand);
            }
        }

        void AttemptToFireWeapon()
        {
            //ignore if we are hovering over UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            //if we have no mag/clip loaded play empty sound
            if (CurrentMagazine == null)
            {
                InAutomaticAction = false;
                PlayEmptySFX();
            }
            //if we are out of ammo for this weapon eject magazine and play out of ammo sfx
            else if (Projectile != null && CurrentMagazine.ammoRemains <= 0 && FireCountDown <= 0)
            {
                InAutomaticAction = false;
                ManualUnload(CurrentMagazine);
                OutOfAmmoSFX();
            }
            else
            {
                //if we have a projectile to shoot, we have ammo and we are not waiting to be allowed to shoot again, Fire!
                if (Projectile != null && CurrentMagazine.ammoRemains > 0 && FireCountDown <= 0)
                {
                    //Add too the cooldown timer to being allowed to shoot again
                    FireCountDown += 1.0 / FireRate;
                    //fire a single round if its a semi or automatic weapon
                    if (WeaponType == WeaponType.SemiAutomatic || WeaponType == WeaponType.FullyAutomatic)
                    {
                        Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) -
                                       PlayerManager.LocalPlayer.transform.position).normalized;
                        var lps = PlayerManager.LocalPlayerScript;
                        lps.weaponNetworkActions.CmdShootBullet(gameObject, CurrentMagazine.gameObject, dir,
                            Projectile.name, UIManager.DamageZone /*PlayerScript.SelectedDamageZone*/);
                        if (WeaponType == WeaponType.FullyAutomatic)
                            lps.inputController.OnMouseDownDir(dir);
                    }

                    if (WeaponType == WeaponType.FullyAutomatic)
                    {
                        InAutomaticAction = true;
                    }
                }
            }
        }

        #endregion

        #region Weapon Loading and Unloading

        void Reload(GameObject m, string hand)
        {
            Debug.Log("Reloading");
            PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, m);
            UIManager.Hands.CurrentSlot.Clear();
            PlayerManager.LocalPlayerScript.playerNetworkActions.ClearInventorySlot(hand);
        }

        //atm unload with shortcut 'e'
        //TODO dev right click unloading so it goes into the opposite hand if it is selected
        void ManualUnload(MagazineBehaviour m)
        {
            Debug.Log("Unloading");
            if (m != null)
            {
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemNotInUISlot(m.gameObject);
                PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
            }
        }

        #endregion

        #region Weapon Inventory Management

        //Check which slot it was just added too (broadcast from UI_itemSlot)
        public void OnAddToInventory(string slotName)
        {
            //This checks to see if a new player who has joined needs to load up any weapon magazines because of missing sync hooks
            if (MagNetID != NetworkInstanceId.Invalid)
            {
                if (CurrentMagazine /* && PlayerManager.LocalPlayer != null*/)
                    PlayerManager.LocalPlayerScript.playerNetworkActions.AddToEquipmentPool(CurrentMagazine.gameObject);
            }
        }

        //recieve broadcast msg when item is dropped from hand
        public void OnRemoveFromInventory()
        {
            if (MagNetID != NetworkInstanceId.Invalid && CustomNetworkManager.Instance._isServer)
            {
                if (CurrentMagazine)
                    PlayerManager.LocalPlayerScript.playerNetworkActions.DisposeOfChildItem(CurrentMagazine.gameObject);
            }
            Debug.Log("Dropped Weapon");
        }

        public void LoadUnloadAmmo(NetworkInstanceId magazineID)
        {
            //if the magazine ID is invalid remove the magazine
            if (magazineID == NetworkInstanceId.Invalid)
            {
                CurrentMagazine = null;
            }
            else
            {
                //find the magazine by NetworkID
                GameObject magazine = ClientScene.FindLocalObject(magazineID);
                if (magazine != null)
                {
                    MagazineBehaviour magazineBehavior = magazine.GetComponent<MagazineBehaviour>();
                    CurrentMagazine = magazineBehavior;
                    //					Debug.LogFormat("MagazineBehaviour found ok: {0}", magazineID);
                }
                else
                {
                    //					Debug.Log("Could not find MagazineBehaviour");
                }
            }
        }

        #endregion

        #region Weapon Pooling

        //This is only called on the serverside
        public void OnAddToPool(NetworkInstanceId ownerId)
        {
            ControlledByPlayer = ownerId;
            if (CurrentMagazine != null && PlayerManager.LocalPlayer == ClientScene.FindLocalObject(ownerId))
            {
                //As the magazine loaded is part of the weapon, then we do not need to add to server cache, we only need to add the item to the equipment pool
                PlayerManager.LocalPlayerScript.playerNetworkActions.AddToEquipmentPool(CurrentMagazine.gameObject);
            }
        }

        public void OnRemoveFromPool()
        {
            ControlledByPlayer = NetworkInstanceId.Invalid;
        }

        #endregion

        #region Weapon Sounds

        void OutOfAmmoSFX()
        {
            PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("OutOfAmmoAlarm");
        }

        void PlayEmptySFX()
        {
            PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("EmptyGunClick");
        }

        #endregion
    }
}