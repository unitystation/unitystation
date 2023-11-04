using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;
using Items;
using AddressableReferences;
using HealthV2;
using Logs;
using Messages.Server;
using Messages.Server.SoundMessages;
using Weapons.Projectiles;
using NaughtyAttributes;
using Player;
using Player.Movement;


namespace Weapons
{
	/// <summary>
	///  Allows an object to behave like a gun and fire shots. Server authoritative with client prediction.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(ItemStorage))]
	public class Gun : NetworkBehaviour, ICheckedInteractable<AimApply>, ICheckedInteractable<HandActivate>,
		ICheckedInteractable<InventoryApply>, IServerInventoryMove, IServerSpawn, IExaminable, ISuicide
	{
		/// <summary>
		/// Prefab to be spawned within on roundstart
		/// </summary>
		[SerializeField, Tooltip("The prefab to be spawned within the weapon on roundstart")]
		protected GameObject ammoPrefab = null;

		/// <summary>
		/// Optional ejected casing override, will default to the standard casing if left null and will only be used if SpawnsCasing is true
		/// </summary>
		[SerializeField, Tooltip("Optional casing override, defaults to standard casing when null")]
		private GameObject casingPrefabOverride = null;

		/// <summary>
		/// If false prevents players from removing the magazine from their weapon.
		/// </summary>
		[SerializeField, Tooltip("If the player is allowed to remove a loaded mag")]
		private bool allowMagazineRemoval = true;

		/// <summary>
		/// The type of ammo this weapon will allow, this is a string and not an enum for diversity
		/// </summary>
		[FormerlySerializedAs("AmmoType"), Tooltip("The type of ammo this weapon will use")]
		public AmmoType ammoType;

		[SerializeField, Tooltip("The sound this weapon makes when a magazine is loaded")]
		public AddressableAudioSource loadMagSound;

		[SerializeField, Tooltip("The sound this weapon makes when a magazine is removed")]
		public AddressableAudioSource unloadMagSound;

		//server-side object indicating the player holding the weapon (null if none)
		protected GameObject serverHolder;
		private RegisterTile shooterRegisterTile;

		protected StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer);

		/// <summary>
		/// The current magazine for this weapon, null means empty
		/// </summary>
		public MagazineBehaviour CurrentMagazine =>
			magSlot.Item != null ? magSlot.Item.GetComponent<MagazineBehaviour>() : null;

		/// <summary>
		/// The firing pin currently inside the weapon
		/// </summary>
		public PinBase FiringPin =>
			pinSlot.Item != null ? pinSlot.Item.GetComponent<PinBase>() : null;

		/// <summary>
		/// The firing pin to initally spawn within the gun
		/// </summary>
		[SerializeField, Tooltip("The firing pin initally inside the gun")]
		private GameObject pinPrefab = null;

		/// <summary>
		/// The suppressor that will initally spawn attached to the gun provided the gun is suppressable
		/// </summary>
		[SerializeField, Tooltip("The suppressor initally attached to the gun (if there is one)")]
		private GameObject suppressorPrefab = default;

		/// <summary>
		/// Checks if the weapon should spawn weapon casings
		/// </summary>
		[Tooltip("If the weapon should spawn casings ")]
		public bool SpawnsCasing = true;

		/// <summary>
		/// Whether the gun uses an internal magazine.
		/// </summary>
		[HideIf(nameof(SmartGun)), Tooltip("Effects if the gun will use an internal mag")]
		public bool MagInternal = false;

		/// <summary>
		/// If the gun should eject it's magazine automatically (external-magazine-specific)
		/// </summary>
		[HideIf(nameof(MagInternal)), Tooltip("If the gun should eject an empty mag automatically")]
		public bool SmartGun = false;

		/// <summary>
		/// The the current recoil variance this weapon has reached
		/// </summary>
		[NonSerialized] public float CurrentRecoilVariance;

		/// <summary>
		/// The countdown untill we can shoot again (seconds)
		/// </summary>
		[NonSerialized] public bool FireOnCooldowne = false;

