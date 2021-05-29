using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Construction;
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
	public class Turret : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		//Only used when a mapped turret is destroyed
		private GameObject spawnGun = null;

		[SerializeField]
		private int range = 7;

		[SerializeField]
		//Used to set the lethal bullet, uses gun in construction but this for mapped
		private Bullet laserBullet = null;

		[SerializeField]
		//Always used as the non-lethal bullet
		private Bullet stunBullet = null;

		[SerializeField]
		[Range(1,100)]
		[Tooltip("Chance to lose gun during deconstruction")]
		private int gunLossChance = 30;

		[SerializeField]
		private GameObject framePrefab = null;

		[SerializeField]
		private SpriteHandler gunSprite = null;

		[SerializeField]
		private SpriteHandler frameSprite = null;

		[SerializeField]
		private AddressableAudioSource taserSound = null;

		private RegisterTile registerTile;
		private ItemStorage itemStorage;
		private Integrity integrity;

		private GameObject target;
		private bool shootingTarget;

		private Gun gun;

		//Has power
		private bool hasPower;

		//Cover open or closed
		private bool coverOpen;

		//Is stun or lethal
		private bool isStun;

		private ISetMultitoolMaster setiMaster;
		private LineRenderer lineRenderer;

		[SyncVar(hook = nameof(SyncRotation))]
		private Vector2 rotationAngle;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			integrity = GetComponent<Integrity>();
			lineRenderer = GetComponentInChildren<LineRenderer>();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateLoop, 1.5f);
			integrity.OnWillDestroyServer.AddListener(OnTurretDestroy);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
			integrity.OnWillDestroyServer.RemoveListener(OnTurretDestroy);

			if (setiMaster is TurretSwitch generalSwitch)
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
			if(hasPower == false) return;

			SearchForMobs();

			ChangeCoverState();

			if (shootingTarget || target == null || coverOpen == false) return;
			shootingTarget = true;

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

			//Order mobs by distance
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

			//TODO make turret smoothly rotate to angle, target could move so get position every tick
			//TODO But target could be set to null during move so check before
			//TODO also keep checking power state

			if (hasPower)
			{
				ShootAtDirection(angleToShooter);
			}

			shootingTarget = false;;
		}

		private Vector2 CalculateAngle(Vector2 target)
		{
			return (registerTile.WorldPosition.To2Int() - target).Rotate(180);
		}

		private void ShootAtDirection(Vector2 rotationToShoot)
		{
			if (hasPower == false) return;

			String bullet;
			AddressableAudioSource sound;

			if (isStun)
			{
				bullet = stunBullet.name;
				sound = taserSound;
			}
			else if (gun != null && gun.CurrentMagazine != null)
			{
				bullet = gun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().PrefabName;
				sound = gun.FiringSoundA;
			}
			else
			{
				bullet = laserBullet.name;
				sound = spawnGun.GetComponent<Gun>().FiringSoundA;
			}

			SoundManager.PlayNetworkedAtPos(sound, registerTile.WorldPosition, sourceObj: gameObject);

			CastProjectileMessage.SendToAll(gameObject, bullet, rotationToShoot, default);
		}

		#endregion

		public void SetUpTurret(Gun newGun, ItemSlot fromSlot, ISetMultitoolMaster iMaster)
		{
			setiMaster = iMaster;

			if (setiMaster is TurretSwitch generalSwitch)
			{
				generalSwitch.AddTurretToSwitch(this);
			}

			gun = newGun;
			itemStorage.ServerTryTransferFrom(fromSlot);
		}

		public void SetPower(bool newState)
		{
			hasPower = newState;

			if (newState)
			{
				//Change back to active bullet state
				gunSprite.ChangeSprite(isStun ? 1 : 2);
				return;
			}

			//Turn to off sprite
			gunSprite.ChangeSprite(0);

			ChangeCoverState();
		}

		public void ChangeBulletState(bool newState)
		{
			isStun = newState;

			if(hasPower == false) return;

			gunSprite.ChangeSprite(newState ? 1 : 2);
		}

		private void ChangeCoverState()
		{
			//Open if we need to
			if (target != null && coverOpen == false)
			{
				StartCoroutine(WaitForAnimation(true));
				return;
			}

			//Only close if no targets and we are open, and dont bother closing if we are already closed
			if((target != null && coverOpen) || coverOpen == false) return;

			//Close
			StartCoroutine(WaitForAnimation(false));
		}

		//Wait for animation before allowing to fire
		private IEnumerator WaitForAnimation(bool stateAfter)
		{
			frameSprite.AnimateOnce(stateAfter ? 1 : 3);

			yield return new WaitForSeconds(0.55f);

			coverOpen = stateAfter;
		}

		#region Hand Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Try Deconstruct
			ToolUtils.ServerUseToolWithActionMessages(interaction, 10f,
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

					frame.GameObject.GetComponent<TurretFrame>().SetUp(gun != null ? gun.GetComponent<Pickupable>().ItemSlot : null, spawnGun);
				});
		}

		#endregion

		private void OnTurretDestroy(DestructionInfo info)
		{
			Spawn.ServerPrefab(framePrefab, registerTile.WorldPosition, transform.parent);

			//Destroy gun
			_ = Despawn.ServerSingle(gun != null ? gun.gameObject : spawnGun.gameObject);
		}
	}
}
