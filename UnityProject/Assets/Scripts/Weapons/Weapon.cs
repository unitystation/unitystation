using System.Collections;
using System.Collections.Generic;
using Light2D;
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
///     Generic weapon base. Weapons are server authoritative.
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
	[HideInInspector] public float CurrentRecoilVariance;

	/// <summary>
	///     The countdown untill we can shoot again (seconds)
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

	/// <summary>
	/// NetID of the current magazine. The server only updates this once the shot queue is empty and the load/unload
	/// is processed, and then each client (and the server itself) sees this and updates their copy of the Weapon (and the currently loaded
	/// mag) accordingly in OnMagNetIDChanged. This field is set to NetworkInstanceID.Invalid when no mag is loaded.
	/// </summary>
	[SyncVar(hook = nameof(OnMagNetIDChanged))]
	private NetworkInstanceId MagNetID;

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

	/// <summary>
	/// Used only in server, the queued up shots that need to be performed when the weapon FireCountDown hits
	/// 0.
	/// </summary>
	private System.Collections.Generic.Queue<QueuedShot> queuedShots;

	/// <summary>
	/// We don't want to eject the magazine or reload as soon as the client says its time to do those things - if the client is
	/// firing a burst, they might shoot the whole magazine then eject it and reload before server is done processing the last shot.
	/// Instead, these vars are set when the client says to eject or load a new magazine, and the server will only process
	/// the actual unload / load (updating MagNetID) once the shot queue is empty.
	/// </summary>
	private bool queuedUnload = false;
	private NetworkInstanceId queuedLoadMagNetID = NetworkInstanceId.Invalid;

	/// <summary>
	/// RNG whose seed is based on the netID of the magazine and reset when the mag is loaded and synced when
	/// the client joins.
	/// </summary>
	private System.Random magSyncedRNG;

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

		queuedShots = new Queue<QueuedShot>();
	}

	private void Update()
	{
		if (ControlledByPlayer == NetworkInstanceId.Invalid)
		{
			return;
		}

		//don't start it too early:
		if (!isServer && !PlayerManager.LocalPlayer)
		{
			return;
		}

		//only perform the rest of the update if the weapon is in the hand of the local player or
		//we are the server
		if (!isServer && PlayerManager.LocalPlayer != ClientScene.FindLocalObject(ControlledByPlayer) )
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

		//this will only be executed on the server since only the server
		//maintains the queued actions
		if (queuedShots.Count > 0 && FireCountDown <= 0)
		{
			//fire the next shot in the queue
			DequeueAndProcessServerShot();
		}

		if (queuedUnload && queuedShots.Count == 0)
		{
			// done processing shot queue,
			// perform the queued unload action, causing all clients and server to update their version of this Weapon
			//due to the syncvar hook
			MagNetID = NetworkInstanceId.Invalid;
			queuedUnload = false;

		}
		if (queuedLoadMagNetID != NetworkInstanceId.Invalid && queuedShots.Count == 0)
		{
			//done processing shot queue, perform the reload, causing all clients and server to update their version of this Weapon
			//due to the syncvar hook
			MagNetID = queuedLoadMagNetID;
			queuedLoadMagNetID = NetworkInstanceId.Invalid;
		}

		//rest of the Update is only needed for the weapon if it is held by the local player
		if (PlayerManager.LocalPlayer != ClientScene.FindLocalObject(ControlledByPlayer))
		{
			return;
		}

		//check if burst should stop if the weapon is held by the local player
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
		OnMagNetIDChanged(MagNetID);
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
		//shoot gun interation if its in hand
		if (gameObject == UIManager.Hands.CurrentSlot.Item)
		{
			return AttemptToFireWeapon(false);
		}
		//if the weapon is not in our hands not in hands, pick it up
		else
		{
			//sync ammo count now that we will potentially be shooting this locally.
			if (CurrentMagazine != null)
			{
				SyncMagAmmoAndRNG();
			}
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

	public void TryReload()
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
						RequestReload(currentHandItem, hand, true);
					}

					if (AmmoType != ammoType)
					{
						ChatRelay.Instance.AddToChatLogClient("You try to load the wrong ammo into your weapon",
							ChatChannel.Examine);
					}
				}

				if (otherHandItem.GetComponent<MagazineBehaviour>() && currentHandItem.GetComponent<Weapon>())
				{
					string ammoType = otherHandItem.GetComponent<MagazineBehaviour>().ammoType;

					if (AmmoType == ammoType)
					{
						hand = UIManager.Hands.OtherSlot.eventName;
						RequestReload(otherHandItem, hand, false);
					}

					if (AmmoType != ammoType)
					{
						ChatRelay.Instance.AddToChatLogClient("You try to load the wrong ammo into your weapon",
							ChatChannel.Examine);
					}
				}
			}
			else
			{
				//UNLOAD

				if (currentHandItem.GetComponent<Weapon>() && otherHandItem == null)
				{
					RequestUnload(CurrentMagazine);
				}
				else if (currentHandItem.GetComponent<Weapon>() && otherHandItem.GetComponent<MagazineBehaviour>())
				{
					ChatRelay.Instance.AddToChatLogClient(
						"You weapon is already loaded, you can't fit more Magazines in it, silly!",
						ChatChannel.Examine);
				}
				else if (otherHandItem.GetComponent<Weapon>() && currentHandItem.GetComponent<MagazineBehaviour>())
				{
					ChatRelay.Instance.AddToChatLogClient(
						"You weapon is already loaded, you can't fit more Magazines in it, silly!",
						ChatChannel.Examine);
				}
			}
		}
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
	/// Attempt to fire a single shot of the weapon (this is called once per bullet for a burst of automatic fire). This
	/// should be invoked by the client when they want to perform a shot.
	/// </summary>
	/// <param name="isSuicide">if the shot should be a suicide shot, striking the weapon holder</param>
	/// <returns>true iff something happened</returns>
	private bool AttemptToFireWeapon(bool isSuicide)
	{
		PlayerMove shooter = ClientScene.FindLocalObject(ControlledByPlayer).GetComponent<PlayerMove>();
		if (!shooter.allowInput || shooter.isGhost)
		{
			return false;
		}
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
			RequestUnload(CurrentMagazine);
			OutOfAmmoSFX();
			return true;
		}
		else
		{
			//if we have a projectile to shoot, we have ammo and we are not waiting to be allowed to shoot again, Fire!
			if (Projectile != null && CurrentMagazine.ammoRemains > 0 && FireCountDown <= 0)
			{
				//fire a single round if its a semi or automatic weapon
				if (WeaponType == WeaponType.SemiAutomatic || WeaponType == WeaponType.FullyAutomatic)
				{
					//shot direction
					Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) -
	                                   PlayerManager.LocalPlayer.transform.position).normalized;
					if (!isServer)
					{
						//we're a client, request the server to make us shoot
						RequestShootMessage.Send(gameObject, dir, Projectile.name, UIManager.DamageZone, isSuicide,
							PlayerManager.LocalPlayer);
						//Prediction (client bullets don't do any damage)
						dir = ApplyRecoil(dir);
						DisplayShot(PlayerManager.LocalPlayer, dir, UIManager.DamageZone, isSuicide);
					}
					else
					{
						// we are the server, go ahead and shoot
						ServerShoot(PlayerManager.LocalPlayer, dir, UIManager.DamageZone, isSuicide);
					}

					if (WeaponType == WeaponType.FullyAutomatic)
					{
						PlayerManager.LocalPlayerScript.mouseInputController.OnMouseDownDir(dir);
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

	/// <summary>
	/// Perform an actual shot on the server, putting the requesst to shoot into our queue
	/// and informing all clients of the shot once the shot is processed
	/// </summary>
	/// <param name="shotBy">gameobject of the player performing the shot</param>
	/// <param name="target">targeted spot (actual trajectory will differ due to accuracy)</param>
	/// <param name="damageZone">targeted body part</param>
	/// <param name="isSuicideShot">if this is a suicide shot</param>
	[Server]
	public void ServerShoot(GameObject shotBy, Vector2 target,
		BodyPartType damageZone, bool isSuicideShot)
	{
		var finalDirection = ApplyRecoil(target);
		//don't enqueue the shot if the player is no longer able to shoot
		PlayerMove shooter = shotBy.GetComponent<PlayerMove>();
		if (!shooter.allowInput || shooter.isGhost)
		{
			return;
		}
		//simply enqueue the shot
		//but only enqueue the shot if we have not yet queued up all the shots in the magazine
		if (CurrentMagazine != null && queuedShots.Count < CurrentMagazine.ammoRemains)
		{
			queuedShots.Enqueue(new QueuedShot(shotBy, finalDirection, damageZone, isSuicideShot));
		}
	}

	/// <summary>
	/// Gets the next shot from the queue (if there is one) and performs the server-side shot (which will
	/// perform damage calculation when the bullet hits stuff), informing all
	/// clients to display the shot.
	/// </summary>
	[Server]
	private void DequeueAndProcessServerShot()
	{
		if (queuedShots.Count > 0)
		{
			QueuedShot nextShot = queuedShots.Dequeue();

			// check if we can still shoot
			PlayerMove shooter = nextShot.shooter.GetComponent<PlayerMove>();
			if (!shooter.allowInput || shooter.isGhost)
			{
				Logger.LogWarning("A shot was attempted when shooter is a ghost or is not allowed to shoot.");
				return;
			}


			if (CurrentMagazine == null || CurrentMagazine.ammoRemains <= 0 || Projectile == null)
			{
				Logger.LogWarning("A shot was attempted when there is no ammo.");
				return;
			}

			if (FireCountDown > 0)
			{
				Logger.LogWarning("Shot was attempted to be dequeued when the fire count down is not yet at 0.");
				return;
			}

			//perform the actual server side shooting, creating the bullet that does actual damage
			DisplayShot(nextShot.shooter, nextShot.finalDirection, nextShot.damageZone, nextShot.isSuicide);

			//tell all the clients to display the shot
			ShootMessage.SendToAll(nextShot.finalDirection, nextShot.damageZone, nextShot.shooter, this.gameObject, nextShot.isSuicide);

			if (SpawnsCaseing)
			{
				if (casingPrefab == null)
				{
					casingPrefab = Resources.Load("BulletCasing") as GameObject;
				}

				ItemFactory.SpawnItem(casingPrefab, nextShot.shooter.transform.position, nextShot.shooter.transform.parent);
			}
		}
	}

	/// <summary>
	/// Perform and display the shot locally (i.e. only on this instance of the game). Does not
	/// communicate anything to other players (unless this is the server, in which case the server
	/// will determine the effects of the bullet). Does not do any validation. This should only be invoked
	/// when displaying the results of a shot (i.e. after receiving a ShootMessage or after this client performs a shot)
	/// or when server is determining the outcome of the shot.
	/// </summary>
	/// <param name="shooter">gameobject of the shooter</param>
	/// <param name="finalDirection">direction the shot should travel (accuracy deviation should already be factored into this)</param>
	/// <param name="damageZone">targeted damage zone</param>
	/// <param name="isSuicideShot">if this is a suicide shot (aimed at shooter)</param>
	public void DisplayShot(GameObject shooter, Vector2 finalDirection,
		BodyPartType damageZone, bool isSuicideShot)
	{
		//Add too the cooldown timer to being allowed to shoot again
		FireCountDown += 1.0 / FireRate;
		CurrentMagazine.ammoRemains--;
		//get the bullet prefab being shot
		GameObject bullet = PoolManager.Instance.PoolClientInstantiate(Resources.Load(Projectile.name) as GameObject,
			shooter.transform.position, Quaternion.identity);
		float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;

		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		if (isSuicideShot)
		{
			b.Suicide(shooter, this, damageZone);
		}
		else
		{
			b.Shoot(finalDirection, angle, shooter, this, damageZone);
		}


		//add additional recoil after shooting for the next round
		AppendRecoil(angle);

		SoundManager.PlayAtPosition(FireingSound, shooter.transform.position);
	}

	#endregion

	#region Weapon Loading and Unloading

	/// <summary>
	/// Tells the server we want to reload and what magazine we want to use to do it and clears our hands. Does
	/// no other logic - the server takes care of the next step to handle the reload.
	/// </summary>
	/// <param name="m"></param>
	/// <param name="hand"></param>
	/// <param name="current"></param>
	private void RequestReload(GameObject m, string hand, bool current)
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

	/// <summary>
	/// Invoked when server receives a request to load a new magazine. Queues up the reload to occur
	/// when the shot queue is empty.
	/// </summary>
	/// <param name="newMagazineID">id of the new magazine</param>
	[Server]
	public void ServerHandleReloadRequest(NetworkInstanceId newMagazineID)
	{
		if (queuedLoadMagNetID != NetworkInstanceId.Invalid)
		{
			//can happen if client is spamming CmdLoadWeapon
			Logger.LogWarning("Player tried to queue a load action while a load action was already queued, ignoring the" +
			                  " second load.");
		}
		else
		{
			queuedLoadMagNetID = newMagazineID;
		}
	}

	/// <summary>
	/// Invoked when server recieves a request to unload the current magazine. Queues up the unload to occur
	/// when the shot queue is empty.
	/// </summary>
	public void ServerHandleUnloadRequest()
	{
		if (queuedUnload)
		{
			//this can happen if client is spamming CmdUnloadWeapon
			Logger.LogWarning("Player tried to queue an unload action while an unload action was already queued. Ignoring the" +
			                  " second unload.");
		}
		else if (queuedLoadMagNetID != NetworkInstanceId.Invalid)
		{
			Logger.LogWarning("Player tried to queue an unload action while a load action was already queued. Ignoring the unload.");
		}
		else
		{
			queuedUnload = true;
		}
	}

	//atm unload with shortcut 'e'
	//TODO dev right click unloading so it goes into the opposite hand if it is selected
	/// <summary>
	/// Tells the server we want to unload and does nothing else. Server takes care of the next step to handle the unload.
	/// </summary>
	/// <param name="m"></param>
	private void RequestUnload(MagazineBehaviour m)
	{
		Logger.LogTrace("Unloading", Category.Firearms);
		if (m != null)
		{
			//PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDropItemNotInUISlot(m.gameObject);
			PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdUnloadWeapon(gameObject);
		}
	}

	/// <summary>
	/// Invoked on all clients and server when the server updates MagNetID, updating the local version of this weapon to
	/// load / unload the mag.
	/// </summary>
	/// <param name="magazineID">id of the new magazine to load, NetworkInstanceId.Invalid if we should unload
	/// the current mag</param>
	private void OnMagNetIDChanged(NetworkInstanceId magazineID)
	{
		//if the magazine ID is invalid remove the magazine
		if (magazineID == NetworkInstanceId.Invalid)
		{
			CurrentMagazine = null;
		}
		else
		{
			if (CurrentMagazine != null)
			{
				Logger.LogError("Attempted to load a new mag while a " +
				                "mag is still in the weapon in our local instance of the game. " +
				                "This is probably a bug. Continuing anyway...");
			}
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
				//sync ammo count now that we will potentially be shooting this locally.
				SyncMagAmmoAndRNG();
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

	/// <summary>
	/// Applies recoil to calcuate the final direction of the shot
	/// </summary>
	/// <param name="target">targeted position</param>
	/// <returns>final position after applying recoil</returns>
	private Vector2 ApplyRecoil(Vector2 target)
	{
		float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
		float angleVariance = MagSyncedRandomFloat(-CurrentRecoilVariance, CurrentRecoilVariance);
		float newAngle = angle * Mathf.Deg2Rad + angleVariance;
		Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
		return vec2;
	}

	private float MagSyncedRandomFloat(float min, float max)
	{
		return (float) (magSyncedRNG.NextDouble() * (max - min) + min);
	}

	/// <summary>
	/// Use this whenever the mag-based RNG and ammo count should by synced with the server's authoritative values for thos.
	/// Syncs the ammo count of the loaded mag with the server, also syncing the RNG.
	///
	/// Due to client and server ammo counts not being synced until the local player picks up
	/// a gun or reloads a mag, the RNG of this weapon will be out of sync with the server.
	/// This method re-syncs the local RNG with the server RNG by resetting the seed and
	/// getting as many NextDoubles() as would've been used to generate all the previously spent shots
	/// of the current magazine.
	/// </summary>
	private void SyncMagAmmoAndRNG()
	{
		CurrentMagazine.SyncClientAmmoRemainsWithServer();
		magSyncedRNG = new System.Random(CurrentMagazine.netId.GetHashCode());
		var shots = CurrentMagazine.magazineSize - CurrentMagazine.ammoRemains;
		//each shot causes 2 RNG calls (one for deviation, one for recoil increase), so fast forward RNG for each shot
		for (int i = 0; i < shots; i++)
		{
			magSyncedRNG.NextDouble();
			magSyncedRNG.NextDouble();
		}
	}

	private void AppendRecoil(float angle)
	{
		if (CurrentRecoilVariance < MaxRecoilVariance)
		{
			//get a random recoil
			float randRecoil = MagSyncedRandomFloat(CurrentRecoilVariance, MaxRecoilVariance);;
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

/// <summary>
/// Represents a shot that has been queued up to fire when the weapon is next able to. Only used on server side.
/// </summary>
struct QueuedShot
{
	public readonly GameObject shooter;
	public readonly Vector2 finalDirection;
	public readonly BodyPartType damageZone;
	public readonly bool isSuicide;

	public QueuedShot(GameObject shooter, Vector2 finalDirection, BodyPartType damageZone, bool isSuicide)
	{
		this.shooter = shooter;
		this.finalDirection = finalDirection;
		this.damageZone = damageZone;
		this.isSuicide = isSuicide;
	}
}