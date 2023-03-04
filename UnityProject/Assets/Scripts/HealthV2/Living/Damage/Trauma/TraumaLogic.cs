using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public abstract class TraumaLogic : MonoBehaviour
	{
		[field: SerializeField]
		public TraumaticDamageTypes traumaTypes { get; private set; } = TraumaticDamageTypes.NONE;

		[SerializeField] protected float deadlyDamageInOneHit = 55f;
		[SerializeField] protected BodyPart bodyPart;

		protected int currentStage = 0;

		/// <summary>
		/// Key: Stage Number. Value: Required Damage to trigger.
		/// </summary>
		[SerializeField] protected SerializableDictionary<int, int> stages;

		protected bool RollForArmor(float damage) => true;
		public virtual void ProgressDeadlyEffect() { }

		public virtual void HealStage()
		{
			currentStage--;
			currentStage = Mathf.Clamp(currentStage, 0, stages.Count - 1);
		}

		public virtual void OnTakeDamage(BodyPartDamageData data)
		{
			if ( bodyPart.HealthMaster == null ) return;
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( data.DamageAmount >= deadlyDamageInOneHit ) ProgressDeadlyEffect();
		}

		protected void GenericStageProgression()
		{
			Debug.Log($"{currentStage} - {bodyPart.TotalDamage} / {stages[currentStage + 1]}");
			if (bodyPart.TotalDamage >= stages[currentStage + 1]) ProgressDeadlyEffect();
		}

		/// <summary>
		/// Describes the current affect of trauma on the body part.
		/// Usually used for things like the Advanced Health Scanner
		/// </summary>
		/// <returns>A string describing damage. Null for no description.</returns>
		public virtual string StageDescriptor()
		{
			return null;
		}
	}
}