		/// <summary>
		/// The name of the sound this gun makes when shooting
		/// </summary>
		[Tooltip("The name of the sound the gun uses when shooting (must be in soundmanager")]
		public AddressableAudioSource FiringSoundA = null;

		/// <summary>
		/// The name of the sound this gun makes when shooting
		/// </summary>
		[Tooltip(
			"The name of the sound the gun uses when shooting with a suppressor attached (must be in soundmanager")]
		public AddressableAudioSource SuppressedSoundA;

		/// <summary>
		/// The sound the gun makes while trying to fire without ammo.
		/// </summary>
		[Tooltip("The sound the gun uses when trying to fire without ammo.")]
		public AddressableAudioSource DryFireSound;

		/// <summary>
		/// The time in seconds before this weapon can fire again.
		/// </summary>
		[Tooltip("The time in seconds before this weapon can fire again.")]
		public float FireDelay = 0.5f;

		/// <summary>
		/// If suicide shooting should be prevented (for when user inadvertently drags over themselves during a burst)
		/// </summary>
		[Tooltip("If suicide shots should be prevented (if the user accidentally mouses over themselves during a shot")]
		public bool AllowSuicide;

		//TODO connect these with the actual shooting of a projectile
		/// <summary>
		/// The max recoil angle this weapon can reach with sustained fire
		/// </summary>
		public float MaxRecoilVariance;

		/// <summary>
		/// The traveling speed for this weapons projectile
		/// </summary>
		[Tooltip("The speed of the projectile")]
		public int ProjectileVelocity;

		/// <summary>
		/// Describes the recoil behavior of the camera when this gun is fired
		/// </summary>
		[Tooltip("Describes the recoil behavior of the camera when this gun is fired"),
		 SyncVar(hook = nameof(SyncCameraRecoilConfig))]
		public CameraRecoilConfig CameraRecoilConfig;

		/// <summary>
		/// The firemode this weapon will use (burst,semi,auto)
		/// </summary>
		[Tooltip("The firemode this weapon will use")]
		public WeaponType WeaponType;

		/// <summary>
		/// Bool that dictates if players can switch out the firing pin
		/// </summary>
		[SerializeField] protected bool allowPinSwap = true;


		private RegisterTile registerTile;
		[ReadOnly] public ItemSlot magSlot;
		[ReadOnly] public ItemSlot pinSlot;
		[ReadOnly] public ItemSlot suppressorSlot;

		protected const float PinRemoveTime = 10f;

		/// <summary>
		/// If true, displays a message whenever a gun is shot
		/// </summary>
		[SerializeField, SyncVar(hook = nameof(SyncIsSuppressed)), Tooltip("If the gun displays a shooter message")]
		private bool isSuppressed;

		public bool IsSuppressed => isSuppressed;

		/// <summary>
		/// Enables or disables the behaviour related to applying and removing suppressors from the gun
		/// </summary>
		[SerializeField, Tooltip("If suppressors can be applied or removed")]
		private bool isSuppressible = default;

		#region Init Logic

