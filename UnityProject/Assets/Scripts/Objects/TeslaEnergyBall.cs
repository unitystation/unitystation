using System;
using System.Collections.Generic;
using System.Linq;
using Systems.ElectricalArcs;
using Systems.Explosions;
using AddressableReferences;
using HealthV2;
using Mirror;
using Objects.Engineering;
using UnityEngine;
using Weapons.Projectiles.Behaviours;
using Random = UnityEngine.Random;
using Tiles;

namespace Objects
{
	public class TeslaEnergyBall : NetworkBehaviour, IOnHitDetect, IExaminable
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

		[SerializeField]
		[Tooltip("layer tiles to ignore")]
		private LayerTile[] layerTilesToIgnore = null;

		[Tooltip("How many secondary targets there are.")]
		[SerializeField, Range(1, 5)]
		private int arcCount = 3;
		[SerializeField, Range(0.5f, 10)]
		private float duration = 2;
		[SerializeField, Range(0, 20)]
		[Tooltip("How much damage should each electrical arc apply every arc pulse (0.5 seconds)")]
		private float damage = 3;
		[SerializeField, Range(3, 12)]
		private int primaryRange = 8;
		[SerializeField, Range(3, 12)]
		private int secondaryRange = 4;

		[SerializeField]
		private AddressableAudioSource lightningSound = null;

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private UniversalObjectPhysics ObjectPhysics ;

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
		//Cant sync enums on this version of Mirror, pls update Mirror!
		private TeslaEnergyBallStages currentStage = TeslaEnergyBallStages.Stage0;

		[SyncVar(hook = nameof(SyncStage))]
		private int stage;

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			ChangeStage(startingStage);
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
				Chat.AddActionMsgToChat(gameObject, "The energy ball fizzles out!");
				_ = Despawn.ServerSingle(gameObject);
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

		#region StageChange

		private void TryChangeStage()
		{
			if (teslaPoints < 200)
			{
				ChangeStage(TeslaEnergyBallStages.Stage0);
			}
			else if (teslaPoints < 500)
			{
				ChangeStage(TeslaEnergyBallStages.Stage1);
			}
			else if (teslaPoints < 1000)
			{
				ChangeStage(TeslaEnergyBallStages.Stage2);
			}
			else if (teslaPoints < 2000)
			{
				ChangeStage(TeslaEnergyBallStages.Stage3);
			}
			else if (teslaPoints >= 2000)
			{
				ChangeStage(TeslaEnergyBallStages.Stage4);
			}
			else if (teslaPoints >= 3000)
			{
				ChangeStage(TeslaEnergyBallStages.Stage5);
			}
		}

		private void ChangeStage(TeslaEnergyBallStages newStage)
		{
			if (currentStage == newStage) return;

			if(preventDowngrade && newStage < currentStage) return;

			Chat.AddActionMsgToChat(gameObject, $"The energy ball fluctuates and {(newStage < currentStage ? "decreases" : "increases")} in size!");

			currentStage = newStage;

			stage = (int)newStage;
		}

		private void SyncStage(int oldStage, int newStage)
		{
			stage = newStage;
			currentStage = (TeslaEnergyBallStages)newStage;

			for (int i = 0; i <= 5; i++)
			{
				if (i <= stage)
				{
					orbitingBalls[i].SetActive(true);
				}
				else
				{
					orbitingBalls[i].SetActive(false);
				}
			}
		}

		#endregion

		#region ShootLightning

		private void LightningObjects()
		{
			var objectsToShoot = new List<GameObject>();

			var machines = GetNearbyEntities(registerTile.WorldPositionServer, LayerMask.GetMask("Machines", "WallMounts", "Objects", "Players", "NPC"), primaryRange).ToList();

			foreach (Collider2D entity in machines)
			{
				if (entity.gameObject == gameObject) continue;

				objectsToShoot.Add(entity.gameObject);
			}

			for (int i = 0; i < (int)currentStage + 1; i++)
			{
				var target = GetTarget(objectsToShoot, doTeslaFirst: false);

				if(target == null)
				{
					//If no target objects shoot random tile instead
					var pos = GetRandomTile(primaryRange);
					if(pos == null) continue;

					Zap(gameObject, null, Random.Range(1,3), pos.Value);
					continue;
				}

				ShootLightning(target);

				objectsToShoot.Remove(target);
			}
		}

		private GameObject GetTarget(List<GameObject> objectsToShoot, bool random = true, bool doTeslaFirst = true)
		{
			if (objectsToShoot.Count == 0) return null;

			var teslaCoils = objectsToShoot.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null && teslaCoil.IsWrenched).ToList();

			if (teslaCoils.Any())
			{
				var groundingRods = teslaCoils.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null &&
				                                          teslaCoil.CurrentState == TeslaCoil.TeslaCoilState.Grounding).ToList();

				if (doTeslaFirst == false)
				{
					return groundingRods.Any() ? groundingRods.PickRandom() : objectsToShoot.PickRandom();
				}

