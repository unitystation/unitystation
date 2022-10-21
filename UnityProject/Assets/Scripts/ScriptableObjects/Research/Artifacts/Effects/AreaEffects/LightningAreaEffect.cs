using UnityEngine;
using System.Collections.Generic;
using HealthV2;
using System.Linq;
using Systems.ElectricalArcs;
using Objects.Engineering;
using Systems.Explosions;
using Tiles;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "LightningAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/LightningAreaEffect")]
	public class LightningAreaEffect : AreaEffectBase
	{

		[SerializeField]
		private int shots = 3;

		[SerializeField]
		private float arcDuration = 1;

		[SerializeField]
		private float arcDamage = 20;

		[SerializeField]
		[Tooltip("arc effect")]
		private GameObject arcEffect = null;

		[SerializeField]
		[Tooltip("layer tiles to ignore")]
		private LayerTile[] layerTilesToIgnore = null;

		private GameObject artifact;

		//Lightning hit logic taken and adjusted from TeslaEnergyBall.cs and LightningBolt.cs
		public override void DoEffectAura(GameObject centeredAround)
		{
			artifact = centeredAround;
			var objectsToShoot = new List<GameObject>();

			var position = centeredAround.RegisterTile().WorldPositionServer;
			var machines = GetNearbyEntities(position, LayerMask.GetMask("Machines", "WallMounts", "Objects", "Players", "NPC"), AuraRadius).ToList();

			foreach (Collider2D entity in machines)
			{
				if (entity.gameObject == centeredAround) continue; //Don't hit artifact

				objectsToShoot.Add(entity.gameObject);
			}

			for (int i = 0; i < shots; i++)
			{
				var target = GetTarget(objectsToShoot);

				if (target == null)
				{
					//If no target objects shoot random tile instead
					var tPosition = EffectShape.CreateEffectShape(effectShapeType, position, AuraRadius).PickRandom();
					Zap(centeredAround, null, Random.Range(1, shots), tPosition);
				}
				else
				{
					target.TryGetComponent<PlayerScript>(out var player);

					if (player != null && base.TryEffectPlayer(player))
					{
						Zap(centeredAround, target, Random.Range(1, shots));
					}
					else if (player == null)
					{
						Zap(centeredAround, target, Random.Range(1, shots));
					}
				}
			
				objectsToShoot.Remove(target);
			}
		}

		private void Zap(GameObject originatingObject, GameObject targetObject, int arcs, Vector3 targetPosition = default)
		{
			ElectricalArcSettings arcSettings = new ElectricalArcSettings(
					arcEffect, originatingObject, targetObject, default, targetPosition, arcs, arcDuration,
					false);

			if (targetObject != null)
			{
				var interfaces = targetObject.GetComponents<IOnLightningHit>();

				foreach (var lightningHit in interfaces)
				{
					lightningHit.OnLightningHit(arcDuration + 1, arcDamage);
				}
			}

			ElectricalArc.ServerCreateNetworkedArcs(arcSettings).OnArcPulse += OnPulse;
		}

		private GameObject GetTarget(List<GameObject> objectsToShoot)
		{
			if (objectsToShoot.Count == 0) return null;

			var teslaCoils = objectsToShoot.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null && teslaCoil.IsWrenched).ToList();

			if (teslaCoils.Any())
			{
				var groundingRods = teslaCoils.Where(o => o.TryGetComponent<TeslaCoil>(out var coil) && coil.CurrentState == TeslaCoil.TeslaCoilState.Grounding).ToList();

				return groundingRods.Any() ? groundingRods.PickRandom() : objectsToShoot.PickRandom();			
			}
			return objectsToShoot.PickRandom();
		}

		private void OnPulse(ElectricalArc arc)
		{
			if (arc.Settings.endObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthMasterBase>(out var health) && health != null)
			{
				health.ApplyDamageAll(artifact, arcDamage, AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<Integrity>(out var integrity) && integrity != null && integrity.Resistances.LightningDamageProof == false)
			{
				integrity.ApplyDamage(arcDamage, AttackType.Magic, DamageType.Burn, true, explodeOnDestroy: true);
			}
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

		private MatrixManager.CustomPhysicsHit RaycastToTarget(Vector3 start, Vector3 end)
		{
			return MatrixManager.RayCast(start, default, AuraRadius,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed"),
					end, layerTilesToIgnore);
		}
	}
}
