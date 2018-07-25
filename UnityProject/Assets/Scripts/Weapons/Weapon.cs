﻿using System.Collections;
using System.Runtime.Serialization.Formatters;
using Items;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Equipment;

namespace Weapons
{
	/// <summary>
	///     Generic weapon types
	/// </summary>
	public enum WeaponType
	{
		Melee, //TODO: SUPPORT MELEE WEAPONS
		SemiAutomatic,
		FullyAutomatic,
		Burst //TODO: SUPPORT BURST WEAPONS
	}

	/// <summary>
	///     Generic weapon base
	/// </summary>
	public class Weapon : PickUpTrigger
	{
		/// <summary>
		///     The type of ammo this weapon will allow, this is a string and not an enum for diversity
		/// </summary>
		public string AmmoType;

		[SyncVar] public NetworkInstanceId ControlledByPlayer;

		/// <summary>
		///     The current magazine for this weapon, null means empty
		/// </summary>
		private MagazineBehaviour CurrentMagazine;

		/// <summary>
		///     Checks if the weapon should spawn weapon casings
		/// </summary>
		public bool SpawnsCaseing = true;

		/// <summary>
		///     The the current recoil variance this weapon has reached
		/// </summary>
		[SyncVar] [HideInInspector] public float CurrentRecoilVariance;

		/// <summary>
		///     The countdown untill we can shoot again
		/// </summary>
		[HideInInspector] public double FireCountDown;

		/// <summary>
		///     The the name of the sound this gun makes when shooting
		/// </summary>
		public string FireingSound;

		/// <summary>
		///     The amount of times per second this weapon can fire
		/// </summary>
		public double FireRate;

		/// <summary>
		///     If the weapon is currently in automatic action
		/// </summary>
		[HideInInspector] public bool InAutomaticAction;

		[SyncVar(hook = nameof(LoadUnloadAmmo))] public NetworkInstanceId MagNetID;

		//TODO connect these with the actual shooting of a projectile
		/// <summary>
		///     The max recoil angle this weapon can reach with sustained fire
		/// </summary>
		public float MaxRecoilVariance;

		/// <summary>
		///     The projectile fired from this weapon
		/// </summary>
		public GameObject Projectile;

		/// <summary>
		///     The damage for this weapon
		/// </summary>
		public int ProjectileDamage;

		/// <summary>
		///     The amount of projectiles expended per shot
		/// </summary>
		public int ProjectilesFired;

		/// <summary>
		///     The traveling speed for this weapons projectile
		/// </summary>
		public int ProjectileVelocity;

		/// <summary>
		///     Current Weapon Type
		/// </summary>
		public WeaponType WeaponType;

		private GameObject casingPrefab;

		private void Start()
		{
			InAutomaticAction = false;
			//init weapon with missing settings
			if (AmmoType == null)
			{
				AmmoType = "12mm";
			}
			if (Projectile == null)
			{
				Projectile = Resources.Load("Bullet_12mm") as GameObject;
			}
		}

