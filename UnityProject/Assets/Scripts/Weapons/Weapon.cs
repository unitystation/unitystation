using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;


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
	public MagazineBehaviour CurrentMagazine { get; private set; }

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
	private bool InAutomaticAction;

	/// <summary>
	///     If suicide shooting should be prevented (for when user inadvertently drags over themselves during a burst)
	/// </summary>
	private bool AllowSuicide;

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
		StopAutomaticBurst();
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

		//update the time until the next shot can happen
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
		if (Input.GetKeyDown(KeyCode.R) && !UIManager.IsInputFocus)
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
							ChatRelay.Instance.AddToChatLogClient("You try to load the wrong ammo into your weapon", ChatChannel.Examine);
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
							ChatRelay.Instance.AddToChatLogClient("You try to load the wrong ammo into your weapon", ChatChannel.Examine);
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
			StopAutomaticBurst();
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
		while (MagNetID == NetworkInstanceId.Invalid)
		{
			yield return YieldHelper.EndOfFrame;
		}
		yield return YieldHelper.EndOfFrame;
		LoadUnloadAmmo(MagNetID);
	}

	#region Weapon Server Init

	public override void OnStartServer()
	{
		GameObject ammoPrefab = Resources.Load("Rifles/Magazine_" + AmmoType) as GameObject;

		GameObject m = ItemFactory.SpawnItem(ammoPrefab, transform.parent);
		var cnt = m.GetComponent<CustomNetTransform>();
		cnt.DisappearFromWorldServer();

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

	/// <summary>
	/// Occurs when shooting by clicking on empty space
	/// </summary>
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//todo: validate fire attempts on server
		//shoot gun interation if its in hand
		if (gameObject == UIManager.Hands.CurrentSlot.Item)
		{
			return AttemptToFireWeapon(false);
		}
		//if the weapon is not in our hands not in hands, pick it up
		else
		{
			return base.Interact(originator, position, hand);
		}
	}

	/// <summary>
	/// Occurs when shooting by clicking on empty space
	/// </summary>
	public override bool DragInteract(GameObject originator, Vector3 position, string hand)
	{
		//continue automatic fire while dragging
		if (InAutomaticAction && FireCountDown <= 0)
		{
			return AttemptToFireWeapon(false);
		}

		return false;
	}

	/// <summary>
	/// Shoot the weapon holder.
	/// </summary>
	/// <param name="isDrag">if this suicide is being attempted during a drag interaction</param>
	/// <returns>true iff something happened</returns>
	public bool AttemptSuicideShot(bool isDrag)
	{
		//Only allow drag shot suicide if we've already started a burst
		if (isDrag && InAutomaticAction && FireCountDown <= 0)
		{
			return AttemptToFireWeapon(true);
		}
		else if (!isDrag)
		{
			return AttemptToFireWeapon(true);
		}

		return false;
	}

	/// <summary>
	/// Attempt to fire a single shot of the weapon (this is called once per bullet for a burst of automatic fire)
	/// </summary>
	/// <param name="isSuicide">if the shot should be a suicide shot, striking the weapon holder</param>
	/// <returns>true iff something happened</returns>
	private bool AttemptToFireWeapon(bool isSuicide)
	{
		//suicide is not allowed in some cases.
		isSuicide = isSuicide && AllowSuicide;
		//ignore if we are hovering over UI
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return false;
		}

		//if we have no mag/clip loaded play empty sound
		if (CurrentMagazine == null)
		{
			StopAutomaticBurst();
			PlayEmptySFX();
			return true;
		}
		//if we are out of ammo for this weapon eject magazine and play out of ammo sfx
		else if (Projectile != null && CurrentMagazine.ammoRemains <= 0 && FireCountDown <= 0)
		{
			StopAutomaticBurst();
			ManualUnload(CurrentMagazine);
			OutOfAmmoSFX();
			return true;
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

					RequestShootMessage.Send(gameObject, dir, Projectile.name, UIManager.DamageZone, isSuicide, PlayerManager.LocalPlayer);
					if (!isServer)
					{
						//Prediction (client bullets don't do any damage)
						Shoot(PlayerManager.LocalPlayer, dir, Projectile.name, UIManager.DamageZone, isSuicide);
					}

					if (WeaponType == WeaponType.FullyAutomatic)
					{
						PlayerManager.LocalPlayerScript.inputController.OnMouseDownDir(dir);
					}
				}

				if (WeaponType == WeaponType.FullyAutomatic && !InAutomaticAction)
				{
					StartBurst(isSuicide);
				}
				else
				{
					ContinueBurst(isSuicide);
				}

				return true;
			}
		}

		return true;
	}

	/// <summary>
	/// Stop a burst of automatic fire.
	/// </summary>
	private void StopAutomaticBurst()
	{
		InAutomaticAction = false;
		AllowSuicide = true;
		//remove recoil after shooting is released
		CurrentRecoilVariance = 0;
	}

	/// <summary>
	/// Start a burst of automatic fire
	/// </summary>
	/// <param name="isSuicide">if burst is starting with gun pointing at own player</param>
	private void StartBurst(bool isSuicide)
	{
		//only allow suicide if the burst starts out as a suicide.
		AllowSuicide = isSuicide;
		InAutomaticAction = true;
	}

	/// <summary>
	/// Continue a burst, turning off suicide allowance if the burst is no longer pointing at the player
	/// </summary>
	/// <param name="isSuicide"></param>
	private void ContinueBurst(bool isSuicide)
	{
		if (isSuicide == false)
		{
			AllowSuicide = false;
		}
	}

	[Server]
	public void ServerShoot(GameObject shotBy, Vector2 direction, string bulletName,
							BodyPartType damageZone, bool isSuicideShot)
	{
		PlayerMove shooter = shotBy.GetComponent<PlayerMove>();
		if (!shooter.allowInput || shooter.isGhost)
		{
			return;
		}

		Shoot(shotBy, direction, bulletName, damageZone, isSuicideShot);

		//This is used to determine where bullet shot should head towards on client
		if (isSuicideShot)
		{
			//no need for the bullet to travel if it's a suicide, just have it stay right where it is
			ShootMessage.SendToAll(gameObject, shotBy.transform.position, bulletName, damageZone, shotBy);
		}
		else
		{
			Ray2D ray = new Ray2D(shotBy.transform.position, direction);
			ShootMessage.SendToAll(gameObject, ray.GetPoint(30f), bulletName, damageZone, shotBy);
		}

		if (SpawnsCaseing)
		{
			if (casingPrefab == null)
			{
				casingPrefab = Resources.Load("BulletCasing") as GameObject;
			}
			ItemFactory.SpawnItem(casingPrefab, shotBy.transform.position, shotBy.transform.parent);
		}
	}

	//This is only for the shooters client and the server. Rest is done via msg
	private void Shoot(GameObject shooter, Vector2 direction, string bulletName,
							BodyPartType damageZone, bool isSuicideShot)
	{
		CurrentMagazine.ammoRemains--;
		//get the bullet prefab being shot
		GameObject bullet = PoolManager.Instance.PoolClientInstantiate(Resources.Load(bulletName) as GameObject,
			shooter.transform.position, Quaternion.identity);
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

		//if we have recoil variance add it, and get the new attack angle
		if (CurrentRecoilVariance > 0)
		{
			direction = GetRecoilOffset(angle);
		}

		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		if (isSuicideShot)
		{
			b.Suicide(shooter, this, damageZone);
		}
		else
		{
			b.Shoot(direction, angle, shooter, this, damageZone);
		}


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
			//PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemNotInUISlot(m.gameObject);
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
		}
	}

	#endregion

	#region Weapon Inventory Management

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
				var cnt = magazine.GetComponent<CustomNetTransform>();
				if (isServer)
				{
					cnt.DisappearFromWorldServer();
				}
				else
				{
					cnt.DisappearFromWorld();
				}
				Logger.LogTraceFormat("MagazineBehaviour found ok: {0}", Category.Firearms, magazineID);
			}
		}
	}

	#endregion

	#region Weapon Pooling

	//This is only called on the serverside
	public override void OnPickUpServer(NetworkInstanceId ownerId)
	{
		ControlledByPlayer = ownerId;
	}

	public override void OnDropItemServer()
	{
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
		if (CurrentRecoilVariance < MaxRecoilVariance)
		{
			//get a random recoil
			float randRecoil = Random.Range(CurrentRecoilVariance, MaxRecoilVariance);
			CurrentRecoilVariance += randRecoil;
			//make sure the recoil is not too high
			if (CurrentRecoilVariance > MaxRecoilVariance)
			{
				CurrentRecoilVariance = MaxRecoilVariance;
			}
		}
	}

	#endregion
}
