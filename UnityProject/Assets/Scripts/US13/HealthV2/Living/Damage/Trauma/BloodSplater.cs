using UnityEngine;
using US13.Health.Objects;
using US13.Managers.MatrixManager;
using US13.Objects.Directionals;
using Util;
using Random = UnityEngine.Random;

namespace US13.HealthV2.Living.Damage.Trauma
{
	public class BloodSplater : MonoBehaviour
	{
		[SerializeField] private CirculatorySystem.BodyPart bodyPart;
		[SerializeField] private float minimumDamageRequired = 8;
		private void Awake()
		{
			if (bodyPart == null) bodyPart = GetComponentInParent<CirculatorySystem.BodyPart>();
			bodyPart.OnDamageTaken += OnTakeDamage;
		}

		private void OnDestroy()
		{
			if (bodyPart != null) bodyPart.OnDamageTaken -= OnTakeDamage;
		}

		private void OnTakeDamage(BodyPartDamageData data)
		{
			if (data.DamageAmount < 0) return;
			if (data.AttackType == AttackType.Internal || data.AttackType == AttackType.Fire || data.AttackType == AttackType.Rad) return;
			if (data.DamageType == DamageType.Clone || data.DamageType == DamageType.Radiation
			                                        || data.DamageType == DamageType.Stamina || data.DamageType == DamageType.Tox) return;
			if (data.DamageAmount > minimumDamageRequired) SpewBloodSpat();
		}

		public void SpewBloodSpat(float chanceToSpew = 35)
		{
			if (DMMath.Prob(chanceToSpew) == false) return;
			if (bodyPart.HealthMaster == null || bodyPart.HealthMaster.TryGetComponent<Rotatable>(out var banana) == false) return;
			if (bodyPart.HealthMaster.reagentPoolSystem == null) return;

			var direction = banana.GetOppositeVectorToDirection();
			if (MatrixManager.IsWallAt(direction, true) || MatrixManager.IsSpaceAt(direction, true))
			{
				direction = SweetExtensions.AssumedWorldPosServer(bodyPart.HealthMaster.gameObject).CutToInt();
			}
			bodyPart.HealthMaster.reagentPoolSystem.Bleed(Random.Range(10,30), true, direction);
		}
	}
}