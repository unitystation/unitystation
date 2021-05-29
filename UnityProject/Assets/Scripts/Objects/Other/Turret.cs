using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Input_System.InteractionV2.Interactions;
using Messages.Server;
using Objects.Wallmounts;
using Objects.Wallmounts.Switches;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;
using Random = UnityEngine.Random;

namespace Objects.Other
{
	[RequireComponent(typeof(ItemStorage))]
	public class Turret : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private Gun currentGun = null;

		[SerializeField]
		private int range = 8;

		[SerializeField]
		private Gun stunGun = null;

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

		private RegisterTile registerTile;
		private ItemStorage itemStorage;
		private Integrity integrity;

		private PlayerScript target;
		private bool shootingTarget;

		private TurretPowerState turretPowerState = TurretPowerState.Off;
		private TurretCoverState turretCoverState = TurretCoverState.Closed;
		private TurretBulletState turretBulletState = TurretBulletState.Stun;

		private ISetMultitoolMaster setiMaster;

		private bool coverOpen;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			itemStorage = GetComponent<ItemStorage>();
			integrity = GetComponent<Integrity>();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateLoop, 0.5f);
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

		private void UpdateLoop()
		{
			if(turretPowerState == TurretPowerState.Off) return;

			SearchForPlayers();

			ChangeCoverState();

			if (shootingTarget || target == null) return;
			shootingTarget = true;

			StartCoroutine(ShootTarget());
		}

		#region Shooting

		private void SearchForPlayers()
		{
			var turretPos = registerTile.WorldPosition;
			var playersFound = Physics2D.OverlapCircleAll(turretPos.To2Int(), range, LayerMask.NameToLayer("Players"));

			if (playersFound.Length == 0)
			{
				//No targets
				target = null;
				return;
			}

			//Order players by distance
			var orderedPlayers = playersFound.OrderBy(
				x => (turretPos - x.transform.position).sqrMagnitude).ToList();

			foreach (var player in orderedPlayers)
			{
				if(player.TryGetComponent<PlayerScript>(out var script) == false) continue;

				//Only target normal players
				if(script.PlayerState != PlayerScript.PlayerStates.Normal) continue;

				//TODO maybe add bool to linecast to only do raycast if linecast is true
				var linecast = MatrixManager.Linecast(turretPos,
					LayerTypeSelection.Walls, LayerMask.NameToLayer("Door Closed"), script.WorldPos);

				//Check to see if we hit a wall or closed door, allow for tolerance
				if(linecast.ItHit && Vector3.Distance(script.WorldPos, linecast.HitWorld) > 0.5f) continue;

				//Set our target
				target = script;
				return;
			}

			//No targets
			target = null;
		}

		private IEnumerator ShootTarget()
		{
			var angleToShooter = CalculateAngle(target.WorldPos);

			//TODO make turret smoothly rotate to angle, target could move so get position every tick
			//TODO But target could be set to null during move so check before
			//TODO also keep checking power state

			if (turretPowerState != TurretPowerState.Off)
			{
				ShootAtDirection(angleToShooter);
			}

			shootingTarget = false;
			yield break;
		}

		private float CalculateAngle(Vector3 target)
		{
			return Vector3.Angle(registerTile.WorldPosition, target);
		}

		private void ShootAtDirection(float rotationToShoot)
		{
			if(currentGun == null || currentGun.CurrentMagazine == null || turretPowerState == TurretPowerState.Off) return;

			SoundManager.PlayNetworkedAtPos(turretBulletState == TurretBulletState.Stun ? stunGun.FiringSoundA : currentGun.FiringSoundA,
				registerTile.WorldPosition, sourceObj: gameObject);

			CastProjectileMessage.SendToAll(gameObject,
				turretBulletState == TurretBulletState.Stun ? stunGun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().PrefabName :
					currentGun.CurrentMagazine.initalProjectile.GetComponent<Bullet>().PrefabName,
				VectorExtensions.DegreeToVector2(rotationToShoot), default);
		}

		#endregion

		public void SetUpTurret(Gun newGun, ItemSlot fromSlot, ISetMultitoolMaster iMaster)
		{
			setiMaster = iMaster;

			if (setiMaster is TurretSwitch generalSwitch)
			{
				generalSwitch.AddTurretToSwitch(this);
			}

			currentGun = newGun;
			itemStorage.ServerTryTransferFrom(fromSlot);
		}

		public void SetPower(TurretPowerState newState)
		{
			if (newState == TurretPowerState.On)
			{
				//Change back to active bullet state
				gunSprite.ChangeSprite((int)turretBulletState + 1);
				return;
			}

			//Turn to off sprite
			gunSprite.ChangeSprite(0);

			ChangeCoverState();
		}

		public void ChangeBulletState(TurretBulletState newState)
		{
			turretBulletState = newState;

			if(turretPowerState == TurretPowerState.On) return;

			gunSprite.ChangeSprite((int)newState + 1);
		}

		private void ChangeCoverState()
		{
			//Open if we need to
			if (target != null && turretCoverState == TurretCoverState.Closed && turretCoverState != TurretCoverState.Open)
			{
				StartCoroutine(WaitForAnimation(TurretCoverState.Open));
			}

			//However if no targets and already closed
			if(turretCoverState == TurretCoverState.Closed) return;

			//Close
			StartCoroutine(WaitForAnimation(TurretCoverState.Closed));
		}

		//Wait for animation before allowing to fire
		private IEnumerator WaitForAnimation(TurretCoverState stateAfter)
		{
			//TODo correct time
			yield return new WaitForSeconds(1f);

			turretCoverState = stateAfter;
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
					Spawn.ServerPrefab(framePrefab, registerTile.WorldPosition, transform.parent);

					if (DMMath.Prob(gunLossChance))
					{
						_ = Despawn.ServerSingle(currentGun.gameObject);
					}
				});
		}

		#endregion

		private void OnTurretDestroy(DestructionInfo info)
		{
			Spawn.ServerPrefab(framePrefab, registerTile.WorldPosition, transform.parent);

			//Destroy gun
			_ = Despawn.ServerSingle(currentGun.gameObject);
		}

		public enum TurretPowerState
		{
			On,
			Off
		}

		public enum TurretCoverState
		{
			Open,
			Closed
		}

		public enum TurretBulletState
		{
			Stun,
			Lethal
		}
	}
}
