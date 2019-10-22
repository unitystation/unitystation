using System.Collections;
using System.Collections.Generic;
using System.Linq;
 using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
 /// <summary>
///  Allows an object to behave like a gun and fire shots. Server authoritative with client prediction.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Gun : NBAimApplyInteractable, IInteractable<HandActivate>, IInteractable<InventoryApply>, IOnStageServer
{
	//constants for calculating screen shake due to recoil
	private static readonly float MAX_PROJECTILE_VELOCITY = 48f;
	private static readonly float MAX_SHAKE_INTENSITY = 1f;
	private static readonly float MIN_SHAKE_INTENSITY = 0.01f;

	/// <summary>
	///     The type of ammo this weapon will allow, this is a string and not an enum for diversity
	/// </summary>
	public string AmmoType;

	//server-side flag indicating if the gun is currently held by a player
	private bool serverIsHeld;

	/// <summary>
	///     The current magazine for this weapon, null means empty
	/// </summary>
	public MagazineBehaviour CurrentMagazine { get; private set; }

	/// <summary>
	///     Checks if the weapon should spawn weapon casings
	/// </summary>
	public bool SpawnsCaseing = true;

	/// <summary>
	///     If the gun should eject it's magazine automatically
	/// </summary>
	public bool SmartGun = false;

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
	[FormerlySerializedAs("FireingSound")]
	public string FiringSound;

	/// <summary>
	///     The amount of times per second this weapon can fire
	/// </summary>
	public double FireRate;

	/// <summary>
	///     If suicide shooting should be prevented (for when user inadvertently drags over themselves during a burst)
	/// </summary>
	private bool AllowSuicide;

	/// <summary>
	/// NetID of the current magazine. The server only updates this once the shot queue is empty and the load/unload
	/// is processed, and then each client (and the server itself) sees this and updates their copy of the Weapon (and the currently loaded
	/// mag) accordingly in OnMagNetIDChanged. This field is set to NetworkInstanceID.Invalid when no mag is loaded.
	/// </summary>
	[SyncVar(hook = nameof(SyncMagNetID))]
	private uint magNetID = NetId.Invalid;

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
	///     The amount of projectiles expended per shot
	/// </summary>
	public int ProjectilesFired;

	/// <summary>
	///     The traveling speed for this weapons projectile
	/// </summary>
	public int ProjectileVelocity;

	/// <summary>
	/// Describes the recoil behavior of the camera when this gun is fired
	/// </summary>
	[Tooltip("Describes the recoil behavior of the camera when this gun is fired")]
	public CameraRecoilConfig CameraRecoilConfig;

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
	private uint queuedLoadMagNetID = NetId.Invalid;

	private RegisterTile registerTile;


	#region Init Logic

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
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

	private void Start()
	{
		//register server side drop / pickup hooks
		if (isServer)
		{
			var pickup = GetComponent<Pickupable>();
			if (pickup != null)
			{
				pickup.OnPickupServer.AddListener(OnPickupServer);
				pickup.OnDropServer.AddListener(OnDropItemServer);
			}

			ServerEnsureMag();
		}
		base.Start();
	}

	//if no mag is already in the gun, creates one at max capacity
	private void ServerEnsureMag()
	{
		if (CurrentMagazine == null)
		{
			GameObject ammoPrefab = Resources.Load("Rifles/Magazine_" + AmmoType) as GameObject;

			GameObject m = PoolManager.PoolNetworkInstantiate(ammoPrefab, parent: transform.parent);
			var cnt = m.GetComponent<CustomNetTransform>();
			cnt.DisappearFromWorldServer();

			var networkID = m.GetComponent<NetworkIdentity>().netId;
			SyncMagNetID(networkID);
		}
	}

	public override void OnStartClient()
	{
		SyncMagNetID(magNetID);
	}

	public void GoingOnStageServer(OnStageInfo info)
	{
		ServerEnsureMag();
		if (info.IsCloned)
		{
			//set initial ammo from cloned
			var otherMag = info.ClonedFrom.GetComponent<Gun>().CurrentMagazine;
			if (otherMag == null)
			{
				CurrentMagazine.ServerSetAmmoRemains(0);
			}
			else
			{
				CurrentMagazine.ServerSetAmmoRemains(otherMag.ServerAmmoRemains);
			}
		}
		else
		{
			//reinit ammo to max
			CurrentMagazine.ServerSetAmmoRemains(CurrentMagazine.magazineSize);
		}
	}

	#endregion

	#region Interaction

	protected override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		if (CurrentMagazine == null)
		{
			PlayEmptySFX();
			if (interaction.Performer != PlayerManager.LocalPlayer)
			{
				Logger.LogTrace("Server rejected shot - No magazine being loaded", Category.Firearms);
			}

			return false;
		}

		//note: fire count down is only checked local player side, as server processes all the shots in a queue
		//anyway so client cannot exceed that firing rate no matter what. If we validate firing rate server
		//side at the moment of interaction, it will reject client's shots because of lag between server / client
		//firing countdown
		if (Projectile != null && CurrentMagazine.ClientAmmoRemains <= 0 && (interaction.Performer != PlayerManager.LocalPlayer || FireCountDown <= 0))
		{
			if (SmartGun)
			{
				RequestUnload(CurrentMagazine);
				OutOfAmmoSFX();
			}
			else
			{
				PlayEmptySFX();
			}
			if (interaction.Performer != PlayerManager.LocalPlayer)
			{
				Logger.LogTrace("Server rejected shot - out of ammo", Category.Firearms);
			}

			return false;
		}

		if (Projectile != null && CurrentMagazine.ClientAmmoRemains > 0 && (interaction.Performer != PlayerManager.LocalPlayer || FireCountDown <= 0))
		{
			if (interaction.MouseButtonState == MouseButtonState.PRESS)
			{
				return true;
			}
			else
			{
				//being held, only can shoot if this is an automatic
				return WeaponType == WeaponType.FullyAutomatic;
			}
		}

		if (interaction.Performer != PlayerManager.LocalPlayer)
		{
			Logger.LogTraceFormat("Server rejected shot - unknown reason. MouseButtonState {0} ammo remains {1} weapon type {2}", Category.Firearms,
				interaction.MouseButtonState, CurrentMagazine.ClientAmmoRemains, WeaponType);
		}
		return false;
	}

	protected override void ClientPredictInteraction(AimApply interaction)
	{
		//do we need to check if this is a suicide (want to avoid the check because it involves a raycast).
		//case 1 - we are beginning a new shot, need to see if we are shooting ourselves
		//case 2 - we are firing an automatic and are currently shooting ourselves, need to see if we moused off
		//	ourselves.
		var isSuicide = false;
		if (interaction.MouseButtonState == MouseButtonState.PRESS ||
		    (WeaponType == WeaponType.FullyAutomatic && AllowSuicide))
		{
			isSuicide = interaction.IsAimingAtSelf;
			AllowSuicide = isSuicide;
		}

		var dir = ApplyRecoil(interaction.TargetVector.normalized);
		DisplayShot(PlayerManager.LocalPlayer, dir, UIManager.DamageZone, isSuicide);
	}

	protected override void ServerPerformInteraction(AimApply interaction)
	{
		//do we need to check if this is a suicide (want to avoid the check because it involves a raycast).
		//case 1 - we are beginning a new shot, need to see if we are shooting ourselves
		//case 2 - we are firing an automatic and are currently shooting ourselves, need to see if we moused off
		//	ourselves.
		var isSuicide = false;
		if (interaction.MouseButtonState == MouseButtonState.PRESS ||
		    (WeaponType == WeaponType.FullyAutomatic && AllowSuicide))
		{
			isSuicide = interaction.IsAimingAtSelf;
			AllowSuicide = isSuicide;
		}

		//enqueue the shot (will be processed in Update)
		ServerShoot(interaction.Performer, interaction.TargetVector.normalized, UIManager.DamageZone, isSuicide);
	}



	public bool Interact(HandActivate interaction)
	{
		//try ejecting the mag
		if(CurrentMagazine != null)
		{
			RequestUnload(CurrentMagazine);
			return true;
		}

		return false;
	}

	public bool Interact(InventoryApply interaction)
	{
		//only reload if the gun is the target
		if (interaction.TargetObject == gameObject)
		{
			TryReload(interaction.HandObject);
			return true;
		}

		return false;
	}

	private void OnPickupServer(HandApply interaction)
	{
		serverIsHeld = true;
	}

	#endregion


	#region Weapon Firing Mechanism

	private void Update()
	{
		//don't process if we are server and the gun is not held by anyone
		if (isServer && !serverIsHeld) return;

		//if we are client, make sure we've initialized
		if (!isServer && !PlayerManager.LocalPlayer) return;

		//if we are client, only process this if we are holding it
		if (!isServer)
		{
			if (UIManager.Hands == null || UIManager.Hands.CurrentSlot == null) return;
			var heldItem = UIManager.Hands.CurrentSlot.Item;
			if (gameObject != heldItem) return;
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

		//remaining logic is server side only

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
			SyncMagNetID(NetId.Invalid);
			queuedUnload = false;

		}
		if (queuedLoadMagNetID != NetId.Invalid && queuedShots.Count == 0)
		{
			//done processing shot queue, perform the reload, causing all clients and server to update their version of this Weapon
			//due to the syncvar hook
			SyncMagNetID(queuedLoadMagNetID);
			queuedLoadMagNetID = NetId.Invalid;
		}
	}

	/// <summary>
	/// attempt to reload the weapon with the item given
	/// </summary>
	private void TryReload(GameObject item)
	{
		string hand;
		if (item != null)
		{
			MagazineBehaviour magazine = item.GetComponent<MagazineBehaviour>();
			if (magazine)
			{
				if (CurrentMagazine == null)
				{
					//RELOAD
					// If the item used on the gun is a magazine, check type and reload
					string ammoType = magazine.ammoType;
					if (AmmoType == ammoType)
					{
						hand = UIManager.Hands.CurrentSlot.eventName;
						RequestReload(item, hand, true);
					}
					if (AmmoType != ammoType)
					{
						Chat.AddExamineMsgToClient("You try to load the wrong ammo into your weapon");
					}
				}
				else  if (AmmoType == magazine.ammoType)
				{
					Chat.AddExamineMsgToClient("You weapon is already loaded, you can't fit more Magazines in it, silly!");
				}
			}
		}
	}

	/// <summary>
	/// Perform an actual shot on the server, putting the requesst to shoot into our queue
	/// and informing all clients of the shot once the shot is processed
	/// </summary>
	/// <param name="shotBy">gameobject of the player performing the shot</param>
	/// <param name="target">normalized target vector(actual trajectory will differ due to accuracy)</param>
	/// <param name="damageZone">targeted body part</param>
	/// <param name="isSuicideShot">if this is a suicide shot</param>
	[Server]
	public void ServerShoot(GameObject shotBy, Vector2 target,
		BodyPartType damageZone, bool isSuicideShot)
	{
		var finalDirection = ApplyRecoil(target);
		//don't enqueue the shot if the player is no longer able to shoot
		PlayerScript shooter = shotBy.GetComponent<PlayerScript>();
		if ( shooter.canNotInteract() )
		{
			Logger.LogTrace("Server rejected shot: shooter cannot interact", Category.Firearms);
			return;
		}
		//simply enqueue the shot
		//but only enqueue the shot if we have not yet queued up all the shots in the magazine
		if (CurrentMagazine != null && queuedShots.Count < CurrentMagazine.ServerAmmoRemains)
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
			PlayerScript shooterScript = nextShot.shooter.GetComponent<PlayerScript>();
			if (!shooter.allowInput || shooterScript.IsGhost)
			{
				Logger.Log("A player tried to shoot when not allowed or when they were a ghost.", Category.Exploits);
				Logger.LogWarning("A shot was attempted when shooter is a ghost or is not allowed to shoot.", Category.Firearms);
				return;
			}


			if (CurrentMagazine == null || CurrentMagazine.ServerAmmoRemains <= 0 || Projectile == null)
			{
				Logger.LogTrace("Player tried to shoot when there was no ammo.", Category.Exploits);
				Logger.LogWarning("A shot was attempted when there is no ammo.", Category.Firearms);
				return;
			}

			if (FireCountDown > 0)
			{
				Logger.LogTrace("Player tried to shoot too fast.", Category.Exploits);
				Logger.LogWarning("Shot was attempted to be dequeued when the fire count down is not yet at 0.", Category.Exploits);
				return;
			}

			//perform the actual server side shooting, creating the bullet that does actual damage
			DisplayShot(nextShot.shooter, nextShot.finalDirection, nextShot.damageZone, nextShot.isSuicide);

			//trigger a hotspot caused by gun firing
			registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(nextShot.shooter.TileWorldPosition(), 3200, 0.005f);

			//tell all the clients to display the shot
			ShootMessage.SendToAll(nextShot.finalDirection, nextShot.damageZone, nextShot.shooter, this.gameObject, nextShot.isSuicide);

			if (SpawnsCaseing)
			{
				if (casingPrefab == null)
				{
					casingPrefab = Resources.Load("BulletCasing") as GameObject;
				}

				PoolManager.PoolNetworkInstantiate(casingPrefab, nextShot.shooter.transform.position, nextShot.shooter.transform.parent);
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
		//if this is our gun (or server), last check to ensure we really can shoot
		if ((isServer || PlayerManager.LocalPlayer == shooter) &&
		    CurrentMagazine.ClientAmmoRemains <= 0)
		{
			if (isServer)
			{
				Logger.LogTrace("Server rejected shot - out of ammo", Category.Firearms);
			}

			return;
		}
		//Add too the cooldown timer to being allowed to shoot again
		FireCountDown += 1.0 / FireRate;
		CurrentMagazine.ExpendAmmo();
		//get the bullet prefab being shot
		GameObject bullet = PoolManager.PoolClientInstantiate(Resources.Load(Projectile.name) as GameObject,
			shooter.transform.position);

		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		if (isSuicideShot)
		{
			b.Suicide(shooter, this, damageZone);
		}
		else
		{
			b.Shoot(finalDirection, shooter, this, damageZone);
		}


		//add additional recoil after shooting for the next round
		AppendRecoil();

		SoundManager.PlayAtPosition(FiringSound, shooter.transform.position);
		//jerk screen back based on recoil angle and power
		if (shooter == PlayerManager.LocalPlayer)
		{
			//Default recoil params until each gun is configured separately
			if (CameraRecoilConfig == null || CameraRecoilConfig.Distance == 0f)
			{
				CameraRecoilConfig = new CameraRecoilConfig
				{
					Distance = 0.2f,
					RecoilDuration = 0.05f,
					RecoveryDuration = 0.6f
				};
			}
			Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
		}


		shooter.GetComponent<PlayerSprites>().ShowMuzzleFlash();
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
	public void ServerHandleReloadRequest(uint newMagazineID)
	{
		if (queuedLoadMagNetID != NetId.Invalid)
		{
			//can happen if client is spamming CmdLoadWeapon
			Logger.LogWarning("Player tried to queue a load action while a load action was already queued, ignoring the" +
			                  " second load.", Category.Firearms);
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
			                  " second unload.", Category.Firearms);
		}
		else if (queuedLoadMagNetID != NetId.Invalid)
		{
			Logger.LogWarning("Player tried to queue an unload action while a load action was already queued. Ignoring the unload.", Category.Firearms);
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

	private void SyncMagNetID(uint magazineID)
	{
		magNetID = magazineID;
		//if the magazine ID is invalid remove the magazine
		if (magazineID == NetId.Invalid)
		{
			CurrentMagazine = null;
		}
		else
		{
			//find the magazine by NetworkID
			GameObject magazine;
			if (isServer)
			{
				magazine = NetworkServer.FindLocalObject(magazineID);
			}
			else
			{
				magazine = ClientScene.FindLocalObject(magazineID);
			}

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
					//force a sync when mag is loaded
					CurrentMagazine.SyncPredictionWithServer();
				}

				Logger.LogTraceFormat("MagazineBehaviour found ok: {0}", Category.Firearms, magazineID);
			}
		}
	}

	#endregion

	#region Weapon Pooling

	private void OnDropItemServer()
	{
		serverIsHeld = false;
	}

	#endregion

	#region Weapon Sounds

	private void OutOfAmmoSFX()
	{
		SoundManager.PlayNetworkedAtPos( "OutOfAmmoAlarm", transform.position );
	}

	private void PlayEmptySFX()
	{
		SoundManager.PlayNetworkedAtPos( "EmptyGunClick", transform.position );
	}

	#endregion

	#region Weapon Network Supporting Methods

	/// <summary>
	/// Applies recoil to calcuate the final direction of the shot
	/// </summary>
	/// <param name="target">normalized target vector</param>
	/// <returns>final position after applying recoil</returns>
	private Vector2 ApplyRecoil(Vector2 target)
	{
		float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
		float angleVariance = MagSyncedRandomFloat(-CurrentRecoilVariance, CurrentRecoilVariance);
		Logger.LogTraceFormat("angleVariance {0}", Category.Firearms, angleVariance);
		float newAngle = angle * Mathf.Deg2Rad + angleVariance;
		Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
		return vec2;
	}

	private float MagSyncedRandomFloat(float min, float max)
	{
		return (float) (CurrentMagazine.CurrentRNG() * (max - min) + min);
	}

	private void AppendRecoil()
	{
		if (CurrentRecoilVariance < MaxRecoilVariance)
		{
			//get a random recoil
			float randRecoil = MagSyncedRandomFloat(CurrentRecoilVariance, MaxRecoilVariance);
			Logger.LogTraceFormat("randRecoil {0}", Category.Firearms, randRecoil);
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

 /// <summary>
 ///     Generic weapon types
 /// </summary>
 public enum WeaponType
 {
	 SemiAutomatic = 0,
	 FullyAutomatic = 1,
	 Burst = 2
 }