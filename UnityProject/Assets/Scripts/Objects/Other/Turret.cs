using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Construction;
using Systems.Electricity;
using Systems.MobAIs;
using AddressableReferences;
using Core.Input_System.InteractionV2.Interactions;
using Messages.Server;
using Mirror;
using Objects.Wallmounts;
using Objects.Wallmounts.Switches;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;
using Random = UnityEngine.Random;

namespace Objects.Other
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(APCPoweredDevice))]
	[RequireComponent(typeof(AccessRestrictions))]
	public class Turret : NetworkBehaviour, ICheckedInteractable<HandApply>, ISetMultitoolSlave, IExaminable, IServerSpawn
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
		[Tooltip("The shooting speed, ONLY IN MULTIPLES OF 0.1")]
		private float shootSpeed = 1.5f;

		[SerializeField]
		private GameObject framePrefab = null;

		[SerializeField]
		private SpriteHandler gunSprite = null;

		[SerializeField]
		private SpriteHandler frameSprite = null;

		[SerializeField]
		private AddressableAudioSource taserSound = null;

		private RegisterTile registerTile;
		private Integrity integrity;

		private GameObject target;
		private bool shootingTarget;

		private Gun gun;

		private const float UpdateTimer = 0.1f;
		private const float DetectTime = 1.5f;

		private float shootingTimer = 0;
		private float detectTimer = 0;

		//Has power
		private bool hasPower;

		//Cover open or closed
		private bool coverOpen;
		private bool coverStateChanging;

		//Is off, stun or lethal
		private TurretState turretState;

		//Unlocked
		private bool unlocked;

		private ItemStorage itemStorage;
		private APCPoweredDevice apcPoweredDevice;
		private AccessRestrictions accessRestrictions;

		//Used to debug player searching linecast
		private LineRenderer lineRenderer;

		[SyncVar(hook = nameof(SyncRotation))]
		private Vector2 rotationAngle;

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.Turret;
		public MultitoolConnectionType ConType => conType;

		private TurretSwitch connectedSwitch;

		public void SetMaster(ISetMultitoolMaster iMaster)
		{
			if (unlocked == false)
			{
				//TODO how do you tell player you need to unlock??
				return;
			}

			if (iMaster is TurretSwitch turretSwitch)
			{
				//Already connected so disconnect
				if (connectedSwitch != null)
				{
					connectedSwitch.RemoveTurretFromSwitch(this);
				}

				connectedSwitch = turretSwitch;
				turretSwitch.AddTurretToSwitch(this);
			}
		}

		private string bulletName;
		private AddressableAudioSource bulletSound;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
			accessRestrictions = GetComponent<AccessRestrictions>();
			lineRenderer = GetComponentInChildren<LineRenderer>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if(spawnGun == null || gun != null) return;

			var newGun = Spawn.ServerPrefab(spawnGun, registerTile.WorldPosition, transform.parent);

			if (newGun.Successful == false) return;

			gun = newGun.GameObject.GetComponent<Gun>();

			SetUpBullet();

			//For some reason couldnt use item storage, would just stay above the turret
			gun.GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateLoop, UpdateTimer);
			integrity.OnWillDestroyServer.AddListener(OnTurretDestroy);
			apcPoweredDevice.OnStateChangeEvent.AddListener(OnPowerStateChange);

			SetUpBullet();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
			integrity.OnWillDestroyServer.RemoveListener(OnTurretDestroy);
			apcPoweredDevice.OnStateChangeEvent.RemoveListener(OnPowerStateChange);
			apcPoweredDevice.LockApcLinking(unlocked == false);

			if (connectedSwitch is TurretSwitch generalSwitch)
			{
				generalSwitch.RemoveTurretFromSwitch(this);
			}
		}

		private void SyncRotation(Vector2 oldValue, Vector2 newValue)
		{
			//transform.localEulerAngles = new Vector3(0, 0, rotation);
			var angle = Mathf.Atan2(newValue.y, newValue.x) * Mathf.Rad2Deg;
			gunSprite.transform.rotation = Quaternion.AngleAxis(angle + 90, Vector3.forward);
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
					if(script.PlayerState != PlayerScript.PlayerStates.Normal || script.IsDeadOrGhost) continue;

					worldPos = script.WorldPos;
				}
				//Test for mob
				else if (mob.TryGetComponent<MobAI>(out var mobAi))
				{
					//Only target alive mobs
					if(mobAi.IsDead) continue;

					worldPos = mobAi.Cnt.ServerPosition;
				}
				else
				{
					//No idea what it could be on player and Npc layer but not be a mob or player
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

		private void ShootTarget()
		{
			var angleToShooter = CalculateAngle(target.WorldPosServer());
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

			CastProjectileMessage.SendToAll(gameObject, bulletName, rotationToShoot, default);
		}

		#endregion

		public void SetUpTurret(Gun newGun, ItemSlot fromSlot)
		{
			gun = newGun;
			SetUpBullet();

			itemStorage.ServerTryTransferFrom(fromSlot);
		}

		//Called when ApcPoweredDevice changes state
		private void OnPowerStateChange(Tuple<PowerState, PowerState> newStates)
		{
			SetPower(newStates.Item2 != PowerState.Off);

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
			if (turretState == TurretState.Stun)
			{
				bulletName = stunBullet.name;
				bulletSound = taserSound;
			}
			else if (gun != null)
			{
				if (gun is GunElectrical electrical && electrical.firemodeProjectiles.Count > 0 && electrical.firemodeFiringSound.Count > 0)
				{
					bulletName = electrical.firemodeProjectiles[0].GetComponent<Bullet>().name;
					bulletSound = electrical.firemodeFiringSound[0];
				}
				else if (gun.CurrentMagazine != null)
				{
					bulletName = gun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().name;
					bulletSound = gun.FiringSoundA;
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
		}

		private void ChangeCoverState()
		{
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
			frameSprite.AnimateOnce(stateAfter ? 1 : 3);

			//Wait for animation to complete before allowing to fire or change cover state
			yield return new WaitForSeconds(0.55f);

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
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Id)) return true;

			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//If Id try unlock
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Id))
			{
				if (accessRestrictions.CheckAccessCard(interaction.HandObject) == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You need higher authorisation to unlock this {gameObject.ExpensiveName()}");
					return;
				}

				var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, true), Perform);
				bar.ServerStartProgress(interaction.Performer.RegisterTile(), unlocked ? 5f : 15f, interaction.Performer);

				void Perform()
				{
					unlocked = !unlocked;
					apcPoweredDevice.LockApcLinking(unlocked == false);

					Chat.AddActionMsgToChat(interaction.Performer, $"You {(unlocked ? "unlock" : "lock")} the {gameObject.ExpensiveName()}",
						$"{interaction.Performer.ExpensiveName()} {(unlocked ? "unlocks" : "locks")} the {gameObject.ExpensiveName()}");
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
	}
}