		private void Awake()
		{
			//init weapon with missing settings
			GetComponent<ItemAttributesV2>().AddTrait(CommonTraits.Instance.Gun);
			ItemStorage itemStorage = GetComponent<ItemStorage>();
			magSlot = itemStorage.GetIndexedItemSlot(0);
			pinSlot = itemStorage.GetIndexedItemSlot(1);
			suppressorSlot = itemStorage.GetIndexedItemSlot(2);
			registerTile = GetComponent<RegisterTile>();
			if (pinSlot == null || magSlot == null || itemStorage == null)
			{
				Loggy.LogWarning($"{gameObject.name} missing components, may cause issues", Category.Firearms);
			}
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			if (MagInternal)
			{
				//automatic ejection always disabled
				SmartGun = false;
				//ejecting an internal mag should never be allowed
				allowMagazineRemoval = false;
			}

			//Default recoil if one has not been set already
			if (CameraRecoilConfig == null || CameraRecoilConfig.Distance == 0f)
			{
				var Recoil = new CameraRecoilConfig
				{
					Distance = 0.2f,
					RecoilDuration = 0.05f,
					RecoveryDuration = 0.6f
				};
				SyncCameraRecoilConfig(CameraRecoilConfig, Recoil);
			}

			if (ammoPrefab == null)
			{
				Loggy.LogError($"{gameObject.name} magazine prefab was null, cannot auto-populate.",
					Category.Firearms);
				return;
			}

			//populate with a full external mag on spawn
			Loggy.LogTraceFormat("Auto-populate external magazine for {0}", Category.Firearms, name);
			Inventory.ServerAdd(Spawn.ServerPrefab(ammoPrefab).GameObject, magSlot);

			if (pinPrefab == null)
			{
				Loggy.LogError($"{gameObject.name} firing pin prefab was null, cannot auto-populate.",
					Category.Firearms);
				return;
			}

			Inventory.ServerAdd(Spawn.ServerPrefab(pinPrefab).GameObject, pinSlot);
			FiringPin.gunComp = this;

			if (suppressorPrefab != null && isSuppressed && isSuppressible)
			{
				Inventory.ServerAdd(Spawn.ServerPrefab(suppressorPrefab).GameObject, suppressorSlot);
			}
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;

			if (info.ToPlayer != null)
			{
				serverHolder = info.ToPlayer.gameObject;
				shooterRegisterTile = serverHolder.GetComponent<RegisterTile>();
			}
			else
			{
				serverHolder = null;
				shooterRegisterTile = null;
			}
		}

		#endregion

		#region HandActivate

		public virtual bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//try ejecting the mag if external
			if (CurrentMagazine != null && allowMagazineRemoval && !MagInternal)
			{
				return true;
			}

