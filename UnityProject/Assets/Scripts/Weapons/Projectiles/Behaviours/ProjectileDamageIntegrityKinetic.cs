using UnityEngine;
using ScriptableObjects.Gun;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages integrity on collision
	/// only for kinetic weapons
	/// </summary>
	public class ProjectileDamageIntegrityKinetic : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null)
			{
				return false;
			}

			float pressure = MatrixManager.AtPoint(
				(Vector3Int)hit.point.To2Int(),
				true
			).MetaDataLayer.Get(hit.transform.localPosition.RoundToInt()).GasMix.Pressure;

			var newDamage = DamageByPressureModifier(pressure);

			integrity.ApplyDamage(newDamage, damageData.AttackType, damageData.DamageType);

			Chat.AddAttackMsgToChat(
				shooter,
				coll.gameObject,
				BodyPartType.None,
				weapon.gameObject);

			Logger.LogTraceFormat(
				"Hit {0} for {1} with Integrity! bullet absorbed",
				Category.Firearms,
				integrity.gameObject.name,
				newDamage);

			return true;
		}

		private float DamageByPressureModifier(float pressure)
		{
			float newDamage = damageData.Damage * (-pressure / 135);
			return Mathf.Clamp(newDamage, -1.0f, 0.0f) + 1;
		}

		private void OnDisable()
		{
			weapon = null;
			shooter = null;
		}
	}
}

