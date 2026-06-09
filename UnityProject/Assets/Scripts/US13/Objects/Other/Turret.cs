using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;
using US13.Core;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Core.Sprite_Handler;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Items.Weapons;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Messages.Server;
using US13.NPC.AI;
using US13.Objects.Engineering;
using US13.Objects.Wallmounts.Switches;
using US13.Player;
using US13.Projectiles;
using US13.Systems.Clearance;
using US13.Systems.Construction;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using US13.UI.Core.Net;
using US13.UI.Core.ProgressBar;
using US13.UI.Objects.Security.SecurityRecordsConsole;
using US13.UI.Systems.Jobs;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;


namespace US13.Objects.Other
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(APCPoweredDevice))]
	public class Turret : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable, IExaminable, IServerSpawn, ICanOpenNetTab
	{
		[SerializeField]
		[Tooltip("Installed gun, determining projectile fired and base fire delay when set to lethal mode and gun to spawn when deconstructed")]
		private GameObject spawnGun = null;

		[SerializeField]
		private int range = 7;

		[SerializeField]
		[Tooltip("Used as backup if no gun installed or installed gun's bullet can't be found")]
		private Bullet defaultProjectile = null;

		[SerializeField]
		[Tooltip("Projectile the turret fires when set to stun mode")]
		private Bullet stunBullet = null;

		[SerializeField]
		[Range(1,100)]
		[Tooltip("Chance to lose gun during deconstruction")]
		private int gunLossChance = 30;

		[SerializeField]
		[Range(0.1f,30f)]
		[Tooltip("Multiplier applied to the fire delay of the gun installed in this turret (Lower is quicker; Minimum final fire delay bound of 0.5). Not applied to Default or Stun projectiles.")]
		private float fireDelayMultiplier = 1.5f;

		[SerializeField]
		[Range(0.5f,60f)]
		[Tooltip("Fire delay for assigned lethal mode projectile if no gun installed (Lower is quicker)")]
		private float defaultFireDelay = 1.5f;

		[SerializeField]
		[Range(0.5f,60f)]
		[Tooltip("Fire delay for assigned stun mode projectile (Lower is quicker)")]
		private float stunFireDelay = 1.5f;

		[SerializeField]
		private GameObject framePrefab = null;

		[SerializeField]
		private SpriteHandler gunSprite = null;

		[SerializeField]
		private SpriteHandler frameSprite = null;

		[SerializeField]
		private AddressableAudioSource taserSound = null;

		[SerializeField]
		private bool ballisticTurret;

		[SerializeField] private LayerMask lineOfSightMask = new();

		[SerializeField]
		[Tooltip("If turret not normal, then will always target mobs, if turret is AI then will ignore all other checks and always fire")]
		//If turret not normal, then will always target mobs, if turret is Ai then will ignore all other checks and
		//always fire
		private TurretType turretType = TurretType.Normal;

		[SerializeField] private LayerTile[] toIgnore;

#pragma warning disable CS0414 // disable unused compiler warning
		[SyncVar(hook = nameof(SyncOpen))]
		private bool open;
#pragma warning restore CS0414

		private enum TurretType
		{
			Normal = 0,
			Ai = 1,
			Syndicate = 2
		}

		//Check for Weapon Authorization:
		[Tooltip("Fire at individuals holding a weapon (currently checks for Gun trait) without displaying a clearance that passes AllowedWeaponBearersRestrictions")]
		public bool CheckWeaponAuthorisation;

		[SerializeField] private ClearanceRestricted weaponAuthorisationClearance;

		//Check Security Records:
		//Yes/No - searches Security Records for criminals.
		[Tooltip("Fire at individuals set to Criminal in the Security Records")]
		public bool CheckSecurityRecords = true;

		//Neutralize Identified Criminals:
		//Yes/No - neutralizes crew members set to Arrest in the Security Records.
		[Tooltip("Fire at individuals set to Arrest in the Security Records")]
		public bool CheckForArrest = true;

		//Fire at individuals not displaying a clearance that passes AllowedCrewRestrictions list conditions
		//No/Yes - self explanatory.
		[Tooltip("Fire at individuals not displaying a clearance that passes AllowedCrewRestrictions")]
		public bool CheckUnauthorisedPersonnel;

		[SerializeField] private ClearanceRestricted authorisedClearance;

		//Neutralize All Unidentified Life Signs:
		//Yes/No - neutralizes aliens. <= this might no longer be true w/r/t MobV1s
		[Tooltip("Neutralize all unidentified life signs")]
		public bool CheckUnidentifiedLifeSigns = true;

		private RegisterTile registerTile;
		private Integrity integrity;

		private GameObject target;
		private bool shootingTarget;

		private Gun gun;
		private string cachedDefaultProjectileName;

		private const float UpdateTimer = 0.1f;
		private const float DetectTime = 1.5f;
		private const float MinimumShotCooldown = 0.5f;

		private const string MISSING_PROJECTILE = "MissingDefaultProjectile";

		private float shootingTimer = 0;
		private float detectTimer = 0;

		private float shootSpeed = 1.5f;

		//Has power
		private bool hasPower;
		public bool HasPower => hasPower;

		//Cover open or closed
		private bool coverOpen;
		private bool coverStateChanging;

		//Is off, stun or lethal
		private TurretState turretState;
		public TurretState CurrentTurretState => turretState;

		//Unlocked
		private bool unlocked;

		private ItemStorage itemStorage;
		private APCPoweredDevice apcPoweredDevice;
		[SerializeField]  private ClearanceRestricted tinkerRequiredClearance;

		//Used to debug player searching linecast
		private LineRenderer lineRenderer;

		[SyncVar(hook = nameof(SyncRotation))]
		private Vector2 rotationAngle;

		private TurretSwitch connectedSwitch;

		private string bulletName;
		private AddressableAudioSource bulletSound;

		[field: SerializeField] public bool CanRelink { get; set; } = true;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
			lineRenderer = GetComponentInChildren<LineRenderer>();
			cachedDefaultProjectileName = defaultProjectile != null ? defaultProjectile.name : MISSING_PROJECTILE;

		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (ballisticTurret)
			{
				coverOpen = true;
			}

			if(spawnGun == null || gun != null) return;

			var newGun = Spawn.ServerPrefab(spawnGun, registerTile.WorldPosition, transform.parent);

			if (newGun.Successful == false) return;

			gun = newGun.GameObject.GetComponent<Gun>();

			SetUpBullet();

			//For some reason couldnt use item storage, would just stay above the turret
			gun.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateLoop, UpdateTimer);
			integrity.OnWillDestroyServer.AddListener(OnTurretDestroy);
			apcPoweredDevice.OnStateChangeEvent += (OnPowerStateChange);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
			integrity.OnWillDestroyServer.RemoveListener(OnTurretDestroy);
			apcPoweredDevice.OnStateChangeEvent -= OnPowerStateChange;
			apcPoweredDevice.LockApcLinking(unlocked == false);

			if (connectedSwitch is TurretSwitch generalSwitch)
			{
				generalSwitch.RemoveTurretFromSwitch(this);
			}
		}

		#endregion

		private void SyncRotation(Vector2 oldValue, Vector2 newValue)
		{
			//transform.localEulerAngles = new Vector3(0, 0, rotation);
			var angle = Mathf.Atan2(newValue.y, newValue.x) * Mathf.Rad2Deg;

			//Ballistic turret sprite is initially pointing upwards, whereas we assume it points down for laser
			//so add 180
			if (ballisticTurret)
			{
				angle += 180;
			}

			gunSprite.transform.rotation = Quaternion.AngleAxis(angle + 90, Vector3.forward);
		}

		private void SyncOpen(bool oldState, bool newState)
		{
			//Changes gun sprite order so it is either in front of or behind frame
			gunSprite.SpriteRenderer.sortingOrder = newState ? 1 : 0;
		}

		private void UpdateLoop()
		{
			//Need to have power
			if(hasPower == false) return;

			shootingTimer += UpdateTimer;
			detectTimer += UpdateTimer;

			if (turretState != TurretState.Off && detectTimer >= DetectTime)
			{
				detectTimer = 0;
				SearchForMobs();
			}

			ChangeCoverState();

			if (shootingTarget || coverOpen == false || turretState == TurretState.Off || target == null) return;
			if(shootingTimer < shootSpeed) return;
			shootingTarget = true;
			shootingTimer = 0;

			ShootTarget();
		}

		#region Shooting

		private void SearchForMobs()
		{
			var turretPos = registerTile.WorldPosition;
			var foundPlayers = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToLocation(turretPos, range, false);
			if (foundPlayers == null)
			{
				//No targets
				target = null;
				return;
			}
			foreach (var player in foundPlayers.OrderBy(player => (turretPos - player.transform.position).sqrMagnitude))
			{
				if (player == null) continue;
				if (player.playerScript.IsNormal == false || player.playerScript.IsDeadOrGhost) continue;
				if(turretType != TurretType.Ai && ValidatePlayer(player.playerScript)) continue;
				Vector3 worldPos = player.gameObject.AssumedWorldPosServer();

				var linecast = MatrixManager.Linecast(turretPos, LayerTypeSelection.Walls, lineOfSightMask, worldPos, toIgnore);
				if(linecast.ItHit) continue;

				target = player.gameObject;
				return;
			}
			//No targets
			target = null;
		}

		/// <summary>
		/// Validate player based on settings of turret, true if player is allowed, false if player failed validation
		/// </summary>
		private bool ValidatePlayer(PlayerScript script)
		{
			//Neutralize All Unauthorised Personnel
			if (CheckUnauthorisedPersonnel)
			{
				var allowed = authorisedClearance.HasClearance(script.gameObject);
				//Check for failure
				if (allowed == false) return false;
			}

			//Check for Weapon Authorization
			if (CheckWeaponAuthorisation && script.Equipment != null)
			{
				if (CheckSlot(NamedSlot.rightHand) == false || CheckSlot(NamedSlot.leftHand) == false) return false;
				bool CheckSlot(NamedSlot slot)
				{
					var handItem = script.Equipment.GetClothingItem(slot);
					if (handItem == null) return true;
					//TODO: add melee weapons, grenades, etc., using traits as most appropriate
					if (Validations.HasItemTrait(handItem.ServerGameObjectReference, CommonTraits.Instance.Gun))
					{
						//Only allow authorised people to have weapons
						bool allowed = weaponAuthorisationClearance.HasClearance(script.gameObject);
						//Check for failure
						if (allowed == false) return false;
					}
					//Passed weapons check for this hand
					return true;
				}
			}

			if ((CheckForArrest || CheckSecurityRecords) == false) return true;

			var hasRecord = false;
			foreach (var record in CrewManifestManager.Instance.SecurityRecords)
			{
				//Check to see if we have record
				if (record.characterSettings.Name.Equals(script.visibleName) == false) continue;

				//Check Security Records For Criminals (and shoot at them)
				if (CheckSecurityRecords && record.Status == SecurityStatus.Criminal) return false;

				//Neutralize Identified Criminals
				if (CheckForArrest && record.Status == SecurityStatus.Arrest) return false;

				hasRecord = true;
				break;
			}

			//Neutralize All Unidentified Life Signs
			//Unknown name check here or else it would be possible for someone to add record with the name Unknown
			//and then wouldn't be targeted by turrets
			if (CheckUnidentifiedLifeSigns && (hasRecord == false || script.visibleName.Equals("Unknown")))
			{
				return false;
			}

			return true;
		}

		private void ShootTarget()
		{
			var angleToShooter = CalculateAngle(target.AssumedWorldPosServer());
			rotationAngle = angleToShooter;

			ShootAtDirection(angleToShooter);
			shootingTarget = false;
		}

		private Vector2 CalculateAngle(Vector2 target)
		{
			return (registerTile.WorldPosition.To2Int() - target).Rotate(180);
		}

		private void ShootAtDirection(Vector2 rotationToShoot)
		{
			if (hasPower == false || turretState == TurretState.Off) return;

			SoundManager.PlayNetworkedAtPos(bulletSound, registerTile.WorldPosition, sourceObj: gameObject);

			ProjectileManager.InstantiateAndShoot(bulletName,
				rotationToShoot, gameObject, null, BodyPartType.None, Target: target);
		}

		#endregion

		public void SetUpTurret(Gun newGun, ItemSlot fromSlot)
		{
			gun = newGun;
			SetUpBullet();

			itemStorage.ServerTryTransferFrom(fromSlot);
		}

		//Called when ApcPoweredDevice changes state
		private void OnPowerStateChange(PowerState old,  PowerState newStates)
		{
			SetPower(newStates != PowerState.Off);

			//Allow for instant shoot
			shootingTimer = shootSpeed;
			detectTimer = DetectTime;
		}

		private void SetPower(bool newState)
		{
			hasPower = newState;

			//Change state to what it needs to be
			//Will switch off if needed
			ChangeBulletState(turretState);

			if(newState == false) return;

			//If we have power check to see if we need to close or open
			ChangeCoverState();
		}

		public void ChangeBulletState(TurretState newState)
		{
			turretState = newState;
			UpdateGui();

			//No power or off set sprite to off state
			if (hasPower == false || newState == TurretState.Off)
			{
				gunSprite.SetCatalogueIndexSprite(0);
				return;
			}

			//Set up bullets
			SetUpBullet();

			//Stun or lethal
			gunSprite.SetCatalogueIndexSprite(newState == TurretState.Stun ? 1 : 2);
		}

		private void SetUpBullet()
		{
			if (turretState == TurretState.Stun)
			{
				bulletName = stunBullet.name;
				bulletSound = taserSound;
				// Limit the final shot cooldown to the minimum floor constant
				shootSpeed = stunFireDelay;
				return;
			}

			shootSpeed = defaultFireDelay;

			if (gun != null)
			{
				if (gun is GunElectrical electrical && electrical.firemodeProjectiles.Count > 0 && electrical.firemodeFiringSound.Count > 0)
				{
					bulletName = electrical.firemodeProjectiles[0].GetComponent<Bullet>().name;
					bulletSound = electrical.firemodeFiringSound[0];
					shootSpeed = Mathf.Max((float)electrical.FireDelay * fireDelayMultiplier, MinimumShotCooldown);
					return;
				}
				else if (gun.CurrentMagazine != null)
				{
					bulletName = gun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().name;
					bulletSound = gun.FiringSoundA;
					shootSpeed = Mathf.Max((float)gun.FireDelay * fireDelayMultiplier, MinimumShotCooldown);
					return;
				}
				else
				{
					// Default to assigned projectile otherwise
					// Do we actually want to let the turret fire an arbitrary projectile if the installed gun exists but has no magazine? -FD
					bulletName = cachedDefaultProjectileName;
					bulletSound = gun.FiringSoundA;
				}
			}
			else
			{
				bulletName = cachedDefaultProjectileName;
			}
			// Limit the final shot cooldown to the minimum floor constant
			shootSpeed = Mathf.Max(shootSpeed, MinimumShotCooldown);
		}

		private void ChangeCoverState()
		{
			//Don't have cover on ballistic turret
			if(ballisticTurret) return;

			//If changing cover state don't check
			if (coverStateChanging) return;

			//Open if we need to
			if (coverOpen == false && turretState != TurretState.Off && target != null)
			{
				StartCoroutine(WaitForAnimation(true));
				return;
			}

			//Dont bother closing if we are already closed and Only close if no targets and we are open and have power
			if(coverOpen == false || (coverOpen && turretState != TurretState.Off && target != null)) return;

			//Close
			StartCoroutine(WaitForAnimation(false));
		}

		//Wait for animation before allowing to fire
		private IEnumerator WaitForAnimation(bool stateAfter)
		{
			coverStateChanging = true;

			//Set gun behind frame before closing
			if (stateAfter == false)
			{
				open = false;
			}

			frameSprite.AnimateOnce(stateAfter ? 1 : 3);

			//Wait for animation to complete before allowing to fire or change cover state
			yield return new WaitForSeconds(0.55f);

			//Set gun in front frame when open
			if (stateAfter)
			{
				open = true;
			}

			coverOpen = stateAfter;
			coverStateChanging = false;
		}

		public enum TurretState
		{
			Off,
			Stun,
			Lethal
		}

		#region Hand Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Id)) return true;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//If Id try unlock
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Id))
			{
				if (tinkerRequiredClearance.HasClearance(interaction.PerformerPlayerScript.Access) == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You need higher authorisation to unlock this {gameObject.ExpensiveName()}");
					return;
				}

				//If unlocked then quick to lock, if locked then if unconnected to switch quick to unlock
				//Else locked and connected so take long to stop rush unlocking to switch turrets off
				var time = unlocked ? 1f :
					connectedSwitch != null ? 10f : 1f;

				var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, true), Perform);
				bar.ServerStartProgress(interaction.Performer.RegisterTile(), time, interaction.Performer);

				void Perform()
				{
					unlocked = !unlocked;
					apcPoweredDevice.LockApcLinking(unlocked == false);

					Chat.AddActionMsgToChat(interaction.Performer, $"You {(unlocked ? "unlock" : "lock")} the {gameObject.ExpensiveName()}",
						$"{interaction.Performer.ExpensiveName()} {(unlocked ? "unlocks" : "locks")} the {gameObject.ExpensiveName()}");

					if (unlocked == false)
					{
						//Force close net tab when locked
						TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Turret, TabAction.Close);
					}
				}

				return;
			}

			//Try Deconstruct
			ToolUtils.ServerUseToolWithActionMessages(interaction, 15f,
				"You start prying off the outer metal cover from the turret...",
				$"{interaction.Performer.ExpensiveName()} starts prying off the outer metal cover from the turret...",
				"You pry off the outer metal cover from the turret.",
				$"{interaction.Performer.ExpensiveName()} prys off the outer metal cover from the turret.",
				() =>
				{
					var frame = Spawn.ServerPrefab(framePrefab, registerTile.WorldPosition, transform.parent);

					if (DMMath.Prob(gunLossChance) && gun != null)
					{
						_ = Despawn.ServerSingle(gun.gameObject);
						return;
					}

					frame.GameObject.GetComponent<TurretFrame>().SetUp(gun != null ? gun.GetComponent<Pickupable>() : null);

					_ = Despawn.ServerSingle(gameObject);
				});
		}

		#endregion

		private void OnTurretDestroy(DestructionInfo info)
		{
			Spawn.ServerPrefab(framePrefab, registerTile.WorldPosition, transform.parent);

			//Destroy gun
			if (gun == null) return;

			_ = Despawn.ServerSingle(gun.gameObject);
		}

		public void UpdateGui()
		{
			if(NetworkTabManager.Instance == null) return;

			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.Turret);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();

			valuesToSend.Add(new ElementValue() { Id = "PowerLabel", Value = Encoding.UTF8.GetBytes(
				hasPower ? turretState == TurretState.Off ? "Off" : "On" : "No Power") });

			valuesToSend.Add(new ElementValue() { Id = "WeaponsLabel", Value = Encoding.UTF8.GetBytes(CheckWeaponAuthorisation ? "Yes" : "No") });
			valuesToSend.Add(new ElementValue() { Id = "RecordLabel", Value = Encoding.UTF8.GetBytes(CheckSecurityRecords ? "Yes" : "No") });
			valuesToSend.Add(new ElementValue() { Id = "ArrestLabel", Value = Encoding.UTF8.GetBytes(CheckForArrest ? "Yes" : "No") });
			valuesToSend.Add(new ElementValue() { Id = "PersonnelLabel", Value = Encoding.UTF8.GetBytes(CheckUnauthorisedPersonnel ? "Yes" : "No") });
			valuesToSend.Add(new ElementValue() { Id = "LifeSignsLabel", Value = Encoding.UTF8.GetBytes(CheckUnidentifiedLifeSigns ? "Yes" : "No") });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Turret, TabAction.Update, valuesToSend.ToArray());
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var message = new StringBuilder();

			message.AppendLine($"The turret is {(unlocked ? "unlocked" : "locked")}, use an authorised id to {(unlocked ? "lock" : "unlock")} it");

			message.AppendLine("Turret must be unlocked to change connected APC or turret switch.");

			if (hasPower == false || turretState == TurretState.Off)
			{
				message.AppendLine("It is turned off");
			}
			else
			{
				message.AppendLine($"It is set to {(turretState == TurretState.Stun ? "stun" : "<color=red>lethal</color>")}");
			}

			return message.ToString();
		}

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (turretType != TurretType.Ai && unlocked == false && playerObject.GetComponent<PlayerScript>().PlayerType != PlayerTypes.Ai)
			{
				Chat.AddExamineMsgFromServer(playerObject, "Turret is locked");
				return false;
			}

			//Only allow changing settings on non Ai turrets, as the settings only work on those
			return turretType != TurretType.Ai;
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Turret;
		IMultitoolMasterable IMultitoolSlaveable.Master => connectedSwitch;
		bool IMultitoolSlaveable.RequireLink => false; // TODO: set to false to ignore false positive; currently links are serialized on the switch
		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			if (unlocked == false)
			{
				Chat.AddExamineMsgFromServer(performer, "You try to link the controller but the turret interface is locked!");
				return false;
			}

			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			// Already connected so disconnect
			if (connectedSwitch != null)
			{
				connectedSwitch.RemoveTurretFromSwitch(this);
				connectedSwitch = null;
			}

			if (master is TurretSwitch turretSwitch)
			{
				connectedSwitch = turretSwitch;
				turretSwitch.AddTurretToSwitch(this);
			}
		}

		#endregion
	}
}
