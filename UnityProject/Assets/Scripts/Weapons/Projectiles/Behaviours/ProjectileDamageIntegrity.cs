using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages integrity on collision
	/// </summary>
	public class ProjectileDamageIntegrity : MonoBehaviour, IOnShoot, IOnHit
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
			var newDamage = damageData.Damage;
			var coll = hit.collider;
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null) return false;
			// checks if its a kinetic weapon is a high atmostphere 
			if (damageData.DamageType == DamageType.Kinetic)
			{

				if (MatrixManager.AtPoint((Vector3Int)hit.point.To2Int(), true).MetaDataLayer.Get(hit.transform.localPosition.RoundToInt()).GasMix.Pressure <= 50)
				{
					newDamage = damageData.Damage * .25f;
				}
			}
			integrity.ApplyDamage(newDamage, damageData.AttackType, damageData.DamageType);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, BodyPartType.None, weapon.gameObject);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
				integrity.gameObject.name, damageData.Damage);

			return true;
		}

		private void OnDisable()
		{
			weapon = null;
			shooter = null;
		}
	}
}