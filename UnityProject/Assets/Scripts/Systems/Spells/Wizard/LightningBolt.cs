using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.ElectricalArcs;
using Systems.Mob;

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
		[Tooltip("How much damage should each electrical arc apply every arc pulse (0.5 seconds)")]
		private float damage = 3;
		[SerializeField, Range(3, 12)]
		private int primaryRange = 8;
		[SerializeField, Range(3, 12)]
		private int secondaryRange = 4;

		private ConnectedPlayer caster;

		public override bool CastSpellServer(ConnectedPlayer caster, Vector3 clickPosition)
		{
			this.caster = caster;

			Vector3 targetPosition = clickPosition;
			if (Vector3.Distance(caster.Script.WorldPos, targetPosition) > primaryRange)
			{
				var direction = (targetPosition - caster.Script.WorldPos).normalized;
				targetPosition = caster.Script.WorldPos + (direction * primaryRange);
			}

			GameObject primaryTarget = ZapPrimaryTarget(caster, targetPosition);
			if (primaryTarget != null)
			{
				ZapSecondaryTargets(primaryTarget, targetPosition);
			}

			return true;
		}

		private GameObject ZapPrimaryTarget(ConnectedPlayer caster, Vector3 targetPosition)
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

			Zap(caster.GameObject, targetObject, arcCount, targetObject == null ? targetPosition : default);

			return targetObject;
		}

		private void ZapSecondaryTargets(GameObject originatingObject, Vector3 centrepoint)
		{
			var ignored = new GameObject[2] { caster.GameObject, originatingObject };
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
				health.ApplyDamage(caster.GameObject, damage * arc.Settings.arcCount, AttackType.Magic, DamageType.Burn);
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
	}
}