		private void Update()
		{
			if (ControlledByPlayer == NetworkInstanceId.Invalid)
			{
				return;
			}

			//don't start it too early:
			if (!PlayerManager.LocalPlayer)
			{
				return;
			}

			//Only update if it is inhand of localplayer
			if (PlayerManager.LocalPlayer != ClientScene.FindLocalObject(ControlledByPlayer))
			{
				return;
			}

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

				if ((currentHandItem != null) && (otherHandItem != null))
				{
					if (CurrentMagazine == null)
					{
						//RELOAD
						if (currentHandItem.GetComponent<MagazineBehaviour>() && otherHandItem.GetComponent<Weapon>())
						{
							string ammoType = currentHandItem.GetComponent<MagazineBehaviour>().ammoType;

							if (AmmoType == ammoType)
							{
								hand = UIManager.Hands.CurrentSlot.eventName;
								Reload(currentHandItem, hand, true);

							}

							if (AmmoType != ammoType)
							{
								ChatRelay.Instance.AddToChatLogClient( "You try to load the wrong ammo into your weapon", ChatChannel.Examine );
							}
						}

						if (otherHandItem.GetComponent<MagazineBehaviour>() && currentHandItem.GetComponent<Weapon>())
						{
							string ammoType = otherHandItem.GetComponent<MagazineBehaviour>().ammoType;

							if (AmmoType == ammoType)
							{
								hand = UIManager.Hands.OtherSlot.eventName;
								Reload(otherHandItem, hand, false);
							}
							if (AmmoType != ammoType)
							{
								ChatRelay.Instance.AddToChatLogClient( "You try to load the wrong ammo into your weapon", ChatChannel.Examine );
							}
						}


					}
					else
					{
						//UNLOAD

						if (currentHandItem.GetComponent<Weapon>() && otherHandItem == null)
						{
							ManualUnload(CurrentMagazine);
						}

						else if (currentHandItem.GetComponent<Weapon>() && otherHandItem.GetComponent<MagazineBehaviour>())
						{
							ChatRelay.Instance.AddToChatLogClient("You weapon is already loaded, you cant fit more Magazines in it, silly!", ChatChannel.Examine);

						}
						else if (otherHandItem.GetComponent<Weapon>() && currentHandItem.GetComponent<MagazineBehaviour>())
						{
							ChatRelay.Instance.AddToChatLogClient("You weapon is already loaded, you cant fit more Magazines in it, silly!", ChatChannel.Examine);

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

		//Do all the weapon init for connecting clients
		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(3f);
			LoadUnloadAmmo(MagNetID);
		}

		#region Weapon Server Init

		public override void OnStartServer()
		{
			GameObject ammoPrefab = Resources.Load("Rifles/Magazine_" + AmmoType)  as GameObject;

			GameObject m = ItemFactory.SpawnItem(ammoPrefab, transform.parent);

			StartCoroutine(SetMagazineOnStart(m));

			base.OnStartServer();
		}

		//Gives it a chance for weaponNetworkActions to init
		private IEnumerator SetMagazineOnStart(GameObject magazine)
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

		#region Weapon Firing Mechanism

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			//todo: validate fire attempts on server
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
			{
				return;
			}
			//shoot gun interation if its in hand
			if (gameObject == UIManager.Hands.CurrentSlot.Item)
			{
				AttemptToFireWeapon();
			}
			//if the weapon is not in our hands not in hands, pick it up
			else
			{
				base.Interact(originator, position, hand);
			}
		}

		public void AttemptSuicideShot(){
			//Hand slot checks are already done before calling this method (i.e. is weapon in current hand)
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) {
				return;
			}

			AttemptToFireWeapon(true);
		}

		private void AttemptToFireWeapon(bool suicideShot = false)
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
						Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;

						RequestShootMessage.Send(gameObject, dir, Projectile.name, UIManager.DamageZone, suicideShot, PlayerManager.LocalPlayer);

						if (!isServer) {
							//Prediction (client bullets don't do any damage)
							Shoot(PlayerManager.LocalPlayer, dir, Projectile.name, UIManager.DamageZone, suicideShot);
						}

						if (WeaponType == WeaponType.FullyAutomatic)
						{
							PlayerManager.LocalPlayerScript.inputController.OnMouseDownDir(dir);
						}
					}

