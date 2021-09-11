using System;
using HealthV2;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class Limb : MonoBehaviour, IMovementEffect
	{
		[SerializeField]
		[Tooltip("The walking speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float walkingSpeed = 1.5f;

		[SerializeField]
		[Tooltip("The running speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float runningSpeed = 3f;

		[SerializeField] [Tooltip("The crawling speed used for when the limb is attached as an arm.\n")]
		private float crawlingSpeed = 0.3f;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient an arm this limb is.\n" +
		         "1 is a human hand.")]
		private float armEfficiency = 1f;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient a leg this limb is.\n" +
		         "1 is a human leg.")]
		private float legEfficiency = 1f;

		private BodyPart bodyPart;
		private PlayerHealthV2 playerHealth;

		public void Initialize()
		{
			bodyPart = GetComponent<BodyPart>();
			playerHealth = bodyPart.HealthMaster as PlayerHealthV2;
			bodyPart.ModifierChange += ModifierChanged;
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);
		}

		public float RunningSpeedModifier => runningSpeed * legEfficiency * bodyPart.TotalModified;

		public float WalkingSpeedModifier => walkingSpeed * legEfficiency * bodyPart.TotalModified;

		public float CrawlingSpeedModifier => crawlingSpeed * armEfficiency * bodyPart.TotalModified;

		public void ModifierChanged()
		{
			playerHealth.PlayerMove.UpdateSpeeds();
		}
	}

}
