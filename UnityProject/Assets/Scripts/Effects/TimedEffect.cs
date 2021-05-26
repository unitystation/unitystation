using System.Collections;
using UnityEngine;

namespace Effects
{
	public class TimedEffect : MonoBehaviour
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

			if(CustomNetworkManager.IsServer == false) yield break;

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
