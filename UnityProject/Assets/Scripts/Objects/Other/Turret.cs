using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems;
using Systems.Construction;
using Systems.Electricity;
using Systems.MobAIs;
using AddressableReferences;
using Messages.Server;
using Mirror;
using Objects.Security;
using Objects.Wallmounts.Switches;
using Shared.Systems.ObjectConnection;
using Systems.Clearance;
using UI.Core.Net;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;


namespace Objects.Other
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(APCPoweredDevice))]
	public class Turret : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable, IExaminable, IServerSpawn, ICanOpenNetTab
	{
		[SerializeField]
		[Tooltip("Used to get the lethal bullet and spawn the gun when deconstructed")]
		private GameObject spawnGun = null;

		[SerializeField]
		private int range = 7;

		[SerializeField]
		[Tooltip("Used as back up if cant find bullet from mapped gun or gun put in")]
		private Bullet laserBullet = null;

		[SerializeField]
		[Tooltip("Used as the stun mode bullet")]
		private Bullet stunBullet = null;

		[SerializeField]
		[Range(1,100)]
		[Tooltip("Chance to lose gun during deconstruction")]
		private int gunLossChance = 30;

		[SerializeField]
		[Range(0.1f,10f)]
		[Tooltip("Multiplies on the default fire delay of the gun inside, lower is quicker")]
		private float shootSpeedMultiplier = 1f;

		[SerializeField]
		[Range(0.1f,10f)]
		[Tooltip("Used for taser or default laser when theres no gun inside, multiplied by shootSpeedMultiplier")]
		private float defaultShootSpeed = 1.5f;

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

		[SerializeField]
		[Tooltip("If turret not normal, then will always target mobs, if turret is Ai then will ignore all other checks and always fire")]
		//If turret not normal, then will always target mobs, if turret is Ai then will ignore all other checks and
		//always fire
		private TurretType turretType = TurretType.Normal;

#pragma warning disable CS0414 // disable unused compiler warning
		[SyncVar(hook = nameof(SyncOpen))]
		private bool open;
#pragma warning restore CS0414

		private enum TurretType
		{
			Normal,
			Ai,
			Syndicate
		}

		//Check for Weapon Authorization:
		//No/Yes - neutralizes people who have a weapon out but are not Heads or Security staff.
		[Tooltip("Neutralize people who have a weapon out but are not Heads or Security staff")]
		public bool CheckWeaponAuthorisation;

		[SerializeField] private ClearanceRestricted weaponAuthorisationClearance;

		//Check Security Records:
		//Yes/No - searches Security Records for criminals.
		[Tooltip("Search Security Records for criminals")]
		public bool CheckSecurityRecords = true;

		//Neutralize Identified Criminals:
		//Yes/No - neutralizes crew members set to Arrest on the Security Records.
		[Tooltip("Neutralize crew members set to Arrest on the Security Records")]
		public bool CheckForArrest = true;

		//Neutralize All Non-Security and Non-Command Personnel:
		//No/Yes - self explanatory.
		[Tooltip("Neutralize All Non-Security and Non-Command Personnel")]
		public bool CheckUnauthorisedPersonnel;

		[SerializeField] private ClearanceRestricted authorisedClearance;

		//Neutralize All Unidentified Life Signs:
		//Yes/No - neutralizes aliens.
		[Tooltip("Neutralize All Unidentified Life Signs")]
		public bool CheckUnidentifiedLifeSigns = true;

		private RegisterTile registerTile;
		private Integrity integrity;

		private GameObject target;
		private bool shootingTarget;

		private Gun gun;

		private const float UpdateTimer = 0.1f;
		private const float DetectTime = 1.5f;

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
		private ClearanceRestricted restricted;

		//Used to debug player searching linecast
		private LineRenderer lineRenderer;

		[SyncVar(hook = nameof(SyncRotation))]
		private Vector2 rotationAngle;

		private TurretSwitch connectedSwitch;

		private string bulletName;
		private AddressableAudioSource bulletSound;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
			restricted = GetComponent<ClearanceRestricted>();
			lineRenderer = GetComponentInChildren<LineRenderer>();
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

			SetUpBullet();
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
			var mobsFound = Physics2D.OverlapCircleAll(turretPos.To2Int(), range, LayerMask.GetMask("Players", "NPC"));

			if (mobsFound.Length == 0)
			{
				//No targets
				target = null;
				return;
			}

			//Order mobs by distance, sqrMag distance cheaper to calculate
			var orderedMobs = mobsFound.OrderBy(
				x => (turretPos - x.transform.position).sqrMagnitude).ToList();

			foreach (var mob in orderedMobs)
			{
				Vector3 worldPos;

				//Testing for player
				if (mob.TryGetComponent<PlayerScript>(out var script))
				{
					//Only target normal players and alive players
					if(script.IsNormal == false || script.IsDeadOrGhost) continue;

					//Check if player is allowed, but only if not an Ai turret as those will shoot all targets
					if(turretType != TurretType.Ai && ValidatePlayer(script)) continue;

					worldPos = script.ObjectPhysics.OfficialPosition;
				}
				//Test for mob, syndicate and AI will always target mobs, otherwise only on unidentified
				else if ((turretType != TurretType.Normal || CheckUnidentifiedLifeSigns) && mob.TryGetComponent<MobAI>(out var mobAi))
				{
					//Only target alive mobs
					if(mobAi.IsDead) continue;

					worldPos = mobAi.ObjectPhysics.OfficialPosition;
				}
				else
				{
					//Must be allowed mob or player so dont target them
					continue;
				}

				//TODO maybe add bool to linecast to only do raycast if linecast reaches WorldTo
				var linecast = MatrixManager.Linecast(turretPos,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed", "Walls"), worldPos);

				// TODO theres something buggy with linecast, seems like the raycast check is going through doors??
				// lineRenderer.positionCount = 2;
				// lineRenderer.SetPositions(new []{turretPos, linecast.ItHit ? linecast.HitWorld : worldPos});
				// lineRenderer.enabled = true;

				//Check to see if we hit a wall or closed door, allow for tolerance
				if(linecast.ItHit) continue; // && Vector3.Distance(worldPos, linecast.HitWorld) > 0.2f

				//Set our target
				target = mob.gameObject;
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

			//Record Checking
			if (CheckUnidentifiedLifeSigns || CheckForArrest || CheckSecurityRecords)
			{
				var hasRecord = false;
				foreach (var record in CrewManifestManager.Instance.SecurityRecords)
				{
					//Check to see if we have record
					if (record.characterSettings.Name.Equals(script.visibleName) == false) continue;

					//Check Security Records For Criminals
					if (CheckSecurityRecords && record.Status == SecurityStatus.Criminal)
					{
						return false;
					}

					//Neutralize Identified Criminals
					if (CheckForArrest && record.Status == SecurityStatus.Arrest)
					{
						return false;
					}

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
			}

			//Check for Weapon Authorization
			if (CheckWeaponAuthorisation && script.Equipment != null)
			{
				if (CheckSlot(NamedSlot.rightHand) == false || CheckSlot(NamedSlot.leftHand) == false)
				{
					return false;
				}

				bool CheckSlot(NamedSlot slot)
				{
					var handItem = script.Equipment.GetClothingItem(slot);
					if (handItem == null) return true;

					if (Validations.HasItemTrait(handItem.GameObjectReference, CommonTraits.Instance.Gun))
					{
						//Only allow authorised people to have guns
						bool allowed = weaponAuthorisationClearance.HasClearance(script.gameObject);

						//Check for failure
						if (allowed == false) return false;
					}

					//Passed weapons check for this hand
					return true;
				}
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
				rotationToShoot, gameObject, null, BodyPartType.None);
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
				gunSprite.ChangeSprite(0);
				return;
			}

			//Set up bullets
			SetUpBullet();

			//Stun or lethal
			gunSprite.ChangeSprite(newState == TurretState.Stun ? 1 : 2);
		}

		private void SetUpBullet()
		{
			shootSpeed = defaultShootSpeed;

			if (turretState == TurretState.Stun)
			{
				bulletName = stunBullet.name;
				bulletSound = taserSound;
				shootSpeed *= shootSpeedMultiplier;
				return;
			}

			if (gun != null)
			{
				if (gun is GunElectrical electrical && electrical.firemodeProjectiles.Count > 0 && electrical.firemodeFiringSound.Count > 0)
				{
					bulletName = electrical.firemodeProjectiles[0].GetComponent<Bullet>().name;
					bulletSound = electrical.firemodeFiringSound[0];

					//Min shoot speed 0.5
					shootSpeed = Mathf.Max((float)electrical.FireDelay, 0.5f);
				}
				else if (gun.CurrentMagazine != null)
				{
					bulletName = gun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().name;
					bulletSound = gun.FiringSoundA;

					//Min shoot speed 0.5
					shootSpeed = Mathf.Max((float)gun.FireDelay, 0.5f);
				}
				else
				{
					//Default to laser otherwise
					bulletName = laserBullet.name;
					bulletSound = gun.FiringSoundA;
				}
			}
			else
			{
				bulletName = laserBullet.name;
				bulletSound = spawnGun.GetComponent<Gun>().FiringSoundA;
			}

			shootSpeedMultiplier = Mathf.Clamp(shootSpeedMultiplier, 0.1f, 10f);
			shootSpeed *= shootSpeedMultiplier;
		}

		private void ChangeCoverState()
		{
			//Dont have cover on ballistic turret
			if(ballisticTurret) return;

			//If changing cover state dont check
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
				if (restricted.HasClearance(interaction.HandObject) == false)
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
