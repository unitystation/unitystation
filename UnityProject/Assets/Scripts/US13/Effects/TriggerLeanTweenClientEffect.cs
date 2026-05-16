using UnityEngine;

namespace ClientEffecte
{
	public class TriggerLeanTweenClientEffect : MonoBehaviour
	{
		[SerializeField] private LeanTweenAnimationsEnum LeanTweenClientEffect;

		public float Duration = 1;

		public LeanTweenType LeanTweenType = LeanTweenType.easeInOutQuad;

		private void OnEnable()
		{
			LeanTweenAnimations.DeEffectClient(LeanTweenClientEffect, this.gameObject, null, LeanTweenType, Duration);
		}

	}
}