				if (random)
				{
					return groundingRods.Any() ? groundingRods.PickRandom() : teslaCoils.PickRandom();
				}

				return groundingRods.Any() ? groundingRods[0] : teslaCoils[0];
			}

			return random ? objectsToShoot.PickRandom() : objectsToShoot[0];
		}

		private void ShootLightning(GameObject targetObject)
		{
			GameObject primaryTarget = ZapPrimaryTarget(targetObject);
			if (primaryTarget != null)
			{
				ZapSecondaryTargets(primaryTarget, targetObject.AssumedWorldPosServer());
			}
		}

		private GameObject ZapPrimaryTarget(GameObject targetObject)
		{
			Zap(gameObject, targetObject, Random.Range(1,3), targetObject == null ? targetObject.AssumedWorldPosServer() : default);

			SoundManager.PlayNetworkedAtPos(lightningSound, targetObject == null ? targetObject.AssumedWorldPosServer() : default, sourceObj: targetObject);

			return targetObject;
		}

		private void ZapSecondaryTargets(GameObject originatingObject, Vector3 centrepoint)
		{
			var ignored = new GameObject[2] { gameObject, originatingObject };

			var targets = new List<GameObject>();

			foreach (var entity in GetNearbyEntities(centrepoint, LayerMask.GetMask("Machines", "WallMounts", "Objects", "Players", "NPC"), secondaryRange, ignored))
			{
				targets.Add(entity.gameObject);
			}

			for (int i = 0; i < Random.Range(1, arcCount + 1); i++)
			{
				if(targets.Count == 0) break;

				var target = GetTarget(targets, false);
				Zap(originatingObject, target, 1);
				targets.Remove(target);
			}
		}

		private void Zap(GameObject originatingObject, GameObject targetObject, int arcs, Vector3 targetPosition = default)
		{
			ElectricalArcSettings arcSettings = new ElectricalArcSettings(
					arcEffect, originatingObject, targetObject, default, targetPosition, arcs, duration,
					false);

			if (targetObject != null)
			{
				var interfaces = targetObject.GetComponents<IOnLightningHit>();

				foreach (var lightningHit in interfaces)
				{
					lightningHit.OnLightningHit(duration + 1, damage * ((int)currentStage + 1));
				}
			}

			ElectricalArc.ServerCreateNetworkedArcs(arcSettings).OnArcPulse += OnPulse;
		}

		private void OnPulse(ElectricalArc arc)
		{
			if (arc.Settings.endObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthMasterBase>(out var health) && health != null)
			{
				health.ApplyDamageAll(gameObject, damage * ((int)currentStage + 1), AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<Integrity>(out var integrity) && integrity != null && integrity.Resistances.LightningDamageProof == false)
			{
				integrity.ApplyDamage(damage * ((int)currentStage + 1), AttackType.Magic, DamageType.Burn, true, explodeOnDestroy: true);
			}
		}

		private Vector3Int? GetRandomTile(int range)
		{
			var overloadPrevent = 0;

			while (overloadPrevent < 20)
			{
				var pos = registerTile.WorldPositionServer;
				pos.x += Random.Range(-range, range + 1);
				pos.y += Random.Range(-range, range + 1);

				if (MatrixManager.IsEmptyAt(pos, true, registerTile.Matrix.MatrixInfo))
				{
					return pos;
				}

				overloadPrevent++;
			}

			return null;
		}

		#region Helpers

		private T GetFirstAt<T>(Vector3Int position) where T : MonoBehaviour
		{
			return MatrixManager.GetAt<T>(position, true).FirstOrDefault();
		}

		private GameObject TryGetGameObjectAt(Vector3 targetPosition)
		{
			var mob = GetFirstAt<LivingHealthMasterBase>(targetPosition.CutToInt());
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
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed"),
					end, layerTilesToIgnore);
		}

		private IEnumerable<Collider2D> GetNearbyEntities(Vector3 centrepoint, int mask, int range, GameObject[] ignored = default)
		{
			Collider2D[] entities = Physics2D.OverlapCircleAll(centrepoint, range, mask);
			foreach (Collider2D coll in entities)
			{
				if (ignored != null && ignored.Contains(coll.gameObject)) continue;

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

			var matrixInfo = MatrixManager.AtPoint(coord, true);

			var layerTile = matrixInfo.TileChangeManager.MetaTileMap
				.GetTile(MatrixManager.WorldToLocalInt(coord, matrixInfo), LayerType.Walls);

			//Cant go through shields
			if (layerTile != null && layerTilesToIgnore.Any(l => l.name == layerTile.name )) return;

			//Move
			ObjectPhysics.AppearAtWorldPositionServer(coord);
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

		public void OnHitDetect(OnHitDetectData data)
		{
			if(data.DamageData.AttackType != AttackType.Rad) return;

			if (data.DamageData.Damage >= 19f)
			{
				//PA at setting 0 will do 20 damage
				pointLock = true;
				lockTimer = 20;
			}

			ChangePoints((int)data.DamageData.Damage);
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

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"You count {(int)currentStage} secondary energy balls";
		}
	}
}
