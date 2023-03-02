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
		public virtual void OnTakeDamage(float damage, DamageType damageType, AttackType attackType)
		{
			if ( bodyPart.HealthMaster == null ) return;
			if ( damage >= deadlyDamageInOneHit ) ProgressDeadlyEffect();
		}

		public void GenericStageProgression()
		{
			Debug.Log($"{currentStage} - {bodyPart.TotalDamage} / {stages[currentStage + 1]}");
			if (bodyPart.TotalDamage >= stages[currentStage + 1]) ProgressDeadlyEffect();
		}
	}
}