			return false;
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			if (CurrentMagazine != null && allowMagazineRemoval && !MagInternal)
			{
				ServerHandleUnloadRequest();
			}
		}

		#endregion

		#region InventoryApply

		public virtual bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only reload if the gun is the target and item being used on us is in hand slot
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Suppressor) ||
				    Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) ||
				    Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin) ||
				    interaction.IsAltClick)
				{
					return true;
				}
				else if (interaction.UsedObject != null)
				{
					MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
					if (mag)
					{
						return TryReload(mag.gameObject);
					}
				}
			}

			return false;
		}

		public virtual void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (interaction.UsedObject != null)
				{
					MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
					if (mag)
					{
						ServerHandleReloadRequest(mag.gameObject);
					}
					else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Suppressor) &&
					         !isSuppressed && isSuppressible)
					{
						SyncIsSuppressed(isSuppressed, true);
						Inventory.ServerTransfer(interaction.FromSlot, suppressorSlot);
					}
					else
					{
						PinInteraction(interaction);
					}
				}
				else if (isSuppressed && isSuppressible && suppressorSlot.Item != null)
				{
					SyncIsSuppressed(isSuppressed, false);
					Inventory.ServerTransfer(suppressorSlot, interaction.FromSlot);
				}
			}
		}

		#endregion

		#region AimApply

		public virtual bool WillInteract(AimApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (CurrentMagazine == null)
			{
				PlayEmptySfx();
				if (side == NetworkSide.Server)
				{
					Loggy.LogTrace("Server rejected shot - No magazine being loaded", Category.Firearms);
				}

				return false;
			}

			if (FiringPin == null)
			{
				if (interaction.Performer == PlayerManager.LocalPlayerObject)
				{
					Chat.AddExamineMsgToClient("The " + gameObject.ExpensiveName() +
					                           "'s trigger is locked. It doesn't have a firing pin installed!");
				}

				Loggy.LogTrace("Rejected shot - no firing pin", Category.Firearms);
				return false;
			}


			if (FireOnCooldowne == false)
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

			return false;
		}

		private IEnumerator DelayGun()
		{
			FireOnCooldowne = true;
			yield return WaitFor.Seconds(FireDelay);
			FireOnCooldowne = false;
		}

		public virtual void ServerPerformInteraction(AimApply interaction)
		{
			if (CurrentMagazine.ServerAmmoRemains <= 0)
			{
				if (SmartGun && allowMagazineRemoval) // smartGun is forced off when using an internal magazine
				{
					ServerHandleUnloadRequest();
					OutOfAmmoSfx();
				}
				else
				{
					PlayEmptySfx();
				}

				StartCoroutine(DelayGun());
				return;
			}

			//do we need to check if this is a suicide (want to avoid the check because it involves a raycast).
			//case 1 - we are beginning a new shot, need to see if we are shooting ourselves
			//case 2 - we are firing an automatic and are currently shooting ourselves, need to see if we moused off
			//	ourselves.
			var isSuicide = false;
			if (interaction.MouseButtonState == MouseButtonState.PRESS ||
			    (WeaponType != WeaponType.SemiAutomatic && AllowSuicide))
			{
				if (Manager3D.Is3D == false)
				{
					isSuicide = interaction.IsAimingAtSelf;
					AllowSuicide = isSuicide;
				}
			}

			if (FiringPin != null)
			{
				FiringPin.ServerBehaviour(interaction, isSuicide);
			}
		}

		#endregion

		public virtual string Examine(Vector3 pos)
		{
			StringBuilder exam = new StringBuilder();
			exam.AppendLine($"{WeaponType} - Fires {ammoType} ammunition")
				.AppendLine(CurrentMagazine != null
					? $"{CurrentMagazine.ServerAmmoRemains} rounds loaded"
					: "It's empty!")
				.AppendLine(FiringPin != null
					? $"It has a {FiringPin.gameObject.ExpensiveName()} installed"
					: "It doesn't have a firing pin installed, it won't fire");
			return exam.ToString();
		}

		#region Weapon Firing Mechanism

		protected void PinInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) && allowPinSwap)
			{
				PinRemoval(interaction);
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin) && allowPinSwap)
			{
				PinAddition(interaction);
			}
		}

		/// <summary>
		/// method that handles inserting a firing pin into a firearm
		/// </summary>
		private void PinAddition(InventoryApply interaction)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You insert the {interaction.UsedObject.gameObject.ExpensiveName()} into {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} inserts the {interaction.UsedObject.gameObject.ExpensiveName()} into {gameObject.ExpensiveName()}.");
			Inventory.ServerTransfer(interaction.FromSlot, pinSlot);
			FiringPin.gunComp = this;
		}

		/// <summary>
		/// method that handles removal of a firing pin
		/// </summary>
		private void PinRemoval(InventoryApply interaction)
		{
			void ProgressFinishAction()
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You remove the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} removes the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}.");

				FiringPin.gunComp = null;

				Inventory.ServerDrop(pinSlot);
			}

			var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
				.ServerStartProgress(interaction.Performer.RegisterTile(), PinRemoveTime, interaction.Performer);

			if (bar != null)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You begin removing the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} begins removing the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}.");

				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.WireCutter,
					interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: serverHolder);
			}
		}

		/// <summary>
		/// clientside method that checks if the player can reload
		/// </summary>
		/// <param name="ammo">gameobject of the magazine</param>
		private bool TryReload(GameObject ammo)
		{
			MagazineBehaviour magazine = ammo.GetComponent<MagazineBehaviour>();
			if (CurrentMagazine == null || (MagInternal && magazine.magType == MagType.Clip))
			{
				//RELOAD
				// If the item used on the gun is a magazine, check type and reload
				if (ammoType == magazine.ammoType)
				{
					return true;
				}

				if (ammoType != magazine.ammoType)
				{
					Chat.AddExamineMsgToClient("You try to load the wrong ammo into your weapon.");
					return false;
				}
			}
			else if (ammoType == magazine.ammoType)
			{
				Chat.AddExamineMsgToClient("Your weapon is already loaded, you can't fit more Magazines in it, silly!");
				return false;
			}

			return false;
		}

		/// <summary>
		/// Perform an actual shot on the server, putting the request to shoot into our queue
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
			//don't enqueue the shot if the player is no longer able to shoot
			PlayerScript shooter = shotBy.GetComponent<PlayerScript>();
			if (!Validations.CanInteract(shooter, NetworkSide.Server))
			{
				Loggy.LogTrace("Server rejected shot: shooter cannot interact", Category.Firearms);
				return;
			}

			DequeueAndProcessServerShot(shotBy, target,
				damageZone, isSuicideShot);
		}

		/// <summary>
		/// Gets the next shot from the queue (if there is one) and performs the server-side shot (which will
		/// perform damage calculation when the bullet hits stuff), informing all
		/// clients to display the shot.
		/// </summary>
		[Server]
		private void DequeueAndProcessServerShot(GameObject shotBy, Vector2 target,
			BodyPartType damageZone, bool isSuicideShot)
		{
			// check if we can still shoot
			MovementSynchronisation shooter = shotBy.GetComponent<MovementSynchronisation>();
			PlayerScript shooterScript = shotBy.GetComponent<PlayerScript>();
			if (shooter.AllowInput == false || shooterScript.IsNormal == false)
			{
				Loggy.Log("A player tried to shoot when not allowed or when they were a ghost.", Category.Exploits);
				Loggy.LogWarning("A shot was attempted when shooter is a ghost or is not allowed to shoot.",
					Category.Firearms);
				return;
			}


			if (CurrentMagazine == null || CurrentMagazine.ServerAmmoRemains <= 0 ||
			    CurrentMagazine.containedBullets[0] == null)
			{
				Loggy.LogTrace("Player tried to shoot when there was no ammo.", Category.Exploits);
				Loggy.LogWarning("A shot was attempted when there is no ammo.", Category.Firearms);
				return;
			}

			if (FireOnCooldowne)
			{
				Loggy.LogTrace("Player tried to shoot too fast.", Category.Exploits);
				Loggy.LogWarning("Shot was attempted to be dequeued when the fire count down is not yet at 0.",
					Category.Exploits);
				return;
			}

			GameObject toShoot = CurrentMagazine.containedBullets[0];
			int quantity = CurrentMagazine.containedProjectilesFired[0];

			if (toShoot == null)
			{
				Loggy.LogError("Shot was attempted but no projectile or quantity was found to use", Category.Firearms);
				return;
			}

			var finalDirection = ApplyRecoil(target);
			//perform the actual server side shooting, creating the bullet that does actual damage
			DisplayShot(shotBy, finalDirection, damageZone, isSuicideShot, toShoot,
				quantity);

			StartCoroutine(DelayGun());
			//trigger a hotspot caused by gun firing
			shooterRegisterTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(
				shooter.gameObject.TileWorldPosition(), 500);

			if (isSuppressed == false && isSuicideShot == false)
			{
				Chat.AddActionMsgToChat(serverHolder,
					$"You fire your {gameObject.ExpensiveName()}",
					$"{serverHolder.ExpensiveName()} fires their {gameObject.ExpensiveName()}");
			}

			//kickback
			shooterScript.ObjectPhysics.NewtonianPush((-finalDirection).NormalizeToInt(), 1);

			if (SpawnsCasing)
			{
				if (casingPrefabOverride == null)
				{
					//no casing override set, use normal casing prefab
					casingPrefabOverride = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("BulletCasing");
				}

				Spawn.ServerPrefab(casingPrefabOverride, shooterScript.transform.position,
					shooterScript.transform.parent);
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
		/// <param name="projectileName">the name of the projectile that should be spawned</param>
		/// <param name="quantity">the amount of projectiles to spawn when displaying the shot</param>
		public void DisplayShot(GameObject shooter, Vector2 finalDirection,
			BodyPartType damageZone, bool isSuicideShot, GameObject projectile, int quantity)
		{
			if (!MatrixManager.IsInitialized) return;

			//if this is our gun (or server), last check to ensure we really can shoot
			if (isServer || PlayerManager.LocalPlayerObject == shooter)
			{
				if (CurrentMagazine.ClientAmmoRemains <= 0)
				{
					if (isServer)
					{
						Loggy.LogTrace("Server rejected shot - out of ammo", Category.Firearms);
					}

					return;
				}

				CurrentMagazine.ExpendAmmo();
			}
			//TODO: If this is not our gun, simply display the shot, don't run any other logic

			//this is our gun so we need to update our predictions

			//add additional recoil after shooting for the next round
			AppendRecoil();


			if (isSuicideShot)
			{
				GameObject Newprojectile = Spawn.ServerPrefab(projectile,
					shooter.transform.position, parent: shooter.transform.parent).GameObject;
				Projectile projectileComponent = Newprojectile.GetComponent<Projectile>();
				projectileComponent.Suicide(shooter, this, damageZone);
			}
			else
			{
				for (int n = 0; n < quantity; n++)
				{
					GameObject Newprojectile = Spawn.ServerPrefab(projectile,
						shooter.transform.position, parent: shooter.transform.parent).GameObject;
					Projectile projectileComponent = Newprojectile.GetComponent<Projectile>();
					Vector2 finalDirectionOverride = CalcDirection(finalDirection, n);
					projectileComponent.Shoot(finalDirectionOverride, shooter, this, damageZone);
				}
			}

			if (isSuppressed && SuppressedSoundA != null)
			{
				SoundManager.PlayNetworkedAtPos(SuppressedSoundA, shooter.transform.position);
			}
			else
			{
				SoundManager.PlayNetworkedAtPos(FiringSoundA, shooter.transform.position);
			}

			var identity = shooter.GetComponent<NetworkIdentity>();
			RPCShowMuzzleFlash(identity);
			if (identity.OrNull()?.connectionToClient != null)
			{
				if (isServer && shooter == PlayerManager.LocalPlayerObject)
				{
					Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
				}

				RPCShowRecoil(identity.connectionToClient, finalDirection);
			}
		}

		[TargetRpc]
		public void RPCShowRecoil(NetworkConnection target, Vector2 finalDirection)
		{
			Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
		}

		[ClientRpc]
		public void RPCShowMuzzleFlash(NetworkIdentity target)
		{
			target.GetComponent<PlayerSprites>().ShowMuzzleFlash();
		}

		private Vector2 CalcDirection(Vector2 direction, int iteration)
		{
			if (iteration == 0) return direction;

			// trying to get clientside prediction work with random spread
			// is far too difficult/expensive, so set spread it is
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			float angleVariance = iteration / 1f;
			float angleDeviation;
			if (iteration % 2 == 0)
			{
				angleDeviation = angleVariance; //even
			}
			else
			{
				angleDeviation = -angleVariance; //odd
			}

			float newAngle = (angle + angleDeviation) * Mathf.Deg2Rad;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		#endregion

		#region Weapon Loading and Unloading

		/// <summary>
		/// Invoked when server receives a request to load a new magazine. Queues up the reload to occur
		/// when the shot queue is empty.
		/// </summary>
		/// <param name="mag">gameobject of the magazine we are trying to load</param>
		[Server]
		public void ServerHandleReloadRequest(GameObject mag)
		{
			if (CurrentMagazine == null)
			{
				Loggy.LogWarning($"Why is {nameof(CurrentMagazine)} null for {this}?", Category.Firearms);
			}

			if (MagInternal)
			{
				var clip = mag;
				MagazineBehaviour clipComp = clip.GetComponent<MagazineBehaviour>();
				string message = CurrentMagazine.LoadFromClip(clipComp);
				Chat.AddExamineMsg(serverHolder, message);
			}
			else
			{
				LoadMagSound();
				var magazine = mag;
				var fromSlot = magazine.GetComponent<Pickupable>().ItemSlot;
				Inventory.ServerTransfer(fromSlot, magSlot);
			}
		}

		/// <summary>
		/// Invoked when server recieves a request to unload the current magazine. Queues up the unload to occur
		/// when the shot queue is empty.
		/// </summary>
		public void ServerHandleUnloadRequest()
		{
			ItemSlot hand = GetComponent<Pickupable>()?.ItemSlot?.Player.OrNull()?.GetComponent<DynamicItemStorage>()?
				.GetBestHand();

			if (MagInternal == true)
			{
				return;
			}


			UnloadMagSound();
			Inventory.ServerDrop(magSlot);
		}

		#endregion

		#region Weapon Sounds

		private void OutOfAmmoSfx()
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GunEmptyAlarm, transform.position,
				sourceObj: serverHolder);
		}

		public void LoadMagSound()
		{
			SoundManager.PlayNetworkedAtPos(loadMagSound, gameObject.AssumedWorldPosServer());
		}

		public void PlayEmptySfx()
		{
			SoundManager.PlayNetworkedAtPos(DryFireSound, transform.position, sourceObj: serverHolder);
		}

		#endregion

		#region Weapon Network Supporting Methods

		/// <summary>
		/// Syncs suppressed bool.
		/// </summary>
		private void SyncIsSuppressed(bool oldValue, bool newValue)
		{
			isSuppressed = newValue;
		}

		public void UnloadMagSound()
		{
			SoundManager.PlayNetworkedAtPos(unloadMagSound, transform.position,
				sourceObj: serverHolder);
		}

		/// <summary>
		/// Syncs the recoil config.
		/// </summary>
		public void SyncCameraRecoilConfig(CameraRecoilConfig oldValue, CameraRecoilConfig newValue)
		{
			CameraRecoilConfig = newValue;
		}

		/// <summary>
		/// Applies recoil to calcuate the final direction of the shot
		/// </summary>
		/// <param name="target">normalized target vector</param>
		/// <returns>final position after applying recoil</returns>
		public Vector2 ApplyRecoil(Vector2 target)
		{
			float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
			float angleVariance = MagSyncedRandomFloat(-CurrentRecoilVariance, CurrentRecoilVariance);
			Loggy.LogTraceFormat("angleVariance {0}", Category.Firearms, angleVariance);
			float newAngle = angle * Mathf.Deg2Rad + angleVariance;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		private float MagSyncedRandomFloat(float min, float max)
		{
			return (float) (CurrentMagazine.CurrentRng() * (max - min) + min);
		}

		private void AppendRecoil()
		{
			if (CurrentRecoilVariance < MaxRecoilVariance)
			{
				//get a random recoil
				float randRecoil = MagSyncedRandomFloat(CurrentRecoilVariance, MaxRecoilVariance);
				Loggy.LogTraceFormat("randRecoil {0}", Category.Firearms, randRecoil);
				CurrentRecoilVariance += randRecoil;
				//make sure the recoil is not too high
				if (CurrentRecoilVariance > MaxRecoilVariance)
				{
					CurrentRecoilVariance = MaxRecoilVariance;
				}
			}
		}

		#endregion

		public bool CanSuicide(GameObject performer)
		{
			if (AllowSuicide == false) return false;
			return CurrentMagazine != null && CurrentMagazine.ServerAmmoRemains != 0;
		}

		public IEnumerator OnSuicide(GameObject performer)
		{
			yield return SuicideAction(performer);
		}

		/// <summary>
		/// Because each gun can have it's own functionality related to death (example : russian roulette with a revolver)
		/// this functionality is split in a virtual function that all scripts that inherit from the gun class can modify.
		/// </summary>
		protected virtual IEnumerator SuicideAction(GameObject performer)
		{
			DequeueAndProcessServerShot(performer, performer.RegisterTile().LocalPosition.ToLocal(), BodyPartType.Head,
				true);
			var health = performer.GetComponent<LivingHealthMasterBase>();
			health.ApplyDamageAll(performer, health.MaxHealth / 2, AttackType.Bullet, DamageType.Brute);
			yield return null;
		}
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
	/// Generic weapon types
	/// </summary>
	public enum WeaponType
	{
		SemiAutomatic = 0,
		FullyAutomatic = 1,
		Burst = 2
	}
}