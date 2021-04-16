using System;
using System.Collections;
using Light2D;
using UnityEngine;

namespace Effects
{
	public class SparkEffect : MonoBehaviour
	{
		[SerializeField]
		[Min(0)]
		private float time = 1f;

		[SerializeField]
		private LightSprite light = null;

		private void OnEnable()
		{
			light.Color.a = 1f;

			StartCoroutine(EffectTimer());
		}

		private IEnumerator EffectTimer()
		{
			float totalTime = 0;

			while (totalTime < time)
			{
				totalTime += Time.deltaTime;

				light.Color.a = 1 - (totalTime / time);
				yield return WaitFor.EndOfFrame;
			}

			if(CustomNetworkManager.IsServer == false) yield break;

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
