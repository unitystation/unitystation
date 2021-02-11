using System;
using System.Collections.Generic;
using System.Linq;
using Systems.ElectricalArcs;
using Systems.Mob;
using Mirror;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;

namespace Objects
{
	public class TeslaEnergyBall : MonoBehaviour, IOnHitDetect
	{
		[SerializeField]
		private TeslaEnergyBallStages startingStage = TeslaEnergyBallStages.Stage0;

		[SerializeField]
		private int teslaPoints = 100;

		[SerializeField]
		private int maximumPoints = 3250;

		[SerializeField]
		private int pointLossRate = 4;

		[SerializeField]
		[Tooltip("If true tesla energy ball wont go down a stage if it loses points")]
		private bool preventDowngrade = false;

		[SerializeField]
		[Tooltip("If true tesla energy ball wont die at 0 points")]
		private bool zeroPointDeath = true;

		[SerializeField]
		[Tooltip("List of orbiting Energy Balls")]
		private List<Transform> orbitingBalls = new List<Transform>();

		[SerializeField]
		[Tooltip("arc effect")]
		private GameObject arcEffect = null;

		[Tooltip("How many primary arcs to form from the caster to the target. Also affects how many secondary targets there are.")]
		[SerializeField, Range(1, 5)]
		public int arcCount = 3;
		[SerializeField, Range(0.5f, 10)]
		public float duration = 2;
		[SerializeField, Range(0, 20)]
		[Tooltip("How much damage should each electrical arc apply every arc pulse (0.5 seconds)")]
		public float damage = 3;
		[SerializeField, Range(3, 12)]
		public int primaryRange = 8;
		[SerializeField, Range(3, 12)]
		public int secondaryRange = 4;

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private CustomNetTransform customNetTransform;

		private int lockTimer;
		private bool pointLock;

		private List<Vector3Int> adjacentCoords = new List<Vector3Int>
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		//[SyncVar(hook = nameof(SyncStage))]
		private TeslaEnergyBallStages currentStage = TeslaEnergyBallStages.Stage0;

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			customNetTransform = GetComponent<CustomNetTransform>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			currentStage = startingStage;
		}

		private void OnEnable()
		{
			UpdateManager.Add(TeslaUpdate, 0.5f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TeslaUpdate);
		}

		#endregion

		private void TeslaUpdate()
		{
			if(!CustomNetworkManager.IsServer) return;

			if (lockTimer > 0)
			{
				lockTimer--;
			}

			if (lockTimer == 0)
			{
				pointLock = false;
			}

			if (teslaPoints <= 0 && zeroPointDeath)
			{
				Chat.AddLocalMsgToChat("The energy ball fizzles out", gameObject);
				Despawn.ServerSingle(gameObject);
				return;
			}

			//Points decrease by 4 every 0.5 seconds, unless locked by PA at setting 0
			if (pointLock == false)
			{
				ChangePoints(-pointLossRate);
			}

			TryChangeStage();

			LightningObjects();

			TryMove();
		}

		private void SyncStage(TeslaEnergyBallStages oldStage, TeslaEnergyBallStages newStage)
		{
			currentStage = newStage;

			//TODO set orbiting balls active
		}

		#region StageChange

		private void TryChangeStage()
		{

		}

		#endregion

		#region ShootLightning

		private void LightningObjects()
		{
			var objectsToShoot = new List<GameObject>();

			//TODO serialise shoot range
			foreach (var coord in EffectShape.CreateEffectShape(EffectShapeType.Circle, registerTile.WorldPositionServer, 6))
			{
				var objects = MatrixManager.GetAt<PushPull>(coord, true);

				if(objects.Count == 0) continue;

				foreach (var itemObject in objects)
				{
					if(itemObject.registerTile.ObjectType == ObjectType.Item) continue;

					objectsToShoot.Add(itemObject.gameObject);
				}
			}

			if(objectsToShoot.Count == 0) return;

			var teslaCoils = objectsToShoot.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null).ToList();

