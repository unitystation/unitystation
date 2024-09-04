using Core.Utils;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class Limb : BodyPartFunctionality, IMovementEffect
	{
		[SerializeField]
		[Tooltip("The walking speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		protected float walkingSpeed = 1.5f;
		public float WalkingSpeed => walkingSpeed;

		[SerializeField]
		[Tooltip("The running speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		protected float runningSpeed = 3f;
		public float RunningSpeed => runningSpeed;

		[SerializeField]
		[Tooltip("The running speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		protected float crawlingSpeed = 0.5f;
		public float CrawlingSpeed => crawlingSpeed;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient a limb is. 1 Is a human leg.")]
		private float initialLimbEfficiency = 1f;

		private readonly MultiInterestFloat limbEfficiency =
			new(InSetFloatBehaviour: MultiInterestFloat.FloatBehaviour.AddBehaviour);

		public float LimbEfficiency => limbEfficiency;

		public float RunningSpeedModifier => runningSpeed * limbEfficiency * RelatedPart.TotalModified;
		public float WalkingSpeedModifier => walkingSpeed * limbEfficiency * RelatedPart.TotalModified;

		public float CrawlingSpeedModifier
		{
			get => crawlingSpeed * limbEfficiency * RelatedPart.TotalModified;
			protected set => crawlingSpeed = value;
		}

		protected PlayerHealthV2 playerHealth;

		public override void Awake()
		{
			base.Awake();
			limbEfficiency.RecordPosition(this, initialLimbEfficiency);
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart = GetComponent<BodyPart>();
			playerHealth = RelatedPart.HealthMaster as PlayerHealthV2;
			RelatedPart.ModifierChange += ModifierChanged;
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			playerHealth.OrNull()?.PlayerMove.RemoveModifier(this);
		}

		public void ModifierChanged()
		{
			playerHealth.PlayerMove.UpdateSpeeds();
		}

		public void SetNewCrawlingSpeeds(float newCrawlingSpeed)
		{
			CrawlingSpeedModifier = newCrawlingSpeed;
			ModifierChanged();
		}

		public void SetNewWalkingSpeeds(float newSpeed)
		{
			walkingSpeed = newSpeed;
			ModifierChanged();
		}

		public void SetNewRunningSpeeds(float newSpeed)
		{
			runningSpeed = newSpeed;
			ModifierChanged();
		}

		/// <summary>
		/// Changes the efficiency of the limb.
		/// </summary>
		/// <param name="newEfficiency">The new buff/debuff to add.</param>
		/// <param name="changer">Who is giving the buff?</param>
		public void SetNewEfficiency(float newEfficiency, object changer)
		{
			if (limbEfficiency.InterestedParties.ContainsKey(changer))
			{
				ModifierChanged();
				return;
			}
			limbEfficiency.RecordPosition(changer, newEfficiency);
			ModifierChanged();
		}

		public void RemoveEfficiency(object changer)
		{
			limbEfficiency.RemovePosition(changer);
			ModifierChanged();
		}
	}
}
