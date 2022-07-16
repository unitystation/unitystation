using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.ElectricalArcs;
using Systems.Explosions;
using HealthV2;

namespace Systems.Spells.Wizard
{
	/// <summary>
	/// Creates electrical arcs from the caster to the target. Can spread to other nearby targets.
	/// </summary>
	public class LightningBolt : Spell
	{
		[SerializeField]
		private GameObject arcEffect = default;
		[Tooltip("How many primary arcs to form from the caster to the target. Also affects how many secondary targets there are.")]
		[SerializeField, Range(1, 5)]
		private int arcCount = 3;
		[SerializeField, Range(0.5f, 10)]
		private float duration = 2;
		[SerializeField, Range(0, 20)]
		[Tooltip("How much damage each electrical arc should apply every arc pulse (0.5 seconds).")]
		private float damage = 3;
		[SerializeField, Range(3, 12)]
		private int primaryRange = 8;
		[SerializeField, Range(3, 12)]
		private int secondaryRange = 4;

		private PlayerInfo caster;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition)
		{
			this.caster = caster;

			Vector3 targetPosition = clickPosition;
			if (Vector3.Distance(caster.Script.WorldPos, targetPosition) > primaryRange)
			{
				var direction = (targetPosition - caster.Script.WorldPos).normalized;
				targetPosition = caster.Script.WorldPos + (direction * primaryRange);
			}

			GameObject primaryTarget = ZapPrimaryTarget(caster, targetPosition);
			StartCoroutine(ZapSecondaryTargets(primaryTarget, targetPosition));

			return true;
		}

		private GameObject ZapPrimaryTarget(PlayerInfo caster, Vector3 targetPosition)
		{
			GameObject targetObject = default;

			var raycast = RaycastToTarget(caster.Script.WorldPos, targetPosition);
			if (raycast.ItHit)
			{
				targetPosition = raycast.HitWorld;
			}
			else
			{
				targetObject = TryGetGameObjectAt(targetPosition);
			}

			Zap(arcCount, caster.GameObject, targetObject, endPos: targetPosition);

			return targetObject;
		}

		private IEnumerator ZapSecondaryTargets(GameObject originatingObject, Vector3 centrepoint)
		{
			var ignored = new GameObject[2] { caster.GameObject, originatingObject };
			int i = 0;

			var mobs = GetNearbyEntities(centrepoint, secondaryRange, LayerMask.GetMask("Players", "NPC"), ignored);
			foreach (Collider2D entity in mobs)
			{
				if (i >= arcCount) yield break;
				if (entity.gameObject == originatingObject) continue;

				yield return WaitFor.Seconds(0.2f);
				Zap(1, originatingObject, entity.gameObject, startPos: centrepoint);
				i++;
			}

			// Not enough mobs around, try zapping nearby machines
			// "Unshootable Machines" are so bullets pass over them, which doesn't need to apply here, so we target them too.
			var machines = GetNearbyEntities(centrepoint, secondaryRange,
					LayerMask.GetMask("Machines", "Unshootable Machines", "WallMounts", "Objects", "Items"), ignored);
			foreach (Collider2D entity in machines)
			{
				if (i >= arcCount) yield break;
				if (entity.gameObject == originatingObject) continue;

				yield return WaitFor.Seconds(0.2f);
				Zap(1, originatingObject, entity.gameObject, startPos: centrepoint);
				i++;
			}
		}

		private void Zap(int arcs, GameObject startObject, GameObject endObject, Vector3 startPos = default, Vector3 endPos = default)
		{
			ElectricalArcSettings arcSettings = new(
					arcEffect, startObject, endObject,
					startObject ? default : startPos,
					endObject ? default : endPos,
					arcs, duration);

			if (endObject != null)
			{
				var interfaces = endObject.GetComponents<IOnLightningHit>();

				foreach (var lightningHit in interfaces)
				{
					lightningHit.OnLightningHit(duration + 1, damage * arcSettings.arcCount);
				}
			}

			ElectricalArc.ServerCreateNetworkedArcs(arcSettings).OnArcPulse += OnPulse;
		}

		private void OnPulse(ElectricalArc arc)
		{
			if (arc.Settings.endObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthMasterBase>(out var health))
			{
				health.ApplyDamageAll(caster.GameObject, damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<LivingHealthBehaviour>(out var healthOld))
			{
				healthOld.ApplyDamage(caster.GameObject, damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<Integrity>(out var integrity))
			{
				integrity.ApplyDamage(damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
			}
		}

		#region Helpers

		private static T GetFirstAt<T>(Vector3Int position) where T : MonoBehaviour
		{
			return MatrixManager.GetAt<T>(position, true).FirstOrDefault();
		}

		private static GameObject TryGetGameObjectAt(Vector3 targetPosition)
		{
			var mob = GetFirstAt<LivingHealthMasterBase>(targetPosition.CutToInt());
			if (mob != null)
			{
				return mob.gameObject;
			}
			var mobOld = GetFirstAt<LivingHealthBehaviour>(targetPosition.CutToInt());
			if (mobOld != null)
			{
				return mobOld.gameObject;
			}

			var integrity = GetFirstAt<Integrity>(targetPosition.CutToInt());
			if (integrity != null)
			{
				return integrity.gameObject;
			}

			return default;
		}

		private static MatrixManager.CustomPhysicsHit RaycastToTarget(Vector3 start, Vector3 end)
		{
			return MatrixManager.RayCast(start, default, default,
					LayerTypeSelection.Walls | LayerTypeSelection.Windows, LayerMask.GetMask("Door Closed"),
					end);
		}

		private static IEnumerable<Collider2D> GetNearbyEntities(Vector3 point, float radius, int mask, GameObject[] ignored = default)
		{
			Collider2D[] entities = Physics2D.OverlapCircleAll(point, radius, mask);
			foreach (Collider2D coll in entities)
			{
				if (ignored != null && ignored.Contains(coll.gameObject)) continue;

				if (RaycastToTarget(point, coll.transform.position).ItHit == false)
				{
					yield return coll;
				}
			}
		}

		#endregion
	}
}
