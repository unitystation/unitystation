using Player.Movement;
using UnityEngine;

namespace HealthV2.Limbs
{
	public class Arm : Limb, IMovementEffect
	{
		[SerializeField]
		[Tooltip("A generalized number representing how efficient an arm this limb is.\n" +
		         "1 is a human hand.")]
		private float armEfficiency = 1f;
		public float ArmEfficiency => armEfficiency;

		public float CrawlingSpeedModifier => crawlingSpeed * armEfficiency * bodyPart.TotalModified;

		[SerializeField] [Tooltip("The crawling speed used for when the limb is attached as an arm.\n")]
		private float crawlingSpeed = 0.3f;
		public float CrawlingSpeed => crawlingSpeed;

		public void SetNewSpeeds(float newCrawlingSpeed)
		{
			crawlingSpeed = newCrawlingSpeed;
			ModifierChanged();
		}
	}
}