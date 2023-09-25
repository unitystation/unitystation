using System.Collections.Generic;
using Chemistry.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HealthV2
{
	public class BloodSplater : MonoBehaviour
	{
		[SerializeField] private BodyPart bodyPart;
		[SerializeField] private float minimumDamageRequired = 8;
		[SerializeField] private List<GameObject> bloodSplats = new List<GameObject>();

		private void Awake()
		{
			if (bodyPart == null) bodyPart = GetComponentInParent<BodyPart>();
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
			bodyPart.HealthMaster.reagentPoolSystem.Bleed(Random.Range(1,8), false);
			var direction = banana.GetOppositeVectorToDirection();
			if (MatrixManager.IsWallAt(direction, true) || MatrixManager.IsSpaceAt(direction, true))
			{
				direction = bodyPart.HealthMaster.gameObject.AssumedWorldPosServer().CutToInt();
			}
			var result = Spawn.ServerPrefab(bloodSplats.PickRandom(), direction);
			if (result.Successful == false || result.GameObject.TryGetComponent<ReagentContainer>(out var container) == false) return;
			container.SetSpriteColor(bodyPart.HealthMaster.reagentPoolSystem.BloodPool.MixColor);
		}
	}
}