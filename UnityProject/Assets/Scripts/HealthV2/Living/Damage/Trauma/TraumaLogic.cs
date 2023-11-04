using Logs;
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
		public int CurrentStage => currentStage;

		/// <summary>
		/// Key: Stage Number. Value: Required Damage to trigger.
		/// </summary>
		[SerializeField] protected SerializableDictionary<int, int> stages;

		/// <summary>
		/// Used to apply the effects of the next trauma stage.
		/// </summary>
		public virtual void ProgressDeadlyEffect() { }

		public virtual void HealStage()
		{
			currentStage--;
			currentStage = Mathf.Clamp(currentStage, 0, stages.Count - 1);
		}

		/// <summary>
		/// Trauma damage initiator, checks if all the requirements are met stage progression.
		/// And can house different functionalities based on the trauma's behavior.
		/// </summary>
		/// <param name="data"></param>
		public virtual void OnTakeDamage(BodyPartDamageData data)
		{
			if ( bodyPart.HealthMaster == null ) return;
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( data.DamageAmount >= deadlyDamageInOneHit ) ProgressDeadlyEffect();
		}

		/// <summary>
		/// The most basic of stage progression checks. Uses the body's total damage to move forward.
		/// </summary>
		protected void GenericStageProgression()
		{
			if (gameObject == null || bodyPart == null)
			{
				Loggy.LogWarning(
					"[TraumaLogic/GenericStageProgression] - bodyPart might have been destroyed during other stage progressions. Skipping..");
				return;
			}
			if (stages == null || stages.ContainsKey(currentStage + 1) == false) return;
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