			if (teslaCoils.Any())
			{
				ShootLightning(teslaCoils.PickRandom().WorldPosServer());
			}
			else
			{
				ShootLightning(objectsToShoot.PickRandom().WorldPosServer());
			}
		}

		public bool ShootLightning(Vector3 targetPosition)
		{
			if (Vector3.Distance(registerTile.WorldPositionServer, targetPosition) > primaryRange)
			{
				var direction = (targetPosition - registerTile.WorldPositionServer).normalized;
				targetPosition = registerTile.WorldPositionServer + (direction * primaryRange);
			}

			GameObject primaryTarget = ZapPrimaryTarget(targetPosition);
			if (primaryTarget != null)
			{
				ZapSecondaryTargets(primaryTarget, targetPosition);
			}

			return true;
		}

		private GameObject ZapPrimaryTarget(Vector3 targetPosition)
		{
			GameObject targetObject = default;

			var raycast = RaycastToTarget(registerTile.WorldPositionServer, targetPosition);
			if (raycast.ItHit)
			{
				targetPosition = raycast.HitWorld;
			}
			else
			{
				targetObject = TryGetGameObjectAt(targetPosition);
			}

			Zap(gameObject, targetObject, arcCount, targetObject == null ? targetPosition : default);

			return targetObject;
		}

		private void ZapSecondaryTargets(GameObject originatingObject, Vector3 centrepoint)
		{
			var ignored = new GameObject[2] { gameObject, originatingObject };
			int i = 0;

			var mobs = GetNearbyEntities(centrepoint, LayerMask.GetMask("Players", "NPC"), ignored);
			foreach (Collider2D entity in mobs)
			{
				if (i >= arcCount) return;
				if (entity.gameObject == originatingObject) continue;

				Zap(originatingObject, entity.gameObject, 1);
				i++;
			}

			// Not enough mobs around, try zapping nearby machines.
			var machines = GetNearbyEntities(centrepoint, LayerMask.GetMask("Machines", "Wallmounts", "Objects"), ignored);
			foreach (Collider2D entity in machines)
			{
				if (i >= arcCount) return;
				if (entity.gameObject == originatingObject) continue;

				Zap(originatingObject, entity.gameObject, 1);
				i++;
			}
		}

		private void Zap(GameObject originatingObject, GameObject targetObject, int arcs, Vector3 targetPosition = default)
		{
			ElectricalArcSettings arcSettings = new ElectricalArcSettings(
					arcEffect, originatingObject, targetObject, default, targetPosition, arcs, duration);

			if (targetObject != null && targetObject.TryGetComponent<PlayerSprites>(out var playerSprites))
			{
				playerSprites.EnableElectrocutedOverlay(duration + 1);
			}
			else if (targetObject != null && targetObject.TryGetComponent<MobSprite>(out var mobSprites))
			{
				mobSprites.EnableElectrocutedOverlay(duration + 1);
			}

			ElectricalArc.ServerCreateNetworkedArcs(arcSettings).OnArcPulse += OnPulse;
		}

		private void OnPulse(ElectricalArc arc)
		{
			if (arc.Settings.endObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthBehaviour>(out var health))
			{
				health.ApplyDamage(gameObject, damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<Integrity>(out var integrity))
			{
				integrity.ApplyDamage(damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
			}
		}

		#region Helpers

		private T GetFirstAt<T>(Vector3Int position) where T : MonoBehaviour
		{
			return MatrixManager.GetAt<T>(position, true).FirstOrDefault();
		}

		private GameObject TryGetGameObjectAt(Vector3 targetPosition)
		{
			var mob = GetFirstAt<LivingHealthBehaviour>(targetPosition.CutToInt());
			if (mob != null)
			{
				return mob.gameObject;
			}

			var integrity = GetFirstAt<Integrity>(targetPosition.CutToInt());
			if (integrity != null)
			{
				return integrity.gameObject;
			}

			return default;
		}

		private MatrixManager.CustomPhysicsHit RaycastToTarget(Vector3 start, Vector3 end)
		{
			return MatrixManager.RayCast(start, default, primaryRange,
					LayerTypeSelection.Walls | LayerTypeSelection.Windows, LayerMask.GetMask("Door Closed"),
					end);
		}

		private IEnumerable<Collider2D> GetNearbyEntities(Vector3 centrepoint, int mask, GameObject[] ignored = default)
		{
			Collider2D[] entities = Physics2D.OverlapCircleAll(centrepoint, secondaryRange, mask);
			foreach (Collider2D coll in entities)
			{
				if (ignored.Contains(coll.gameObject)) continue;

				if (RaycastToTarget(centrepoint, coll.transform.position).ItHit == false)
				{
					yield return coll;
				}
			}
		}

		#endregion

		#endregion

		#region Movement

		private void TryMove()
		{
			//Get random coordinate adjacent to current
			var coord = adjacentCoords.GetRandom() + registerTile.WorldPositionServer;

			//TODO might not need to exclude objects
			if (MatrixManager.IsPassableAtAllMatricesOneTile(coord, true, false, new List<LayerType>{LayerType.Objects}) == false)
			{
				//Tile blocked
				return;
			}

			//Move
			customNetTransform.SetPosition(coord);
		}

		#endregion

		#region Points

		private void ChangePoints(int pointChange)
		{
			if (teslaPoints + pointChange >= maximumPoints)
			{
				teslaPoints = maximumPoints;
				return;
			}

			if (teslaPoints + pointChange < maximumPoints)
			{
				teslaPoints += pointChange;
				return;
			}

			if (teslaPoints < 0)
			{
				teslaPoints = 0;
			}
		}

		public void OnHitDetect(DamageData damageData)
		{
			if(damageData.AttackType != AttackType.Rad) return;

			if (damageData.Damage <= 20f)
			{
				//PA at setting 0 will do 20 damage
				pointLock = true;
				lockTimer = 5;
				return;
			}

			ChangePoints((int)damageData.Damage);
		}

		#endregion

		private enum TeslaEnergyBallStages
		{
			Stage0 = 0,
			Stage1 = 1,
			Stage2 = 2,
			Stage3 = 3,
			Stage4 = 4,
			Stage5 = 5
		}
	}
}
