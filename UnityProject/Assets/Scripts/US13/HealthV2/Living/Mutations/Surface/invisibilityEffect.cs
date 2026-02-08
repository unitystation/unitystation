using UnityEngine;
using US13.HealthV2.Living.Damage;
using BodyPart = US13.HealthV2.Living.CirculatorySystem.BodyPart;

namespace US13.HealthV2.Living.Mutations.Surface
{
	[CreateAssetMenu(fileName = "InvisibilityEffect", menuName = "ScriptableObjects/Mutations/InvisibilityEffect")]
	public class InvisibilityEffect : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InInvisibilityEffect(BodyPart,_RelatedMutationSO);
		}

		private class InInvisibilityEffect: Mutation
		{
			private BodyPart relatedPart;
			private BodySpritesInvisbility invisibility;

			public InInvisibilityEffect(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{
				relatedPart = BodyPart;
			}

			public override void SetUp()
			{
				if (relatedPart != null && relatedPart.HealthMaster.gameObject.TryGetComponent<BodySpritesInvisbility>(out var inv))
				{
					invisibility = inv;
					invisibility.Alpha = 0.05f;
					relatedPart.OnDamageTaken += OnDamageTaken;
				}
			}

			private void OnDamageTaken(BodyPartDamageData obj)
			{
				invisibility.Alpha = GetInvertedNormalizedValue(relatedPart.HealthMaster.OverallHealth, relatedPart.HealthMaster.MaxHealth);
			}

			public float GetInvertedNormalizedValue(float currentValue, float maxValue)
			{
				if (maxValue <= 0 || currentValue == 0) return 1f; // If maxValue is 0 or negative, assume fully inverted (returns 1)
				return 1f - Mathf.Clamp01(currentValue / maxValue);
			}


			public override void Remove()
			{
				if (relatedPart != null) relatedPart.OnDamageTaken -= OnDamageTaken;
			}
		}
	}
}