					if (WeaponType == WeaponType.FullyAutomatic)
					{
						InAutomaticAction = true;
					}
				}
			}
		}

		[Server]
		public void ServerShoot(GameObject shotBy, Vector2 direction, string bulletName,
		                        BodyPartType damageZone, bool isSuicideShot){
			PlayerMove shooter = shotBy.GetComponent<PlayerMove>();
			if(!shooter.allowInput || shooter.isGhost){
				return;
			}

			Shoot(shotBy, direction, bulletName, damageZone, isSuicideShot);

			//This is used to determine where bullet shot should head towards on client
			Ray2D ray = new Ray2D(shotBy.transform.position, direction);
			ShootMessage.SendToAll(gameObject, ray.GetPoint(30f), bulletName, damageZone, shotBy);

			if (SpawnsCaseing) {
				if(casingPrefab == null){
					casingPrefab = Resources.Load("BulletCasing") as GameObject;
				}
				ItemFactory.SpawnItem(casingPrefab, shotBy.transform.position, shotBy.transform.parent);
			}
		}

		//This is only for the shooters client and the server. Rest is done via msg
		private void Shoot(GameObject shooter, Vector2 direction, string bulletName,
								BodyPartType damageZone, bool isSuicideShot){
			CurrentMagazine.ammoRemains--;
			//get the bullet prefab being shot
			GameObject bullet = PoolManager.Instance.PoolClientInstantiate(Resources.Load(bulletName) as GameObject,
				shooter.transform.position, Quaternion.identity);
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

			//if we have recoil variance add it, and get the new attack angle
			if (CurrentRecoilVariance > 0) {
				direction = GetRecoilOffset(angle);
			}

			BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
			b.isSuicide = isSuicideShot;
			b.Shoot(direction, angle, shooter, damageZone);

			//add additional recoil after shooting for the next round
			AppendRecoil(angle);

			SoundManager.PlayAtPosition(FireingSound, shooter.transform.position);
		}

		#endregion

		#region Weapon Loading and Unloading

		private void Reload(GameObject m, string hand, bool current)
		{
			Logger.LogTrace("Reloading", Category.Firearms);
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdLoadMagazine(gameObject, m, hand);
			if (current)
			{
				UIManager.Hands.CurrentSlot.Clear();
			}
			else
			{
				UIManager.Hands.OtherSlot.Clear();
			}

		}

		//atm unload with shortcut 'e'
		//TODO dev right click unloading so it goes into the opposite hand if it is selected
		private void ManualUnload(MagazineBehaviour m)
		{
			Logger.LogTrace("Unloading", Category.Firearms);
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
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.AddToEquipmentPool(CurrentMagazine.gameObject);
				}
			}
		}

		//recieve broadcast msg when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			if (MagNetID != NetworkInstanceId.Invalid && CustomNetworkManager.Instance._isServer)
			{
				if (CurrentMagazine)
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.DisposeOfChildItem(CurrentMagazine.gameObject);
				}
			}
			Logger.LogTrace("Dropped Weapon", Category.Firearms);
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
					Logger.LogTraceFormat("MagazineBehaviour found ok: {0}", Category.Firearms, magazineID);
				}
			}
		}

		#endregion

		#region Weapon Pooling

		//This is only called on the serverside
		public void OnAddToPool(NetworkInstanceId ownerId)
		{
			ControlledByPlayer = ownerId;
			if (CurrentMagazine != null)
			{
				//As the magazine loaded is part of the weapon, then we do not need to add to server cache, we only need to add the item to the equipment pool
				NetworkServer.FindLocalObject(ownerId).GetComponent<PlayerNetworkActions>().AddToEquipmentPool(CurrentMagazine.gameObject);
			}
		}

		public void OnRemoveFromPool()
		{
			if (CurrentMagazine != null)
			{
				EquipmentPool.DisposeOfObject(NetworkServer.FindLocalObject(ControlledByPlayer).gameObject, CurrentMagazine.gameObject);
			}
			ControlledByPlayer = NetworkInstanceId.Invalid;
		}

		#endregion

		#region Weapon Sounds

		private void OutOfAmmoSFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("OutOfAmmoAlarm");
		}

		private void PlayEmptySFX()
		{
			PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("EmptyGunClick");
		}

		#endregion

		#region Weapon Network Supporting Methods

		private Vector2 GetRecoilOffset(float angle)
		{
			float angleVariance = Random.Range(-CurrentRecoilVariance, CurrentRecoilVariance);
			float newAngle = angle * Mathf.Deg2Rad + angleVariance;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		private void AppendRecoil(float angle)
		{
			if (CurrentRecoilVariance < MaxRecoilVariance) {
				//get a random recoil
				float randRecoil = Random.Range(CurrentRecoilVariance, MaxRecoilVariance);
				CurrentRecoilVariance += randRecoil;
				//make sure the recoil is not too high
				if (CurrentRecoilVariance > MaxRecoilVariance) {
					CurrentRecoilVariance = MaxRecoilVariance;
				}
			}
		}

		#endregion
	}
}