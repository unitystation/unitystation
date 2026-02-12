using System.Collections;
using UnityEngine;

namespace US13.Effects
{
	public class TimedClientEffect : MonoBehaviour
	{
		[SerializeField]
		[Min(0)]
		private float time = 1f;

		private void OnEnable()
		{
			StartCoroutine(EffectTimer());
		}

		private IEnumerator EffectTimer()
		{
			float totalTime = 0;

			while (totalTime < time)
			{
				totalTime += Time.deltaTime;
				yield return WaitFor.EndOfFrame;
			}

			Destroy(this.gameObject);
		}
	}
}
